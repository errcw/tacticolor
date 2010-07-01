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
        public MatchView(Match match)
        {
            _match = match;
        }

        public void Update(float time)
        {
        }

        public void Draw()
        {
        }

        private Match _match;
    }
}
