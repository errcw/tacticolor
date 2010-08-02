using System;
using System.Collections.Generic;

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
        public MatchView(Match match, ICollection<Player> players, InterfaceContext context)
        {
            _match = match;
            _context = context;

            _mapView = new MapView(match.Map, match, players, context);
            _playerViews = new PlayerView[match.PlayerCount];
            _piecesAvailableViews = new PiecesAvailableView[match.PlayerCount];
            int i = 0;
            foreach (Player player in players)
            {
                _playerViews[i] = new PlayerView(player, context);
                _piecesAvailableViews[i] = new PiecesAvailableView(match, (PlayerId)i, context);
                i += 1;
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
        private InterfaceContext _context;

        private SpriteBatch _spriteBatch;
        private MapView _mapView;
        private PlayerView[] _playerViews;
        private PiecesAvailableView[] _piecesAvailableViews;
        private BackgroundView _backgroundView;
    }
}
