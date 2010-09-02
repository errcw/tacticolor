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
        public GameplayScreen(StrategyGame game, NetworkSession session, ICollection<Player> players, Map map, Random random)
        {
            _session = session;
            if (_session != null)
            {
                _session.GamerLeft += OnGamerLeft;
                _session.SessionEnded += OnSessionEnded;
            }

            _players = players;

            // isometric context
            _context = new InterfaceContext(game, game.Content, new IsometricParameters(17, 9, 16, -9));

            // create the model
            Match match = new Match(map, random);
            match.PlayerEliminated += OnPlayerEliminated;
            match.Ended += OnMatchEnded;

            _lockstepMatch = new LockstepMatch(match);
            foreach (Player player in players)
            {
                if (player.Controller.HasValue)
                {
                    player.Input = new LocalInput(player.Id, player.Controller.Value, match, _context);
                }
            }
            _lockstepInput = new LockstepInput(_lockstepMatch, players);

            IDictionary<string, PlayerId> awardmentPlayers = new Dictionary<string, PlayerId>(match.PlayerCount);
            foreach (Player player in players)
            {
                if (player.Gamer != null && player.Gamer.IsLocal)
                {
                    awardmentPlayers[player.Gamer.Gamertag] = player.Id;
                }
            }
            _awardments = game.Services.GetService<Awardments>();
            _awardments.MatchStarted(awardmentPlayers, match);

            // create the views
            _backgroundView = new BackgroundView(_context);
            _mapView = new MapView(match, _context); // created first to center subsequent views
            _inputViews = new List<LocalInputView>(players.Count);
            _playerViews = new List<PlayerView>(players.Count);
            _piecesAvailableViews = new List<PiecesAvailableView>(players.Count);
            foreach (Player player in players)
            {
                _playerViews.Add(new PlayerView(player, _context));
                _piecesAvailableViews.Add(new PiecesAvailableView(match, player.Id, _context));
                LocalInput input = player.Input as LocalInput;
                if (input != null)
                {
                    input.SelectedChanged += OnSelectionChanged;
                    _inputViews.Add(new LocalInputView(input, _context));
                }
            }

            Gamer primaryGamer = game.Services.GetService<MenuInput>().Controller.Value.GetSignedInGamer();
            LocalInput primaryInput = (LocalInput)players.ElementAt(0).Input;// players.First(p => p.Gamer.Gamertag == primaryGamer.Gamertag).Input;
            _instructions = new Instructions(primaryInput, match, _context);

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
            _isoBatch = new IsometricBatch(_spriteBatch);
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            UpdateInternal(gameTime);
        }

        protected override void UpdateInactive(GameTime gameTime)
        {
            // for local games pause
            if (_session == null || _session.IsLocalSession())
            {
                return;
            }

            // for networked games we can never pause
            UpdateInternal(gameTime);
        }

        private void UpdateInternal(GameTime gameTime)
        {
            if (_session != null)
            {
                _session.Update();
            }

            float seconds = gameTime.GetElapsedSeconds();
            int milliseconds = gameTime.GetElapsedMilliseconds();

            _lockstepInput.Update(milliseconds);
            _lockstepMatch.Update(milliseconds);

            _mapView.Update(seconds);
            _inputViews.ForEach(view => view.Update(seconds));
            _playerViews.ForEach(view => view.Update(seconds));
            _piecesAvailableViews.ForEach(view => view.Update(seconds));
            _instructions.Update(seconds);
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _backgroundView.Draw(_spriteBatch);
            _playerViews.ForEach(view => view.Draw(_spriteBatch));
            _piecesAvailableViews.ForEach(view => view.Draw(_spriteBatch));
            _instructions.Draw(_spriteBatch);
            _spriteBatch.End();

            _isoBatch.Begin();
            _inputViews.ForEach(view => view.Draw(_isoBatch));
            _mapView.Draw(_isoBatch);
            _isoBatch.End();
        }

        protected internal override void Hide(bool popped)
        {
            // unwire the handlers once this screen disappears
            if (_session != null && popped)
            {
                _session.GamerLeft -= OnGamerLeft;
                _session.SessionEnded -= OnSessionEnded;
            }
            // unhook listeners to allow garbage collection
            if (popped)
            {
                _lockstepMatch.Match.ResetEvents();
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
            // update the player view
            PlayerView playerView = _playerViews.First(view => view.Player.Id == args.Player);
            playerView.ShowEliminated();

            // remove the local input view
            _inputViews.RemoveAll(view => view.Input.Player == args.Player);
        }

        private void OnMatchEnded(object matchObj, PlayerEventArgs args)
        {
            Player player = _players.First(p => p.Id == args.Player);

            _awardments.MatchEnded(player.Id);

            string message = string.Format(Resources.GameWon, player.DisplayName);
            MessageScreen messageScreen = new MessageScreen(Stack.Game, message, typeof(LobbyScreen));
            Stack.Push(messageScreen);
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            // have the player sit idle
            Player player = _players.First(p => p.Gamer == args.Gamer);
            player.Gamer = null;
            player.Controller = null;
            player.Input = null;

            // update the player view
            PlayerView playerView = _playerViews.Find(view => view.Player == player);
            playerView.ShowDropped();

            // remove the local input view
            _inputViews.RemoveAll(view => view.Input.Player == player.Id);
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            // if the session ended before the game is over then we encountered an error
            MessageScreen messageScreen = new MessageScreen(Stack.Game, Resources.NetworkError);
            Stack.Push(messageScreen);
        }

        private NetworkSession _session;

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

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
    }
}
