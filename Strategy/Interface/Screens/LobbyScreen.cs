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
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Input;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Sets up the networking.
    /// </summary>
    public class LobbyScreen : MenuScreen
    {
        public LobbyScreen(Game game, NetworkSession session) : base(game)
        {
            _input = game.Services.GetService<MenuInput>();
            _players = new List<Player>();

            _slots = new List<PlayerSlot>();
            for (int p = 0; p < session.MaxGamers; p++)
            {
                _slots.Add(session.IsLocalSession()
                    ? (PlayerSlot)(new LocalPlayerSlot((PlayerId)p, game.Content))
                    : (PlayerSlot)(new NetworkPlayerSlot(p, game.Content)));
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

            if (!_session.IsLocalSession())
            {
                ImageSprite readyImage = new ImageSprite(game.Content.Load<Texture2D>("Images/ButtonX"));
                TextSprite readyText = new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLarge"), Resources.MenuToggleReady);
                readyText.Position = new Vector2(readyImage.Size.X + 5, (readyImage.Size.Y - readyText.Size.Y) / 2);
                _legend = new CompositeSprite(readyImage, readyText);
                _legend.Position = new Vector2(ControlsBasePosition.X, ControlsBasePosition.Y - _legend.Size.Y - 10);
            }

            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuStartGame, OnStartGame, out _startEntry)
                .CreateCycleButtonEntry(Resources.MenuMapType, OnMapTypeCycled, _configuration.MapType, out _mapTypeEntry)
                .CreateCycleButtonEntry(Resources.MenuMapSize, OnMapSizeCycled, _configuration.MapSize, out _mapSizeEntry)
                .CreateCycleButtonEntry(Resources.MenuAiDifficulty, OnDifficultyCycled, _configuration.Difficulty, out _difficultyEntry);

            // seed the UI with the host status
            UpdateUiForHostChange();

            BasePosition = new Vector2(270f, 590f);
            TransitionOnTime = 0.5f;
            TransitionOffTime = 0.5f;
            ShowBeneath = true; // for the transition on
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            _session.Update();
            if (_session.IsHost && _session.SessionState == NetworkSessionState.Playing && !_isMatchRunning)
            {
                // if we are in the lobby and in the playing state but the match is not running
                // then something has gone wrong and we should move back to the lobby state
                // (probably the host died before its end game packets were sent)
                _session.EndGame();
            }

            _configuration.Update(); // network input
            HandleLocalInput();

            _slots.ForEach(slot => slot.Update(gameTime.GetElapsedSeconds()));

            base.UpdateActive(gameTime);
        }

        protected override void UpdateInactive(GameTime gameTime)
        {
            // continue updating the network session even if other temporary screens are on top
            if (_session != null && _session.SessionState == NetworkSessionState.Lobby)
            {
                _session.Update();
                _configuration.Update();
            }
            base.UpdateInactive(gameTime);
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            if (pushed)
            {
                _transitionProgress = progress;
            }
            base.UpdateTransitionOn(gameTime, progress, pushed);
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            if (popped)
            {
                _transitionProgress = 1 - progress;
            }
            base.UpdateTransitionOff(gameTime, progress, popped);
        }

        public override void Draw()
        {
            float slidePosition = 1280 * (1 - _transitionProgress);
            Matrix m = Matrix.CreateTranslation(slidePosition, 0f, 0f);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, m);
            _background.Draw(_spriteBatch);
            if (_legend != null)
            {
                _legend.Draw(_spriteBatch);
            }
            _slots.ForEach(slot => slot.Draw(_spriteBatch));
            _spriteBatch.End();
            base.Draw();
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

            Player player = new Player() { Gamer = args.Gamer };
            _players.Add(player);

            PlayerSlot slot = FindSlotByPlayer(null);
            slot.Player = player;

            // for local players find the local controller
            if (args.Gamer.IsLocal)
            {
                for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
                {
                    if (Gamer.SignedInGamers[p].Gamertag == args.Gamer.Gamertag)
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

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " left");

            Player playerToRemove = FindPlayerByGamer(args.Gamer);
            _players.Remove(playerToRemove);

            PlayerSlot slot = FindSlotByPlayer(playerToRemove);
            slot.Player = null;
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

            // update the configuration ui
            UpdateUiForHostChange();

            // update the slots to show the new host
            Player oldHostPlayer = FindPlayerByGamer(args.OldHost);
            if (oldHostPlayer != null)
            {
                NetworkPlayerSlot oldHostSlot = FindSlotByPlayer(oldHostPlayer) as NetworkPlayerSlot;
                oldHostSlot.UpdateHostStatus();
            }
            Player newHostPlayer = FindPlayerByGamer(args.NewHost);
            NetworkPlayerSlot newHostSlot = FindSlotByPlayer(newHostPlayer) as NetworkPlayerSlot;
            newHostSlot.UpdateHostStatus();
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

            if (!_session.IsLocalSession())
            {
                // net games: assign ids to players by sorting based on unique id
                // this assignment guarantees identical assignments across machines
                // local games: assign ids based on order joined because that order
                // corresponds to the piece colours displayed in the interface
                gamePlayers.Sort((a, b) => a.Gamer.Id.CompareTo(b.Gamer.Id));
            }

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
            if (_mapTypeEntry == null || _mapSizeEntry == null || _difficultyEntry == null)
            {
                // menu creation has not yet happened; nothing to update
                return;
            }
            _mapTypeEntry.SetEntry(_configuration.MapType.ToString());
            _mapSizeEntry.SetEntry(_configuration.MapSize.ToString());
            _difficultyEntry.SetEntry(_configuration.Difficulty.ToString());
        }

        private void OnReadyChanged(object sender, ReadyChangedEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " is ready changed to " + args.IsReady);

            Player player = FindPlayerByGamer(args.Gamer);
            NetworkPlayerSlot slot = FindSlotByPlayer(player) as NetworkPlayerSlot;
            slot.IsReady = args.IsReady;

            _startEntry.TargetColor = CanStartGame() ? Color.White : CannotStartGameColor;
        }

        private void OnMapSizeCycled(object sender, EventArgs args)
        {
            if (Guide.IsTrialMode)
            {
                _mapSizeEntry.SetEntry(_configuration.MapSize.ToString()); // force the entry back
                PurchaseScreen purchaseScreen = new PurchaseScreen(Stack.Game, Resources.TrialMatchConfiguration, typeof(LobbyScreen));
                Stack.Push(purchaseScreen);
                return;
            }
            switch (_configuration.MapSize)
            {
                case MapSize.Tiny: _configuration.MapSize = MapSize.Small; break;
                case MapSize.Small: _configuration.MapSize = MapSize.Normal; break;
                case MapSize.Normal: _configuration.MapSize = MapSize.Large; break;
                case MapSize.Large: _configuration.MapSize = MapSize.Tiny; break;
            }
        }

        private void OnMapTypeCycled(object sender, EventArgs args)
        {
            if (Guide.IsTrialMode)
            {
                _mapTypeEntry.SetEntry(_configuration.MapType.ToString()); // force the entry back
                PurchaseScreen purchaseScreen = new PurchaseScreen(Stack.Game, Resources.TrialMatchConfiguration, typeof(LobbyScreen));
                Stack.Push(purchaseScreen);
                return;
            }
            switch (_configuration.MapType)
            {
                case MapType.LandRush: _configuration.MapType = MapType.Filled; break;
                case MapType.Filled: _configuration.MapType = MapType.LandRush; break;
            }
        }

        private void OnDifficultyCycled(object sender, EventArgs args)
        {
            if (Guide.IsTrialMode)
            {
                _difficultyEntry.SetEntry(_configuration.Difficulty.ToString()); // force the entry back
                PurchaseScreen purchaseScreen = new PurchaseScreen(Stack.Game, Resources.TrialMatchConfiguration, typeof(LobbyScreen));
                Stack.Push(purchaseScreen);
                return;
            }
            switch (_configuration.Difficulty)
            {
                case AiDifficulty.Easy: _configuration.Difficulty = AiDifficulty.Normal; break;
                case AiDifficulty.Normal: _configuration.Difficulty = AiDifficulty.Hard; break;
                case AiDifficulty.Hard: _configuration.Difficulty = AiDifficulty.Easy; break;
            }
        }

        private void OnStartGame(object sender, EventArgs args)
        {
            if (CanStartGame())
            {
                _session.StartGame();
            }
        }

        private bool CanStartGame()
        {
            bool readyOk = (_session.IsLocalSession() || _configuration.IsEveryoneReady);
            return readyOk && _session.IsHost && _session.SessionState == NetworkSessionState.Lobby;
        }

        private void UpdateUiForHostChange()
        {
            _startEntry.IsVisible = _session.IsHost;
            _startEntry.IsSelectable = _session.IsHost;
            _mapTypeEntry.IsSelectable = _session.IsHost;
            _mapSizeEntry.IsSelectable = _session.IsHost;
            _difficultyEntry.IsSelectable = _session.IsHost;
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
            Player playerToRemove = FindPlayerByGamer(gamer);
            _players.Remove(playerToRemove);

            PlayerSlot slot = FindSlotByPlayer(playerToRemove);
            slot.Player = null;

            if (_players.Count == 0 && !_isMatchRunning)
            {
                // lost all the players, back out to the main menu
                Stack.Pop();
            }
        }

        private Player FindPlayerByGamer(Gamer gamer)
        {
            return _players.FirstOrDefault(player => player.Gamer == gamer);
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
                if (player != null && _input.ToggleReady[(int)p].Pressed && !_session.IsLocalSession())
                {
                    // switch the ready state for this player
                    LocalNetworkGamer gamer = (LocalNetworkGamer)player.Gamer;
                    _configuration.SetIsReady(gamer, !_configuration.IsReady(gamer));
                }
                if (player == null && _input.Join[(int)p].Pressed)
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
                            // may not succeed if the session is full
                            _session.AddLocalGamer(p.GetSignedInGamer());
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
        private Sprite _legend;
        private List<PlayerSlot> _slots;
        private MenuEntry _startEntry;
        private CyclingTextMenuEntry _mapTypeEntry;
        private CyclingTextMenuEntry _mapSizeEntry;
        private CyclingTextMenuEntry _difficultyEntry;
        private float _transitionProgress;

        private static readonly Color CannotStartGameColor = new Color(100, 100, 100);
    }

    /// <summary>
    /// Displays a match slot for a player.
    /// </summary>
    abstract class PlayerSlot
    {
        public Player Player
        {
            get { return _player; }
            set { SetPlayer(value); }
        }

        public PlayerSlot(int slotNumber, Sprite iconSprite, ContentManager content)
        {
            _backgroundSprite = new ImageSprite(content.Load<Texture2D>("Images/LobbyBox"));
            _backgroundSprite.Position = new Vector2(
                (1280 - _backgroundSprite.Size.X) / 2,
                100 + (_backgroundSprite.Size.Y + 20) * slotNumber);

            _labelSprite = new TextSprite(content.Load<SpriteFont>("Fonts/TextSmall"), Resources.MenuJoin);
            _labelSprite.Color = Color.Black;
            _labelSprite.Origin = new Vector2(
                (int)(_labelSprite.Size.X / 2),
                (int)(_labelSprite.Size.Y / 2));
            _labelSprite.Position = new Vector2(
                (int)(_backgroundSprite.Position.X + 25),
                (int)(_backgroundSprite.Position.Y + (_backgroundSprite.Size.Y - _labelSprite.Size.Y) / 2))
                + _labelSprite.Origin;

            _iconSprite = iconSprite;
            _iconSprite.Position = new Vector2(
                _backgroundSprite.Position.X + _backgroundSprite.Size.X - _iconSprite.Size.X - 25,
                _backgroundSprite.Position.Y + (_backgroundSprite.Size.Y - _iconSprite.Size.Y) / 2);
            _iconSprite.Color = Color.Transparent;
        }

        public void Update(float time)
        {
            if (_animation != null)
            {
                if (!_animation.Update(time))
                {
                    _animation = null;
                }
            }
            if (_animation == null && _player == null)
            {
                _animation = new SequentialAnimation(
                    new ScaleAnimation(_labelSprite, new Vector2(1.1f, 1.1f), 1f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                    new ScaleAnimation(_labelSprite, Vector2.One, 1f, Interpolation.InterpolateVector2(Easing.QuadraticIn)));
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _backgroundSprite.Draw(spriteBatch);
            _labelSprite.Draw(spriteBatch);
            _iconSprite.Draw(spriteBatch);
        }

        private void SetPlayer(Player player)
        {
            // skip the transition if the set is a no-op
            if (_player == player)
            {
               return;
            }

            _player = player;

            string newLabelText = GetLabelText();
            Color newIconColor = GetIconColor();
            _animation = new SequentialAnimation(
                new ColorAnimation(_labelSprite, Color.Transparent, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)),
                new TextAnimation(_labelSprite, newLabelText),
                new ScaleAnimation(_labelSprite, Vector2.One, 0f, Interpolation.InterpolateVector2(Easing.Uniform)),
                new CompositeAnimation(
                    new ColorAnimation(_labelSprite, Color.Black, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)),
                    new ColorAnimation(_iconSprite, newIconColor, 0.25f, Interpolation.InterpolateColor(Easing.Uniform))));
        }

        protected abstract string GetLabelText();

        protected abstract Color GetIconColor();

        private Player _player;

        protected ImageSprite _backgroundSprite;
        protected TextSprite _labelSprite;
        protected Sprite _iconSprite;

        private IAnimation _animation;
    }

    class LocalPlayerSlot : PlayerSlot
    {
        public LocalPlayerSlot(PlayerId playerId, ContentManager content)
            : base((int)playerId, new ImageSprite(content.Load<Texture2D>("Images/PieceAvailable")), content)
        {
            _playerId = playerId;
        }

        protected override string GetLabelText()
        {
            return (Player != null)
                ? string.Format(Resources.MenuPlayerSlot, Player.DisplayName)
                : Resources.MenuJoin;
        }

        protected override Color GetIconColor()
        {
            return _playerId.GetPieceColor();
        }

        private PlayerId _playerId;
    }

    class NetworkPlayerSlot : PlayerSlot
    {
        public bool IsReady
        {
            get { return _ready; }
            set { SetReady(value); }
        }

        public NetworkPlayerSlot(int slotNumber, ContentManager content)
            : base(slotNumber, new ImageSprite(content.Load<Texture2D>("Images/Ready")), content)
        {
        }

        public void UpdateHostStatus()
        {
            _labelSprite.Text = GetLabelText();
        }

        protected override string GetLabelText()
        {
            return (Player != null)
                ? string.Format(
                    Player.Gamer.IsHost ? Resources.MenuPlayerSlotHost : Resources.MenuPlayerSlot,
                    Player.DisplayName)
                : Resources.MenuJoin;
        }

        protected override Color GetIconColor()
        {
            return _ready ? ReadyColor : UnreadyColor;
        }

        private void SetReady(bool ready)
        {
            _ready = ready;
            _iconSprite.Color = GetIconColor();
        }

        private bool _ready;

        private readonly Color ReadyColor = PlayerId.C.GetPieceColor();
        private readonly Color UnreadyColor = PlayerId.A.GetPieceColor();
        private readonly Color NoPlayerColor = Color.Transparent;
    }
}