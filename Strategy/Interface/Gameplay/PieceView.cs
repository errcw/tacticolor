using System;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
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

        public PieceView(Piece piece, InterfaceContext context)
        {
            _piece = piece;
            _context = context;

            PlayerColor = _piece.Owner.GetPieceColor();
            UnreadyColor = Color.Lerp(PlayerColor, Color.White, 0.75f);

            Texture2D pieceTex = context.Content.Load<Texture2D>("Images/Piece");
            ImageSprite pieceBase = new ImageSprite(pieceTex);
            pieceBase.Origin = new Vector2(12, 22);
            pieceBase.Color = PlayerColor;

            Texture2D pieceShadowTex = context.Content.Load<Texture2D>("Images/PieceShadow");
            _shadowSprite = new ImageSprite(pieceShadowTex);
            _shadowSprite.Origin = pieceBase.Origin;
            _shadowSprite.Color = PlayerColor;

            Texture2D readyTex = context.Content.Load<Texture2D>("Images/PieceReadyOverlay");
            _readySprite = new ImageSprite(readyTex);
            _readySprite.Origin = pieceBase.Origin;
            _readySprite.Color = _piece.Ready ? PlayerColor : UnreadyColor;

            _pyramidSprite = new CompositeSprite(pieceBase, _readySprite);
            _pieceSprite = new CompositeSprite(_shadowSprite, _pyramidSprite);

            SpriteFont rollFont = context.Content.Load<SpriteFont>("Fonts/Roll");
            _rollSprite = new TextSprite(rollFont);
            _rollSprite.Color = Color.White;
            _rollSprite.OutlineWidth = 1;
            _rollSprite.OutlineColor = Color.Black;
            _rollSprite.Layer = 0f;

            _wasReady = _piece.Ready;
            _wasSelected = false;
        }

        /// <summary>
        /// Notifies this view that it was added to a territory.
        /// </summary>
        public void SetTerritory(TerritoryView territoryView)
        {
            _territoryView = territoryView;
            _wasSelected = _territoryView.IsSelected;
        }

        /// <summary>
        /// Notifies this piece that it was placed.
        /// </summary>
        public void OnPlaced(Cell placement, bool initialPlacement)
        {
            _pieceSprite.Position = GetPosition(placement);
            if (initialPlacement)
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
            _actionAnimation = new CompositeAnimation(
                GetMoveAnimation(destination),
                GetUnreadyAnimation(),
                GetDeselectedAnimation());
        }

        /// <summary>
        /// Notifies this piece it participated in an attack.
        /// </summary>
        public void OnAttacked(int roll, bool survived, Cell? destination, float showDelay, float actionDelay)
        {
            // build the roll animations
            _rollSprite.Position = _pieceSprite.Position + new Vector2(-5, -20);
            _rollSprite.Text = "+" + roll.ToString("D1", CultureInfo.CurrentCulture);
            _rollSprite.Color = Color.Transparent;

            IAnimation showRoll =
                new CompositeAnimation(
                    new ColorAnimation(_rollSprite, Color.White, 0.15f, Interpolation.InterpolateColor(Easing.QuadraticIn)),
                    new PositionAnimation(_rollSprite, _rollSprite.Position + new Vector2(0, -10), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

            IAnimation hideRoll =
                new CompositeAnimation(
                    new ColorAnimation(_rollSprite, Color.Transparent, 0.15f, Interpolation.InterpolateColor(Easing.QuadraticIn)),
                    new PositionAnimation(_rollSprite, _rollSprite.Position + new Vector2(0, -30), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

            // build the piece animations
            IAnimation pieceAction = null;
            if (survived && destination.HasValue) // survived and moved
            {
                pieceAction = GetMoveAnimation(destination.Value);
            }
            else if (survived) // survived and stayed
            {
                pieceAction = new DelayAnimation(0.5f);
            }
            else // did not survive
            {
                pieceAction = new CompositeAnimation(
                    new ScaleAnimation(_pieceSprite, Vector2.Zero, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                    new ColorAnimation(_pieceSprite, Color.Transparent, 0.45f, Interpolation.InterpolateColor(Easing.QuadraticOut)));
                _dying = true;
            }

            // hide the ready state if necessary
            if (!_piece.Ready)
            {
                pieceAction = new CompositeAnimation(
                    GetUnreadyAnimation(),
                    pieceAction);
            }

            // hide the selected state if necessary 
            if (_showingSelected)
            {
                pieceAction = new CompositeAnimation(
                    GetDeselectedAnimation(),
                    pieceAction);
            }

            // put together the sequence
            IAnimation animation = new SequentialAnimation(
                new DelayAnimation(showDelay),
                showRoll,
                new DelayAnimation(actionDelay),
                new CompositeAnimation(pieceAction, hideRoll));

            if (_actionAnimation == null)
            {
                _actionAnimation = animation;
            }
            else
            {
                // keep running any existing animation to completion
                _actionAnimation = new CompositeAnimation(_actionAnimation, animation);
            }
        }

        public void Update(float time)
        {
            if (!_dying)
            {
                // check if the territory holding us changed selected state
                if (!_wasSelected && _territoryView.IsSelected && _piece.Ready && !_showingSelected)
                {
                    _selectionAnimation = GetSelectedAnimation();
                }
                else if (_wasSelected && !_territoryView.IsSelected && _piece.Ready && _showingSelected)
                {
                    _selectionAnimation = GetDeselectedAnimation();
                }
                _wasSelected = _territoryView.IsSelected;

                // check if we just became ready and the territory is selected
                if (_piece.Ready && !_wasReady && _territoryView.IsSelected)
                {
                    _selectionAnimation = GetSelectedAnimation();
                }
                _wasReady = _piece.Ready;

                // show the readiness
                if (_actionAnimation == null)
                {
                    _readySprite.Color = Interpolation.InterpolateColor(Easing.Uniform)(UnreadyColor, PlayerColor, _piece.ReadyProgress);
                }
            }

            // update the animation
            if (_actionAnimation != null)
            {
                if (!_actionAnimation.Update(time))
                {
                    _actionAnimation = null;
                }
            }
            else if (_selectionAnimation != null)
            {
                if (!_selectionAnimation.Update(time))
                {
                    _selectionAnimation = null;
                }
            }
        }

        public void Draw(IsometricView isoView)
        {
            isoView.Add(_pieceSprite);
            isoView.Add(_rollSprite);
        }

        private Vector2 GetPosition(Cell cell)
        {
            Point point = _context.IsoParams.GetPoint(cell);
            Vector2 position = new Vector2(point.X, point.Y);
            position += new Vector2(18, 13); // offset in tile
            return position;
        }

        private IAnimation GetMoveAnimation(Cell destination)
        {
            Vector2 newPosition = GetPosition(destination);
            return new PositionAnimation(_pieceSprite, newPosition, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
        }

        private IAnimation GetUnreadyAnimation()
        {
            return new ColorAnimation(_readySprite, UnreadyColor, 0.5f, Interpolation.InterpolateColor(Easing.Uniform));
        }

        private IAnimation GetSelectedAnimation()
        {
            _showingSelected = true;
            return new PositionAnimation(_pyramidSprite, SelectionOffset, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticIn));
        }

        private IAnimation GetDeselectedAnimation()
        {
            _showingSelected = false;
            return new PositionAnimation(_pyramidSprite, Vector2.Zero, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
        }

        private Piece _piece;
        private InterfaceContext _context;

        private bool _wasReady;
        private bool _wasSelected;
        private bool _showingSelected;
        private bool _dying;
        private TerritoryView _territoryView;

        private Sprite _pieceSprite;
        private Sprite _shadowSprite, _pyramidSprite, _readySprite; // parts of the piece
        private TextSprite _rollSprite;

        private IAnimation _actionAnimation;
        private IAnimation _selectionAnimation;

        private readonly Color PlayerColor;
        private readonly Color UnreadyColor;
        private readonly Vector2 SelectionOffset = new Vector2(0, -5);
    }
}
