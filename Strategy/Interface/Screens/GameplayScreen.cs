using System;

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
        public GameplayScreen(StrategyGame game)
        {
            _isoBatch = new IsometricBatch(new SpriteBatch(game.GraphicsDevice));
            _context = new InterfaceContext(game, game.Content, new IsometricParameters(17, 9, 16, -9));

            _random = new Random();
            _generator = new MapGenerator(_random);
            StartNewMatch();
        }

        private void StartNewMatch()
        {
            // create the model
            Map map = _generator.Generate(16, DebugPlayers, 4, 16);
            Match match = new Match(map, _random);
            LocalInput[] inputs = new LocalInput[DebugPlayers];
            for (int p = 0; p < inputs.Length; p++)
            {
                inputs[p] = new LocalInput((PlayerId)p, match, _context);
                inputs[p].Controller = (PlayerIndex)p;
            }
            Player[] players = new Player[DebugPlayers];
            for (int p = 0; p < players.Length; p++)
            {
                players[p] = new Player();
                players[p].Id = (PlayerId)p;
                players[p].Input = inputs[p];
            }
            _lockstepMatch = new LockstepMatch(match);
            _lockstepInput = new LockstepInput(_lockstepMatch, players);

            // then the view
            _matchView = new MatchView(match, players, _context);
            _inputViews = new LocalInputView[DebugPlayers];
            for (int p = 0; p < inputs.Length; p++)
            {
                _inputViews[p] = new LocalInputView(inputs[p], _context);
            }
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            float seconds = gameTime.GetElapsedSeconds();
            int milliseconds = gameTime.GetElapsedMilliseconds();

            _lockstepMatch.Update(milliseconds);
            _lockstepInput.Update(milliseconds);

            _matchView.Update(seconds);
            for (int p = 0; p < _inputViews.Length; p++)
            {
                _inputViews[p].Update(seconds);
            }
        }

        public override void Draw()
        {
            _matchView.Draw();
            _isoBatch.Begin();
            for (int p = 0; p < _inputViews.Length; p++)
            {
                _inputViews[p].Draw(_isoBatch);
            }
            _isoBatch.End();
        }

        private InterfaceContext _context;

        private Random _random;
        private MapGenerator _generator;

        private LockstepInput _lockstepInput;
        private LockstepMatch _lockstepMatch;

        private MatchView _matchView;
        private LocalInputView[] _inputViews;
        private IsometricBatch _isoBatch;

        private const int DebugPlayers = 2;
    }
}
