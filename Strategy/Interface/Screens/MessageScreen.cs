using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Properties;
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
        public MessageScreen(Game game, string messageText) : this(game, messageText, typeof(MainMenuScreen))
        {
        }

        public MessageScreen(Game game, string messageText, Type popUntilScreen) : this(game, messageText, popUntilScreen, true)
        {
        }

        public MessageScreen(Game game, string messageText, Type popUntilScreen, bool fadeIn)
        {
            _input = game.Services.GetService<MenuInput>();
            _popUntilScreen = popUntilScreen;

            ImageSprite background = new ImageSprite(game.Content.Load<Texture2D>("Images/Colourable"));
            background.Scale = new Vector2(1280, 720);
            background.Color = Color.FromNonPremultiplied(64, 64, 64, 190);
            background.Position = Vector2.Zero;

            ImageSprite box = new ImageSprite(game.Content.Load<Texture2D>("Images/MessageBox"));
            box.Position = new Vector2(
                (int)((1280 - box.Size.X) / 2),
                (int)((720 - box.Size.Y) / 2));
            _boxRightX = box.Position.X + box.Size.X;
            _boxBottomY = box.Position.Y + box.Size.Y;

            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/Text");
            string[] lines = SplitLines(messageText, box.Size.X * 0.8f, font);
            Sprite[] lineSprites = new Sprite[lines.Length];
            float y = box.Position.Y + (box.Size.Y - font.LineSpacing * lines.Length) / 2;
            for (int i = 0; i < lines.Length; i++)
            {
                lineSprites[i] = new TextSprite(font, lines[i]);
                lineSprites[i].Position = new Vector2(
                    (int)(box.Position.X + (box.Size.X - lineSprites[i].Size.X) / 2),
                    (int)(y + i * font.LineSpacing));
                lineSprites[i].Color = Color.FromNonPremultiplied(60, 60, 60, 255);
            }
            CompositeSprite message = new CompositeSprite(lineSprites);

            TextSprite instructions = new TextSprite(font, Resources.MenuContinue);
            instructions.Position = new Vector2(_boxRightX - instructions.Size.X, _boxBottomY + 7);
            instructions.Color = Color.White;

            ImageSprite button = new ImageSprite(game.Content.Load<Texture2D>("Images/ButtonA"));
            button.Position = new Vector2(
                instructions.Position.X - button.Size.X - 5,
                instructions.Position.Y + (instructions.Size.Y - button.Size.Y) / 2);

            _sprite = new CompositeSprite(background, box, message, instructions, button);

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            _dismissEffect = game.Content.Load<SoundEffect>("Sounds/MenuSelect");

            ShowBeneath = true;
            TransitionOnTime = fadeIn ? 0.25f : 0f;
            TransitionOffTime = 0.25f;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _sprite.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Action.Pressed)
            {
                _dismissEffect.Play();
                while (!_popUntilScreen.IsInstanceOfType(Stack.ActiveScreen))
                {
                    Stack.Pop();
                }
            }
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            _sprite.Color = ColorExtensions.FromNonPremultiplied(Color.White, progress);
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            _sprite.Color = ColorExtensions.FromNonPremultiplied(Color.White, 1 - progress);
        }

        private string[] SplitLines(string message, float lineWidth, SpriteFont font)
        {
            List<string> lines = new List<string>();
            string[] words = message.Split(' ');

            StringBuilder currentLine = new StringBuilder(message.Length);
            float currentLineWidth = 0f;

            foreach (string word in words)
            {
                string wordAndSpace = word + " ";
                float wordWidth = font.MeasureString(wordAndSpace).X;
                if (currentLineWidth + wordWidth > lineWidth)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Remove(0, currentLine.Length);
                    currentLineWidth = 0f;
                }
                currentLine.Append(wordAndSpace);
                currentLineWidth += wordWidth;
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            return lines.ToArray();
        }

        protected MenuInput _input;

        protected Type _popUntilScreen;

        protected CompositeSprite _sprite;
        protected float _boxRightX;
        protected float _boxBottomY;
        private SpriteBatch _spriteBatch;

        private SoundEffect _dismissEffect;
    }
}
