using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library;
using Strategy.Library.Animation;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Shows the input cursor.
    /// </summary>
    public class LocalInputView
    {
        public LocalInput Input { get; private set; }

        public LocalInputView(LocalInput input, InterfaceContext context)
        {
            Input = input;
            _context = context;

            Input.HoveredChanged += OnHoveredChanged;
            Input.SelectedChanged += OnSelectedChanged;
            Input.ActionPerformed += OnActionPerformed;
            Input.ActionRejected += OnActionRejected;

            Texture2D cursorTex = context.Content.Load<Texture2D>("Images/Cursor");
            Color cursorColor = input.Player.GetSelectionColor();

            _cursor = new ImageSprite(cursorTex);
            _cursor.Color = cursorColor;
            _cursor.Position = GetPosition(ChooseCell(Input.Hovered));
            _cursor.Origin = new Vector2(1, 14);

            _pickUpEffect = context.Content.Load<SoundEffect>("Sounds/PlayerPickUp");
            _putDownEffect = context.Content.Load<SoundEffect>("Sounds/PlayerPutDown");
            _attackEffect = context.Content.Load<SoundEffect>("Sounds/PlayerAttack");
            _moveEffect = context.Content.Load<SoundEffect>("Sounds/PlayerMove");
            _placeEffect = context.Content.Load<SoundEffect>("Sounds/PlayerPlace");
            _invalidEffect = context.Content.Load<SoundEffect>("Sounds/PlayerInvalid");

            _animation = GetBounceAnimation();
            _repeatAnimation = true;
        }

        public void Update(float time)
        {
            if (_animation != null)
            {
                if (!_animation.Update(time))
                {
                    if (_repeatAnimation)
                    {
                        _animation.Start();
                    }
                    else
                    {
                        _animation = null;
                    }
                }
            }
        }

        public void Draw(IsometricView isoView)
        {
            isoView.Add(_cursor);
        }

        public void Hide()
        {
            _animation = new ColorAnimation(_cursor, Color.Transparent, 0.5f, Interpolation.InterpolateColor(Easing.QuadraticOut));
            _repeatAnimation = false;
        }

        /// <summary>
        /// Updates the view when the hovered territory changes.
        /// </summary>
        private void OnHoveredChanged(object input, InputChangedEventArgs args)
        {
            Cell cell = ChooseCell(Input.Hovered);
            _cursor.Position = GetPosition(cell);
            _animation = GetBounceAnimation();
        }

        /// <summary>
        /// Updates the view when the selected territory changes.
        /// </summary>
        private void OnSelectedChanged(object input, InputChangedEventArgs args)
        {
            if (Input.Selected != null)
            {
                _pickUpEffect.Play();
            }
            else
            {
                if (args.WasPlayerInitiated)
                {
                    _putDownEffect.Play();
                }
            }
        }

        /// <summary>
        /// Updates the view when a player performs an action.
        /// </summary>
        private void OnActionPerformed(object input, ActionEventArgs args)
        {
            if (args.Command is PlaceCommand)
            {
                _placeEffect.Play();
            }
            else if (args.Command is MoveCommand)
            {
                _moveEffect.Play();
            }
            else if (args.Command is AttackCommand)
            {
                _attackEffect.Play();
            }
        }

        /// <summary>
        /// Updates the view when a player attempted an invalid action.
        /// </summary>
        private void OnActionRejected(object input, EventArgs args)
        {
            _invalidEffect.Play();
        }

        /// <summary>
        /// Chooses the cell on which to place the cursor.
        /// </summary>
        private Cell ChooseCell(Territory territory)
        {
            int playerIndex = (int)Input.Player;
            int cellIndex = 0;
            foreach (Cell cell in territory.Area)
            {
                int dr = cell.Row - territory.Location.Row;
                int dc = cell.Col - territory.Location.Col;
                if (Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1)
                {
                    continue; // piece placement
                }
                if (cellIndex == playerIndex)
                {
                    return cell;
                }
                cellIndex += 1;
            }
            // should never reach here
            return territory.Area.First();
        }

        /// <summary>
        /// Returns the pixel position mapped for the given tile.
        /// </summary>
        private Vector2 GetPosition(Cell cell)
        {
            Point point = _context.IsoParams.GetPoint(cell);
            Vector2 position = new Vector2(point.X, point.Y);
            position += new Vector2(9, 8); // offset in tile
            return position;
        }

        /// <summary>
        /// Creates an animation of the cursor bouncing at its current position.
        /// </summary>
        private IAnimation GetBounceAnimation()
        {
            return new SequentialAnimation(
                new PositionAnimation(_cursor, _cursor.Position + new Vector2(0, -10), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticIn)),
                new PositionAnimation(_cursor, _cursor.Position, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                new DelayAnimation(0.5f));
        }

        private InterfaceContext _context;

        private Sprite _cursor;

        private IAnimation _animation;
        private bool _repeatAnimation;

        private SoundEffect _pickUpEffect;
        private SoundEffect _putDownEffect;
        private SoundEffect _attackEffect;
        private SoundEffect _moveEffect;
        private SoundEffect _placeEffect;
        private SoundEffect _invalidEffect;

        private readonly Vector2 HoverOffset = new Vector2(0, -5);
    }
}
