using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
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

            MenuInput i = Stack.Game.Services.GetService<MenuInput>();
            if (i.Action.Released)
            {
                OnSessionEnded(this, null);
            }

            _mapView.Update(seconds);
            _inputViews.ForEach(view => view.Update(seconds));
            _playerViews.ForEach(view => view.Update(seconds));
            _piecesAvailableViews.ForEach(view => view.Update(seconds));
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _backgroundView.Draw(_spriteBatch);
            _playerViews.ForEach(view => view.Draw(_spriteBatch));
            _piecesAvailableViews.ForEach(view => view.Draw(_spriteBatch));
            _spriteBatch.End();

            _isoBatch.Begin();
            _inputViews.ForEach(view => view.Draw(_isoBatch));
            _mapView.Draw(_isoBatch);
            _isoBatch.End();
        }

        protected internal override void Hide(bool popped)
        {
            // unwire the handlers once this screen is disappears
            if (_session != null && popped)
            {
                _session.GamerLeft -= OnGamerLeft;
                _session.SessionEnded -= OnSessionEnded;
            }
        }

        private void OnSelectionChanged(object inputObj, InputChangedEventArgs args)
        {
            LocalInput input = (LocalInput)inputObj;
            _mapView.ShowSelectionChanged(args.PreviousInput, input.Selected);
        }

        private void OnPlayerEliminated(object matchObj, PlayerEventArgs args)
        {
            PlayerView playerView = _playerViews.First(view => view.Player.Id == args.Player);
            playerView.ShowEliminated();
        }

        private void OnMatchEnded(object matchObj, PlayerEventArgs args)
        {
            Player player = _players.First(p => p.Id == args.Player);
            string message = string.Format(Resources.GameWon, player.DisplayName);
            MessageScreen messageScreen = new MessageScreen(Stack.Game, message, typeof(LobbyScreen));
            Stack.Push(messageScreen);
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            // make the player locally controlled by an AI player
            Player player = _players.First(p => p.Gamer == args.Gamer);
            player.Gamer = null;
            player.Controller = null;
            player.Input = new AIInput();

            // update the player view
            PlayerView playerView = _playerViews.Find(view => view.Player == player);
            playerView.ShowDropped();

            // remove the local input view
            LocalInputView inputView = _inputViews.Find(view => view.Input.Player == player.Id);
            if (inputView != null)
            {
                _inputViews.Remove(inputView);
            }
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            // if the session ended before the game is over then we encountered an error
            MessageScreen messageScreen = new MessageScreen(_context.Game, Resources.NetworkError);
            Stack.Push(messageScreen);
        }

        private NetworkSession _session;

        private ICollection<Player> _players;
        private LockstepInput _lockstepInput;
        private LockstepMatch _lockstepMatch;

        private InterfaceContext _context;
        private BackgroundView _backgroundView;
        private MapView _mapView;
        private List<LocalInputView> _inputViews;
        private List<PlayerView> _playerViews;
        private List<PiecesAvailableView> _piecesAvailableViews;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
    }
}
