using FinePdf.Core.Models;

namespace FinePdf.Core.Layout;

/// <summary>
/// Pure layout maths shared by the composer (writes the PDF) and the preview renderer
/// (draws the on-screen sheet), so the two never drift apart.
/// </summary>
public static class SheetLayout
{
    /// <summary>
    /// The rectangle a placement occupies on the sheet. One placement fills the sheet;
    /// two split it along the sheet's long axis; more are arranged in a near-square grid
    /// (four → 2×2). <paramref name="order"/> decides whether the cells are filled
    /// left-to-right then top-to-bottom (<see cref="NUpOrder.RowMajor"/>, the default) or
    /// top-to-bottom then left-to-right (<see cref="NUpOrder.ColumnMajor"/>).
    /// </summary>
    public static Rect CellFor(
        int placementCount,
        int index,
        double sheetWidth,
        double sheetHeight,
        NUpOrder order = NUpOrder.RowMajor)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(placementCount, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, placementCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sheetWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sheetHeight);

        if (placementCount == 1)
        {
            return new Rect(0, 0, sheetWidth, sheetHeight);
        }

        int columns;
        int rows;
        if (placementCount == 2)
        {
            bool sideBySide = sheetWidth >= sheetHeight;
            columns = sideBySide ? 2 : 1;
            rows = sideBySide ? 1 : 2;
        }
        else
        {
            columns = (int)Math.Ceiling(Math.Sqrt(placementCount));
            rows = (int)Math.Ceiling((double)placementCount / columns);
        }

        double cellWidth = sheetWidth / columns;
        double cellHeight = sheetHeight / rows;
        int column;
        int row;
        if (order == NUpOrder.ColumnMajor)
        {
            column = index / rows;
            row = index % rows;
        }
        else
        {
            column = index % columns;
            row = index / columns;
        }

        return new Rect(column * cellWidth, row * cellHeight, cellWidth, cellHeight);
    }

    /// <summary>
    /// The extra clockwise quarter-turn (0 or 90) to add to <paramref name="currentRotation"/>
    /// so the page's aspect orientation matches the cell's — a portrait page lands in a
    /// portrait cell, a landscape page in a landscape cell. Used to auto-orient N-up
    /// placements so they fill their cell instead of leaving wide margins.
    /// </summary>
    public static int OrientationFixupDegrees(
        Rect cell,
        double sourceWidth,
        double sourceHeight,
        int currentRotation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceHeight);

        int rotation = ((currentRotation % 360) + 360) % 360;
        bool quarterTurn = rotation is 90 or 270;
        double width = quarterTurn ? sourceHeight : sourceWidth;
        double height = quarterTurn ? sourceWidth : sourceHeight;

        bool cellLandscape = cell.Width >= cell.Height;
        bool pageLandscape = width >= height;
        return cellLandscape == pageLandscape ? 0 : 90;
    }

    /// <summary>
    /// Places a source page of the given (unrotated) size into <paramref name="cell"/>,
    /// honouring <paramref name="rotationDegrees"/> (a multiple of 90) and <paramref name="fit"/>.
    /// </summary>
    public static PlacedContent Place(
        Rect cell,
        double sourceWidth,
        double sourceHeight,
        int rotationDegrees,
        FitMode fit)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceHeight);

        int rotation = ((rotationDegrees % 360) + 360) % 360;
        if (rotation % 90 != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotationDegrees), rotationDegrees, "Rotation must be a multiple of 90°.");
        }

        bool quarterTurn = rotation is 90 or 270;
        double rotatedWidth = quarterTurn ? sourceHeight : sourceWidth;
        double rotatedHeight = quarterTurn ? sourceWidth : sourceHeight;

        double scale = fit switch
        {
            FitMode.Fit => Math.Min(cell.Width / rotatedWidth, cell.Height / rotatedHeight),
            FitMode.Fill => Math.Max(cell.Width / rotatedWidth, cell.Height / rotatedHeight),
            FitMode.ActualSize => 1d,
            _ => 1d,
        };

        return new PlacedContent(
            cell.CenterX,
            cell.CenterY,
            sourceWidth * scale,
            sourceHeight * scale,
            rotation);
    }
}
