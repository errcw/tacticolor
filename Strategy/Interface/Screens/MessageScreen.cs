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

            _backgroundSprite = new ImageSprite(game.Content.Load<Texture2D>("Images/Colourable"));
            _backgroundSprite.Scale = new Vector2(1280, 720);
            _backgroundSprite.Color = new Color(255, 255, 255, 64);
            _backgroundSprite.Position = Vector2.Zero;

            _boxSprite = new ImageSprite(game.Content.Load<Texture2D>("Images/MessageBox"));
            _boxSprite.Position = new Vector2(
                (int)((1280 - _boxSprite.Size.X) / 2),
                (int)((720 - _boxSprite.Size.Y) / 2));

            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextLarge");
            _messageSprite = new TextSprite(font, message);
            _messageSprite.Position = new Vector2(
                (int)((1280 - _messageSprite.Size.X) / 2),
                (int)((720 - _messageSprite.Size.Y) / 2));
            _messageSprite.Color = new Color(60, 60, 60);

            _instrSprite = new TextSprite(font, "Continue");
            _instrSprite.Position = new Vector2(
                _boxSprite.Position.X + _boxSprite.Size.X - _instrSprite.Size.X,
                _boxSprite.Position.Y + _boxSprite.Size.Y);
            _instrSprite.Color = new Color(120, 120, 120);

            _buttonSprite = new ImageSprite(game.Content.Load<Texture2D>("Images/ButtonA"));
            _buttonSprite.Position = new Vector2(
                _instrSprite.Position.X - _buttonSprite.Size.X - 5,
                _instrSprite.Position.Y);

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            ShowBeneath = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _backgroundSprite.Draw(_spriteBatch);
            _boxSprite.Draw(_spriteBatch);
            _messageSprite.Draw(_spriteBatch);
            _instrSprite.Draw(_spriteBatch);
            _buttonSprite.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Action.Released)
            {
                while (!(Stack.ActiveScreen is MainMenuScreen))
                {
                    Stack.Pop();
                }
            }
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            _backgroundSprite.Color = new Color(_backgroundSprite.Color, (byte)(64 * progress));
            _boxSprite.Color = new Color(_boxSprite.Color, (byte)(255 * progress));
            _messageSprite.Color = new Color(_messageSprite.Color, (byte)(255 * progress));
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            _backgroundSprite.Color = new Color(_backgroundSprite.Color, (byte)(32 * (1 - progress)));
            _boxSprite.Color = new Color(_boxSprite.Color, (byte)(255 * (1 - progress)));
            _messageSprite.Color = new Color(_messageSprite.Color, (byte)(255 * (1 - progress)));
        }

        private MenuInput _input;

        private ImageSprite _backgroundSprite;
        private ImageSprite _boxSprite;
        private ImageSprite _buttonSprite;
        private TextSprite _messageSprite;
        private TextSprite _instrSprite;

        private SpriteBatch _spriteBatch;
    }
}
