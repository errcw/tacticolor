using System;
using System.Globalization;

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
        /// <summary>
        /// Returns true if this piece is visible; otherwise, false.
        /// </summary>
        public bool IsVisible
        {
            get { return _sprite.Color.A > 0; }
        }

        public PieceView(Piece piece, Cell placement, bool wasPlaced, InterfaceContext context)
        {
            _piece = piece;
            _context = context;

            Texture2D pieceTex = context.Content.Load<Texture2D>("Piece");
            _sprite = new ImageSprite(pieceTex);
            _sprite.Position = GetPosition(placement);
            _sprite.Origin = new Vector2(12, 22);

            SpriteFont rollFont = context.Content.Load<SpriteFont>("Fonts/Roll");
            _roll = new TextSprite(rollFont);
            _roll.Color = Color.White;
            _roll.OutlineWidth = 1;
            _roll.OutlineColor = Color.Black;
            _roll.Layer = 0f;

            if (wasPlaced)
            {
                _sprite.Scale = Vector2.Zero;
                _animation = new ScaleAnimation(_sprite, Vector2.One, 0.6f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
            }
        }

        /// <summary>
        /// Notifies this piece that it should animate to a new position.
        /// </summary>
        /// <param name="destination">The destination cell on the map.</param>
        public void OnMoved(Cell destination)
        {
            _animation = GetMoveAnimation(destination);
        }

        /// <summary>
        /// Notifies this piece it participated in an attack.
        /// </summary>
        public void OnAttacked(int roll, bool survived, Cell? destination, float showDelay, float actionDelay)
        {
            _roll.Position = _sprite.Position + new Vector2(-5, -20);
            _roll.Text = roll.ToString("D1", CultureInfo.CurrentCulture);
            _roll.Color = Color.TransparentWhite;

            IAnimation showRoll =
                new CompositeAnimation(
                    new ColorAnimation(_roll, Color.White, 0.15f, Interpolation.InterpolateColor(Easing.QuadraticIn)),
                    new PositionAnimation(_roll, _roll.Position + new Vector2(0, -10), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

            IAnimation hideRoll =
                new CompositeAnimation(
                    new ColorAnimation(_roll, Color.TransparentWhite, 0.15f, Interpolation.InterpolateColor(Easing.QuadraticIn)),
                    new PositionAnimation(_roll, _roll.Position + new Vector2(0, -30), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

            IAnimation pieceAction = null;
            if (survived && destination.HasValue)
            {
                pieceAction = GetMoveAnimation(destination.Value);
            }
            else if (survived)
            {
                pieceAction = new DelayAnimation(0.5f);
            }
            else
            {
                pieceAction = new CompositeAnimation(
                    new ScaleAnimation(_sprite, Vector2.Zero, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                    new ColorAnimation(_sprite, Color.TransparentWhite, 0.45f, Interpolation.InterpolateColor(Easing.QuadraticOut)));
            }

            _animation = new SequentialAnimation(
                new DelayAnimation(showDelay),
                showRoll,
                new DelayAnimation(actionDelay),
                new CompositeAnimation(pieceAction, hideRoll));
        }

        public void Update(float time)
        {
            if (_animation == null)
            {
                // only change colours when the piece is idle
                //TODO need a better way to show readiness
                _sprite.Color =
                    Interpolation.InterpolateColor(Easing.Uniform)(
                        Color.White,
                        GetPlayerColor(_piece.Owner),
                        (float)_piece.TimerValue / _piece.TimerMax);
            }

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
            isoBatch.Draw(_roll);
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
        /// Creates an animation to move to a new cell.
        /// </summary>
        private IAnimation GetMoveAnimation(Cell destination)
        {
            Vector2 newPosition = GetPosition(destination);
            return new PositionAnimation(_sprite, newPosition, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
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
        private TextSprite _roll;
        private IAnimation _animation;
    }
}
