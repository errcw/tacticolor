using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
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

            Texture2D cursorTex = context.Content.Load<Texture2D>("Images/Cursor");

            _cursorHover = new ImageSprite(cursorTex);
            _cursorHover.Color = GetPlayerColor(input.Player);
            _cursorHover.Position = GetPosition(ChooseCell(Input.Hovered));
            _cursorHover.Origin = new Vector2(0, 14);

            _cursorSelect = new ImageSprite(cursorTex);
            _cursorSelect.Color = new Color(GetPlayerColor(input.Player), 128);
            _cursorSelect.Origin = new Vector2(0, 14);

            // bounce the cursor until the player acts
            _animation = new SequentialAnimation(
                new PositionAnimation(_cursorHover, _cursorHover.Position + new Vector2(0, -10), 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticIn)),
                new PositionAnimation(_cursorHover, _cursorHover.Position, 0.5f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                new DelayAnimation(0.5f));
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

        public void Draw(IsometricBatch spriteBatch)
        {
            spriteBatch.Draw(_cursorHover);
            if (_showSelect)
            {
                spriteBatch.Draw(_cursorSelect);
            }
        }

        public void Hide()
        {
            _animation = new CompositeAnimation(
                new ColorAnimation(_cursorHover, Color.TransparentWhite, 0.5f, Interpolation.InterpolateColor(Easing.QuadraticOut)),
                new ColorAnimation(_cursorSelect, Color.TransparentWhite, 0.5f, Interpolation.InterpolateColor(Easing.QuadraticOut)));
            _repeatAnimation = false;
        }

        /// <summary>
        /// Updates the view when the hovered territory changes.
        /// </summary>
        private void OnHoveredChanged(object input, InputChangedEventArgs args)
        {
            Cell cell = ChooseCell(Input.Hovered);
            _cursorHover.Position = GetPosition(cell);
            if (Input.Selected != null && Input.Hovered == Input.Selected)
            {
                _cursorHover.Position += HoverOffset;
            }

            _animation = null; // end the bounce animation
        }

        /// <summary>
        /// Updates the view when the selected territory changes.
        /// </summary>
        private void OnSelectedChanged(object input, InputChangedEventArgs args)
        {
            if (Input.Selected != null)
            {
                Cell cell = ChooseCell(Input.Selected);
                _cursorSelect.Position = GetPosition(cell);
                _cursorHover.Position += HoverOffset;
                _showSelect = true;
            }
            else
            {
                if (Input.Hovered == args.PreviousInput)
                {
                    _cursorHover.Position -= HoverOffset;
                }
                _showSelect = false;
            }

            _animation = null; // end the bounce animation
        }

        /// <summary>
        /// Updates the view when a player attempted an invalid action.
        /// </summary>
        private void OnActionRejected(object input, EventArgs args)
        {
            // vibrate the controller? play a sound?
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
            position += new Vector2(9, 9); // offset in tile
            return position;
        }

        /// <summary>
        /// Returns the color of the given player.
        /// </summary>
        private Color GetPlayerColor(PlayerId player)
        {
            switch (player)
            {
                case PlayerId.A: return new Color(222, 35, 136);
                case PlayerId.B: return new Color(33, 157, 221);
                case PlayerId.C: return new Color(0, 168, 67);
                case PlayerId.D: return new Color(251, 223, 0);
                default: throw new ArgumentException("Invalid player id " + player);
            }
        }

        private InterfaceContext _context;

        private Sprite _cursorHover;
        private Sprite _cursorSelect;
        private bool _showSelect;

        private IAnimation _animation;
        private bool _repeatAnimation;

        private readonly Vector2 HoverOffset = new Vector2(0, -10);
    }
}
