using System;
using System.Collections.Generic;
using System.Text;

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
        public MessageScreen(Game game, string messageText)
        {
            _input = game.Services.GetService<MenuInput>();

            ImageSprite background = new ImageSprite(game.Content.Load<Texture2D>("Images/Colourable"));
            background.Scale = new Vector2(1280, 720);
            background.Color = new Color(255, 255, 255, 64);
            background.Position = Vector2.Zero;

            ImageSprite box = new ImageSprite(game.Content.Load<Texture2D>("Images/MessageBox"));
            box.Position = new Vector2(
                (int)((1280 - box.Size.X) / 2),
                (int)((720 - box.Size.Y) / 2));

            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextLarge");
            string[] lines = SplitLines(messageText, box.Size.X * 0.8f, font);
            Sprite[] lineSprites = new Sprite[lines.Length];
            float y = box.Position.Y + (box.Size.Y - font.LineSpacing * lines.Length) / 2;
            for (int i = 0; i < lines.Length; i++)
            {
                lineSprites[i] = new TextSprite(font, lines[i]);
                lineSprites[i].Position = new Vector2(
                    box.Position.X + (box.Size.X - lineSprites[i].Size.X) / 2,
                    y + i * font.LineSpacing);
                lineSprites[i].Color = new Color(60, 60, 60);
            }
            CompositeSprite message = new CompositeSprite(lineSprites);

            TextSprite instructions = new TextSprite(font, "Continue");
            instructions.Position = new Vector2(
                box.Position.X + box.Size.X - instructions.Size.X,
                box.Position.Y + box.Size.Y + 5);
            instructions.Color = new Color(90, 90, 90);

            ImageSprite button = new ImageSprite(game.Content.Load<Texture2D>("Images/ButtonA"));
            button.Position = new Vector2(
                instructions.Position.X - button.Size.X - 5,
                instructions.Position.Y + (instructions.Size.Y - button.Size.Y) / 2);

            _sprite = new CompositeSprite(background, box, message, instructions, button);
            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            ShowBeneath = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            _sprite.Draw(_spriteBatch);
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
            _sprite.Color = new Color(_sprite.Color, (byte)(255 * progress));
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            _sprite.Color = new Color(_sprite.Color, (byte)(255 * (1 - progress)));
        }

        private string[] SplitLines(string message, float lineWidth, SpriteFont font)
        {
            List<string> lines = new List<string>();
            string[] words = message.Split(new string[]{" "}, StringSplitOptions.RemoveEmptyEntries);

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

        private MenuInput _input;

        private Sprite _sprite;
        private SpriteBatch _spriteBatch;
    }
}
