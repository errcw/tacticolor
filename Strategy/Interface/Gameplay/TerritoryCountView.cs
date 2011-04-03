using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Animation;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
{
    public class TerritoryCountView
    {
        public TerritoryCountView(Match match, PlayerId player, InterfaceContext context)
        {
            _match = match;
            _player = player;

            _lastCount = _match.TerritoriesOwnedCount[(int)_player];

            Texture2D tileTex = context.Content.Load<Texture2D>("Images/Tile");
            SpriteFont countFont = context.Content.Load<SpriteFont>("Fonts/TextLarge");
            Vector2 position = GetBasePosition(player);

            _tileSprite = new ImageSprite(tileTex);
            _tileSprite.Position = position;
            _tileSprite.Color = player.GetTerritoryColor();

            _countSprite = new TextSprite(countFont, _lastCount.ToString());
            _countSprite.Position = position + new Vector2(_tileSprite.Size.X + 5, 3);
            _countSprite.Effect = TextSprite.TextEffect.Shadow;
            _countSprite.EffectColor = new Color(30, 30, 30, 160);
            _countSprite.EffectSize = 1;
        }

        public void Update(float time)
        {
            int currentCount = _match.TerritoriesOwnedCount[(int)_player];
            if (_lastCount != currentCount)
            {
                _lastCount = currentCount;
                _animation = new SequentialAnimation(
                    new ColorAnimation(_countSprite, Color.Transparent, 0.2f, Interpolation.InterpolateColor(Easing.Uniform)),
                    new TextAnimation(_countSprite, currentCount.ToString()),
                    new ColorAnimation(_countSprite, Color.White, 0.2f, Interpolation.InterpolateColor(Easing.Uniform)));
            }

            if (_animation != null)
            {
                if (!_animation.Update(time))
                {
                    _animation = null;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _tileSprite.Draw(spriteBatch);
            _countSprite.Draw(spriteBatch);
        }

        /// <summary>
        /// Returns the position at which to start drawing.
        /// </summary>
        private Vector2 GetBasePosition(PlayerId player)
        {
            const int BaseX = 80;
            const int BaseY = 300;
            const int SpacingY = 40;
            return new Vector2(BaseX, BaseY + (int)player * SpacingY);
        }

        private Match _match;
        private PlayerId _player;

        private int _lastCount;

        private ImageSprite _tileSprite;
        private TextSprite _countSprite;
        private IAnimation _animation;
    }
}
