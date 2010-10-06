﻿using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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
            VisibleEntryCount = 5;
            Spacing = 10f;
        }

        /// <summary>
        /// Adds a menu entry to this menu.
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        public void AddEntry(MenuEntry entry)
        {
            _entries.Add(entry);
            if (_visibleEntries.Count < VisibleEntryCount)
            {
                ShowEntry(entry, _visibleEntries.Count);
            }
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
                bool visRemoved = _visibleEntries.Remove(entry);
                if (visRemoved)
                {
                    _entriesSprite.Remove(entry.Sprite);
                }
                if (entry == selectedEntry)
                {
                    SetSelected(0); // move the focus off the now-defunct entry
                }
                else
                {
                    _selectedEntryAbs = MathHelperExtensions.Clamp(_selectedEntryAbs - 1, 0, _entries.Count); // fix the index
                    if (visRemoved)
                    {
                        _selectedEntryRel = MathHelperExtensions.Clamp(_selectedEntryRel - 1, 0, _visibleEntries.Count);
                    }
                }
            }
            return removed;
        }

        /// <summary>
        /// Removes all the entries from this menu.
        /// </summary>
        public void ClearEntries()
        {
            _entries.ForEach(entry => _entriesSprite.Remove(entry.Sprite));
            _entries.Clear();
            _visibleEntries.Clear();
            _selectedEntryAbs = 0;
            _selectedEntryRel = 0;
            _listWindowBaseIndex = 0;
        }

        /// <summary>
        /// Centers the menu entry sprites vertically and horizontally.
        /// </summary>
        public void LayoutEntries()
        {
            Vector2 menuSize = new Vector2(0, 0); //XXX

            float height = 0f;
            foreach (MenuEntry entry in _visibleEntries)
            {
                height += entry.Sprite.Size.Y + Spacing;
            }
            height -= Spacing; // remove the extra padding at the bottom

            float y = (menuSize.Y - height) / 2f;
            foreach (MenuEntry entry in _visibleEntries)
            {
                float x = (menuSize.X - entry.Sprite.Size.X) / 2f;
                entry.Sprite.Position = new Vector2((int)x, (int)y);
                y += entry.Sprite.Size.Y + Spacing;
            }
        }

        /// <summary>
        /// Adds the given sprite as a decorative entry.
        /// </summary>
        /// <param name="sprite">The sprite to add.</param>
        public void AddDecoration(Sprite sprite)
        {
            _entriesSprite.Add(sprite);
        }

        /// <summary>
        /// Removes the given sprite as a decorative entry.
        /// </summary>
        /// <param name="sprite">The sprite to remove.</param>
        public void RemoveDecoration(Sprite sprite)
        {
            _entriesSprite.Remove(sprite);
        }

        /// <summary>
        /// Draws this screen.
        /// </summary>
        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);
            _entriesSprite.Draw(_spriteBatch);
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
                // reset the list
                _entries[_selectedEntryAbs].OnFocusChanged(false);
                _visibleEntries.ToArray().ForEach(e => HideEntry(e));
                _visibleEntries.Clear();
                for (int i = 0; i < Math.Min(_entries.Count, VisibleEntryCount); i++)
                {
                    ShowEntry(_entries[i], _visibleEntries.Count);
                }
                LayoutEntries();
                _selectedEntryRel = _selectedEntryAbs = _listWindowBaseIndex = 0;
                SetSelected(0);
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

            _entries[_selectedEntryAbs].Update(gameTime.GetElapsedSeconds());
        }

        /// <summary>
        /// Fades this menu in.
        /// </summary>
        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            // let the old menu fade out first
            if (progress < 0.5)
            {
                return;
            }
            progress = (progress - 0.5f) * 2f;
            // then fade in this one
            //_entriesSprite.Color = new Color(Color.White, progress);
            if (IsRoot && pushed)
            {
                //XXX _screenDescriptor.GetSprite("Background").Color = new Color(Color.White, progress);
            }
        }

        /// <summary>
        /// Fades this menu out.
        /// </summary>
        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            //_entriesSprite.Color = new Color(Color.White, 1 - progress);
            if (IsRoot && popped)
            {
                // fade out the background when the last menu screen is popped off
                //XXX _screenDescriptor.GetSprite("Background").Color = new Color(Color.White, 1 - progress);
            }
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

            if (nextRelEntry >= _visibleEntries.Count && nextAbsEntry < _entries.Count)
            {
                HideEntry(_visibleEntries[0]);
                ShowEntry(_entries[nextAbsEntry], _visibleEntries.Count);
                LayoutEntries();
                _listWindowBaseIndex += 1;
            }
            else if (nextRelEntry < 0 && nextAbsEntry >= 0)
            {
                HideEntry(_visibleEntries[_visibleEntries.Count - 1]);
                ShowEntry(_entries[nextAbsEntry], 0);
                LayoutEntries();
                _listWindowBaseIndex -= 1;
            }

            _selectedEntryRel = MathHelperExtensions.Clamp(nextRelEntry, 0, _visibleEntries.Count - 1);
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
                _soundMove.Play();
            }
        }

        /// <summary>
        /// Shows the specified menu entry at a specific position.
        /// </summary>
        private void ShowEntry(MenuEntry entry, int position)
        {
            _visibleEntries.Insert(position, entry);
            _entriesSprite.Add(entry.Sprite);
        }

        /// <summary>
        /// Hides the specified menu entry.
        /// </summary>
        private void HideEntry(MenuEntry entry)
        {
            _visibleEntries.Remove(entry);
            _entriesSprite.Remove(entry.Sprite);
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

            _entriesSprite = new CompositeSprite();
        }

        private CompositeSprite _entriesSprite;
        private Sprite _selectSprite;
        private TextSprite _selectTextSprite;
        private Sprite _backSprite;
        private SpriteBatch _spriteBatch;

        private SoundEffect _soundMove;

        private MenuInput _input;

        protected List<MenuEntry> _entries = new List<MenuEntry>();
        private List<MenuEntry> _visibleEntries = new List<MenuEntry>();
        private int _selectedEntryRel; /// relative index inside visible entries
        private int _selectedEntryAbs; /// absolute index inside all entries
        private int _listWindowBaseIndex; /// index of top entry

        private readonly Vector2 ControlsBasePosition = new Vector2(50f, 650f);
    }

    /// <summary>
    /// An entry in the menu.
    /// </summary>
    public class MenuEntry
    {
        /// <summary>
        /// The sprite used to display this menu entry.
        /// </summary>
        public Sprite Sprite { get; protected set; }

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
        /// Updates this menu entry when it is focused.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public virtual void Update(float time)
        {
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
