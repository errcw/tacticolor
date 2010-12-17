using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Displays instructions how to play the game.
    /// </summary>
    public class HowToPlayScreen : MenuScreen
    {
        public HowToPlayScreen(Game game) : base(game)
        {
            SpriteFont normalFont = game.Content.Load<SpriteFont>("Fonts/TextSmall");
            SpriteFont emphasisFont = game.Content.Load<SpriteFont>("Fonts/TextSmallBold");

            MenuBuilder builder = new MenuBuilder(this, game);
            for (int page = 1; page <= 4; page++)
            {
                string illustration = "Images/HowToPlay" + page;
                Sprite illustrationSprite = new ImageSprite(game.Content.Load<Texture2D>(illustration));

                string text = Resources.ResourceManager.GetString("HowToPlay" + page);
                Sprite textSprite = FormatString(text, LineWidth, normalFont, emphasisFont);

                builder.CreateImageEntry(LayoutPage(illustrationSprite, textSprite));
            }

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(170f, 160f);
            VisibleEntryCount = 1;
        }

        private Sprite LayoutPage(Sprite illustration, Sprite text)
        {
            illustration.Position = new Vector2((text.Size.X - illustration.Size.X) / 2 - 25, 10);
            text.Position = new Vector2(0, illustration.Size.Y + 30);
            return new CompositeSprite(illustration, text);
        }

        private Sprite FormatString(string str, float lineWidth, SpriteFont normalFont, SpriteFont emphasisFont)
        {
            CompositeSprite text = new CompositeSprite();

            float x = 0f;
            float y = 0f;
            bool useEmphasis = false;

            string[] emphasisBlocks = str.Split('*');
            foreach (string emphasisBlock in emphasisBlocks)
            {
                SpriteFont font = useEmphasis ? emphasisFont : normalFont;

                string[] words = emphasisBlock.Split(' ');
                foreach (string word in words)
                {
                    if (String.IsNullOrEmpty(word))
                    {
                        continue;
                    }

                    float wordWidth = font.MeasureString(word).X;
                    if (x + wordWidth > lineWidth)
                    {
                        x = 0f;
                        y += normalFont.LineSpacing;
                    }

                    string wordToPrint = word;
                    // prepend a space between words
                    // (a) not at the start of the line, or
                    // (b) not for punctuation
                    if (x != 0f && !IsPunctuation(wordToPrint[0]))
                    {
                        wordToPrint = " " + wordToPrint;
                    }

                    TextSprite wordSprite = new TextSprite(font, wordToPrint);
                    wordSprite.Position = new Vector2((int)x, (int)y);
                    text.Add(wordSprite);

                    x += wordSprite.Size.X;
                }

                // toggle after every block
                useEmphasis = !useEmphasis;
            }

            return text;
        }

        private bool IsPunctuation(char character)
        {
            return ".;?!".Contains(character.ToString());
        }

        private const float LineWidth = 510f;
    }
}
