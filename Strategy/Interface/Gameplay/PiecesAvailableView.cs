using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Animation;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
{
    public class PiecesAvailableView
    {
        public PlayerId Player { get; private set; }

        public PiecesAvailableView(Match match, PlayerId player, InterfaceContext context)
        {
            _match = match;
            _match.PieceCreated += OnPieceCreated;
            _match.PiecePlaced += OnPiecePlaced;
            Player = player;

            BasePosition = GetBasePosition(Player);
            SolidColor = Player.GetPieceColor();
            TransparentColor = ColorExtensions.FromNonPremultiplied(SolidColor, 0f);

            Texture2D pieceSprite = context.Content.Load<Texture2D>("Images/PieceAvailable");
            int sprites = _match.MaxPiecesAvailable + 1;
            _unused = new Queue<Sprite>(sprites);
            _created = new Stack<Sprite>(sprites);
            for (int p = 0; p < sprites; p++)
            {
                Sprite sprite = new ImageSprite(pieceSprite);
                sprite.Y = BasePosition.Y;
                sprite.Color = TransparentColor;
                sprite.Origin = new Vector2((int)(sprite.Size.X / 2), (int)(sprite.Size.Y / 2));
                _unused.Enqueue(sprite);
            }

            SetUpCreatingSprite();
        }

        public void Update(float time)
        {
            if (_creatingSprite != null)
            {
                float progress = _match.PieceCreationProgress[(int)Player];
                _creatingSprite.Color = ColorExtensions.FromNonPremultiplied(SolidColor, progress);
                _creatingSprite.X = Interpolation.InterpolateFloat(Easing.Uniform)(_creatingSprite.X, _creatingTargetX, 8f * time);
            }
            if (_createdAnimation != null)
            {
                if (!_createdAnimation.Update(time))
                {
                    _createdAnimation = null;
                }
            }
            if (_hideAnimation != null)
            {
                if (!_hideAnimation.Update(time))
                {
                    _hideAnimation = null;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _created.ForEach(sprite => sprite.Draw(spriteBatch));
            _unused.ForEach(sprite => sprite.Draw(spriteBatch));
            if (_creatingSprite != null)
            {
                _creatingSprite.Draw(spriteBatch);
            }
        }

        public void Hide()
        {
            IEnumerable<Sprite> sprites = _created;
            if (_creatingSprite != null)
            {
                sprites = sprites.Concat(Enumerable.Repeat(_creatingSprite, 1));
                _creatingSprite = null; // force it to stop updating
            }
            var animations = sprites.Select(s => new ColorAnimation(s, TransparentColor, 1f, Interpolation.InterpolateColor(Easing.QuadraticOut)));
            _hideAnimation = new CompositeAnimation(animations.ToArray());
        }

        /// <summary>
        /// Notifies this view that a piece was created.
        /// </summary>
        private void OnPieceCreated(object match, PlayerEventArgs args)
        {
            if (args.Player == Player)
            {
                // bail if there is no creating sprite to update (when the view is hidden)
                if (_creatingSprite == null)
                {
                    return;
                }

                _creatingSprite.Color = SolidColor;
                _creatingSprite.X = _creatingTargetX;
                _created.Push(_creatingSprite);

                _createdAnimation = new SequentialAnimation(
                    new ScaleAnimation(_creatingSprite, new Vector2(1.25f, 1.25f), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticIn)),
                    new ScaleAnimation(_creatingSprite, Vector2.One, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)));

                _creatingSprite = null;
                if (_match.PiecesAvailable[(int)Player] < _match.MaxPiecesAvailable)
                {
                    SetUpCreatingSprite();
                }

                System.Diagnostics.Debug.Assert(_created.Count == _match.PiecesAvailable[(int)Player]);
            }
        }

        /// <summary>
        /// Notifies this view that a piece was placed and might need to be removed.
        /// </summary>
        private void OnPiecePlaced(object match, PiecePlacedEventArgs args)
        {
            if (args.Location.Owner == Player)
            {
                System.Diagnostics.Debug.Assert(_created.Count-1 == _match.PiecesAvailable[(int)Player]);

                // hide the old sprite
                Sprite used = _created.Pop();
                if (_hideAnimation != null)
                {
                    // if we are reusing the animation then we need to make sure
                    // the previous one is finished so fake a large time step
                    _hideAnimation.Update(1f);
                }
                _hideAnimation = new CompositeAnimation(
                    new PositionAnimation(used, used.Position + new Vector2(0, 30), 0.3f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                    new ColorAnimation(used, TransparentColor, 0.2f, Interpolation.InterpolateColor(Easing.QuadraticOut)));
                _unused.Enqueue(used);

                if (_creatingSprite != null)
                {
                    // slide into the now-vacant position
                    _creatingTargetX = BasePosition.X + _match.PiecesAvailable[(int)Player] * PieceSpacing;
                }
                else
                {
                    // made room for a new piece
                    SetUpCreatingSprite();
                }
            }
        }

        /// <summary>
        /// Grabs and initializes a new creating sprite.
        /// </summary>
        private void SetUpCreatingSprite()
        {
            _creatingSprite = _unused.Dequeue();
            _creatingSprite.Color = TransparentColor;
            _creatingSprite.Y = BasePosition.Y;
            _creatingSprite.X = BasePosition.X + _match.PiecesAvailable[(int)Player] * PieceSpacing;
            _creatingTargetX = _creatingSprite.X;
        }

        /// <summary>
        /// Returns the position at which to start drawing.
        /// </summary>
        private Vector2 GetBasePosition(PlayerId player)
        {
            const int OriginOffset = 12;
            const int BaseX = 80 + OriginOffset;
            const int BaseY = 80;
            const int SpacingY = 100;
            switch (player)
            {
                case PlayerId.A: return new Vector2(BaseX, BaseY + OriginOffset);
                case PlayerId.B: return new Vector2(BaseX, BaseY + OriginOffset + SpacingY);
                case PlayerId.C: return new Vector2(BaseX, 720 - 25 - BaseY + OriginOffset - SpacingY);
                case PlayerId.D: return new Vector2(BaseX, 720 - 25 - BaseY + OriginOffset);
                default: throw new ArgumentException("Invalid player id " + player);
            }
        }

        private Match _match;

        private Queue<Sprite> _unused;
        private Stack<Sprite> _created;
        private Sprite _creatingSprite;
        private float _creatingTargetX;

        private IAnimation _createdAnimation;
        private IAnimation _hideAnimation;

        private readonly Color TransparentColor;
        private readonly Color SolidColor;

        private readonly Vector2 BasePosition;
        private const float PieceSpacing = 30f;
    }
}
