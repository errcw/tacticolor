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
            if (_input.Controller.HasValue && _input.Controller.Value.IsSignedIn() && _creator == null)
            {
                _creator = new SessionCreator(Stack);
                _creator.SesssionCreated += OnSessionCreated;
                _creator.CreateSession(NetworkSessionType.Local, _input.Controller.Value.GetSignedInGamer());
            }
        }

        private void OnSessionCreated(object creatorObj, EventArgs args)
        {
            if (_creator.Session != null)
            {
                LobbyScreen lobbyScreen = new LobbyScreen((StrategyGame)Stack.Game, _creator.Session);
                Stack.Push(lobbyScreen);
            }
        }

        private MenuInput _input;
        private SessionCreator _creator;
    }
}
