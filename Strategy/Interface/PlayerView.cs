using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Properties;
using Strategy.Library;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a player.
    /// </summary>
    public class PlayerView
    {
        public Player Player { get; private set; }

        public PlayerView(Player player, InterfaceContext context)
        {
            Player = player;

            Vector2 position = GetBasePosition(Player.Id);
            string name = GetDisplayName(Player);
            SpriteFont font = context.Content.Load<SpriteFont>("Fonts/Gamertag");
            float nameWidth = font.MeasureString(name).X;

            _name = new TextSprite(font, name);
            _name.Color = Color.White;
            _name.Position = position;

            _nameShadow = new TextSprite(font, name);
            _nameShadow.Color = new Color(30, 30, 30, 160);
            _nameShadow.Position = position + new Vector2(1, 1);

            Texture2D voiceTex = context.Content.Load<Texture2D>("Images/Voice");
            _voiceSprite = new ImageSprite(voiceTex);
            _voiceSprite.Position = position + new Vector2(nameWidth + 5, 5);
            _voiceSprite.Color = new Color(Color.White, 128);
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
            if (Player.Gamer != null && Player.Gamer.HasVoice)
            {
                byte alpha = (byte)(Player.Gamer.IsTalking ? 255 : Player.Gamer.IsMutedByLocalUser ? 32 : 64);
                _voiceSprite.Color = new Color(Color.White, alpha);
            }
            else
            {
                _voiceSprite.Color = Color.TransparentWhite;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _nameShadow.Draw(spriteBatch);
            _name.Draw(spriteBatch);
            if (_voiceSprite != null)
            {
                _voiceSprite.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Shows that this player was eliminated from the match.
        /// </summary>
        public void ShowEliminated()
        {
            _nameAnimation = new CompositeAnimation(
                new ColorAnimation(_name, new Color(176, 176, 176), 1f, Interpolation.InterpolateColor(Easing.Uniform)),
                new ColorAnimation(_nameShadow, new Color(64, 64, 64, 64), 1f, Interpolation.InterpolateColor(Easing.Uniform)));
        }

        /// <summary>
        /// Shows that this previously human player was dropped from the match.
        /// </summary>
        public void ShowDropped()
        {
            string newDisplayName = GetDisplayName(Player);

            _nameAnimation = new SequentialAnimation(
                new CompositeAnimation(
                    new ColorAnimation(_name, Color.TransparentWhite, 1f, Interpolation.InterpolateColor(Easing.Uniform)),
                    new ColorAnimation(_nameShadow, Color.TransparentWhite, 1f, Interpolation.InterpolateColor(Easing.Uniform))),
                new DelayAnimation(0.1f),
                new CompositeAnimation(
                    new TextAnimation(_name, newDisplayName),
                    new TextAnimation(_nameShadow, newDisplayName)),
                new CompositeAnimation(
                    new ColorAnimation(_name, Color.White, 1f, Interpolation.InterpolateColor(Easing.Uniform)),
                    new ColorAnimation(_nameShadow, new Color(30, 30, 30, 160), 1f, Interpolation.InterpolateColor(Easing.Uniform))));
        }

        /// <summary>
        /// Returns a name to display for a player.
        /// </summary>
        private string GetDisplayName(Player player)
        {
            if (player.Gamer != null)
            {
                return player.Gamer.Gamertag;
            }
            else
            {
                switch (player.Id)
                {
                    case PlayerId.A: return Resources.ComputerA;
                    case PlayerId.B: return Resources.ComputerB;
                    case PlayerId.C: return Resources.ComputerC;
                    case PlayerId.D: return Resources.ComputerD;
                    default: throw new ArgumentException("Invalid player id " + player);
                }
            }
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
        private TextSprite _nameShadow;
        private ImageSprite _voiceSprite;

        private IAnimation _nameAnimation;
    }
}
