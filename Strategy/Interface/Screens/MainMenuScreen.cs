using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;

using Strategy.Library.Extensions;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Displays the main menu.
    /// </summary>
    public class MainMenuScreen : Screen
    {
        public MainMenuScreen(StrategyGame game)
        {
            _input = game.Services.GetService<MenuInput>();

            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
        }

        public override void Draw()
        {
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (!_input.Controller.Value.IsSignedIn())
            {
                Guide.ShowSignIn(4, false);
            }
            if (_input.Controller.HasValue && _input.Controller.Value.IsSignedIn())
            {
                LobbyScreen lobbyScreen = new LobbyScreen((StrategyGame)Stack.Game, true);
                Stack.Push(lobbyScreen);
            }
        }

        private MenuInput _input;
    }
}
