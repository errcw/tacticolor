using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Shows the background.
    /// </summary>
    public class BackgroundView
    {
        public BackgroundView(InterfaceContext context)
        {
            _background = context.Content.Load<Texture2D>("Images/Background");
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_background, Vector2.Zero, Color.White);
        }

        private Texture2D _background;
    }
}
