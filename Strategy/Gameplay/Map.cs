using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace Strategy.Gameplay
{
    /// <summary>
    /// A region of the map.
    /// </summary>
    public class Territory
    {
        /// <summary>
        /// The location of this territory.
        /// </summary>
        public Vector2 Position { get; private set; }

        /// <summary>
        /// The territories adjacent to this one.
        /// </summary>
        public IList<Territory> Adjacent { get; private set; }

        /// <summary>
        /// The player owning this territory.
        /// </summary>
        public PlayerId? Owner { get; set; }

        /// <summary>
        /// Creates a new territory at a fixed location.
        /// </summary>
        /// <param name="x">The x coordinate of the center.</param>
        /// <param name="y">The y coordinate of the center.</param>
        public Territory(int x, int y)
        {
            Position = new Vector2(x, y);
            Adjacent = new List<Territory>();
        }
    }

    /// <summary>
    /// Represents a map.
    /// </summary>
    public class Map
    {
        /// <summary>
        /// The territories contained in this map.
        /// </summary>
        public IEnumerable<Territory> Territories { get; private set; }

        /// <summary>
        /// Creates a new map with a fixed set of territories.
        /// </summary>
        /// <param name="territories">The territories on the map.</param>
        public Map(Territory[] territories)
        {
            Territories = territories;
        }
    }
}
