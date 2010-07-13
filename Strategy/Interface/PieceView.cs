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
            get { return _pieceSprite.Color.A > 0; }
        }

        public PieceView(Piece piece, Cell placement, bool wasPlaced, InterfaceContext context)
        {
            _piece = piece;
            _context = context;

            Texture2D pieceTex = context.Content.Load<Texture2D>("Piece");
            _pieceSprite = new ImageSprite(pieceTex);
            _pieceSprite.Position = GetPosition(placement);
            _pieceSprite.Origin = new Vector2(12, 22);
            _pieceSprite.Color = GetPlayerColor(piece.Owner);

            Texture2D readyTex = context.Content.Load<Texture2D>("PieceReadiness");
            _readySprite = new ImageSprite(readyTex);
            _readySprite.Position = _pieceSprite.Position;
            _readySprite.Origin = _pieceSprite.Origin;
            _readySprite.Layer = 0.1f;

            SpriteFont rollFont = context.Content.Load<SpriteFont>("Fonts/Roll");
            _rollSprite = new TextSprite(rollFont);
            _rollSprite.Color = Color.White;
            _rollSprite.OutlineWidth = 1;
            _rollSprite.OutlineColor = Color.Black;
            _rollSprite.Layer = 0f;

            if (wasPlaced)
            {
                _pieceSprite.Scale = Vector2.Zero;
                _actionAnimation = new ScaleAnimation(_pieceSprite, Vector2.One, 0.6f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
            }
        }

        /// <summary>
        /// Notifies this piece that it should animate to a new position.
        /// </summary>
        /// <param name="destination">The destination cell on the map.</param>
        public void OnMoved(Cell destination)
        {
            _actionAnimation = GetMoveAnimation(destination);
        }

        /// <summary>
        /// Notifies this piece it participated in an attack.
        /// </summary>
        public void OnAttacked(int roll, bool survived, bool attacker, Cell? destination, float showDelay, float actionDelay)
        {
            _rollSprite.Position = _pieceSprite.Position + new Vector2(-5, -20);
            _rollSprite.Text = roll.ToString("D1", CultureInfo.CurrentCulture);
            _rollSprite.Color = Color.TransparentWhite;

            IAnimation showRoll =
                new CompositeAnimation(
                    new ColorAnimation(_rollSprite, Color.White, 0.15f, Interpolation.InterpolateColor(Easing.QuadraticIn)),
                    new PositionAnimation(_rollSprite, _rollSprite.Position + new Vector2(0, -10), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

            IAnimation hideRoll =
                new CompositeAnimation(
                    new ColorAnimation(_rollSprite, Color.TransparentWhite, 0.15f, Interpolation.InterpolateColor(Easing.QuadraticIn)),
                    new PositionAnimation(_rollSprite, _rollSprite.Position + new Vector2(0, -30), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

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
                    new ScaleAnimation(_pieceSprite, Vector2.Zero, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                    new ColorAnimation(_pieceSprite, Color.TransparentWhite, 0.45f, Interpolation.InterpolateColor(Easing.QuadraticOut)));
            }
            if (attacker)
            {
                pieceAction = new CompositeAnimation(
                    pieceAction, 
                    new ColorAnimation(_readySprite, Color.White, 0.5f, Interpolation.InterpolateColor(Easing.Uniform)));
            }

            _actionAnimation = new SequentialAnimation(
                new DelayAnimation(showDelay),
                showRoll,
                new DelayAnimation(actionDelay),
                new CompositeAnimation(pieceAction, hideRoll));
        }

        public void Update(float time)
        {
            if (_actionAnimation == null)
            {
                // only change colours when the piece is idle
                _readySprite.Color =
                    Interpolation.InterpolateColor(Easing.Uniform)(
                        Color.White,
                        GetPlayerColor(_piece.Owner),
                        _piece.ReadyProgress);
            }

            if (_actionAnimation != null)
            {
                if (!_actionAnimation.Update(time))
                {
                    _actionAnimation = null;
                }
            }
        }

        public void Draw(IsometricBatch isoBatch)
        {
            isoBatch.Draw(_pieceSprite);
            isoBatch.Draw(_readySprite);
            isoBatch.Draw(_rollSprite);
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
            //return new PositionAnimation(_pieceSprite, newPosition, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
            return new CompositeAnimation(
                new PositionAnimation(_pieceSprite, newPosition, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                new PositionAnimation(_readySprite, newPosition, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                new ColorAnimation(_readySprite, Color.White, 0.5f, Interpolation.InterpolateColor(Easing.Uniform)));
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

        private Sprite _pieceSprite;
        private Sprite _readySprite;
        private TextSprite _rollSprite;

        private IAnimation _actionAnimation;
    }
}
