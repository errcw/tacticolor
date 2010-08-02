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

            _backgroundView = new BackgroundView(context);
            _mapView = new MapView(match.Map, match, players, context);
            _piecesAvailableViews = new List<PiecesAvailableView>(match.PlayerCount);
            players.ForEach(player => _piecesAvailableViews.Add(new PiecesAvailableView(match, player.Id, context)));

            _spriteBatch = new SpriteBatch(context.Game.GraphicsDevice);
        }

        public void Update(float time)
        {
            _mapView.Update(time);
            _piecesAvailableViews.ForEach(view => view.Update(time));
        }

        public void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _backgroundView.Draw(_spriteBatch);
            _spriteBatch.End();

            _mapView.Draw();

            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _piecesAvailableViews.ForEach(view => view.Draw(_spriteBatch));
            _spriteBatch.End();
        }

        private Match _match;
        private InterfaceContext _context;

        private SpriteBatch _spriteBatch;
        private BackgroundView _backgroundView;
        private MapView _mapView;
        private ICollection<PiecesAvailableView> _piecesAvailableViews;
    }
}
