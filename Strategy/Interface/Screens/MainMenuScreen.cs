using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Net;
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
                SignedInGamer gamer = _input.Controller.Value.GetSignedInGamer();
                IAsyncResult result =
#if WINDOWS
                    NetworkSessionProvider.BeginCreate(NetworkSessionType.Local, gamer, OnSessionProvided, false);
#else
                    NetworkSessionProvider.BeginCreate(NetworkSessionType.SystemLink, gamer, OnSessionProvided, true);
#endif
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                Stack.Push(busyScreen);
            }
        }

        private void OnSessionProvided(IAsyncResult result)
        {
            NetworkSession session = null;
            Boolean isCreating = (Boolean)result.AsyncState;
            if (isCreating)
            {
                session = NetworkSessionProvider.EndCreate(result);
            }
            else
            {
                session = NetworkSessionProvider.EndFindAndJoin(result);
            }
            if (session != null)
            {
                LobbyScreen lobbyScreen = new LobbyScreen((StrategyGame)Stack.Game, session);
                Stack.Push(lobbyScreen);
            }
            else
            {
                // show error screen
            }
        }

        private MenuInput _input;
    }
}
