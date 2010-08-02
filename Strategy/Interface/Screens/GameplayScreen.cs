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
            _isoBatch = new IsometricBatch(new SpriteBatch(game.GraphicsDevice));
            _context = new InterfaceContext(game, game.Content, new IsometricParameters(17, 9, 16, -9));

            // create the model
            Match match = new Match(map, random);
            _lockstepMatch = new LockstepMatch(match);
            foreach (Player player in players)
            {
                if (player.Controller.HasValue)
                {
                    LocalInput input = new LocalInput(player.Id, match, _context);
                    input.Controller = player.Controller.Value;
                    player.Input = input;
                }
            }
            _lockstepInput = new LockstepInput(_lockstepMatch, players);

            // create the view
            _matchView = new MatchView(match, players, _context);
            _inputViews = new List<LocalInputView>(players.Count);
            foreach (Player player in players)
            {
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

            _lockstepMatch.Update(milliseconds);
            _lockstepInput.Update(milliseconds);

            _matchView.Update(seconds);
            foreach (LocalInputView inputView in _inputViews)
            {
                inputView.Update(seconds);
            }
        }

        public override void Draw()
        {
            _matchView.Draw();
            _isoBatch.Begin();
            foreach (LocalInputView inputView in _inputViews)
            {
                inputView.Draw(_isoBatch);
            }
            _isoBatch.End();
        }

        private InterfaceContext _context;

        private LockstepInput _lockstepInput;
        private LockstepMatch _lockstepMatch;

        private MatchView _matchView;
        private List<LocalInputView> _inputViews;
        private IsometricBatch _isoBatch;
    }
}
