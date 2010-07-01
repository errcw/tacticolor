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
        public MapView(Map map)
        {
            _map = map;
        }

        public void Update(float time)
        {
        }

        public void Draw()
        {
        }

        private Map _map;
    }
}
