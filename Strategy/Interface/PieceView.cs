using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a piece.
    /// </summary>
    public class PieceView
    {
        public PieceView(Piece piece, InterfaceContext context)
        {
            _piece = piece;
            _context = context;

            Texture2D pieceTex = context.Content.Load<Texture2D>("Piece");
            _sprite = new IsometricSprite(pieceTex);
            _sprite.Origin = new Vector2(0, 15); // offset to bottom
        }

        public void SetCell(Cell cell)
        {
            Point point = _context.IsoParams.GetPoint(cell);
            _sprite.Position = new Vector2(point.X, point.Y);
            _sprite.Position += new Vector2(7, 8); // offset in tile
        }

        public void Update(float time)
        {
            _sprite.Color =
                Interpolation.InterpolateColor(Easing.Uniform)(
                    Color.White,
                    GetPlayerColor(_piece.Owner),
                    (float)_piece.TimerValue / _piece.TimerMax);
        }

        public void Draw(IsometricBatch isoBatch)
        {
            isoBatch.Draw(_sprite);
        }

        /// <summary>
        /// Returns the color of the given player.
        /// </summary>
        private Color GetPlayerColor(PlayerId player)
        {
            return Color.White;
            switch (player)
            {
                case PlayerId.A: return Color.Tomato;
                case PlayerId.B: return Color.RoyalBlue;
                case PlayerId.C: return Color.SeaGreen;
                case PlayerId.D: return Color.Crimson;
                default: return Color.White;
            }
        }

        private Piece _piece;
        private InterfaceContext _context;

        private IsometricSprite _sprite;
    }
}
