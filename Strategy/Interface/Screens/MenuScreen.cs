using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

using Strategy.Properties;

/* Notes:
 * - full list of menu items
 * - always draw the entire list, though some may have A=0
 * - tag each item with the desired X and Y position
 *   - at each update step animate items towards the desired positions
 *   - start all at BaseX,BaseY and transparent
 *     - use selected item as base (how to pass position?)
 * - store index of the start of the currently visible items
 * - moving up/down changes the base visible index
 *   - fade out old and fade in new
 */
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
        public MenuScreen(StrategyGame game)
        {
            _input = game.Services.GetService<MenuInput>();
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            LoadContent(game.Content);

            ShowBeneath = true;
            TransitionOnTime = 0.4f;
            TransitionOffTime = 0.2f;
            VisibleEntryCount = 8;
            Spacing = 35f;
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
            bool removed = _entries.Remove(entry);
            if (removed)
            {
                //XXX
            }
            return removed;
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
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);
            _entries.ForEach(entry => entry.Sprite.Draw(_spriteBatch));
            _selectSprite.Draw(_spriteBatch);
            _backSprite.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        /// <summary>
        /// Focuses the first entry by default.
        /// </summary>
        protected internal override void Show(bool pushed)
        {
            base.Show(pushed);
            _backSprite.Color = IsRoot && !AllowBackOnRoot ? Color.TransparentWhite : Color.White;
            _selectSprite.Color = Color.White;
            //_screenDescriptor.GetSprite("ArrowUp").Color = Color.TransparentWhite;
            //_screenDescriptor.GetSprite("ArrowDown").Color = Color.TransparentWhite;
            if (pushed)
            {
                // hide all the entries
                foreach (MenuEntry entry in _entries)
                {
                    entry.Sprite.Position = BasePosition;
                    entry.Sprite.Color = Color.TransparentWhite;
                }
                // show the visible entries
                for (int i = 0; i < _entries.Count && i < VisibleEntryCount; i++)
                {
                    Vector2 position = BasePosition;
                    if (i > 0)
                    {
                        float x = _entries[i - 1].TargetPosition.X + _entries[i - 1].Sprite.Size.X + 20f;
                        position = new Vector2(x, BasePosition.Y);
                    }
                    //_entries[i].TargetPosition = BasePosition + i * new Vector2(0, Spacing);
                    _entries[i].TargetPosition = position;
                    _entries[i].TargetColor = Color.White;
                }
                // focus the first entry
                _entries[0].OnFocusChanged(true);
            }
        }

        /// <summary>
        /// Unfocuses the selected entry.
        /// </summary>
        protected internal override void Hide(bool popped)
        {
            base.Hide(popped);
            if (popped)
            {
                _entries[_selectedEntryRel].OnFocusChanged(false);
            }
            _selectSprite.Color = Color.TransparentWhite;
            _backSprite.Color = Color.TransparentWhite;
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
            else if (_input.Up.Pressed)
            {
                SetSelected(-1);
            }
            else if (_input.Down.Pressed)
            {
                SetSelected(1);
            }
            else if (_input.Action.Pressed && _entries[_selectedEntryAbs].IsSelectable)
            {
                _entries[_selectedEntryAbs].OnSelected();
            }

            float time = gameTime.GetElapsedSeconds();
            foreach (MenuEntry entry in _entries)
            {
                entry.Update(time);
            }
        }

        /// <summary>
        /// Animates this menu in.
        /// </summary>
        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
        }

        /// <summary>
        /// Animates this menu out.
        /// </summary>
        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
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
                CycleDown();
                _listWindowBaseIndex += 1;
            }
            else if (nextRelEntry < 0 && nextAbsEntry >= 0)
            {
                CycleUp();
                _listWindowBaseIndex -= 1;
            }

            _selectedEntryRel = MathHelperExtensions.Clamp(nextRelEntry, 0, VisibleEntryCount - 1);
            _selectedEntryAbs = MathHelperExtensions.Clamp(nextAbsEntry, 0, _entries.Count - 1);

            if (_entries.Count > VisibleEntryCount)
            {
                //XXX
                //_screenDescriptor.GetSprite("ArrowUp").Color =
                    //(_listWindowBaseIndex == 0) ? Color.TransparentWhite : Color.White;
                //_screenDescriptor.GetSprite("ArrowDown").Color =
                    //(_listWindowBaseIndex == _entries.Count - NumVisibleEntries) ? Color.TransparentWhite : Color.White;
            }

            _selectSprite.Color = _entries[_selectedEntryAbs].IsSelectable ? Color.White : Color.TransparentWhite;
            _selectTextSprite.Text = _entries[_selectedEntryAbs].SelectText;

            _entries[_selectedEntryAbs].OnFocusChanged(true);

            if (selected != _selectedEntryAbs)
            {
                //_soundMove.Play();
            }
        }

        /// <summary>
        /// Removes the top menu entry and adds an entry at the bottom.
        /// </summary>
        private void CycleDown()
        {
        }

        /// <summary>
        /// Removes the bottom menu entry and adds an entry at the top.
        /// </summary>
        private void CycleUp()
        {
        }

        /// <summary>
        /// Loads the menu content.
        /// </summary>
        private void LoadContent(ContentManager content)
        {
            SpriteFont font = content.Load<SpriteFont>("Fonts/TextLarge");

            ImageSprite selectImage = new ImageSprite(content.Load<Texture2D>("Images/ButtonA"));
            selectImage.Position = ControlsBasePosition;
            _selectTextSprite = new TextSprite(font, Resources.MenuSelect);
            _selectTextSprite.Position = new Vector2(
                selectImage.Position.X + selectImage.Size.X + 5,
                selectImage.Position.Y + (selectImage.Size.Y - _selectTextSprite.Size.Y) / 2);
            _selectSprite = new CompositeSprite(selectImage, _selectTextSprite);

            ImageSprite backImage = new ImageSprite(content.Load<Texture2D>("Images/ButtonB"));
            backImage.Position = new Vector2(
                _selectTextSprite.Position.X + _selectTextSprite.Size.X + 20,
                ControlsBasePosition.Y);
            TextSprite backText = new TextSprite(font, Resources.MenuBack);
            backText.Position = new Vector2(
                backImage.Position.X + backImage.Size.X + 5,
                backImage.Position.Y + (backImage.Size.Y - backText.Size.Y) / 2);
            _backSprite = new CompositeSprite(backImage, backText);
        }

        private Sprite _selectSprite;
        private TextSprite _selectTextSprite;
        private Sprite _backSprite;
        private SpriteBatch _spriteBatch;

        private SoundEffect _soundMove;

        private MenuInput _input;

        protected List<MenuEntry> _entries = new List<MenuEntry>();
        private int _selectedEntryRel; /// relative index inside visible entries
        private int _selectedEntryAbs; /// absolute index inside all entries
        private int _listWindowBaseIndex; /// index of top entry

        private readonly Vector2 ControlsBasePosition = new Vector2(130f, 610f);
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
        public Vector2 TargetPosition { get; set; }

        /// <summary>
        /// The desired color of this entry in [0, 255].
        /// </summary>
        public Color TargetColor { get; set; }

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
            IsSelectable = true;
            SelectText = Resources.MenuSelect;
        }

        /// <summary>
        /// Updates this menu entry.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public virtual void Update(float time)
        {
            Sprite.Position = Interpolation.InterpolateVector2(Easing.Uniform)(Sprite.Position, TargetPosition, 4f * time);
            Sprite.Color = Interpolation.InterpolateColor(Easing.Uniform)(Sprite.Color, TargetColor, 4f * time);
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
            if (!IsSelectable)
            {
                return;
            }
            _fadeElapsed += time;
            if (_fadeElapsed >= FadeDuration)
            {
                _fadeElapsed = 0;
                _fadeIn = !_fadeIn;
            }
            float p = _fadeElapsed / FadeDuration;
            float a = (_fadeIn) ? p : 1 - p;
            TextSprite.OutlineColor = new Color(OutlineColor, a);

            base.Update(time);
        }

        /// <summary>
        /// Sets the outline state.
        /// </summary>
        public override void OnFocusChanged(bool focused)
        {
            if (!IsSelectable)
            {
                return;
            }
            if (focused)
            {
                TextSprite.OutlineColor = OutlineColor;
                TextSprite.OutlineWidth = 2;
                _fadeIn = false;
                _fadeElapsed = 0;
            }
            else
            {
                TextSprite.OutlineWidth = 0;
            }
        }

        private bool _fadeIn;
        private float _fadeElapsed;

        private readonly Color OutlineColor = new Color(207, 115, 115);
        private const float FadeDuration = 0.6f;
    }
}
