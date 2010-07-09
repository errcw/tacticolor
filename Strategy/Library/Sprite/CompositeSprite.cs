using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Library.Sprite
{
    /// <summary>
    /// A sprite composed of one or more children sprites.
    /// </summary>
    /// <remarks>
    /// Each child sprite's transformation is interpreted relative to that of
    /// its parent.
    /// </remarks>
    public class CompositeSprite : Sprite
    {
        /// <summary>
        /// The size of all the children before any transforms are applied.
        /// </summary>
        public override Vector2 Size
        {
            get { return GetSize(); }
        }

        /// <summary>
        /// Creates a new composite sprite.
        /// </summary>
        /// <param name="sprites">Sprites to include in the composite.</param>
        public CompositeSprite(params Sprite[] sprites)
        {
            _sprites.AddRange(sprites);
        }

        /// <summary>
        /// Adds a child sprite to this composite.
        /// </summary>
        /// <param name="sprite">The sprite to add.</param>
        public void Add(Sprite sprite)
        {
            _sprites.Add(sprite);
        }

        /// <summary>
        /// Removes a sprite from this composite.
        /// </summary>
        /// <param name="sprite">The sprite to remove.</param>
        /// <returns>True if the sprite was successfully removed; otherwise, false.</returns>
        public bool Remove(Sprite sprite)
        {
            return _sprites.Remove(sprite);
        }

        /// <summary>
        /// Removes all sprites from this composite.
        /// </summary>
        public void Clear()
        {
            _sprites.Clear();
        }

        /// <summary>
        /// Returns a deep copy of this composite and all its children.
        /// </summary>
        public override Sprite Clone()
        {
            CompositeSprite c = (CompositeSprite)MemberwiseClone();
            c._sprites = _sprites.Select(s => s.Clone()).ToList();
            return c;
        }

        /// <summary>
        /// Draws the children of this composite with the given transformation.
        /// </summary>
        internal override void Draw(SpriteBatch spriteBatch, Vector2 position, Vector2 origin, float rotation, Vector2 scale, Color color, float layer)
        {
            rotation = TransformRotation(scale, rotation);
            Quaternion rotationQ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -rotation);

            _sprites.ForEach(sprite =>
                sprite.Draw(spriteBatch,
                            position + origin + Vector2.Transform(sprite.Position * scale, rotationQ),
                            sprite.Origin,
                            rotation + TransformRotation(scale, sprite.Rotation),
                            scale * sprite.Scale,
                            new Color(color.ToVector4() * sprite.Color.ToVector4()),
                            layer + (sprite.Layer * ChildLayerScale)));
        }

        /// <summary>
        /// Transforms a rotation for the given scale.
        /// </summary>
        private float TransformRotation(Vector2 scale, float rotation)
        {
            if ((scale.X < 0) ^ (scale.Y < 0))
            {
                rotation = -rotation;
            }
            return rotation;
        }

        /// <summary>
        /// Calculates the size of this sprite.
        /// </summary>
        private Vector2 GetSize()
        {
            if (_sprites.Count == 0)
            {
                return Vector2.Zero;
            }

            Vector4 bounds = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
            foreach (Sprite sprite in _sprites)
            {
                Vector2 position = sprite.Position - sprite.Origin;
                bounds.X = Math.Min(position.X, bounds.X);
                bounds.Y = Math.Min(position.Y, bounds.Y);

                Vector2 size = sprite.Size;
                bounds.Z = Math.Max(position.X + size.X, bounds.Z);
                bounds.W = Math.Max(position.Y + size.Y, bounds.W);
            }
            return new Vector2(bounds.Z - bounds.X, bounds.W - bounds.Y);
        }

        private List<Sprite> _sprites = new List<Sprite>();

        private const float ChildLayerScale = 0.0001f;
    }
}
