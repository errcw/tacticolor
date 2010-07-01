using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a piece.
    /// </summary>
    public class PieceView
    {
        public PieceView(Piece piece)
        {
            _piece = piece;
        }

        public void Update(float time)
        {
        }

        public void Draw()
        {
        }

        private Piece _piece;
    }
}
