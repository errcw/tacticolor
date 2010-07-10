using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a piece.
    /// </summary>
    public class PieceView
    {
        public PieceView(Piece piece, Cell placement, bool wasPlaced, InterfaceContext context)
        {
            _piece = piece;
            _context = context;

            Texture2D pieceTex = context.Content.Load<Texture2D>("PieceAlt");
            _sprite = new ImageSprite(pieceTex);
            _sprite.Position = GetPosition(placement);
            _sprite.Origin = new Vector2(12, 22);

            if (wasPlaced)
            {
                _sprite.Scale = Vector2.Zero;
                _animation = new ScaleAnimation(_sprite, Vector2.One, 0.6f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
            }
        }

        public void OnMoved(Cell destination)
        {
            Vector2 newPosition = GetPosition(destination);
            _animation = new PositionAnimation(_sprite, newPosition, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
        }

        public void Update(float time)
        {
            _sprite.Color =
                Interpolation.InterpolateColor(Easing.Uniform)(
                    Color.White,
                    GetPlayerColor(_piece.Owner),
                    (float)_piece.TimerValue / _piece.TimerMax);

            if (_animation != null)
            {
                if (!_animation.Update(time))
                {
                    _animation = null;
                }
            }
        }

        public void Draw(IsometricBatch isoBatch)
        {
            isoBatch.Draw(_sprite);
        }

        /// <summary>
        /// Returns the pixel position mapped for the given tile.
        /// </summary>
        private Vector2 GetPosition(Cell cell)
        {
            Point point = _context.IsoParams.GetPoint(cell);
            Vector2 position = new Vector2(point.X, point.Y);
            position += new Vector2(18, 13); // offset in tile
            return position;
        }

        /// <summary>
        /// Returns the color of the given player.
        /// </summary>
        private Color GetPlayerColor(PlayerId player)
        {
            switch (player)
            {
                case PlayerId.A: return new Color(221, 104, 168);
                case PlayerId.B: return new Color(103, 180, 219);
                case PlayerId.C: return new Color(82, 165, 114);
                case PlayerId.D: return new Color(249, 235, 124);
                default: throw new ArgumentException("Invalid player id " + player);
            }
        }

        private Piece _piece;
        private InterfaceContext _context;

        private Sprite _sprite;
        private IAnimation _animation;
    }
}
