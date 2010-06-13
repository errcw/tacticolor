using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Interface
{
    public class IsometricSprite
    {
        /// <summary>
        /// The location where this sprite will be drawn.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// The x coordinate of this sprite's position.
        /// </summary>
        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2(value, Position.Y); }
        }

        /// <summary>
        /// The y coordinate of this sprite's position.
        /// </summary>
        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2(Position.X, value); }
        }

        /// <summary>
        /// The origin of this sprite, where it touches the isometric plane.
        /// </summary>
        public Vector2 Origin { get; set; }

        /// <summary>
        /// The color tint to apply to this sprite.
        /// </summary>
        public Color Color { get; set; }

        public IsometricSprite(Texture2D texture)
        {
            _texture = texture;
            Color = Color.White;
        }

        public void Draw(SpriteBatch batch)
        {
            batch.Draw(_texture, Position - Origin, Color);
        }

        private Texture2D _texture;
    }
}
