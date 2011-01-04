using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

using Strategy.AI;
using Strategy.Gameplay;
using Strategy.Interface;
using Strategy.Interface.Gameplay;
using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Extensions;
using Strategy.Library.Input;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Sets up the networking.
    /// </summary>
    public class LobbyScreen : Screen
    {
        public LobbyScreen(Game game, NetworkSession session)
        {
            _input = game.Services.GetService<MenuInput>();
            _players = new List<Player>();

            _slots = new List<PlayerSlot>();
            for (int p = 0; p < session.MaxGamers; p++)
            {
                _slots.Add(new PlayerSlot(game.Content, p));
            }

            _session = session;
            _session.GamerJoined += OnGamerJoined;
            _session.GamerLeft += OnGamerLeft;
            _session.HostChanged += OnHostChanged;
            _session.GameStarted += OnGameStarted;
            _session.SessionEnded += OnSessionEnded;

            _configuration = new MatchConfigurationManager(_session);
            _configuration.ConfigurationChanged += OnConfigurationChanged;
            _configuration.ReadyChanged += OnReadyChanged;

            if (_session.IsHost)
            {
                // choose a default configuration to start
                _configuration.SetConfiguration(
                    _random.Next(1, int.MaxValue),
                    MapType.LandRush,
                    MapSize.Normal,
                    AiDifficulty.Normal);
            }

            _background = new ImageSprite(game.Content.Load<Texture2D>("Images/BackgroundLobby"));
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            TransitionOnTime = 0.5f;
            TransitionOffTime = 0.5f;
            ShowBeneath = true; // for the transition on
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            _session.Update();
            if (_session.IsHost && _session.SessionState == NetworkSessionState.Lobby && _configuration.IsEveryoneReady)
            {
                _session.StartGame();
            }
            else if (_session.IsHost && _session.SessionState == NetworkSessionState.Playing && !_isMatchRunning)
            {
                // if we are in the lobby and in the playing state but the match is not running
                // then something has gone wrong and we should move back to the lobby state
                // (probably the host died before its end game packets were sent)
                _session.EndGame();
            }

            _configuration.Update(); // network input
            HandleLocalInput();

            _slots.ForEach(slot => slot.Update(gameTime.GetElapsedSeconds()));
        }

        protected override void UpdateInactive(GameTime gameTime)
        {
            // continue updating the network session even if other temporary screens are on top
            if (_session != null && _session.SessionState == NetworkSessionState.Lobby)
            {
                _session.Update();
                _configuration.Update();
            }
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            if (pushed)
            {
                _transitionProgress = progress;
            }
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            if (popped)
            {
                _transitionProgress = 1 - progress;
            }
        }

        public override void Draw()
        {
            float slidePosition = 1280 * (1 - _transitionProgress);
            Matrix m = Matrix.CreateTranslation(slidePosition, 0f, 0f);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, m);
            _background.Draw(_spriteBatch);
            _slots.ForEach(slot => slot.Draw(_spriteBatch));
            _spriteBatch.End();
        }

        protected internal override void Show(bool pushed)
        {
            if (!pushed)
            {
                // handle the case where we are returning to the lobby after a game
                if (_isMatchRunning)
                {
                    _isMatchRunning = false;
                    _configuration.ResetForNextMatch();
                }
            }
            base.Show(pushed);
        }

        protected internal override void Hide(bool popped)
        {
            if (popped)
            {
                _session.Dispose();
                _session = null;
            }
            base.Hide(popped);
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " joined");
            Debug.Assert(_session.SessionState == NetworkSessionState.Lobby);
            AddPlayer(args.Gamer);
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " left");
            RemovePlayer(args.Gamer);
        }

        private void OnHostChanged(object sender, HostChangedEventArgs args)
        {
            Debug.WriteLine(args.NewHost.Gamertag + " is now host (was " + args.OldHost.Gamertag + ")");

            // once the game has started host changes do not matter
            if (_session.SessionState == NetworkSessionState.Playing)
            {
                return;
            }

            if (_session.IsHost)
            {
                // use a new configuration with the new host to sync all players
                _configuration.Seed = _random.Next(1, int.MaxValue);
            }
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
            Debug.WriteLine("Game starting");

            Debug.Assert(!_isMatchRunning);
            _isMatchRunning = true;

            // create the game objects
            Random gameRandom = new Random(_configuration.Seed);
            MapGenerator generator = new MapGenerator(gameRandom);
            Map map = generator.Generate(_configuration.MapType, _configuration.MapSize);
            Match match = new Match(map, gameRandom);

            // create a copy of the list so that the match can continue to
            // manipulate the list of remaining gamers while the match runs
            List<Player> gamePlayers = new List<Player>(_players);

            // assign ids to players by sorting based on unique id
            // this assignment guarantees identical assignments across machines
            gamePlayers.Sort((a, b) => a.Gamer.Id.CompareTo(b.Gamer.Id));
            for (int p = 0; p < gamePlayers.Count; p++)
            {
                gamePlayers[p].Id = (PlayerId)p;
                if (gamePlayers[p].Gamer.IsLocal)
                {
                    gamePlayers[p].Input = new LocalInput(gamePlayers[p].Id, gamePlayers[p].Controller.Value, match, GameplayScreen.IsoParams);
                }
            }

            // fill out the remaining players with AI
            int humanPlayerCount = gamePlayers.Count;
            int aiPlayerCount = Match.MaxPlayerCount - humanPlayerCount;
            for (int p = 0; p < aiPlayerCount; p++)
            {
                Player aiPlayer = new Player();
                aiPlayer.Id = (PlayerId)(p + humanPlayerCount);
                aiPlayer.Input = new AiInput(aiPlayer.Id, match, _configuration.Difficulty, gameRandom);
                gamePlayers.Add(aiPlayer);
            }

            GameplayScreen gameplayScreen = new GameplayScreen(Stack.Game, _session, gamePlayers, match);
            Stack.Push(gameplayScreen);
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            // the gameplay screen has already consumed this event
            if (_isMatchRunning)
            {
                return;
            }
            // if the session ended before the game started then we encountered an error
            MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.NetworkError);
            Stack.Push(messageScreen);
        }

        private void OnConfigurationChanged(object sender, EventArgs args)
        {
            Debug.WriteLine("Configuration changed");
            //TODO show the new configuration
        }

        private void OnReadyChanged(object sender, ReadyChangedEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " is ready changed to " + args.IsReady);

            Player player = _players.Single(p => p.Gamer == args.Gamer);
            PlayerSlot slot = FindSlotByPlayer(player);
            slot.IsReady = args.IsReady;
        }

        private void AddPlayer(NetworkGamer gamer)
        {
            Player player = new Player() { Gamer = gamer };
            _players.Add(player);

            PlayerSlot slot = FindSlotByPlayer(null);
            slot.Player = player;

            // for local players find the local controller
            if (gamer.IsLocal)
            {
                for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
                {
                    if (Gamer.SignedInGamers[p].Gamertag == gamer.Gamertag)
                    {
                        player.Controller = p;
                        break;
                    }
                }
                if (!player.Controller.HasValue)
                {
                    Debug.WriteLine("Local player with no controller! Falling back to controller one.");
                    player.Controller = PlayerIndex.One;
                }
            }
        }

        private void RemovePlayer(NetworkGamer gamer)
        {
            Player playerToRemove = _players.Single(player => player.Gamer == gamer);
            _players.Remove(playerToRemove);

            PlayerSlot slot = FindSlotByPlayer(playerToRemove);
            slot.Player = null;

            if (_players.Count == 0 && !_isMatchRunning)
            {
                // lost all the players, back out to the main menu
                Stack.Pop();
            }
        }

        private Player FindPlayerByController(PlayerIndex index)
        {
            return _players.FirstOrDefault(player => player.Controller == index);
        }

        private PlayerSlot FindSlotByPlayer(Player player)
        {
            return _slots.First(slot => slot.Player == player);
        }

        /// <summary>
        /// Handle input for every local player in the lobby.
        /// </summary>
        private void HandleLocalInput()
        {
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                Player player = FindPlayerByController(p);
                if (_input.Join[(int)p].Pressed)
                {
                    if (player != null)
                    {
                        // mark this player as ready
                        if (_configuration.HasValidConfiguration)
                        {
                            _configuration.SetIsReady((LocalNetworkGamer)player.Gamer, true);
                        }
                    }
                    else
                    {
                        // first time we saw this player
                        if (!p.IsSignedIn() && !Guide.IsVisible)
                        {
                            try
                            {
                                // prompt the player to sign in
                                Guide.ShowSignIn(1, _session.IsOnlineSession());
                            }
                            catch
                            {
                                // ignore whatever guide errors occur
                            }
                        }
                        if (p.IsSignedIn())
                        {
                            if (_session.IsOnlineSession() && !p.CanPlayOnline())
                            {
                                // cannot join this online session
                                MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.NetworkErrorCannotPlayOnline);
                                Stack.Push(messageScreen);
                            }
                            else
                            {
                                // join the existing session
                                _session.AddLocalGamer(p.GetSignedInGamer());
                            }
                        }
                    }
                }
                else if (_input.Leave[(int)p].Pressed)
                {
                    // ignore leave requests from non-players
                    if (player != null)
                    {
                        if (_configuration.IsReady(player.Gamer))
                        {
                            // back out of the ready state
                            _configuration.SetIsReady((LocalNetworkGamer)player.Gamer, false);
                        }
                        else
                        {
                            // leave the session if the request came from the main controller
                            if (p == _input.Controller)
                            {
                                Stack.Pop();
                            }
                        }
                    }
                }
            }
        }

        private NetworkSession _session = null;
        private List<Player> _players;

        private MenuInput _input;

        private Random _random = new Random();
        private MatchConfigurationManager _configuration;

        private bool _isMatchRunning = false;

        private Sprite _background;
        private List<PlayerSlot> _slots;
        private SpriteBatch _spriteBatch;
        private float _transitionProgress;
    }

    /// <summary>
    /// Displays a match slot for a player.
    /// </summary>
    class PlayerSlot
    {
        public Player Player
        {
            get { return _player; }
            set { SetPlayer(value); }
        }

        public bool IsReady
        {
            get { return _ready; }
            set { _ready = value; }
        }

        public PlayerSlot(ContentManager content, int index)
        {
            _backgroundSprite = new ImageSprite(content.Load<Texture2D>("Images/LobbyBox"));
            _backgroundSprite.Position = new Vector2(
                (1280 - _backgroundSprite.Size.X) / 2,
                100 + (_backgroundSprite.Size.Y + 20) * index);

            _nameSprite = new TextSprite(content.Load<SpriteFont>("Fonts/TextSmall"));
            _nameSprite.Color = Color.Black;
            _nameSprite.Position = new Vector2(
                (int)_backgroundSprite.Position.X + 15,
                (int)_backgroundSprite.Position.Y + 20);

            _readySprite = new ImageSprite(content.Load<Texture2D>("Images/Piece"));
            _readySprite.Position = new Vector2(
                _backgroundSprite.Position.X + _backgroundSprite.Size.X - _readySprite.Size.X - 15,
                _backgroundSprite.Position.Y + (_backgroundSprite.Size.Y - _readySprite.Size.Y) / 2);

            Player = null;
            IsReady = false;
        }

        public void Update(float time)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _backgroundSprite.Draw(spriteBatch);
            _nameSprite.Draw(spriteBatch);
            _readySprite.Draw(spriteBatch);
        }

        private void SetPlayer(Player player)
        {
            _player = player;
            if (player != null)
            {
                _nameSprite.Text = player.DisplayName;
            }
            else
            {
                _nameSprite.Text = Resources.MenuJoin;
            }
        }

        private Player _player;
        private bool _ready;

        private ImageSprite _backgroundSprite;
        private TextSprite _nameSprite;
        private ImageSprite _readySprite;
    }
}