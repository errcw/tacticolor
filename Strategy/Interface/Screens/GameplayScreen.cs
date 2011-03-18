using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Interface.Gameplay;
using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    public class GameplayScreen : Screen
    {
        /// <summary>
        /// Isometric parameters used to display the map. Hack.
        /// </summary>
        public static readonly IsometricParameters IsoParams = new IsometricParameters(17, 9, 16, -9);

        public GameplayScreen(Game game, StrategyNetworkSession net, ICollection<Player> players, Match match)
        {
            _net = net;
            if (_net != null)
            {
                _net.Session.GamerLeft += OnGamerLeft;
                _net.Session.SessionEnded += OnSessionEnded;
            }

            _players = players;

            _input = game.Services.GetService<MenuInput>();

            // create the model
            _lockstepMatch = new LockstepMatch(match);
            _lockstepMatch.Match.PlayerEliminated += OnPlayerEliminated;
            _lockstepMatch.Match.Ended += OnMatchEndedWithWinner;

            if (_net.Session.IsLocalSession())
            {
                // local matches should execute commands immediately
                _lockstepMatch.SchedulingOffset = 0;
                _lockstepMatch.StepTime = int.MaxValue;
            }

            _lockstepInput = new LockstepInput(_lockstepMatch, players, _net);

            // while the game is starting send out start game synchronization commands
            // by the time the commands arrive the game should be ready
            _lockstepInput.OnGameWillStart();

            IDictionary<string, PlayerId> awardmentPlayers = new Dictionary<string, PlayerId>(match.PlayerCount);
            foreach (Player player in players)
            {
                if (player.Gamer != null && player.Gamer.IsLocal)
                {
                    awardmentPlayers[player.Gamer.Gamertag] = player.Id;
                }
            }
            _awardments = game.Services.GetService<Awardments>();
            _awardments.MatchStarted(match, awardmentPlayers);

            // create the views
            _context = new InterfaceContext(game, game.Content, IsoParams, players);
            _backgroundView = new BackgroundView(_context);
            _mapView = new MapView(match, _context); // created first to center subsequent views
            _inputViews = new List<LocalInputView>(players.Count);
            _playerViews = new List<PlayerView>(players.Count);
            _piecesAvailableViews = new List<PiecesAvailableView>(players.Count);
            foreach (Player player in players)
            {
                _playerViews.Add(new PlayerView(player, _lockstepMatch, _context));
                _piecesAvailableViews.Add(new PiecesAvailableView(match, player.Id, _context));
                LocalInput input = player.Input as LocalInput;
                if (input != null)
                {
                    input.SelectedChanged += OnSelectionChanged;
                    input.ControllerDisconnected += OnControllerDisconnected;
                    _inputViews.Add(new LocalInputView(input, _context));
                }
            }

            if (_net != null)
            {
                Gamer primaryGamer = game.Services.GetService<MenuInput>().Controller.Value.GetSignedInGamer();
                LocalInput primaryInput = (LocalInput)players.Single(p => p.Gamer != null ? p.Gamer.Gamertag == primaryGamer.Gamertag : false).Input;
                _instructions = new Instructions(primaryInput, match, _context);
                _instructions.Enabled = (_net.Session.LocalGamers.Count == 1); // only instuct with one local player
            }
            else
            {
                LocalInput primaryInput = (LocalInput)players.ElementAt(0).Input;
                _instructions = new Instructions(primaryInput, match, _context);
            }

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
            _isoView = new IsometricView();

            TransitionOnTime = 0.5f;
            TransitionOffTime = 0.5f;
            StateChanged += (s, a) => ShowBeneath = (State == ScreenState.TransitionOn || State == ScreenState.TransitionOff);
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            UpdateInternal(gameTime, true);
        }

        protected override void UpdateInactive(GameTime gameTime)
        {
            // for local games pause
            if (_net == null || _net.Session.IsLocalSession())
            {
                return;
            }

            // for networked games we can never pause
            UpdateInternal(gameTime, false);
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

        private void UpdateInternal(GameTime gameTime, bool isActive)
        {
            if (_net != null)
            {
                _net.Update();
            }

            if (!_isEnded)
            {
                try
                {
                    int milliseconds = gameTime.GetElapsedMilliseconds();
                    _lockstepInput.Update(milliseconds, !isActive);
                    _lockstepMatch.Update(milliseconds);
                }
                catch (OutOfSyncException oos)
                {
                    Debug.WriteLine(oos);
                    HandleNetworkError();
                }
            }

            float seconds = gameTime.GetElapsedSeconds();
            _mapView.Update(seconds);
            _inputViews.ForEach(view => view.Update(seconds));
            _playerViews.ForEach(view => view.Update(seconds));
            _piecesAvailableViews.ForEach(view => view.Update(seconds));
            _instructions.Update(seconds);

            if (_endScreen != null)
            {
                _endTime -= seconds;
                if (_endTime <= 0f)
                {
                    Stack.PushOn(_endScreen, this);
                    _endScreen = null; // do not push multiple times
                }
            }

            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                if (_input.Activate[(int)p].Pressed)
                {
                    InGameMenuScreen menuScreen = new InGameMenuScreen(Stack.Game, p);
                    Stack.Push(menuScreen);
                    break;
                }
            }

            if (_input.Debug.Pressed)
            {
                // game is almost over, show the purchase screen
                _endScreen = new PurchaseScreen(Stack.Game, Resources.TrialMatchEnd, typeof(LobbyScreen));
                _endTime = 0f;
            }
        }

        public override void Draw()
        {
            // rebuild the isometric view before drawing anything
            _isoView.Clear();
            _inputViews.ForEach(view => view.Draw(_isoView));
            _mapView.Draw(_isoView);

            float slidePosition = 1280 * (1 - _transitionProgress);
            Matrix m = Matrix.CreateTranslation(slidePosition, 0f, 0f);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, m);

            _backgroundView.Draw(_spriteBatch);
            _isoView.Draw(_spriteBatch);
            _playerViews.ForEach(view => view.Draw(_spriteBatch));
            _piecesAvailableViews.ForEach(view => view.Draw(_spriteBatch));
            _instructions.Draw(_spriteBatch);

            _spriteBatch.End();
        }

        protected internal override void Hide(bool popped)
        {
            if (popped)
            {
                // unwire the handlers once this screen disappears
                if (_net != null)
                {
                    _net.Session.GamerLeft -= OnGamerLeft;
                    _net.Session.SessionEnded -= OnSessionEnded;
                }

                // notify the awardments the match was abandonned
                if (!_isEnded)
                {
                    _awardments.MatchEnded(null);
                }
            }
            base.Hide(popped);
        }

        private void OnSelectionChanged(object inputObj, InputChangedEventArgs args)
        {
            LocalInput input = (LocalInput)inputObj;
            _mapView.ShowSelectionChanged(args.PreviousInput, input.Selected);
        }

        private void OnPlayerEliminated(object matchObj, PlayerEventArgs args)
        {
            Player player = _players.Single(p => p.Id == args.Player);
            ShowPlayerLeftMatch(player);

            // check if all the human players have been defeated
            int maxHumanTerritories = _players.Where(p => p.Gamer != null).Max(p => _lockstepMatch.Match.TerritoriesOwnedCount[(int)p.Id]);
            if (maxHumanTerritories == 0)
            {
                PlayerId aiWinner = _players.First(p => p.Gamer == null).Id; // have an arbitrary AI player "win"
                OnMatchEnded(Resources.GameLost, aiWinner);
            }

            // stop the match before it finishes in trial mode
            Match match = _lockstepMatch.Match;
            if (Guide.IsTrialMode && match.RemainingPlayerCount == 2)
            {
                // game is almost over, show the purchase screen
                _endScreen = new PurchaseScreen(Stack.Game, Resources.TrialMatchEnd, typeof(LobbyScreen));
                _endTime = 0f;
            }
        }

        private void OnMatchEndedWithWinner(object matchObj, PlayerEventArgs args)
        {
            Player winner = _players.Single(p => p.Id == args.Player);
            OnMatchEnded(string.Format(Resources.GameWon, winner.DisplayName), winner.Id);
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            Player player = _players.Single(p => p.Gamer == args.Gamer);
            _lockstepInput.OnPlayerLeft(player);
            ShowPlayerLeftMatch(player);
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            // if the session ended before the match ended then we encountered an error
            HandleNetworkError();
        }

        private void OnMatchEnded(string message, PlayerId winner)
        {
            if (_net != null && _net.Session.IsHost)
            {
                _net.EndGame();
            }

            _awardments.MatchEnded(winner);

            _endScreen = new MessageScreen(Stack.Game, message, typeof(LobbyScreen));
            _endTime = _lockstepMatch.Match.Map.Territories.Max(t => t.Cooldown) / 1000f;
            _isEnded = true;
        }

        private void OnControllerDisconnected(object sender, EventArgs args)
        {
            if (_net.Session.IsLocalSession())
            {
                Player player = _players.Single(p => p.Id == ((LocalInput)sender).Player);
                InGameMenuScreen menuScreen = new InGameMenuScreen(Stack.Game, player.Controller.Value);
                Stack.Push(menuScreen);
            }
        }

        private void HandleNetworkError()
        {
            _net = null;
            _isEnded = true;

            MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.NetworkError);
            Stack.Push(messageScreen);
        }

        private void ShowPlayerLeftMatch(Player player)
        {
            // have the player sit idle
            player.Input = null;

            // update the player view
            PlayerView playerView = _playerViews.Single(view => view.Player == player);
            playerView.ShowLeft();

            // hide the pieces view
            PiecesAvailableView piecesView = _piecesAvailableViews.Single(view => view.Player == player.Id);
            piecesView.Hide();

            // hide the local input view
            LocalInputView inputView = _inputViews.FirstOrDefault(view => view.Input.Player == player.Id);
            if (inputView != null)
            {
                inputView.Hide();
            }
        }

        private StrategyNetworkSession _net;

        private Awardments _awardments;

        private ICollection<Player> _players;
        private LockstepInput _lockstepInput;
        private LockstepMatch _lockstepMatch;

        private InterfaceContext _context;
        private BackgroundView _backgroundView;
        private MapView _mapView;
        private List<LocalInputView> _inputViews;
        private List<PlayerView> _playerViews;
        private List<PiecesAvailableView> _piecesAvailableViews;
        private Instructions _instructions;

        private MenuInput _input;

        private Screen _endScreen;
        private float _endTime;
        private bool _isEnded;

        private SpriteBatch _spriteBatch;
        private IsometricView _isoView;

        private float _transitionProgress;
    }
}
