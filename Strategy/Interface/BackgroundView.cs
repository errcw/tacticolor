using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;


namespace Strategy.Interface
{
    /// <summary>
    /// Shows the background.
    /// </summary>
    public class BackgroundView
    {
        public BackgroundView(InterfaceContext context)
        {
            _background = context.Content.Load<Texture2D>("Background");
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_background, Vector2.Zero, Color.White);
        }

        private Texture2D _background;
    }
}
