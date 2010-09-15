using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

using Strategy.AI;
using Strategy.Gameplay;
using Strategy.Interface;
using Strategy.Interface.Gameplay;
using Strategy.Interface.Screens;
using Strategy.Net;
using Strategy.Library;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Storage;
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
            Window.Title = Resources.Strategy;

            Content.RootDirectory = "Content";

            Components.Add(_screens = new ScreenStack(this));
            Components.Add(_input = new MenuInput(this));
            Components.Add(_storage = new SharedStorage(this, "Strategy"));

            //Components.Add(new TitleSafeAreaOverlayComponent(this));
            //Components.Add(new FPSOverlay(this));
            Components.Add(new TrialModeObserverComponent(this));
            Components.Add(new GamerServicesComponent(this));

            Services.AddService<MenuInput>(_input);
            Services.AddService<Storage>(_storage);
            Services.AddService<Options>(_options = new Options());
            Services.AddService<Awardments>(_awardments = new Awardments());
        }

        /// <summary>
        /// Sets up the initial screen.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            _storage.DeviceSelected += delegate(object s, EventArgs a)
            {
                _options.Load(_storage);
                _awardments.Load(_storage);
            };

            SignedInGamer.SignedOut += OnGamerSignedOut;

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

        /// <summary>
        /// Flushes persistent state to storage.
        /// </summary>
        protected override void OnExiting(object sender, EventArgs args)
        {
            if (_storage.IsValid)
            {
                _options.Save(_storage);
                if (!Guide.IsTrialMode)
                {
                    _awardments.Save(_storage);
                }
            }
            base.OnExiting(sender, args);
        }

        private void OnContentLoaded(object sender, EventArgs args)
        {
            // wire up the invite method only when the game has loaded
            // lest we receive an invitation before initialization
            NetworkSession.InviteAccepted += OnInviteAccepted;

            // wire up the awardments now that we have assets to handle it
            _screens.AddOverlay(new AwardmentOverlay(this, _awardments));
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

                // if the invite was accepted before the game started
                // ensure we have a menu screen to return to
                if (_screens.ActiveScreen is TitleScreen)
                {
                    MainMenuScreen menuScreen = new MainMenuScreen(this);
                    _screens.Push(menuScreen);

                    // transfer control to the gamer that initiated the action
                    _input.Controller = args.Gamer.PlayerIndex;
                }

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

                LobbyScreen lobbyScreen = new LobbyScreen(this, session);
                _screens.Push(lobbyScreen);
            }
            else
            {
                MessageScreen messageScreen = new MessageScreen(this, Resources.NetworkErrorAcceptInvite);
                _screens.Push(messageScreen);
            }
        }

        private void OnGamerSignedOut(object sender, SignedOutEventArgs args)
        {
            bool shouldBail = false;
            if (NetworkSessionProvider.CurrentSession != null)
            {
                // player that signed out was playing a game
                Gamer player = NetworkSessionProvider.CurrentSession.LocalGamers.AsEnumerable<Gamer>().FirstOrDefault(gamer => gamer.Gamertag == args.Gamer.Gamertag);
                shouldBail = player != null;
            }
            else
            {
                // player that signed out was controlling the menus
                shouldBail = _input.Controller.Value == args.Gamer.PlayerIndex;
            }
            // only bail if we have passed the title screen
            shouldBail &= !(_screens.ActiveScreen is TitleScreen);
            if (shouldBail)
            {
                MessageScreen messageScreen = new MessageScreen(this, Resources.SignedOutError, typeof(TitleScreen));
                _screens.Push(messageScreen);
            }
        }

        private void CreateDebugGame()
        {
            const int DebugHuman = 1;
            const int DebugAI = 3;

            Random random = new Random();
            MapGenerator generator = new MapGenerator(random);
            Map map = generator.Generate(16, DebugHuman + DebugAI, 1, 2);
            Match match = new Match(map, random);

            Player[] players = new Player[DebugHuman + DebugAI];
            for (int p = 0; p < players.Length; p++)
            {
                players[p] = new Player();
                players[p].Id = (PlayerId)p;
                if (p < DebugHuman)
                {
                    players[p].Controller = (PlayerIndex)p;
                    players[p].Input = new LocalInput(players[p].Id, players[p].Controller.Value, match, GameplayScreen.IsoParams);
                }
                else
                {
                    players[p].Input = new AIInput(players[p].Id, match, AIDifficulty.Normal, random);
                }
            }

            _input.Controller = PlayerIndex.One;

            GameplayScreen gameplayScreen = new GameplayScreen(this, null, players, match);
            _screens.Push(gameplayScreen);
        }

        private ScreenStack _screens;
        private MenuInput _input;
        private Options _options;
        private Storage _storage;
        private Awardments _awardments;
    }
}
