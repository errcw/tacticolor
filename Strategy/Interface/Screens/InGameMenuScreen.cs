using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Allows the player to confirm exiting the game.
    /// </summary>
    public class InGameMenuScreen : MenuScreen
    {
        public InGameMenuScreen(Game game, PlayerIndex controller) : base(game)
        {
            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuExitNo, OnReturnSelected)
                .CreateButtonEntry(Resources.MenuExitYes, OnExitSelected);

            // transfer control to the player opening the menu
            _input = game.Services.GetService<MenuInput>();
            _previousController = _input.Controller.Value;
            _input.Controller = controller;

            TransitionOnTime = 0.02f;
            BasePosition = new Vector2(150f, 120f);
        }

        protected internal override void Hide(bool popped)
        {
            if (popped)
            {
                // restore control to the menu owner
                _input.Controller = _previousController;
            }
            base.Hide(popped);
        }

        private void OnReturnSelected(object sender, EventArgs args)
        {
            Stack.Pop();
        }

        private void OnExitSelected(object sender, EventArgs args)
        {
            Stack.Pop(); // pop this
            Stack.Pop(); // pop the gameplay screen
            Stack.Pop(); // pop the lobby screen
        }

        private MenuInput _input;
        private PlayerIndex _previousController;
    }
}
