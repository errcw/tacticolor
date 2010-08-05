using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    public class GameplayScreen : Screen
    {
        public GameplayScreen(StrategyGame game, NetworkSession session, ICollection<Player> players, Map map, Random random)
        {
            _session = session;

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
            _isoBatch = new IsometricBatch(_spriteBatch);

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
            _matchView = new MatchView(match, players, _context);
            _inputViews = new List<LocalInputView>(players.Count);
            _playerViews = new List<PlayerView>(players.Count);
            foreach (Player player in players)
            {
                _playerViews.Add(new PlayerView(player, _context));
                LocalInput input = player.Input as LocalInput;
                if (input != null)
                {
                    _inputViews.Add(new LocalInputView(input, _context));
                }
            }
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_session != null)
            {
                _session.Update();
            }

            float seconds = gameTime.GetElapsedSeconds();
            int milliseconds = gameTime.GetElapsedMilliseconds();

            _lockstepInput.Update(milliseconds);
            _lockstepMatch.Update(milliseconds);

            _matchView.Update(seconds);
            _inputViews.ForEach(view => view.Update(seconds));
            _playerViews.ForEach(view => view.Update(seconds));
        }

        public override void Draw()
        {
            _matchView.Draw();

            _spriteBatch.Begin();
            _playerViews.ForEach(view => view.Draw(_spriteBatch));
            _spriteBatch.End();

            _isoBatch.Begin();
            _inputViews.ForEach(view => view.Draw(_isoBatch));
            _isoBatch.End();
        }

        private void OnPlayerEliminated(object matchObj, PlayerEventArgs args)
        {
            PlayerView playerView = _playerViews.First(view => view.Player.Id == args.Player);
            playerView.ShowEliminated();
        }

        private void OnMatchEnded(object matchObj, PlayerEventArgs args)
        {
            MatchOverScreen matchOverScreen = new MatchOverScreen((StrategyGame)Stack.Game, args.Player);
            Stack.Push(matchOverScreen);
        }

        private NetworkSession _session;

        private InterfaceContext _context;

        private LockstepInput _lockstepInput;
        private LockstepMatch _lockstepMatch;

        private MatchView _matchView;
        private ICollection<LocalInputView> _inputViews;
        private ICollection<PlayerView> _playerViews;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
    }
}
