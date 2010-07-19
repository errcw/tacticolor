using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library.Extensions;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a match.
    /// </summary>
    public class MatchView
    {
        public MatchView(Match match, Player[] players, InterfaceContext context)
        {
            _match = match;
            _players = players;
            _context = context;

            _mapView = new MapView(match.Map, match, players, context);
            _playerViews = new PlayerView[match.PlayerCount];
            _piecesAvailableViews = new PiecesAvailableView[match.PlayerCount];
            for (int i = 0; i < _piecesAvailableViews.Length; i++)
            {
                _playerViews[i] = new PlayerView(players[i], context);
                _piecesAvailableViews[i] = new PiecesAvailableView(match, (PlayerId)i, context);
            }
            _backgroundView = new BackgroundView(context);
            _spriteBatch = new SpriteBatch(context.Game.GraphicsDevice);
        }

        public void Update(float time)
        {
            _mapView.Update(time);
            _playerViews.ForEach(view => view.Update(time));
            _piecesAvailableViews.ForEach(view => view.Update(time));
        }

        public void Draw()
        {
            _spriteBatch.Begin();
            _backgroundView.Draw(_spriteBatch);
            _spriteBatch.End();

            _mapView.Draw();

            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _playerViews.ForEach(view => view.Draw(_spriteBatch));
            _piecesAvailableViews.ForEach(view => view.Draw(_spriteBatch));
            _spriteBatch.End();
        }

        private Match _match;
        private Player[] _players;
        private InterfaceContext _context;

        private SpriteBatch _spriteBatch;
        private MapView _mapView;
        private PlayerView[] _playerViews;
        private PiecesAvailableView[] _piecesAvailableViews;
        private BackgroundView _backgroundView;
    }
}
