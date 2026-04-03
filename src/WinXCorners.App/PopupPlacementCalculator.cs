namespace WinXCorners.App;

internal static class PopupPlacementCalculator
{
    internal static int CalculateTop(
        int anchorTop,
        int menuHeight,
        int workAreaTop,
        int workAreaBottom,
        bool preferAbove)
    {
        var yAbove = anchorTop - menuHeight - 4;
        var yBelow = anchorTop - 11;

        var availableAbove = anchorTop - workAreaTop;
        var availableBelow = workAreaBottom - anchorTop;

        int y;
        if (preferAbove && availableAbove >= menuHeight)
        {
            y = yAbove;
        }
        else if (!preferAbove && availableBelow >= menuHeight)
        {
            y = yBelow;
        }
        else if (availableBelow >= menuHeight)
        {
            y = yBelow;
        }
        else if (availableAbove >= menuHeight)
        {
            y = yAbove;
        }
        else
        {
            y = availableBelow >= availableAbove ? yBelow : yAbove;
        }

        if (y < workAreaTop)
        {
            y = workAreaTop;
        }

        if (y + menuHeight > workAreaBottom)
        {
            y = workAreaBottom - menuHeight;
        }

        return y;
    }
}
