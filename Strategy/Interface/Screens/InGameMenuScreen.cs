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
        public InGameMenuScreen(Game game) : base(game)
        {
            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuExitNo, OnReturnSelected)
                .CreateButtonEntry(Resources.MenuExitYes, OnExitSelected);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(150f, 120f);
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
    }
}
