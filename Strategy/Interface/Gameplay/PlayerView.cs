using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Properties;
using Strategy.Library;
using Strategy.Library.Extensions;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Shows a player.
    /// </summary>
    public class PlayerView
    {
        public Player Player { get; private set; }

        public PlayerView(Player player, LockstepMatch match, InterfaceContext context)
        {
            Player = player;
            _match = match;

            Vector2 position = GetBasePosition(Player.Id);
            SpriteFont font = context.Content.Load<SpriteFont>("Fonts/Gamertag");
            string name = Player.DisplayName;
            float nameWidth = font.MeasureString(name).X;

            _name = new TextSprite(font, name);
            _name.Color = Color.White;
            _name.Position = position;
            _name.Effect = TextSprite.TextEffect.Shadow;
            _name.EffectColor = new Color(30, 30, 30, 160);
            _name.EffectSize = 1;

            Texture2D voiceTex = context.Content.Load<Texture2D>("Images/Voice");
            _voiceSprite = new ImageSprite(voiceTex);
            _voiceSprite.Position = _name.Position + new Vector2(nameWidth + 5, 5);
            _voiceSprite.Color = Color.Transparent;

            Texture2D lagTex = context.Content.Load<Texture2D>("Images/Lag");
            _lagSprite = new ImageSprite(lagTex);
            _lagSprite.Position = _voiceSprite.Position + new Vector2(_voiceSprite.Size.X + 5, 0);
            _lagSprite.Color = Color.Transparent;
        }

        public void Update(float time)
        {
            if (_nameAnimation != null)
            {
                if (!_nameAnimation.Update(time))
                {
                    _nameAnimation = null;
                }
            }

            if (_lagAnimation != null)
            {
                if (!_lagAnimation.Update(time))
                {
                    _lagAnimation = null;
                }
            }

            if (Player.Gamer != null && Player.Gamer.HasVoice)
            {
                float alpha = Player.Gamer.IsTalking ? 1f : Player.Gamer.IsMutedByLocalUser ? 0.1f : 0.25f;
                _voiceSprite.Color = ColorExtensions.FromNonPremultiplied(Color.White, alpha);
            }
            else
            {
                _voiceSprite.Color = Color.Transparent;
            }

            if (_match.BlockingPlayers.Contains(Player.Id))
            {
                _blockedUpdateCount += 1;
            }
            else
            {
                _blockedUpdateCount = 0;
                _lagSprite.Color = Color.Transparent;
                _lagAnimation = null;
            }
            if (_blockedUpdateCount > BlockedUpdateLagThreshold && _lagAnimation == null)
            {
                _lagAnimation = new SequentialAnimation(
                    new ColorAnimation(_lagSprite, ColorExtensions.FromNonPremultiplied(Color.White, 0.5f), 0.25f, Interpolation.InterpolateColor(Easing.Uniform)),
                    new ColorAnimation(_lagSprite, Color.White, 0.25f, Interpolation.InterpolateColor(Easing.Uniform)));
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _name.Draw(spriteBatch);
            _voiceSprite.Draw(spriteBatch);
            _lagSprite.Draw(spriteBatch);
        }

        /// <summary>
        /// Shows that this player was eliminated from the match.
        /// </summary>
        public void ShowLeft()
        {
            _nameAnimation = new ColorAnimation(_name, new Color(176, 176, 176), 1f, Interpolation.InterpolateColor(Easing.Uniform));
        }

        /// <summary>
        /// Returns the position at which to start drawing.
        /// </summary>
        private Vector2 GetBasePosition(PlayerId player)
        {
            const int BaseX = 80;
            const int BaseY = 50;
            const int SpacingY = 100;
            switch (player)
            {
                case PlayerId.A: return new Vector2(BaseX, BaseY);
                case PlayerId.B: return new Vector2(BaseX, BaseY + SpacingY);
                case PlayerId.C: return new Vector2(BaseX, 720 - 25 - BaseY - SpacingY);
                case PlayerId.D: return new Vector2(BaseX, 720 - 25 - BaseY);
                default: throw new ArgumentException("Invalid player id " + player);
            }
        }

        private TextSprite _name;
        private ImageSprite _voiceSprite;
        private ImageSprite _lagSprite;

        private IAnimation _nameAnimation;
        private IAnimation _lagAnimation;

        private int _blockedUpdateCount = 0;
        private LockstepMatch _match;

        private const int BlockedUpdateLagThreshold = 15;
    }
}
