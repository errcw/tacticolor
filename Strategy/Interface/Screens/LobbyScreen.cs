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
using Strategy.Library.Animation;
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
        public LobbyScreen(Game game, StrategyNetworkSession net) : base(game)
        {
            _input = game.Services.GetService<MenuInput>();
            _players = new List<Player>();

            _slots = new List<PlayerSlot>();
            for (int p = 0; p < net.Session.MaxGamers; p++)
            {
                _slots.Add(net.Session.IsLocalSession()
                    ? (PlayerSlot)(new LocalPlayerSlot((PlayerId)p, game.Content))
                    : (PlayerSlot)(new NetworkPlayerSlot(p, game.Content)));
            }

            _net = net;
            _net.Session.GamerJoined += OnGamerJoined;
            _net.Session.GamerLeft += OnGamerLeft;
            _net.Session.GameStarted += OnGameStarted;
            _net.Session.GameEnded += OnGameEnded;
            _net.Session.HostChanged += OnHostChanged;
            _net.Session.SessionEnded += OnSessionEnded;

            _configuration = new MatchConfigurationManager(_net);
            _configuration.ConfigurationChanged += OnConfigurationChanged;
            _configuration.ReadyChanged += OnReadyChanged;

            if (_net.Session.IsHost)
            {
                // choose a default configuration to start
                _configuration.SetConfiguration(
                    _random.Next(1, int.MaxValue),
                    MapType.LandRush,
                    MapSize.Normal,
                    AiDifficulty.Easy);
            }

            _background = new ImageSprite(game.Content.Load<Texture2D>("Images/BackgroundLobby"));

            ImageSprite readyImage = new ImageSprite(game.Content.Load<Texture2D>("Images/ButtonX"));
            TextSprite readyText = new TextSprite(game.Content.Load<SpriteFont>("Fonts/Text"), Resources.MenuToggleReady);
            readyText.Position = new Vector2(readyImage.Size.X + 5, (readyImage.Size.Y - readyText.Size.Y) / 2 + 2);

            ImageSprite inviteImage = new ImageSprite(game.Content.Load<Texture2D>("Images/ButtonY"));
            inviteImage.Position = new Vector2(0, readyImage.Position.Y + readyImage.Size.Y + 10);
            TextSprite inviteText = new TextSprite(game.Content.Load<SpriteFont>("Fonts/Text"), Resources.MenuInvite);
            inviteText.Position = new Vector2(inviteImage.Size.X + 5, inviteImage.Position.Y + (inviteImage.Size.Y - inviteText.Size.Y) / 2 + 2);

            _buttonsLegend = new CompositeSprite(readyImage, readyText, inviteImage, inviteText);
            _buttonsLegend.Position = new Vector2(ControlsBasePosition.X + 150, ControlsBasePosition.Y);
            _buttonsLegend.Color = _net.Session.IsLocalSession() ? Color.Transparent : Color.White;

            _optionsLegend = new TextSprite(game.Content.Load<SpriteFont>("Fonts/TextLight"));
            _optionsLegend.Position = LegendBase;

            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuStartGame, OnStartGame, out _startEntry)
                .CreateCycleButtonEntry(Resources.MenuMapType, OnMapTypeCycled, _configuration.MapType, out _mapTypeEntry)
                .CreateCycleButtonEntry(Resources.MenuMapSize, OnMapSizeCycled, _configuration.MapSize, out _mapSizeEntry)
                .CreateCycleButtonEntry(Resources.MenuAiDifficulty, OnDifficultyCycled, _configuration.Difficulty, out _difficultyEntry);

            // seed the UI with the host/ready status
            UpdateUiForHostChange();
            UpdateUiForCanStartChange();

            BasePosition = new Vector2(130f, 80f);
            TransitionOnTime = 0.5f;
            TransitionOffTime = 0.5f;
            StateChanged += (s, a) => ShowBeneath = (State == ScreenState.TransitionOn || State == ScreenState.TransitionOff);
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            _net.Update();
            if (_net.Session.IsHost && _net.Session.SessionState == NetworkSessionState.Playing && !_isMatchRunning)
            {
                // if we are in the lobby and in the playing state but the match is not running
                // then something has gone wrong and we should move back to the lobby state
                // (probably the host died before its end game packets were sent)
                _net.Session.EndGame();
            }

            _configuration.Update(); // network input
            HandleLocalInput();

            _slots.ForEach(slot => slot.Update(gameTime.GetElapsedSeconds()));
            UpdateLegend();

            base.UpdateActive(gameTime);
        }

        protected override void UpdateInactive(GameTime gameTime)
        {
            // continue updating the network session even if other temporary screens are on top
            if (_net != null && _net.Session.SessionState == NetworkSessionState.Lobby)
            {
                _net.Update();
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
            base.Draw(m);
        }

        protected override void OnDraw(SpriteBatch spriteBatch)
        {
            _background.Draw(spriteBatch);
            _slots.ForEach(slot => slot.Draw(spriteBatch));
            _buttonsLegend.Draw(spriteBatch);
            _optionsLegend.Draw(spriteBatch);
            base.OnDraw(spriteBatch);
        }

        protected internal override void Show(bool pushed)
        {
            if (!pushed)
            {
                _isMatchRunning = false;
            }
            base.Show(pushed);
        }

        protected internal override void Hide(bool popped)
        {
            if (popped)
            {
                _net.Dispose();
                _net = null;
            }
            base.Hide(popped);
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " joined");
            Debug.Assert(_net.Session.SessionState == NetworkSessionState.Lobby);

            Player player = new Player() { Gamer = args.Gamer };
            _players.Add(player);

            PlayerSlot slot = FindSlotByPlayer(null);
            slot.Player = player;

            // for local players find the local controller
            if (args.Gamer.IsLocal)
            {
                for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
                {
                    if (Gamer.SignedInGamers[p] != null && Gamer.SignedInGamers[p].Gamertag == args.Gamer.Gamertag)
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

            UpdateUiForCanStartChange();
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " left");

            Player playerToRemove = FindPlayerByGamer(args.Gamer);
            _players.Remove(playerToRemove);

            PlayerSlot slot = FindSlotByPlayer(playerToRemove);
            slot.Player = null;

            NetworkPlayerSlot networkSlot = slot as NetworkPlayerSlot;
            if (networkSlot != null)
            {
                networkSlot.IsReady = false;
            }

            UpdateUiForCanStartChange();
        }

        private void OnHostChanged(object sender, HostChangedEventArgs args)
        {
            Debug.WriteLine(args.NewHost.Gamertag + " is now host (was " + args.OldHost.Gamertag + ")");

            // host changes only occur in local sessions when a player is
            // signing out, which will ultimately bail us from the lobby
            if (_net.Session.IsLocalSession())
            {
                return;
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

            // use a new configuration with the new host to sync all players
            // not necessary after the match has started
            if (_net.Session.IsHost && _net.Session.SessionState == NetworkSessionState.Lobby)
            {
                _configuration.Seed = _random.Next(1, int.MaxValue);
            }
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
            Debug.WriteLine("Game starting");

            Debug.Assert(!_isMatchRunning);
            _isMatchRunning = true;

            // create the game objects
            int numPlayers = (_configuration.Difficulty == AiDifficulty.None) ? _players.Count : Match.MaxPlayerCount;
            Random gameRandom = new Random(_configuration.Seed);
            MapGenerator generator = new MapGenerator(gameRandom);
            Map map = generator.Generate(_configuration.MapType, _configuration.MapSize, numPlayers);
            Match match = new Match(map, gameRandom);

            // create a copy of the list so that the match can continue to
            // manipulate the list of remaining gamers while the match runs
            List<Player> gamePlayers = new List<Player>(_players);

            if (!_net.Session.IsLocalSession())
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

            // fill out the remaining players with AI, if necessary
            if (_configuration.Difficulty != AiDifficulty.None)
            {
                int humanPlayerCount = gamePlayers.Count;
                int aiPlayerCount = Match.MaxPlayerCount - humanPlayerCount;
                for (int p = 0; p < aiPlayerCount; p++)
                {
                    Player aiPlayer = new Player();
                    aiPlayer.Id = (PlayerId)(p + humanPlayerCount);
                    aiPlayer.Input = new AiInput(aiPlayer.Id, match, _configuration.Difficulty, gameRandom);
                    gamePlayers.Add(aiPlayer);
                }
            }

            GameplayScreen gameplayScreen = new GameplayScreen(Stack.Game, _net, gamePlayers, match);
            Stack.Push(gameplayScreen);
        }

        private void OnGameEnded(object sender, GameEndedEventArgs args)
        {
            _configuration.ResetForNextMatch();
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

            UpdateUiForCanStartChange();
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
                case AiDifficulty.None: _configuration.Difficulty = AiDifficulty.Easy; break;
                case AiDifficulty.Easy: _configuration.Difficulty = AiDifficulty.Normal; break;
                case AiDifficulty.Normal: _configuration.Difficulty = AiDifficulty.Hard; break;
                case AiDifficulty.Hard: _configuration.Difficulty = AiDifficulty.None; break;
            }
            UpdateUiForCanStartChange();
        }

        private void OnStartGame(object sender, EventArgs args)
        {
            if (CanStartGame())
            {
                _net.Session.StartGame();
            }
        }

        private bool CanStartGame()
        {
            if (_configuration == null)
            {
                return false;
            }
            bool readyOk = (_net.Session.IsLocalSession() || _configuration.IsEveryoneReady);
            bool playerCountOk = (_configuration.Difficulty != AiDifficulty.None || _players.Count >= 2);
            return readyOk && playerCountOk && _net.Session.IsHost;
        }

        private void UpdateUiForCanStartChange()
        {
            if (_net.Session.IsHost && _startEntry != null) // only the host can see the menu entry
            {
                _startEntry.TargetColor = CanStartGame() ? Color.White : CannotStartGameColor;
            }
        }

        private void UpdateUiForHostChange()
        {
            _startEntry.TargetColor = _net.Session.IsHost ? Color.White : Color.Transparent;
            _startEntry.IsSelectable = _net.Session.IsHost;
            _mapTypeEntry.IsSelectable = _net.Session.IsHost;
            _mapSizeEntry.IsSelectable = _net.Session.IsHost;
            _difficultyEntry.IsSelectable = _net.Session.IsHost;
            UpdateUiForCanStartChange();
        }
        
        protected override void SetSelected(int deltaIdx)
        {
            // only allow moving between menu items as the host
            if (!_net.Session.IsHost && deltaIdx != 0)
            {
                return;
            }
            base.SetSelected(deltaIdx);
        }

        private void UpdateLegend()
        {
            string legendKey = null;
            float offsetX = 0f;
            switch (SelectedEntryIndex)
            {
                case 0:
                    legendKey = CanStartGame()
                        ? "Start"
                        : (_configuration.Difficulty == AiDifficulty.None && _players.Count < 2)
                            ? "StartBlockedPlayers"
                            : "StartBlockedReady";
                    offsetX = _startEntry.Sprite.X;
                    break;
                case 1:
                    legendKey = "MapType" + _configuration.MapType.ToString();
                    offsetX = _mapTypeEntry.Sprite.X;
                    break;
                case 2:
                    legendKey = "MapSize" + _configuration.MapSize.ToString();
                    offsetX = _mapSizeEntry.Sprite.X;
                    break;
                case 3:
                    legendKey = "Difficulty" + _configuration.Difficulty.ToString();
                    offsetX = _difficultyEntry.Sprite.X;
                    break;
            }
            _optionsLegend.Text = Resources.ResourceManager.GetString("LobbyLegend" + legendKey);
            _optionsLegend.Position = new Vector2(offsetX, LegendBase.Y);
            _optionsLegend.Color = _net.Session.IsHost ? Color.White : Color.Transparent;
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
                if (player != null && _input.ToggleReady[(int)p].Pressed && !_net.Session.IsLocalSession())
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
                            Guide.ShowSignIn(1, _net.Session.IsOnlineSession());
                        }
                        catch
                        {
                            // ignore whatever guide errors occur
                        }
                    }
                    if (p.IsSignedIn())
                    {
                        if (_net.Session.IsOnlineSession() && !p.CanPlayOnline())
                        {
                            // cannot join this online session
                            MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.NetworkErrorCannotPlayOnline, typeof(LobbyScreen));
                            Stack.Push(messageScreen);
                        }
                        else
                        {
                            // join the existing session
                            // may not succeed if the session is full
                            _net.Session.AddLocalGamer(p.GetSignedInGamer());
                        }
                    }
                }
            }
            if (_net.Session.IsOnlineSession() && _input.Invite.Pressed)
            {
                try
                {
                    Guide.ShowGameInvite(_input.Controller.Value, null);
                }
                catch
                {
                    // ignore whatever guide errors occur
                }
            }
        }

        private StrategyNetworkSession _net = null;
        private List<Player> _players;

        private MenuInput _input;

        private Random _random = new Random();
        private MatchConfigurationManager _configuration;

        private bool _isMatchRunning = false;

        private Sprite _background;
        private Sprite _buttonsLegend;
        private List<PlayerSlot> _slots;
        private MenuEntry _startEntry;
        private CyclingTextMenuEntry _mapTypeEntry;
        private CyclingTextMenuEntry _mapSizeEntry;
        private CyclingTextMenuEntry _difficultyEntry;
        private TextSprite _optionsLegend;
        private float _transitionProgress;

        private static readonly Color CannotStartGameColor = new Color(100, 100, 100);
        private static readonly Vector2 LegendBase = new Vector2(130, 125);
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

        public PlayerSlot(int slotNumber, ImageSprite iconSprite, ContentManager content)
        {
            _backgroundSprite = new ImageSprite(content.Load<Texture2D>("Images/LobbyBox"));
            _backgroundSprite.Position = new Vector2(
                (1280 - _backgroundSprite.Size.X * 2 + 30) / 2,
                (720 - _backgroundSprite.Size.Y * 2 + 30) / 2 - 35);
            if (slotNumber == 1 || slotNumber == 3)
            {
                _backgroundSprite.Position += new Vector2(_backgroundSprite.Size.X + 30, 0);
            }
            if (slotNumber == 2 || slotNumber == 3)
            {
                _backgroundSprite.Position += new Vector2(0, _backgroundSprite.Size.Y + 30);
            }

            _labelImageSprite = new ImageSprite(content.Load<Texture2D>("Images/ButtonA"));
            _labelImageSprite.Origin = new Vector2(
                (int)(_labelImageSprite.Size.X / 2),
                (int)(_labelImageSprite.Size.Y / 2));
            _labelImageSprite.Position = new Vector2(
                (int)(_backgroundSprite.Position.X + 25),
                (int)(_backgroundSprite.Position.Y + (_backgroundSprite.Size.Y - _labelImageSprite.Size.Y) / 2))
                + _labelImageSprite.Origin;

            _labelTextSprite = new TextSprite(content.Load<SpriteFont>("Fonts/TextLight"), Resources.MenuJoin);
            _labelTextSprite.Color = Color.Black;
            _labelTextSprite.Position = new Vector2(
                (int)(_backgroundSprite.Position.X + 25),
                (int)(_backgroundSprite.Position.Y + (_backgroundSprite.Size.Y - _labelTextSprite.Size.Y) / 2))
                + _labelTextSprite.Origin;

            _iconSprite = iconSprite;
            _iconSprite.Position = new Vector2(
                (int)(_backgroundSprite.Position.X + _backgroundSprite.Size.X - _iconSprite.Size.X - 25),
                (int)(_backgroundSprite.Position.Y + (_backgroundSprite.Size.Y - _iconSprite.Size.Y) / 2));
            _iconSprite.Color = Color.Transparent;
        }

        public void Update(float time)
        {
            if (_labelAnimation != null)
            {
                if (!_labelAnimation.Update(time))
                {
                    _labelAnimation = null;
                }
            }
            if (_iconAnimation != null)
            {
                if (!_iconAnimation.Update(time))
                {
                    _iconAnimation = null;
                }
            }
            if (_labelAnimation == null && _player == null)
            {
                _labelAnimation = new SequentialAnimation(
                    new ScaleAnimation(_labelImageSprite, new Vector2(1.1f, 1.1f), 1f, Interpolation.InterpolateVector2(Easing.QuadraticOut)),
                    new ScaleAnimation(_labelImageSprite, Vector2.One, 1f, Interpolation.InterpolateVector2(Easing.QuadraticIn)));
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _backgroundSprite.Draw(spriteBatch);
            _labelImageSprite.Draw(spriteBatch);
            _labelTextSprite.Draw(spriteBatch);
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
            Color newLabelColor = (_player != null) ? Color.Transparent : Color.White;
            _labelAnimation = new SequentialAnimation(
                new CompositeAnimation(
                    new ColorAnimation(_labelImageSprite, Color.Transparent, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)),
                    new ColorAnimation(_labelTextSprite, Color.Transparent, 0.25f, Interpolation.InterpolateColor(Easing.Uniform))),
                new TextAnimation(_labelTextSprite, newLabelText),
                new ScaleAnimation(_labelImageSprite, Vector2.One, 0f, Interpolation.InterpolateVector2(Easing.Uniform)),
                new CompositeAnimation(
                    new ColorAnimation(_labelImageSprite, newLabelColor, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)),
                    new ColorAnimation(_labelTextSprite, Color.Black, 0.25f, Interpolation.InterpolateColor(Easing.Uniform))));

            Color newIconColor = GetIconColor();
            _iconAnimation = new ColorAnimation(_iconSprite, newIconColor, 0.25f, Interpolation.InterpolateColor(Easing.Uniform));
        }

        protected abstract string GetLabelText();

        protected abstract Color GetIconColor();

        private Player _player;

        protected ImageSprite _backgroundSprite;
        protected ImageSprite _labelImageSprite;
        protected TextSprite _labelTextSprite;
        protected ImageSprite _iconSprite;

        protected IAnimation _labelAnimation;
        protected IAnimation _iconAnimation;
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
            return (Player != null) ? _playerId.GetPieceColor() : Color.Transparent;
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
            : base(slotNumber, new ImageSprite(content.Load<Texture2D>("Images/LobbyUnready")), content)
        {
            _unreadyTexture = content.Load<Texture2D>("Images/LobbyUnready");
            _readyTexture = content.Load<Texture2D>("Images/LobbyReady");
        }

        public void UpdateHostStatus()
        {
            _labelTextSprite.Text = GetLabelText();
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
            return (Player != null) ? Color.White : Color.Transparent;
        }

        private void SetReady(bool ready)
        {
            _ready = ready;
            _iconSprite.Texture = _ready ? _readyTexture : _unreadyTexture;
        }

        private bool _ready;

        private Texture2D _readyTexture;
        private Texture2D _unreadyTexture;

        private readonly Color ReadyColor = PlayerId.C.GetPieceColor();
        private readonly Color UnreadyColor = PlayerId.A.GetPieceColor();
        private readonly Color NoPlayerColor = Color.Transparent;
    }
}