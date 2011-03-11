using System;
using System.Globalization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Animation;
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
        }

        /// <summary>
        /// Notifies this view that it was added to a territory.
        /// </summary>
        public void SetTerritory(TerritoryView territoryView)
        {
            _territoryView = territoryView;
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
            _actionAnimation = GetMoveAnimation(destination);
        }

        /// <summary>
        /// Notifies this piece it participated in an attack.
        /// </summary>
        public void OnAttacked(bool survived, Cell? destination, float attackDelay, float actionDelay)
        {
            // build the attack animation
            Vector2 attackPosition = ShouldShowSelected() ? SelectionOffset + AttackOffset : AttackOffset;
            Vector2 returnPosition = attackPosition - AttackOffset;
            IAnimation pieceAttack = new SequentialAnimation(
                new PositionAnimation(_pyramidSprite, attackPosition, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticIn)),
                new PositionAnimation(_pyramidSprite, returnPosition, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

            // build the action animations
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
            }

            // put together the sequence
            IAnimation animation = new SequentialAnimation(
                new DelayAnimation(attackDelay),
                pieceAttack,
                new DelayAnimation(actionDelay),
                pieceAction);

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
            // show the selected state
            if (ShouldShowSelected() && !_showingSelected)
            {
                _selectionAnimation = new PositionAnimation(_pyramidSprite, SelectionOffset, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticIn));
                _showingSelected = true;
            }
            else if (!ShouldShowSelected() && _showingSelected)
            {
                _selectionAnimation = new PositionAnimation(_pyramidSprite, Vector2.Zero, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
                _showingSelected = false;
            }

            // corner case: territory is selected, piece is not ready, territory
            // is attacked, piece becomes ready during the attack; attack
            // animation cancels out the selection--cope with this case by
            // forcing the piece to show as selected
            if (ShouldShowSelected() && _showingSelected &&
                _pyramidSprite.Position != SelectionOffset && _selectionAnimation == null && _actionAnimation == null)
            {
                _selectionAnimation = new PositionAnimation(_pyramidSprite, SelectionOffset, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticIn));
            }
            // corner case: sometimes pieces are not unselected correctly, for
            // whatever obscure bug that is not yet fixed--cope with this case
            // by forcing the piece to show as unselected
            if (!ShouldShowSelected() && !_showingSelected &&
                _pyramidSprite.Position != Vector2.Zero && _selectionAnimation == null && _actionAnimation == null)
            {
                _selectionAnimation = new PositionAnimation(_pyramidSprite, Vector2.Zero, 0.1f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
            }

            // show the readiness
            _readySprite.Color = Interpolation.InterpolateColor(Easing.Uniform)(UnreadyColor, PlayerColor, _piece.ReadyProgress);

            // update the animation
            if (_actionAnimation != null)
            {
                if (!_actionAnimation.Update(time))
                {
                    _actionAnimation = null;
                }
            }
            if (_selectionAnimation != null)
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

        private bool ShouldShowSelected()
        {
            return _territoryView.IsSelected && _piece.Ready;
        }

        private Piece _piece;
        private InterfaceContext _context;

        private bool _showingSelected;
        private TerritoryView _territoryView;

        private Sprite _pieceSprite;
        private Sprite _shadowSprite, _pyramidSprite, _readySprite; // parts of the piece

        private IAnimation _actionAnimation;
        private IAnimation _selectionAnimation;

        private readonly Color PlayerColor;
        private readonly Color UnreadyColor;
        private readonly Vector2 SelectionOffset = new Vector2(0, -5);
        private readonly Vector2 AttackOffset = new Vector2(0, -5);
    }
}
