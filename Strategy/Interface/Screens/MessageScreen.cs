using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Displays a text message to the user.
    /// </summary>
    public class MessageScreen : Screen
    {
        public MessageScreen(Game game, string message)
        {
            _input = game.Services.GetService<MenuInput>();

            _boxSprite = new ImageSprite(game.Content.Load<Texture2D>("Images/MessageBox"));
            _boxSprite.Position = new Vector2(
                (int)((1280 - _boxSprite.Size.X) / 2),
                (int)((720 - _boxSprite.Size.Y) / 2));

            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextLarge");
            _textSprite = new TextSprite(font, message);
            _textSprite.Position = new Vector2(
                (int)((1280 - _textSprite.Size.X) / 2),
                (int)((720 - _textSprite.Size.Y) / 2));
            _textSprite.Color = new Color(30, 30, 30);

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            ShowBeneath = true;
            TransitionOnTime = 0.5f;
            TransitionOffTime = 0.5f;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _boxSprite.Draw(_spriteBatch);
            _textSprite.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Action.Released)
            {
                Stack.Pop();
            }
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            _boxSprite.Color = new Color(_boxSprite.Color, (byte)(255 * progress));
            _textSprite.Color = new Color(_textSprite.Color, (byte)(255 * progress));
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            _boxSprite.Color = new Color(_boxSprite.Color, (byte)(255 * (1 - progress)));
            _textSprite.Color = new Color(_textSprite.Color, (byte)(255 * (1 - progress)));
        }

        private MenuInput _input;

        private ImageSprite _boxSprite;
        private TextSprite _textSprite;
        private SpriteBatch _spriteBatch;
    }
}
