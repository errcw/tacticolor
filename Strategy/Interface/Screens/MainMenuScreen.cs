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
    /// Displays the main menu.
    /// </summary>
    public class MainMenuScreen : MenuScreen
    {
        MenuEntry purchaseEntry;
        public MainMenuScreen(StrategyGame game) : base(game)
        {
            _input = game.Services.GetService<MenuInput>();


            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuLocalGame, OnLocalGameSelected)
                .CreateButtonEntry(Resources.MenuMultiplayerGame, OnMultiplayerGameSelected)
                .CreateButtonEntry(Resources.MenuPurchase, OnPurchaseSelected, out purchaseEntry)
                .CreateButtonEntry(Resources.MenuAwardments, OnAwardmentsSelected)
                .CreateButtonEntry(Resources.MenuHelpOptions, OnHelpOptionsSelected)
                .CreateButtonEntry(Resources.MenuExit, OnExitSelected);

            TrialModeObserverComponent trialObserver = game.Services.GetService<TrialModeObserverComponent>();
            trialObserver.TrialModeEnded += (s, a) => RemoveEntry(purchaseEntry);

            TransitionOnTime = 0f;
            IsRoot = true;
            AllowBackOnRoot = true;
            BasePosition = new Vector2(130f, 80f);
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Buy.Released)
            {
                SignedInGamer gamer = _input.Controller.Value.GetSignedInGamer();
                IAsyncResult result = NetworkSessionProvider.BeginCreate(NetworkSessionType.Local, gamer, null, true);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(Stack.Game, result);
                busyScreen.OperationCompleted += OnSessionProvided;
                Stack.Push(busyScreen);
            }

            base.UpdateActive(gameTime);
        }

        private void OnLocalGameSelected(object sender, EventArgs args)
        {
        }

        private void OnMultiplayerGameSelected(object sender, EventArgs args)
        {
        }

        private void OnPurchaseSelected(object sender, EventArgs args)
        {
            _input.Controller.Value.PurchaseContent();
        }

        private void OnAwardmentsSelected(object sender, EventArgs args)
        {
        }

        private void OnHelpOptionsSelected(object sender, EventArgs args)
        {
        }

        private void OnExitSelected(object sender, EventArgs args)
        {
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
                LobbyScreen lobbyScreen = new LobbyScreen(Stack.Game, session);
                Stack.Push(lobbyScreen);
            }
            else
            {
                MessageScreen messageScreen = new MessageScreen(Stack.Game, isCreating ? Resources.NetworkErrorCreate : Resources.NetworkErrorJoin);
                Stack.Push(messageScreen);
            }
        }

        private MenuInput _input;
    }
}
