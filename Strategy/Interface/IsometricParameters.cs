using System;

using Microsoft.Xna.Framework;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Describes a mapping between an isometric grid and pixels.
    /// </summary>
    public class IsometricParameters
    {
        public readonly int RowX; /// X pixels per isometric row
        public readonly int RowY; /// Y pixels per isometric row
        public readonly int ColumnX; /// X pixels per isometric column
        public readonly int ColumnY; /// Y pixels per isometric column

        public int OffsetX;
        public int OffsetY;

        public IsometricParameters(int rowX, int rowY, int colX, int colY)
        {
            RowX = rowX;
            RowY = rowY;
            ColumnX = colX;
            ColumnY = colY;
        }

        /// <summary>
        /// Returns the X pixel coordinate for the given isometric row and column.
        /// </summary>
        public int GetX(int row, int col)
        {
            return row * RowX + col * ColumnX + OffsetX;
        }

        /// <summary>
        /// Returns the Y pixel coordinate for the given isometric row and column.
        /// </summary>
        public int GetY(int row, int col)
        {
            return row * RowY + col * ColumnY + OffsetY;
        }

        /// <summary>
        /// Returns the pixel coordinates for the given isometric cell.
        /// </summary>
        public Point GetPoint(Cell cell)
        {
            return new Point(
                GetX(cell.Row, cell.Col),
                GetY(cell.Row, cell.Col));
        }
    }
}
