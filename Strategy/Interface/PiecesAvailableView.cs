using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    public class PiecesAvailableView
    {
        public PiecesAvailableView(Match match, PlayerId player, InterfaceContext context)
        {
            _match = match;
            _match.PiecePlaced += OnPiecePlaced;
            _player = player;
            _context = context;

            SolidColor = GetPlayerColor(_player);
            TransparentColor = new Color(SolidColor, 0);

            Texture2D pieceSprite = context.Content.Load<Texture2D>("PieceAvailable");
            int sprites = _match.MaxPiecesAvailable + 1;
            _unused = new Queue<Sprite>(sprites);
            _created = new Stack<Sprite>(sprites);
            for (int p = 0; p < sprites; p++)
            {
                Sprite sprite = new ImageSprite(pieceSprite);
                sprite.Y = BasePosition.Y + PlayerSpacing.Y * (int)_player;
                sprite.Color = TransparentColor;
                _unused.Enqueue(sprite);
            }

            SetUpCreatingSprite();

            _lastAvailable = 0;
        }

        public void Update(float time)
        {
            // check if a new piece was created this frame
            int available = _match.PiecesAvailable[(int)_player];
            if (available != _lastAvailable)
            {
                if (available > _lastAvailable)
                {
                    OnPieceCreated();
                }
                _lastAvailable = available;
            }

            if (_creatingSprite != null)
            {
                float progress = _match.PieceCreationProgress[(int)_player];
                _creatingSprite.Color = new Color(SolidColor, (byte)(progress * 255));
                _creatingSprite.X = Interpolation.InterpolateFloat(Easing.Uniform)(_creatingSprite.X, _creatingTargetX, 8f * time);
            }

            if (_hideAnimation != null)
            {
                if (!_hideAnimation.Update(time))
                {
                    _hideAnimation = null;
                }
            }

            System.Diagnostics.Debug.Assert(_created.Count == _match.PiecesAvailable[(int)_player]);
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

        /// <summary>
        /// Notifies this view that a piece was created.
        /// </summary>
        private void OnPieceCreated()
        {
            _creatingSprite.Color = SolidColor;
            _creatingSprite.X = _creatingTargetX;
            _created.Push(_creatingSprite);

            _creatingSprite = null;
            if (_match.PiecesAvailable[(int)_player] < _match.MaxPiecesAvailable)
            {
                SetUpCreatingSprite();
            }
        }

        /// <summary>
        /// Notifies this view that a piece was placed and might need to be removed.
        /// </summary>
        private void OnPiecePlaced(object match, PiecePlacedEventArgs args)
        {
            if (args.Location.Owner == _player)
            {
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
                    _creatingTargetX = BasePosition.X + _match.PiecesAvailable[(int)_player] * PieceSpacing.X;
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
            _creatingSprite.Y = BasePosition.Y + (int)_player * PlayerSpacing.Y;
            _creatingSprite.X = BasePosition.X + _match.PiecesAvailable[(int)_player] * PieceSpacing.X;
            _creatingTargetX = _creatingSprite.X;
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

        private Match _match;
        private PlayerId _player;
        private InterfaceContext _context;

        private int _lastAvailable;

        private Queue<Sprite> _unused;
        private Stack<Sprite> _created;
        private Sprite _creatingSprite;
        private float _creatingTargetX;

        private IAnimation _hideAnimation;

        private readonly Color TransparentColor;
        private readonly Color SolidColor;

        private readonly Vector2 BasePosition = new Vector2(80, 50);
        private readonly Vector2 PlayerSpacing = new Vector2(0, 50);
        private readonly Vector2 PieceSpacing = new Vector2(30, 0);
    }
}
