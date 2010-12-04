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
    /// Allows the player to confirm leaving a match.
    /// </summary>
    public class LeaveMatchConfirmationScreen : MenuScreen
    {
        public LeaveMatchConfirmationScreen(Game game) : base(game)
        {
            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuLeaveNo, OnReturnSelected)
                .CreateButtonEntry(Resources.MenuLeaveYes, OnLeaveSelected);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(150f, 120f);
        }

        private void OnReturnSelected(object sender, EventArgs args)
        {
            Stack.Pop();
        }

        private void OnLeaveSelected(object sender, EventArgs args)
        {
            while (!(Stack.ActiveScreen is MainMenuScreen))
            {
                Stack.Pop();
            }
        }
    }
}
