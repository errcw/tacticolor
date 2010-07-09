using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Library.Sprite
{
    /// <summary>
    /// A sprite that displays a string.
    /// </summary>
    public class TextSprite : Sprite
    {
        /// <summary>
        /// The font used to render the text.
        /// </summary>
        public SpriteFont Font { get; set; }

        /// <summary>
        /// The text to display.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The width of the outline around this text.
        /// </summary>
        public int OutlineWidth { get; set; }

        /// <summary>
        /// The colour of the outline around this text.
        /// </summary>
        public Color OutlineColor { get; set; }

        /// <summary>
        /// The size of this text, not including its outline.
        /// </summary>
        public override Vector2 Size
        {
            get { return Font.MeasureString(Text); }
        }

        /// <summary>
        /// Creates an empty text sprite.
        /// </summary>
        public TextSprite(SpriteFont font) : this(font, "")
        {
        }

        /// <summary>
        /// Creates a new text sprite.
        /// </summary>
        public TextSprite(SpriteFont font, string text)
        {
            OutlineWidth = 0;
            OutlineColor = Color.White;
            Font = font;
            Text = text;
        }

        /// <summary>
        /// Draws this text using the given parameters.
        /// </summary>
        internal override void Draw(SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, Vector2 scale, Color color, float layer)
        {
            SpriteEffects effects = ConvertScaling(ref scale, ref origin, ref rotation);

            if (OutlineWidth > 0)
            {
                Color outlineColor = new Color(OutlineColor, (color.A / 255f) * (OutlineColor.A / 255f));
                for (int x = -OutlineWidth; x <= OutlineWidth; x++)
                {
                    for (int y = -OutlineWidth; y <= OutlineWidth; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }
                        Vector2 outlinePos = new Vector2(position.X + x, position.Y + y);
                        spriteBatch.DrawString(Font, Text, outlinePos, outlineColor, -rotation, origin, scale, effects, layer + 0.000001f);
                    }
                }
            }

            spriteBatch.DrawString(Font, Text, position, color, -rotation, origin, scale, effects, layer);
        }
    }
}
