using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;
using Strategy.Properties;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows awardments earned by players.
    /// </summary>
    public class AwardmentOverlay : Screen
    {
        public AwardmentOverlay(StrategyGame game, Awardments awardments)
        {
            awardments.AwardmentEarned += (s, a) => _pendingAwardments.Enqueue(a.Awardment);

            _imageTextures = new Texture2D[4];
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                _imageTextures[(int)p] = game.Content.Load<Texture2D>("Images/AwardmentPlayer" + (int)p);
            }
            _panel = new SlidingPanel(Resources.AwardmentTitle, _imageTextures[0], 720 - 40 - 75, game.Content);
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            TransitionOnTime = 0f;
            TransitionOffTime = 0f;
            ShowBeneath = true;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _panel.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            float time = gameTime.GetElapsedSeconds();

            if (_displayedAwardment != null)
            {
                _displayTime -= time;
                if (_displayTime <= 0)
                {
                    _displayedAwardment = null;
                    _panel.Hide();
                }
            }
            if (_displayedAwardment == null && _pendingAwardments.Count > 0)
            {
                _displayedAwardment = _pendingAwardments.Dequeue();
                _displayTime = DisplayTime;

                string text = string.Format(Resources.AwardmentTitle, _displayedAwardment.Name);
                Texture2D image = _imageTextures[(int)GetIndexForGamertag(_displayedAwardment.OwnerGamertag)];
                _panel.Show(text, image);
            }

            _panel.Update(time);
        }

        private PlayerIndex GetIndexForGamertag(string gamertag)
        {
            foreach (SignedInGamer gamer in SignedInGamer.SignedInGamers)
            {
                if (gamer.Gamertag == gamertag)
                {
                    return gamer.PlayerIndex;
                }
            }
            return PlayerIndex.One;
        }

        private Queue<Awardment> _pendingAwardments = new Queue<Awardment>();
        private Awardment _displayedAwardment = null;
        private float _displayTime;

        private SlidingPanel _panel;
        private Texture2D[] _imageTextures;
        private SpriteBatch _spriteBatch;

        private const float DisplayTime = 4f;
    }
}
