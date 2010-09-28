﻿using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Net;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Displays the main menu.
    /// </summary>
    public class MainMenuScreen : MenuScreen
    {
        public MainMenuScreen(StrategyGame game) : base(game)
        {
            _input = game.Services.GetService<MenuInput>();

            AddEntry(new TextMenuEntry(new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextSmall"), "Test Item")));

            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
            IsRoot = true;
            AllowBackOnRoot = true;
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Buy.Released)
            {
                SignedInGamer gamer = _input.Controller.Value.GetSignedInGamer();
                IAsyncResult result =
#if WINDOWS
                    //NetworkSessionProvider.BeginFindAndJoin(NetworkSessionType.SystemLink, gamer, OnSessionProvided, false);
                    NetworkSessionProvider.BeginCreate(NetworkSessionType.Local, gamer, OnSessionProvided, false);
#else
                NetworkSessionProvider.BeginCreate(NetworkSessionType.SystemLink, gamer, OnSessionProvided, true);
#endif
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                Stack.Push(busyScreen);
            }

            base.UpdateActive(gameTime);
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
