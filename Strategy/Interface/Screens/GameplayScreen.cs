﻿using System;
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

        public GameplayScreen(StrategyGame game, NetworkSession session, ICollection<Player> players, Match match)
        {
            _session = session;
            if (_session != null)
            {
                _session.GamerLeft += OnGamerLeft;
                _session.SessionEnded += OnSessionEnded;
            }

            _players = players;

            // create the model
            _lockstepMatch = new LockstepMatch(match);
            _lockstepMatch.Match.TerritoryAttacked += OnTerritoryAttacked;
            _lockstepMatch.Match.PlayerEliminated += OnPlayerEliminated;
            _lockstepMatch.Match.Ended += OnMatchEnded;

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
            _context = new InterfaceContext(game, game.Content, IsoParams);
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

            if (_session != null)
            {
                Gamer primaryGamer = game.Services.GetService<MenuInput>().Controller.Value.GetSignedInGamer();
                LocalInput primaryInput = (LocalInput)players.Single(p => p.Gamer != null ? p.Gamer.Gamertag == primaryGamer.Gamertag : false).Input;
                _instructions = new Instructions(primaryInput, match, _context);
                _instructions.Enabled = (_session.LocalGamers.Count == 1); // only instuct with one local player
            }
            else
            {
                LocalInput primaryInput = (LocalInput)players.ElementAt(0).Input;
                _instructions = new Instructions(primaryInput, match, _context);
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

            if (!_lockstepMatch.Match.IsEnded)
            {
                int milliseconds = gameTime.GetElapsedMilliseconds();
                _lockstepInput.Update(milliseconds);
                _lockstepMatch.Update(milliseconds);
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
                    Stack.Push(_endScreen);
                }
            }
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
            Player player = _players.Single(p => p.Id == args.Player);
            ShowPlayerLeftMatch(player);
            // check for no more human players?
        }

        private void OnTerritoryAttacked(object matchObj, TerritoryAttackedEventArgs args)
        {
            if (Guide.IsTrialMode)
            {
                Match match = _lockstepMatch.Match;
                if (match.RemainingPlayerCount == 2 && args.Successful)
                {
                    PlayerId playerId = args.Defenders.First().Piece.Owner;
                    Player player = _players.Single(p => p.Id == playerId);
                    if (player.Gamer == null) // computer player lost a territory
                    {
                        int remainingCount = match.Map.Territories.Count(t => t.Owner == playerId);
                        if (remainingCount <= 3)
                        {
                            // game is almost over, show the purchase screen
                            PurchaseScreen purchaseScreen = new PurchaseScreen(Stack.Game, Resources.TrialMatchEnd);
                            Stack.Push(purchaseScreen);
                        }
                    }
                }
            }
        }

        private void OnMatchEnded(object matchObj, PlayerEventArgs args)
        {
            Player player = _players.Single(p => p.Id == args.Player);

            _awardments.MatchEnded(player.Id);

            string message = string.Format(Resources.GameWon, player.DisplayName);
            _endScreen = new MessageScreen(Stack.Game, message, typeof(LobbyScreen));
            _endTime = _lockstepMatch.Match.Map.Territories.Max(t => t.Cooldown) / 1000f;
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            Player player = _players.Single(p => p.Gamer == args.Gamer);
            ShowPlayerLeftMatch(player);
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            // if the session ended before the game is over then we encountered an error
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

        private Screen _endScreen;
        private float _endTime;

        private SpriteBatch _spriteBatch;
        private IsometricBatch _isoBatch;
    }
}
