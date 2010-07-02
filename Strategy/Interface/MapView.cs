using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a map.
    /// </summary>
    public class MapView
    {
        /// <summary>
        /// Calculates the pixel extents of this
        /// </summary>
        public Rectangle Extents { get; private set; }

        public MapView(Map map, InterfaceContext context)
        {
            _map = map;
            _context = context;
        }

        public void Update(float time)
        {
        }

        public void Draw()
        {
        }

        private Map _map;
        private InterfaceContext _context;
    }
}
