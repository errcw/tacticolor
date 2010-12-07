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
    /// Lets the player select a help & options submenu.
    /// </summary>
    public class HelpOptionsScreen : MenuScreen
    {
        public HelpOptionsScreen(Game game) : base(game)
        {
            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuHowToPlay, OnHowToPlaySelected)
                .CreateButtonEntry(Resources.MenuControls, OnControlsSelected)
                .CreateButtonEntry(Resources.MenuOptions, OnOptionsSelected)
                .CreateButtonEntry(Resources.MenuCredits, OnCreditsSelected);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(150f, 120f);
        }

        private void OnHowToPlaySelected(object sender, EventArgs args)
        {
            HowToPlayScreen howToPlayScreen = new HowToPlayScreen(Stack.Game);
            Stack.Push(howToPlayScreen);
        }

        private void OnControlsSelected(object sender, EventArgs args)
        {
            ControlsScreen controlsScreen = new ControlsScreen(Stack.Game);
            Stack.Push(controlsScreen);
        }

        private void OnOptionsSelected(object sender, EventArgs args)
        {
            OptionsScreen optionsScreen = new OptionsScreen(Stack.Game);
            Stack.Push(optionsScreen);
        }

        private void OnCreditsSelected(object sender, EventArgs args)
        {
            CreditsScreen creditsScreen = new CreditsScreen(Stack.Game);
            Stack.Push(creditsScreen);
        }
    }
}
