using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a territory.
    /// </summary>
    public class TerritoryView
    {
        public TerritoryView(Territory territory)
        {
            _territory = territory;
        }

        public void Update(float time)
        {
        }

        public void Draw()
        {
        }

        private Territory _territory;
    }
}
