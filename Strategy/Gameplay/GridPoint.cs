using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Strategy.Gameplay
{
    public struct GridPoint : IEquatable<GridPoint>
    {
        /// <summary>
        /// The row of the point.
        /// </summary>
        public int Row;

        /// <summary>
        /// The column of the point.
        /// </summary>
        public int Col;

        /// <summary>
        /// Creates a new grid point.
        /// </summary>
        /// <param name="row">The row of the point.</param>
        /// <param name="col">The col of the point.</param>
        public GridPoint(int row, int col)
        {
            Row = row;
            Col = col;
        }

        /// <summary>
        /// Determines whether two GridPoint instances are equal.
        /// </summary>
        public static bool operator ==(GridPoint a, GridPoint b)
        {
            return (a.Row == b.Row) && (a.Col == b.Col);
        }

        /// <summary>
        /// Determines whether two GridPoint instances are unequal.
        /// </summary>
        public static bool operator !=(GridPoint a, GridPoint b)
        {
            return (a.Row != b.Row) || (a.Col != b.Col);
        }

        public override bool Equals(object obj)
        {
            return (obj is GridPoint) && (this == (GridPoint)obj);
        }

        public bool Equals(GridPoint other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return Row ^ Col;
        }
    }
}
