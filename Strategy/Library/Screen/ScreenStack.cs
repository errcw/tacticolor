﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Extensions;
using Strategy.Library.Input;

namespace Strategy.Library.Screen
{
    /// <summary>
    /// A stack of screens. The stack is drawn from bottom to top. Only the top screen is active and updated.
    /// </summary>
    public class ScreenStack : DrawableGameComponent
    {
        /// <summary>
        /// The screen at the top of the stack.
        /// </summary>
        public Screen ActiveScreen
        {
            get
            {
                return (_stackScreens.Count > 0) ? _stackScreens[_stackScreens.Count - 1] : null;
            }
        }

        /// <summary>
        /// Creates a new stack of screens.
        /// </summary>
        /// <param name="game">The game context.</param>
        public ScreenStack(Game game) : base(game)
        {
        }

        /// <summary>
        /// Updates the active screen.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            if (Game.IsActive)
            {
                _stackScreens.ForEach(screen => screen.Update(gameTime));
                _poppedScreens.ForEach(screen => screen.Update(gameTime));
                _poppedScreens = _poppedScreens.Where(screen => screen.State != ScreenState.Inactive).ToList();
                _overlayScreens.ForEach(screen => screen.Update(gameTime));
            }
            else
            {
                _stackScreens.ForEach(screen => screen.UpdateForceInactive(gameTime));
            }
        }

        /// <summary>
        /// Draws the visible screens in the stack.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>
        public override void Draw(GameTime gameTime)
        {
            if (_stackScreens.Count > 0)
            {
                // find the bottom opaque screen
                int bottom = _stackScreens.Count - 1;
                while (bottom > 0 && _stackScreens[bottom].ShowBeneath)
                {
                    bottom--;
                }

                // draw from the bottom up
                for (; bottom < _stackScreens.Count; bottom++)
                {
                    _stackScreens[bottom].Draw();
                }
            }

            // draw the screens transitioning off
            _poppedScreens.ForEach(s => s.Draw());

            // draw the overlays on top of the stack
            _overlayScreens.ForEach(s => s.Draw());
        }

        /// <summary>
        /// Adds a screen on the top of the stack.
        /// </summary>
        /// <param name="screen">The screen to push.</param>
        public void Push(Screen screen)
        {
            PushOn(screen, ActiveScreen);
        }

        /// <summary>
        /// Adds a screen on top of a specified screen. If the screen to push on
        /// is not found then the screen is inserted at the bottom of the stack.
        /// </summary>
        /// <param name="screen">The screen to push.</param>
        /// <param name="pushOn">The screen to push on.</param>
        public void PushOn(Screen screen, Screen pushOn)
        {
            int pushLocation = _stackScreens.IndexOf(pushOn);
            _stackScreens.Insert(pushLocation + 1, screen);
            screen.Stack = this;
            screen.Show(true);
            if (pushOn != null)
            {
                pushOn.Hide(false);
            }
        }

        /// <summary>
        /// Removes the screen at the top of the stack.
        /// </summary>
        /// <returns>The popped screen; null if the stack is empty.</returns>
        public Screen Pop()
        {
            // remove and hide the current active screen
            Screen active = ActiveScreen;
            if (active != null)
            {
                active.Hide(true);
                _stackScreens.RemoveAt(_stackScreens.Count - 1);
                _poppedScreens.Insert(0, active);
            }

            // show the new active screen
            active = ActiveScreen;
            if (active != null)
            {
                active.Show(false);
            }
            return active;
        }

        /// <summary>
        /// Pops all the screens from the stack.
        /// </summary>
        public void PopAll()
        {
            _stackScreens.ForEach(screen => screen.Hide(true));
            _poppedScreens.AddRange(_stackScreens);
            _stackScreens.Clear();
        }

        /// <summary>
        /// Adds an overlay screen to be drawn on top of the stack.
        /// </summary>
        /// <param name="overlay">The screen to add.</param>
        public void AddOverlay(Screen overlay)
        {
            overlay.Stack = this;
            overlay.Show(true);
            _overlayScreens.Add(overlay);
        }

        /// <summary>
        /// Removes an overlay screen from the set of overlays.
        /// </summary>
        /// <param name="overlay">The screen to remove.</param>
        /// <returns>True if the screen was successfully removed; otherwise, false.</returns>
        public bool RemoveOverlay(Screen overlay)
        {
            bool removed = _overlayScreens.Remove(overlay);
            if (removed)
            {
                overlay.Hide(true);
                _poppedScreens.Add(overlay);
            }
            return removed;
        }

        private List<Screen> _stackScreens = new List<Screen>();
        private List<Screen> _poppedScreens = new List<Screen>();
        private List<Screen> _overlayScreens = new List<Screen>();
    }
}
