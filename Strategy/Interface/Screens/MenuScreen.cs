using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Helper;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

using Strategy.Properties;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// A menu screen.
    /// </summary>
    public class MenuScreen : Screen
    {
        /// <summary>
        /// If this screen is the root menu of a tree.
        /// </summary>
        public bool IsRoot { get; set; }

        /// <summary>
        /// The position of the first menu item.
        /// </summary>
        public Vector2 BasePosition { get; set; }

        /// <summary>
        /// The number of entries to display on each screen before scrolling.
        /// </summary>
        protected int VisibleEntryCount { get; set; }

        /// <summary>
        /// If the back button should be displayed if this is a root menu screen.
        /// </summary>
        protected bool AllowBackOnRoot { get; set; }

        /// <summary>
        /// The vertical padding, in pixels, between menu entries.
        /// </summary>
        protected float Spacing { get; set; }

        /// <summary>
        /// Creates a new menu screen.
        /// </summary>
        public MenuScreen(Game game)
        {
            _input = game.Services.GetService<MenuInput>();
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            LoadContent(game.Content);

            ShowBeneath = true;
            TransitionOnTime = 0.4f;
            TransitionOffTime = 1.2f;
            VisibleEntryCount = 8;
            Spacing = 20f;
        }

        /// <summary>
        /// Adds a menu entry to this menu.
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        public void AddEntry(MenuEntry entry)
        {
            _entries.Add(entry);
        }

        /// <summary>
        /// Removes a menu entry from this menu.
        /// </summary>
        /// <param name="entry">The entry to remove.</param>
        /// <returns>True if the entry is successfully removed; otherwise, false.</returns>
        public bool RemoveEntry(MenuEntry entry)
        {
            MenuEntry selectedEntry = _entries[_selectedEntryAbs];
            int removalIndex = _entries.IndexOf(entry);
            if (removalIndex >= 0)
            {
                _entries.Remove(entry);

                // move the subsequent entries back
                LayoutEntries();

                if (entry == selectedEntry)
                {
                    // move the focus off the now-defunct entry
                    SetSelected(0);
                }
                else
                {
                    // fix the indices of the selected entry
                    if (_selectedEntryAbs > removalIndex)
                    {
                        _selectedEntryAbs = Math.Max(_selectedEntryAbs - 1, 0);
                        if (removalIndex >= _listWindowBaseIndex && removalIndex < _listWindowBaseIndex + VisibleEntryCount)
                        {
                            _selectedEntryRel = Math.Max(_selectedEntryRel - 1, 0);
                        }
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all the entries from this menu.
        /// </summary>
        public void ClearEntries()
        {
            _entries.Clear();
            _selectedEntryAbs = 0;
            _selectedEntryRel = 0;
            _listWindowBaseIndex = 0;
        }

        /// <summary>
        /// Draws this screen.
        /// </summary>
        public override void Draw()
        {
            Draw(Matrix.Identity);
        }

        /// <summary>
        /// Draws this screen with the given transformation applied.
        /// </summary>
        protected void Draw(Matrix transformation)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, transformation);
            _entries.Where(entry => entry.IsVisible).ForEach(entry => entry.Sprite.Draw(_spriteBatch));
            _selectSprite.Draw(_spriteBatch);
            _backSprite.Draw(_spriteBatch);
            _moveSprite.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        /// <summary>
        /// Focuses the first entry by default.
        /// </summary>
        protected internal override void Show(bool pushed)
        {
            base.Show(pushed);
            _selectSprite.Color = Color.White;
            _backSprite.Color = IsRoot && !AllowBackOnRoot ? Color.Transparent : Color.White;
            _moveSprite.Color = _entries.Count > VisibleEntryCount ? Color.White : Color.Transparent;
            if (pushed)
            {
                foreach (MenuEntry entry in _entries)
                {
                    entry.Sprite.Position = BasePosition;
                    entry.Sprite.Color = Color.Transparent;
                }
                LayoutEntries();

                // focus the first entry
                SetSelected(0);
            }
        }

        /// <summary>
        /// Unfocuses the selected entry.
        /// </summary>
        protected internal override void Hide(bool popped)
        {
            base.Hide(popped);
            _selectSprite.Color = Color.Transparent;
            _backSprite.Color = Color.Transparent;
            _moveSprite.Color = Color.Transparent;
            if (popped)
            {
                _entries[_selectedEntryAbs].OnFocusChanged(false);
                // hide all the entries
                foreach (MenuEntry entry in _entries)
                {
                    entry.TargetPosition = BasePosition;
                    entry.TargetIsVisible = false;
                }
            }
            else
            {
                // reset the focused state of the menu entry so that the
                // focus highlight will be correctly displayed while the
                // parent menu is not being updated
                _entries[_selectedEntryAbs].OnFocusChanged(true);
            }
            _selectSprite.Color = Color.Transparent;
            _backSprite.Color = Color.Transparent;
        }

        /// <summary>
        /// Updates this screen.
        /// </summary>
        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Cancel.Pressed && (!IsRoot || (IsRoot && AllowBackOnRoot)))
            {
                Stack.Pop();
                return;
            }
            else if (_input.Left.Pressed)
            {
                SetSelected(-1);
            }
            else if (_input.Right.Pressed)
            {
                SetSelected(1);
            }
            else if (_input.Action.Pressed && _entries[_selectedEntryAbs].IsSelectable)
            {
                _entries[_selectedEntryAbs].OnSelected();
            }

            float time = gameTime.GetElapsedSeconds();
            _entries.ForEach(entry => entry.Update(time));
        }

        /// <summary>
        /// Animates this menu in.
        /// </summary>
        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            float time = gameTime.GetElapsedSeconds();
            _entries.ForEach(entry => entry.Update(time));
        }

        /// <summary>
        /// Animates this menu out.
        /// </summary>
        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            float time = gameTime.GetElapsedSeconds();
            _entries.ForEach(entry => entry.Update(time));
        }

        /// <summary>
        /// Sets the selected menu item.
        /// </summary>
        /// <param name="deltaIdx">The change in selected index.</param>
        protected virtual void SetSelected(int deltaIdx)
        {
            int selected = _selectedEntryAbs;

            _entries[_selectedEntryAbs].OnFocusChanged(false);

            int nextRelEntry = _selectedEntryRel + deltaIdx;
            int nextAbsEntry = _selectedEntryAbs + deltaIdx;

            if (nextRelEntry >= VisibleEntryCount && nextAbsEntry < _entries.Count)
            {
                _listWindowBaseIndex += 1;
                LayoutEntries();
            }
            else if (nextRelEntry < 0 && nextAbsEntry >= 0)
            {
                _listWindowBaseIndex -= 1;
                LayoutEntries();
            }

            _selectedEntryRel = ExtendedMathHelper.Clamp(nextRelEntry, 0, VisibleEntryCount - 1);
            _selectedEntryAbs = ExtendedMathHelper.Clamp(nextAbsEntry, 0, _entries.Count - 1);

            if (_entries.Count > VisibleEntryCount)
            {
                bool hasNext = (_selectedEntryAbs < _entries.Count - 1);
                bool hasPrev = (_selectedEntryAbs > 0);
                if (hasNext && !hasPrev) { _moveTextSprite.Text = Resources.MenuNext; }
                else if (hasNext && hasPrev) { _moveTextSprite.Text = Resources.MenuNextPrevious; }
                else if (!hasNext && hasPrev) { _moveTextSprite.Text = Resources.MenuPrevious; }
            }

            _selectSprite.Color = _entries[_selectedEntryAbs].IsSelectable ? Color.White : Color.Transparent;
            _selectTextSprite.Text = _entries[_selectedEntryAbs].SelectText;

            _entries[_selectedEntryAbs].OnFocusChanged(true);

            if (selected != _selectedEntryAbs)
            {
                //_soundMove.Play();
            }
        }

        /// <summary>
        /// Sets the target positions of the menu entries.
        /// </summary>
        private void LayoutEntries()
        {
            // anchor the start of the visible entries at the base position
            float previousX = BasePosition.X;
            for (int i = _listWindowBaseIndex; i < _entries.Count; i++)
            {
                Vector2 position = new Vector2(previousX, BasePosition.Y);
                _entries[i].TargetPosition = position;
                _entries[i].TargetIsVisible = IsEntryVisible(i);
                previousX = position.X + _entries[i].Sprite.Size.X + Spacing;
            }
            // then lay out backwards from the base
            previousX = BasePosition.X - Spacing;
            for (int i = _listWindowBaseIndex - 1; i >= 0; i--)
            {
                float width = _entries[i].Sprite.Size.X;
                Vector2 position = new Vector2(previousX - width, BasePosition.Y);
                _entries[i].TargetPosition = position;
                _entries[i].TargetIsVisible = IsEntryVisible(i);
                previousX = position.X - width - Spacing;
            }
        }

        /// <summary>
        /// Checks if the entry with the given index should be visible.
        /// </summary>
        private bool IsEntryVisible(int entryIndex)
        {
            return entryIndex >= _listWindowBaseIndex && entryIndex - _listWindowBaseIndex < VisibleEntryCount;
        }

        /// <summary>
        /// Loads the menu content.
        /// </summary>
        private void LoadContent(ContentManager content)
        {
            SpriteFont font = content.Load<SpriteFont>("Fonts/TextLarge");

            ImageSprite selectImage = new ImageSprite(content.Load<Texture2D>("Images/ButtonA"));
            _selectTextSprite = new TextSprite(font, Resources.MenuSelect);
            _selectTextSprite.Position = new Vector2(
                selectImage.Position.X + selectImage.Size.X + 5,
                selectImage.Position.Y + (selectImage.Size.Y - _selectTextSprite.Size.Y) / 2);
            _selectSprite = new CompositeSprite(selectImage, _selectTextSprite);
            _selectSprite.Position = ControlsBasePosition;

            ImageSprite backImage = new ImageSprite(content.Load<Texture2D>("Images/ButtonB"));
            TextSprite backText = new TextSprite(font, Resources.MenuBack);
            backText.Position = new Vector2(
                backImage.Position.X + backImage.Size.X + 5,
                backImage.Position.Y + (backImage.Size.Y - backText.Size.Y) / 2);
            _backSprite = new CompositeSprite(backImage, backText);
            _backSprite.Position = new Vector2(
                ControlsBasePosition.X,
                _selectSprite.Position.Y + _selectSprite.Size.Y + 10);

            ImageSprite moveImage = new ImageSprite(content.Load<Texture2D>("Images/ButtonThumb"));
            _moveTextSprite = new TextSprite(font, Resources.MenuNext);
            _moveTextSprite.Position = new Vector2(
                moveImage.Position.X + moveImage.Size.X + 5,
                moveImage.Position.Y + (moveImage.Size.Y - _moveTextSprite.Size.Y) / 2);
            _moveSprite = new CompositeSprite(moveImage, _moveTextSprite);
            _moveSprite.Position = ControlsBasePosition;
        }

        private Sprite _selectSprite;
        private TextSprite _selectTextSprite;
        private Sprite _backSprite;
        private Sprite _moveSprite;
        private TextSprite _moveTextSprite;
        protected SpriteBatch _spriteBatch;

        private SoundEffect _soundMove;

        private MenuInput _input;

        protected List<MenuEntry> _entries = new List<MenuEntry>();
        private int _selectedEntryRel; /// relative index inside visible entries
        private int _selectedEntryAbs; /// absolute index inside all entries
        private int _listWindowBaseIndex; /// index of top entry

        protected readonly Vector2 ControlsBasePosition = new Vector2(130f, 565f);
    }

    /// <summary>
    /// An entry in the menu.
    /// </summary>
    public class MenuEntry
    {
        /// <summary>
        /// The sprite used to render this entry.
        /// </summary>
        public Sprite Sprite { get; protected set; }
        
        /// <summary>
        /// The desired position of this entry.
        /// </summary>
        public Vector2 TargetPosition
        {
            set
            {
                _targetPosition = value;
                _positionAnimation = new PositionAnimation(Sprite, _targetPosition, 0.75f, Interpolation.InterpolateVector2(Easing.QuadraticOut));
            }
        }

        /// <summary>
        /// The desired color of this entry in [0, 255].
        /// </summary>
        public Color TargetColor
        {
            set
            {
                _targetColor = value;
                _colorAnimation = new ColorAnimation(Sprite, value, 0.5f, Interpolation.InterpolateColor(Easing.Uniform));
            }
        }

        /// <summary>
        /// The desired visibility of this entry. Invisible entries still contribute to layout.
        /// </summary>
        public bool TargetIsVisible
        {
            set
            {
                _colorAnimation = new ColorAnimation(Sprite, value ? _targetColor : Color.Transparent, 0.5f, Interpolation.InterpolateColor(Easing.Uniform));
            }
        }

        /// <summary>
        /// Returns whether this menu entry is currently visible.
        /// </summary>
        public bool IsVisible
        {
            get { return Sprite.Color.A > 0; }
        }

        /// <summary>
        /// If this entry can be selected.
        /// </summary>
        public bool IsSelectable { get; set; }

        /// <summary>
        /// A verb to describe the action of this menu entry.
        /// </summary>
        public string SelectText { get; set; }

        /// <summary>
        /// Invoked when this menu entry is selected.
        /// </summary>
        public event EventHandler<EventArgs> Selected;

        /// <summary>
        /// Creates a new menu entry.
        /// </summary>
        /// <param name="sprite">The sprite to show.</param>
        public MenuEntry(Sprite sprite)
        {
            Sprite = sprite;
            _targetColor = (Sprite != null) ? Sprite.Color : Color.White;
            _targetPosition = (Sprite != null) ? Sprite.Position : Vector2.Zero;
            IsSelectable = true;
            SelectText = Resources.MenuSelect;
        }

        /// <summary>
        /// Updates this menu entry.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public virtual void Update(float time)
        {
            if (_positionAnimation != null)
            {
                if (!_positionAnimation.Update(time))
                {
                    _positionAnimation = null;
                }
            }
            if (_colorAnimation != null)
            {
                if (!_colorAnimation.Update(time))
                {
                    _colorAnimation = null;
                }
            }
        }

        /// <summary>
        /// Notifies this menu entry that is has been selected.
        /// </summary>
        public virtual void OnSelected()
        {
            if (IsSelectable && Selected != null)
            {
                Selected(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Notifies this menu entry that its focused state changed.
        /// </summary>
        /// <param name="focused">True if the entry is focused; otherwise, false.</param>
        public virtual void OnFocusChanged(bool focused)
        {
        }

        private Vector2 _targetPosition;
        private Color _targetColor;

        private IAnimation _positionAnimation;
        private IAnimation _colorAnimation;
    }

    /// <summary>
    /// A plain text menu entry. 
    /// </summary>
    public class TextMenuEntry : MenuEntry
    {
        public TextSprite LabelSprite { get; protected set; }
        public TextSprite TextSprite { get; protected set; }

        /// <summary>
        /// Creates a new text menu entry.
        /// </summary>
        /// <param name="text">The text sprite to show.</param>
        public TextMenuEntry(TextSprite text) : base(text)
        {
            TextSprite = text;
        }

        /// <summary>
        /// Creates a new text menu entry.
        /// </summary>
        /// <param name="label">The unhighlighted label to show.</param>
        /// <param name="text">The text to show.</param>
        public TextMenuEntry(TextSprite label, TextSprite text) : base(null)
        {
            LabelSprite = label;
            TextSprite = text;

            CompositeSprite composite = new CompositeSprite(LabelSprite, TextSprite);
            LabelSprite.Position = Vector2.Zero;
            TextSprite.Position = new Vector2(LabelSprite.Size.X + LabelSprite.Font.MeasureString(" ").X, 0);
            Sprite = composite;
        }

        /// <summary>
        /// Updates the pulsing outline.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public override void Update(float time)
        {
            if (IsSelectable)
            {
                _fadeElapsed += time;
                if (_fadeElapsed >= FadeDuration)
                {
                    _fadeElapsed = 0;
                    _fadeIn = !_fadeIn;
                }
                float p = _fadeElapsed / FadeDuration;
                float a = (_fadeIn) ? p : 1 - p;
                TextSprite.OutlineColor = ColorExtensions.FromPremultiplied(OutlineColor, a);
            }
            base.Update(time);
        }

        /// <summary>
        /// Sets the outline state.
        /// </summary>
        public override void OnFocusChanged(bool focused)
        {
            if (focused)
            {
                TextSprite.OutlineColor = IsSelectable ? OutlineColor : Color.Transparent;
                TextSprite.OutlineWidth = 2;
                _fadeIn = false;
                _fadeElapsed = 0;
            }
            else
            {
                TextSprite.OutlineWidth = 0;
            }
            base.OnFocusChanged(focused);
        }

        private bool _fadeIn;
        private float _fadeElapsed;

        private readonly Color OutlineColor = new Color(207, 115, 115);
        private const float FadeDuration = 0.6f;
    }

    /// <summary>
    /// A text menu entry with more than one value.
    /// </summary>
    public class CyclingTextMenuEntry : TextMenuEntry
    {
        public CyclingTextMenuEntry(TextSprite label, TextSprite text, string[] entries, int initialEntry) : base(label, text)
        {
            _entries = entries;
            _entryIndex = initialEntry;
            _textColor = TextSprite.Color;
            TextSprite.Text = _entries[_entryIndex];
            SelectText = Resources.MenuCycle;
        }

        public bool SetEntry(string entry)
        {
            int newIndex = Array.IndexOf(_entries, entry);
            if (newIndex < 0 || _entryIndex == newIndex)
            {
                return false;
            }
            _entryIndex = newIndex;
            string newText = _entries[_entryIndex];
            _entryChangeAnimation = new SequentialAnimation(
                new ColorAnimation(TextSprite, Color.Transparent, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)),
                new TextAnimation(TextSprite, newText),
                new ColorAnimation(TextSprite, _textColor, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)));
            return true;
        }

        public override void Update(float time)
        {
            if (_entryChangeAnimation != null)
            {
                if (!_entryChangeAnimation.Update(time))
                {
                    _entryChangeAnimation = null;
                }
            }
            base.Update(time);
        }

        public override void OnSelected()
        {
            _entryIndex = (_entryIndex + 1) % _entries.Length;
            TextSprite.Text = _entries[_entryIndex];
            base.OnSelected();
        }

        private string[] _entries;
        private int _entryIndex;

        private IAnimation _entryChangeAnimation;
        private Color _textColor;
    }
}
