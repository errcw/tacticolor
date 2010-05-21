using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace Strategy.Gameplay
{
    /// <summary>
    /// A contiguous region on a map.
    /// </summary>
    public class GridTerritory
    {
        /// <summary>
        /// The center of the territory.
        /// </summary>
        public Point Location { get; set; }

        /// <summary>
        /// The grid cells occupied by this territory.
        /// </summary>
        public IList<Point> Area { get; private set; }

        /// <summary>
        /// The territories adjacent to this territory.
        /// </summary>
        public IList<GridTerritory> Adjacent { get; private set; }

        /// <summary>
        /// The player owning this territory.
        /// </summary>
        public PlayerId? Owner { get; set; }

        /// <summary>
        /// Creates a new territory.
        /// </summary>
        public GridTerritory(IEnumerable<Point> area)
        {
            Area = new List<Point>();
            Adjacent = new List<GridTerritory>();
        }
    }

    /// <summary>
    /// Represents a map.
    /// </summary>
    public class GridMap
    {
        /// <summary>
        /// The territories contained in this map.
        /// </summary>
        public IEnumerable<GridTerritory> Territories { get; private set; }

        /// <summary>
        /// Creates a new map with a fixed set of territories.
        /// </summary>
        /// <param name="territories">The territories on the map.</param>
        public GridMap(GridTerritory[] territories)
        {
            Territories = territories;
        }
    }
}
