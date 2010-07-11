using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Library.Sprite
{
    /// <summary>
    /// A renderable, transformable graphic.
    /// </summary>
    public abstract class Sprite
    {
        /// <summary>
        /// The location where this sprite will be drawn.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// The X coordinate of the Position.
        /// </summary>
        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2(value, Position.Y); } 
        }

        /// <summary>
        /// The Y coordinate of the Position.
        /// </summary>
        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2(Position.X, value); } 
        }

        /// <summary>
        /// The origin of this sprite. This sprite's rotation, scaling, and
        /// reflection are all interpreted relative to the origin.
        /// </summary>
        public Vector2 Origin { get; set; }

        /// <summary>
        /// The angle, in radians, to rotate this sprite around its origin. A
        /// positive angle rotates the sprite counterclockwise.
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// The factors by which to scale the sprite's width and height.
        /// Negative scaling reflects this sprite around its origin.
        /// </summary>
        public Vector2 Scale { get; set; }

        /// <summary>
        /// The color tint to apply to this sprite.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// The sorting depth of this sprite, between 0 (front) and 1 (back).
        /// </summary>
        public float Layer { get; set; }

        /// <summary>
        /// The size of this sprite before any transformations are applied.
        /// </summary>
        public virtual Vector2 Size { get { return Vector2.Zero; } }

        /// <summary>
        /// Creates a new sprite with a default transformation.
        /// </summary>
        public Sprite()
        {
            Position = Vector2.Zero;
            Origin = Vector2.Zero;
            Rotation = 0f;
            Scale = Vector2.One;
            Color = Color.White;
            Layer = 0.5f; // middle
        }

        /// <summary>
        /// Draws this sprite.
        /// </summary>
        /// <param name="spriteBatch">The batch to draw in.</param>
        public virtual void Draw(SpriteBatch spriteBatch)
        {
            Draw(spriteBatch, Position, Origin, Rotation, Scale, Color, Layer);
        }

        /// <summary>
        /// Draws this sprite using the given transformation.
        /// </summary>
        internal abstract void Draw(SpriteBatch spriteBatch,
                                    Vector2 position,
                                    Vector2 origin,
                                    float rotation,
                                    Vector2 scale,
                                    Color color,
                                    float layer);

        /// <summary>
        /// Returns a deep copy of this sprite.
        /// </summary>
        public virtual Sprite Clone()
        {
            return (Sprite)MemberwiseClone();
        }

        /// <summary>
        /// Converts scaling factors to sprite effects; negative scales are
        /// interpreted as reflections then made positive.
        /// </summary>
        /// <param name="scale">The scaling factors to convert.</param>
        /// <param name="origin">The origin to transform.</param>
        /// <param name="rotation">The rotation to transform.</param>
        protected SpriteEffects ConvertScaling(ref Vector2 scale, ref Vector2 origin, ref float rotation)
        {
            SpriteEffects effects = SpriteEffects.None;
            if (scale.X < 0f)
            {
                effects |= SpriteEffects.FlipHorizontally;
                scale.X = -scale.X;
                origin.X = Size.X - origin.X;
            }
            if (scale.Y < 0f)
            {
                effects |= SpriteEffects.FlipVertically;
                scale.Y = -scale.Y;
                origin.Y = Size.Y - origin.Y;
            }
            return effects;
        }
    }
}
