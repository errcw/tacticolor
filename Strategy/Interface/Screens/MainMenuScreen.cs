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
        public MainMenuScreen(StrategyGame game) : base(game)
        {
            _input = game.Services.GetService<MenuInput>();

            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuLocalGame, OnLocalGameSelected)
                .CreateButtonEntry(Resources.MenuMultiplayerGame, OnMultiplayerGameSelected)
                .CreatePurchaseButtonEntry(Resources.MenuPurchase)
                .CreateButtonEntry(Resources.MenuAwardments, OnAwardmentsSelected)
                .CreateButtonEntry(Resources.MenuHelpOptions, OnHelpOptionsSelected)
                .CreateButtonEntry(Resources.MenuExit, OnExitSelected);

            TransitionOnTime = 0.01f;
            IsRoot = true;
            AllowBackOnRoot = true;
            BasePosition = new Vector2(130f, 80f);
        }

        private void OnLocalGameSelected(object sender, EventArgs args)
        {
            SignedInGamer gamer = _input.Controller.Value.GetSignedInGamer();
            IAsyncResult result = NetworkSessionProvider.BeginCreate(NetworkSessionType.Local, gamer, null, true);
            AsyncBusyScreen busyScreen = new AsyncBusyScreen(Stack.Game, result);
            busyScreen.OperationCompleted += OnSessionProvided;
            Stack.Push(busyScreen);
        }

        private void OnMultiplayerGameSelected(object sender, EventArgs args)
        {
            if (Guide.IsTrialMode)
            {
                PurchaseScreen purchaseScreen = new PurchaseScreen(Stack.Game, Resources.TrialMultiplayer, typeof(MainMenuScreen));
                Stack.Push(purchaseScreen);
                return;
            }

#if !DEBUG // for debug we use system link networking which does not require online privileges
            if (!_input.Controller.Value.CanPlayOnline())
            {
                MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.MenuMultiplayerUnavailable);
                Stack.Push(messageScreen);
                return;
            }
#endif

            MultiplayerSelectionScreen multiplayerScreen = new MultiplayerSelectionScreen(Stack.Game);
            Stack.Push(multiplayerScreen);
        }

        private void OnAwardmentsSelected(object sender, EventArgs args)
        {
            AwardmentsScreen awardmentsScreen = new AwardmentsScreen(Stack.Game);
            Stack.Push(awardmentsScreen);
        }

        private void OnHelpOptionsSelected(object sender, EventArgs args)
        {
            HelpOptionsScreen helpOptionsScreen = new HelpOptionsScreen(Stack.Game);
            Stack.Push(helpOptionsScreen);
        }

        private void OnExitSelected(object sender, EventArgs args)
        {
            ExitConfirmationScreen confirmationScreen = new ExitConfirmationScreen(Stack.Game);
            Stack.Push(confirmationScreen);
        }

        private void OnSessionProvided(object sender, AsyncOperationCompletedEventArgs args)
        {
            NetworkSession session = NetworkSessionProvider.EndCreate(args.AsyncResult);
            if (session != null)
            {
                LobbyScreen lobbyScreen = new LobbyScreen(Stack.Game, session);
                Stack.Push(lobbyScreen);
            }
            else
            {
                MessageScreen messageScreen = new MessageScreen(
                    Stack.Game,
                    Resources.NetworkErrorCreate,
                    typeof(MainMenuScreen),
                    false);
                Stack.Push(messageScreen);
            }
        }

        private MenuInput _input;
    }
}
