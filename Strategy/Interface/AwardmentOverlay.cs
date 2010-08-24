using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows awardments earned by players.
    /// </summary>
    public class AwardmentOverlay : Screen
    {
        public AwardmentOverlay(StrategyGame game, Awardments awardments)
        {
            awardments.AwardmentEarned += (s, a) => _pendingAwardments.Enqueue(a);
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);
            TransitionOnTime = 0f;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            // draw the awardment
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_displayedAwardment == null && _pendingAwardments.Count > 0)
            {
                _displayedAwardment = _pendingAwardments.Dequeue();
            }
            if (_displayedAwardment != null)
            {
                // animate the awardment
            }
        }

        private Queue<AwardmentEventArgs> _pendingAwardments = new Queue<AwardmentEventArgs>();
        private AwardmentEventArgs _displayedAwardment = null;

        private SpriteBatch _spriteBatch;
    }
}
