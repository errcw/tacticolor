using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Net;
using Strategy.Properties;
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

            AddEntry(new TextMenuEntry(new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLarge"), Resources.MenuLocalGame)));
            AddEntry(new TextMenuEntry(new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLarge"), Resources.MenuMultiplayerGame)));
            AddEntry(new TextMenuEntry(new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLarge"), Resources.MenuPurchase)));
            AddEntry(new TextMenuEntry(new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLarge"), Resources.MenuAwardments)));
            AddEntry(new TextMenuEntry(new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLarge"), Resources.MenuHelpOptions)));
            AddEntry(new TextMenuEntry(new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLarge"), Resources.MenuExit)));

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
