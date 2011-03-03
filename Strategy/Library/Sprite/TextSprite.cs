using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Extensions;

namespace Strategy.Library.Sprite
{
    /// <summary>
    /// A sprite that displays a string.
    /// </summary>
    public class TextSprite : Sprite
    {
        /// <summary>
        /// A visual effect applied to the text.
        /// </summary>
        public enum TextEffect
        {
            Outline,
            Shadow,
            None
        }

        /// <summary>
        /// The font used to render the text.
        /// </summary>
        public SpriteFont Font { get; set; }

        /// <summary>
        /// The text to display.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The effect applied to the text.
        /// </summary>
        public TextEffect Effect { get; set; }

        /// <summary>
        /// The color of the effect.
        /// </summary>
        public Color EffectColor { get; set; }

        /// <summary>
        /// The size of the effect.
        /// </summary>
        public int EffectSize { get; set; }

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
            Font = font;
            Text = text;
            Effect = TextEffect.None;
        }

        /// <summary>
        /// Draws this text using the given parameters.
        /// </summary>
        internal override void Draw(SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, Vector2 scale, Color color, float layer)
        {
            SpriteEffects effects = ConvertScaling(ref scale, ref origin, ref rotation);
            Color effectColor = ColorExtensions.FromNonPremultiplied(EffectColor, (color.A / 255f) * (EffectColor.A / 255f));

            switch (Effect)
            {
                case TextEffect.Outline:
                    for (int x = -EffectSize; x <= EffectSize; x++)
                    {
                        for (int y = -EffectSize; y <= EffectSize; y++)
                        {
                            if (x == 0 && y == 0)
                            {
                                continue;
                            }
                            Vector2 outlinePos = new Vector2(position.X + x, position.Y + y);
                            spriteBatch.DrawString(Font, Text, outlinePos, effectColor, -rotation, origin, scale, effects, layer + 0.000001f);
                        }
                    }
                    break;
                case TextEffect.Shadow:
                    Vector2 shadowPos = position + new Vector2(EffectSize, EffectSize);
                    spriteBatch.DrawString(Font, Text, shadowPos, effectColor, -rotation, origin, scale, effects, layer + 0.000001f);
                    break;
            }

            spriteBatch.DrawString(Font, Text, position, color, -rotation, origin, scale, effects, layer);
        }
    }
}
