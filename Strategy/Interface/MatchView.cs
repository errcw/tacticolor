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
        }

        public void Update(float time)
        {
        }

        public void Draw()
        {
        }

        private Match _match;
        private InterfaceContext _context;
    }
}
