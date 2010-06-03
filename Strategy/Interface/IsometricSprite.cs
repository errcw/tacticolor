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
        public Vector2 Position { get; set; }

        public float X
        {
            get { return Position.X; }
            set { Position = new Vector2(value, Position.Y); }
        }

        public float Y
        {
            get { return Position.Y; }
            set { Position = new Vector2(Position.X, value); }
        }

        public Vector2 Origin { get; set; }

        public Color Tint { get; set; }

        public IsometricSprite(Texture2D texture)
        {
            _texture = texture;
            Tint = Color.White;
        }

        public void Draw(SpriteBatch batch)
        {
            batch.Draw(_texture, Position - Origin, Tint);
        }

        private Texture2D _texture;
    }
}
