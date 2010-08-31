using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Library.Sprite
{
    /// <summary>
    /// A sprite that displays an image.
    /// </summary>
    public class ImageSprite : Sprite
    {
        /// <summary>
        /// The texture used to draw this sprite.
        /// </summary>
        public Texture2D Texture { get; set; }

        /// <summary>
        /// The size of this image.
        /// </summary>
        public override Vector2 Size
        {
            get { return GetSize(); }
        }

        /// <summary>
        /// Creates a new image sprite.
        /// </summary>
        /// <param name="texture">The sprite image.</param>
        public ImageSprite(Texture2D texture) : this(texture, null)
        {
        }

        /// <summary>
        /// Creates a new image sprite.
        /// </summary>
        /// <param name="texture">The sprite image.</param>
        /// <param name="sourceRect">The section of the texture to draw; or, null to draw the entire texture.</param>
        public ImageSprite(Texture2D texture, Rectangle? sourceRect)
        {
            Texture = texture;
            _sourceRect = sourceRect;
        }

        /// <summary>
        /// Draws this image using the given transformation.
        /// </summary>
        internal override void Draw(SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, Vector2 scale, Color color, float layer)
        {
            SpriteEffects effects = ConvertScaling(ref scale, ref origin, ref rotation);
            spriteBatch.Draw(Texture, position, _sourceRect, color, -rotation, origin, scale, effects, layer);
        }

        /// <summary>
        /// Returns the pre-transform size of this image.
        /// </summary>
        private Vector2 GetSize()
        {
            return (_sourceRect != null)
                ? new Vector2(_sourceRect.Value.Width, _sourceRect.Value.Height)
                : new Vector2(Texture.Width, Texture.Height);
        }

        private Rectangle? _sourceRect;
    }
}
