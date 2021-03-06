﻿using System;

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
    /// Lets the player select between creating and joining a multiplayer match.
    /// </summary>
    public class MultiplayerSelectionScreen : MenuScreen
    {
        public MultiplayerSelectionScreen(Game game) : base(game)
        {
            _input = game.Services.GetService<MenuInput>();

            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuCreateMatch, OnCreateSelected)
                .CreateButtonEntry(Resources.MenuJoinMatch, OnJoinSelected);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(150f, 120f);
        }

        private void OnCreateSelected(object sender, EventArgs args)
        {
            InitializeSession(true);
        }

        private void OnJoinSelected(object sender, EventArgs args)
        {
            InitializeSession(false);
        }

        private void InitializeSession(bool createSession)
        {
            SignedInGamer gamer = _input.Controller.Value.GetSignedInGamer();

            NetworkSessionType sessionType = NetworkSessionType.PlayerMatch;
#if DEBUG
            // for debug builds only use system link
            sessionType = NetworkSessionType.SystemLink;
#endif

            IAsyncResult result;
            if (createSession)
            {
                result = NetworkSessionProvider.BeginCreate(sessionType, gamer, null, true);
            }
            else
            {
                result = NetworkSessionProvider.BeginFindAndJoin(sessionType, gamer, null, false);
            }

            AsyncBusyScreen busyScreen = new AsyncBusyScreen(Stack.Game, result);
            busyScreen.OperationCompleted += OnSessionProvided;
            Stack.Push(busyScreen);
        }

        private void OnSessionProvided(object sender, AsyncOperationCompletedEventArgs args)
        {
            NetworkSession session = null;
            Boolean isCreating = (Boolean)args.AsyncResult.AsyncState;
            if (isCreating)
            {
                session = NetworkSessionProvider.EndCreate(args.AsyncResult);
            }
            else
            {
                session = NetworkSessionProvider.EndFindAndJoin(args.AsyncResult);
            }
            if (session != null)
            {
                LobbyScreen lobbyScreen = new LobbyScreen(Stack.Game, new StrategyNetworkSession(session));
                Stack.Push(lobbyScreen);
            }
            else
            {
                MessageScreen messageScreen = new MessageScreen(
                    Stack.Game,
                    isCreating ? Resources.NetworkErrorCreate : Resources.NetworkErrorJoin,
                    typeof(MultiplayerSelectionScreen),
                    false);
                Stack.Push(messageScreen);
            }
        }

        private MenuInput _input;
    }
}
