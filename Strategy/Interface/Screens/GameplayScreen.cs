﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            if (_session != null)
            {
                _session.GamerLeft += OnGamerLeft;
                _session.SessionEnded += OnSessionEnded;
            }

            _players = players;

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
            _context = new InterfaceContext(game, game.Content, new IsometricParameters(17, 9, 16, -9));
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

            // dispose the session after the match over screen is done
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            // make the player locally controlled by an AI player
            Player player = _players.Where(player.Gamer == args.Gamer).First();
            player.Gamer = null;
            player.Controller = null;
            player.Input = new AIInput();

            // remove the local input view
            _inputViews.RemoveAll(view => view.Input.Player == player);

            // update the player view
            PlayerView playerView = _playerViews.Where(view => view.Player == player);
            playerView.ShowDropped();
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            // if the session ended before the game is over then we encountered an error
        }

        private NetworkSession _session;

        private MenuInput _input;

        private ICollection<Player> _players;
        private LockstepInput _lockstepInput;
        private LockstepMatch _lockstepMatch;

        private InterfaceContext _context;
        private MatchView _matchView;
        private ICollection<LocalInputView> _inputViews;
        private ICollection<PlayerView> _playerViews;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
    }
}
