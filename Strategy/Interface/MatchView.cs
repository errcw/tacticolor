using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a match.
    /// </summary>
    public class MatchView
    {
        public MatchView(Match match, InterfaceContext context)
        {
            _match = match;
            _context = context;

            _mapView = new MapView(match.Map, match, context);
            _piecesAvailableViews = new PiecesAvailableView[match.Players];
            for (int i = 0; i < _piecesAvailableViews.Length; i++)
            {
                _piecesAvailableViews[i] = new PiecesAvailableView(match, (PlayerId)i, context);
            }
            _backgroundView = new BackgroundView(context);
            _spriteBatch = new SpriteBatch(context.Game.GraphicsDevice);
        }

        public void Update(float time)
        {
            _mapView.Update(time);
            for (int i = 0; i < _piecesAvailableViews.Length; i++)
            {
                _piecesAvailableViews[i].Update(time);
            }
        }

        public void Draw()
        {
            _spriteBatch.Begin();
            _backgroundView.Draw(_spriteBatch);
            _spriteBatch.End();

            _mapView.Draw();

            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.None);
            for (int i = 0; i < _piecesAvailableViews.Length; i++)
            {
                _piecesAvailableViews[i].Draw(_spriteBatch);
            }
            _spriteBatch.End();
        }

        private Match _match;
        private InterfaceContext _context;

        private SpriteBatch _spriteBatch;
        private MapView _mapView;
        private PiecesAvailableView[] _piecesAvailableViews;
        private BackgroundView _backgroundView;
    }
}
