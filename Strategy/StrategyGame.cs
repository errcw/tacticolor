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
using Strategy.Library.Sound;
using Strategy.Library.Storage;
using Strategy.Properties;

namespace Strategy
{
    /// <summary>
    /// Game scaffolding. Handles the game-global state.
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
            Components.Add(_storage = new SharedStorage(this, "Tacticolor"));
            Components.Add(_music = new MusicController(this));
            Components.Add(_trial = new TrialModeObserverComponent(this));

            //Components.Add(new TitleSafeAreaOverlayComponent(this));
            //Components.Add(new FpsOverlay(this));
            Components.Add(new GamerServicesComponent(this));

            Services.AddService<MenuInput>(_input);
            Services.AddService<Storage>(_storage);
            Services.AddService<MusicController>(_music);
            Services.AddService<TrialModeObserverComponent>(_trial);
            Services.AddService<Options>(_options = new Options());
            Services.AddService<Awardments>(_awardments = new Awardments());

#if WINDOWS && DEBUG
            //Guide.SimulateTrialMode = true;
#endif
        }

        /// <summary>
        /// Sets up the initial screen.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            bool loaded = false;
            _storage.DeviceSelected += delegate(object s, EventArgs a)
            {
                if (!loaded)
                {
                    _options.Load(_storage);
                    _awardments.Load(_storage);
                    loaded = true;
                }
            };
            _storage.DeviceDisconnected += delegate(object s, StorageEventArgs a)
            {
                a.ShouldPrompt = true;
                a.PlayerToPrompt = _input.Controller.GetValueOrDefault(PlayerIndex.One);
                loaded = false;
            };
            _storage.DeviceSelectorCanceled += delegate(object s, StorageEventArgs a)
            {
                a.ShouldPrompt = true;
                a.PlayerToPrompt = _input.Controller.GetValueOrDefault(PlayerIndex.One);
            };

            SignedInGamer.SignedOut += OnGamerSignedOut;

            //CreateDebugGame();
            TitleScreen titleScreen = new TitleScreen(this);
            titleScreen.ContentLoaded += OnContentLoaded;
            _screens.Push(titleScreen);
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
                if (NetworkSessionProvider.CurrentSession != null && !NetworkSessionProvider.CurrentSession.IsDisposed)
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
                    null,
                    null);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(this, result);
                busyScreen.OperationCompleted += OnInviteSessionCreated;
                _screens.Push(busyScreen);
            }
        }

        private void OnInviteSessionCreated(object sender, AsyncOperationCompletedEventArgs args)
        {
            NetworkSession session = NetworkSessionProvider.EndJoinInvited(args.AsyncResult);
            if (session != null)
            {

                LobbyScreen lobbyScreen = new LobbyScreen(this, new StrategyNetworkSession(session));
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
            // bail if player that signed out was controlling the menus
            bool shouldBail = _input.Controller.Value == args.Gamer.PlayerIndex;
            // but only bail if we have passed the title screen
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
                    players[p].Input = new AiInput(players[p].Id, match, AiDifficulty.Hard, random);
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
        private TrialModeObserverComponent _trial;
        private MusicController _music;
    }
}
