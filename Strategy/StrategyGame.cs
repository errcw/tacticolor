using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Interface;
using Strategy.Interface.Screens;
using Strategy.Net;
using Strategy.Library;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Properties;

namespace Strategy
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class StrategyGame : Game
    {
        public StrategyGame()
        {
            GraphicsDeviceManager graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            Window.Title = Resources.StrategyGame;

            Content.RootDirectory = "Content";

            Components.Add(_screens = new ScreenStack(this));
            Components.Add(_input = new MenuInput(this));
            Components.Add(_trial = new TrialModeObserverComponent(this));

            //Components.Add(new TitleSafeAreaOverlayComponent(this));
            //Components.Add(new FPSOverlay(this));
            Components.Add(new GamerServicesComponent(this));

            Services.AddService<MenuInput>(_input);
        }

        /// <summary>
        /// Sets up the initial screen.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            CreateDebugGame();
            //TitleScreen titleScreen = new TitleScreen(this);
            //titleScreen.ContentLoaded += OnContentLoaded;
            //_screens.Push(titleScreen);
        }

        /// <summary>
        /// Updates the game state.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            //XXX? _input.Update(gameTime.GetElapsedSeconds());

#if DEBUG
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }
#endif

            if (_screens.ActiveScreen == null)
            {
                Exit();
            }
        }

        /// <summary>
        /// Draws the game to the screen.
        /// </summary>
        /// <param name="gameTime">A snapshot of timing values.</param>     
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(45, 45, 45));
            base.Draw(gameTime);
        }

        private void OnContentLoaded(object sender, EventArgs args)
        {
            // wire up the invite method only when the game has loaded
            // lest we receive an invitation before initialization
            NetworkSession.InviteAccepted += OnInviteAccepted;
        }

        private void OnInviteAccepted(object sender, InviteAcceptedEventArgs args)
        {
            if (args.IsCurrentSession)
            {
                // attach to the current session
                NetworkSessionProvider.CurrentSession.AddLocalGamer(args.Gamer);
            }
            else
            {
                // dispose the current session if one exists
                if (NetworkSessionProvider.CurrentSession != null)
                {
                    NetworkSessionProvider.CurrentSession.Dispose();
                }

                // destroy the current game state
                while (!(_screens.ActiveScreen is MainMenuScreen) && !(_screens.ActiveScreen is TitleScreen))
                {
                    _screens.Pop();
                }

                // transfer control to the gamer that initiated the action
                _input.Controller = args.Gamer.PlayerIndex;

                IAsyncResult result = NetworkSessionProvider.BeginJoinInvited(
                    args.Gamer,
                    OnInviteSessionCreated,
                    null);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                _screens.Push(busyScreen);
            }
        }

        private void OnInviteSessionCreated(IAsyncResult result)
        {
            NetworkSession session = NetworkSessionProvider.EndJoinInvited(result);
            if (session != null)
            {
                // if the invite was accepted before the game started
                // ensure we have a menu screen to return to
                if (_screens.ActiveScreen is TitleScreen)
                {
                    MainMenuScreen menuScreen = new MainMenuScreen(this);
                    _screens.Push(menuScreen);
                }

                LobbyScreen lobbyScreen = new LobbyScreen(this, session);
                _screens.Push(lobbyScreen);
            }
            else
            {
                // show an error message
            }
        }

        private void CreateDebugGame()
        {
            const int DebugPlayers = 4;

            Random random = new Random();

            MapGenerator generator = new MapGenerator(random);
            Map map = generator.Generate(16, DebugPlayers, 1, 2);

            Player[] players = new Player[DebugPlayers];
            for (int p = 0; p < players.Length; p++)
            {
                players[p] = new Player();
                players[p].Id = (PlayerId)p;
                players[p].Controller = (PlayerIndex)p;
            }

            GameplayScreen gameplayScreen = new GameplayScreen(this, null, players, map, random);
            _screens.Push(gameplayScreen);
        }

        private ScreenStack _screens;
        private MenuInput _input;
        private TrialModeObserverComponent _trial;
    }
}
