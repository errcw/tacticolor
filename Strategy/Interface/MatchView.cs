using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a match.
    /// </summary>
    public class MatchView
    {
        public MatchView(Match match, InterfaceContext context)
        {
            _match = match;
            _context = context;

            _mapView = new MapView(match.Map, match, context);
        }

        public void Update(float time)
        {
            _mapView.Update(time);
        }

        public void Draw()
        {
            _mapView.Draw();
        }

        private Match _match;
        private InterfaceContext _context;

        private MapView _mapView;
    }
}
