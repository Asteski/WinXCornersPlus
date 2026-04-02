using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

namespace WinXCorners.App;

internal static class NativeIconResourceLoader
{
    internal static Icon? LoadIconFromResFile(string filePath, string iconName)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(filePath);
        var resources = ParseResources(bytes);
        var groupResource = resources.FirstOrDefault(r => r.TypeId == RtGroupIcon && string.Equals(r.Name, iconName, StringComparison.OrdinalIgnoreCase));
        if (groupResource.Data.Length == 0)
        {
            return null;
        }

        var iconBytes = BuildIconFile(groupResource.Data, resources);
        return iconBytes is null ? null : new Icon(new MemoryStream(iconBytes));
    }

    private const ushort RtIcon = 3;
    private const ushort RtGroupIcon = 14;

    private static byte[]? BuildIconFile(byte[] groupData, IReadOnlyList<ResourceEntry> resources)
    {
        if (groupData.Length < 6)
        {
            return null;
        }

        var reserved = BinaryPrimitives.ReadUInt16LittleEndian(groupData.AsSpan(0, 2));
        var type = BinaryPrimitives.ReadUInt16LittleEndian(groupData.AsSpan(2, 2));
        var count = BinaryPrimitives.ReadUInt16LittleEndian(groupData.AsSpan(4, 2));
        if (reserved != 0 || type != 1 || count == 0)
        {
            return null;
        }

        const int groupEntrySize = 14;
        var imageData = new List<byte[]>(count);
        var output = new MemoryStream();
        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write(count);

        var imageOffset = 6 + (count * 16);
        for (var index = 0; index < count; index++)
        {
            var entryOffset = 6 + (index * groupEntrySize);
            if (entryOffset + groupEntrySize > groupData.Length)
            {
                return null;
            }

            var width = groupData[entryOffset + 0];
            var height = groupData[entryOffset + 1];
            var colorCount = groupData[entryOffset + 2];
            var reservedByte = groupData[entryOffset + 3];
            var planes = BinaryPrimitives.ReadUInt16LittleEndian(groupData.AsSpan(entryOffset + 4, 2));
            var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(groupData.AsSpan(entryOffset + 6, 2));
            var bytesInRes = BinaryPrimitives.ReadUInt32LittleEndian(groupData.AsSpan(entryOffset + 8, 4));
            var resourceId = BinaryPrimitives.ReadUInt16LittleEndian(groupData.AsSpan(entryOffset + 12, 2));

            var imageResource = resources.FirstOrDefault(r => r.TypeId == RtIcon && r.IntegerName == resourceId);
            if (imageResource.Data.Length == 0)
            {
                return null;
            }

            imageData.Add(imageResource.Data);

            writer.Write(width);
            writer.Write(height);
            writer.Write(colorCount);
            writer.Write(reservedByte);
            writer.Write(planes);
            writer.Write(bitCount);
            writer.Write(bytesInRes);
            writer.Write(imageOffset);
            imageOffset += checked((int)bytesInRes);
        }

        foreach (var image in imageData)
        {
            writer.Write(image);
        }

        writer.Flush();
        return output.ToArray();
    }

    private static List<ResourceEntry> ParseResources(byte[] bytes)
    {
        var resources = new List<ResourceEntry>();
        var offset = 0;
        while (offset + 8 <= bytes.Length)
        {
            var dataSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset, 4));
            var headerSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4));
            if (dataSize < 0 || headerSize < 8 || offset + headerSize > bytes.Length)
            {
                break;
            }

            var cursor = offset + 8;
            var (typeId, _, nextAfterType) = ReadResourceIdentifier(bytes, cursor);
            var (_, name, nextAfterName) = ReadResourceIdentifier(bytes, nextAfterType);
            cursor = Align4(nextAfterName);

            if (cursor + 16 > offset + headerSize)
            {
                break;
            }

            cursor += 16;
            var dataOffset = Align4(offset + headerSize);
            if (dataOffset + dataSize > bytes.Length)
            {
                break;
            }

            var data = bytes.AsSpan(dataOffset, dataSize).ToArray();
            if (!(typeId == 0 && string.IsNullOrEmpty(name) && dataSize == 0))
            {
                resources.Add(new ResourceEntry(typeId, name, data));
            }

            offset = Align4(dataOffset + dataSize);
        }

        return resources;
    }

    private static (ushort typeId, string name, int nextOffset) ReadResourceIdentifier(byte[] bytes, int offset)
    {
        if (offset + 2 > bytes.Length)
        {
            return (0, string.Empty, bytes.Length);
        }

        var marker = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset, 2));
        if (marker == 0xFFFF)
        {
            if (offset + 4 > bytes.Length)
            {
                return (0, string.Empty, bytes.Length);
            }

            var ordinal = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 2, 2));
            return (ordinal, ordinal.ToString(), offset + 4);
        }

        var chars = new List<char>();
        var cursor = offset;
        while (cursor + 2 <= bytes.Length)
        {
            var ch = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(cursor, 2));
            cursor += 2;
            if (ch == 0)
            {
                break;
            }

            chars.Add((char)ch);
        }

        return (0, new string(chars.ToArray()), cursor);
    }

    private static int Align4(int value) => (value + 3) & ~3;

    private readonly record struct ResourceEntry(ushort TypeId, string Name, byte[] Data)
    {
        internal ushort IntegerName => ushort.TryParse(Name, out var value) ? value : (ushort)0;
    }
}
