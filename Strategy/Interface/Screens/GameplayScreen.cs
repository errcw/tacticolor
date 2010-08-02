using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    public class GameplayScreen : Screen
    {
        public GameplayScreen(StrategyGame game, ICollection<Player> players, Map map, Random random)
        {
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
            _isoBatch = new IsometricBatch(_spriteBatch);

            _context = new InterfaceContext(game, game.Content, new IsometricParameters(17, 9, 16, -9));

            // create the model
            Match match = new Match(map, random);
            _lockstepMatch = new LockstepMatch(match);
            foreach (Player player in players)
            {
                if (player.Controller.HasValue)
                {
                    player.Input = new LocalInput(player.Id, player.Controller.Value, match, _context);
                }
            }
            _lockstepInput = new LockstepInput(_lockstepMatch, players);

            // create the view
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
