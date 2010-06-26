using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Library.Components
{
    /// <summary>
    /// Renders the unsafe screen areas for any content (10%) and for action or text (20%).
    /// </summary>
    /// <remarks>Adapted from http://www.xnawiki.com/index.php/Unsafe_Screen_Area_Overlay_Drawable_Game_Component </remarks>
    public class TitleSafeAreaOverlayComponent : DrawableGameComponent
    {
        /// <summary>
        /// Color for the area that is unsafe for any content.
        /// </summary>
        public Color UnsafeAreaColor { get; set; }

        /// <summary>
        /// Coloring for the area that is unsafe for game action or text.
        /// </summary>
        public Color NoActionAreaColor { get; set; }

        public TitleSafeAreaOverlayComponent(Game game) : base(game)
        {
            DrawOrder = Int32.MaxValue; // draw last
            NoActionAreaColor = new Color(255, 0, 0, 127);
            UnsafeAreaColor = new Color(255, 255, 0, 127);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // generate a 1x1 texture
            _texture = new Texture2D(GraphicsDevice, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            _texture.SetData<Color>(new Color[] { Color.White });

            // get viewport size and the offset percentage
            int width = GraphicsDevice.Viewport.Width;
            int height = GraphicsDevice.Viewport.Height;
            int dw = (int)(width * 0.05);
            int dh = (int)(height * 0.05);

            // generate the area unsafe for game action or text
            _noActionAreaParts = new Rectangle[4];
            _noActionAreaParts[0] = new Rectangle(0, 0, width, dh);
            _noActionAreaParts[1] = new Rectangle(0, height - dh, width, dh);
            _noActionAreaParts[2] = new Rectangle(0, dh, dw, height - 2 * dh);
            _noActionAreaParts[3] = new Rectangle(width - dw, dh, dw, height - 2 * dh);

            // generate the area not safe for anything
            _unsafeAreaParts = new Rectangle[4];
            _unsafeAreaParts[0] = new Rectangle(dw, dh, width - 2 * dw, dh);
            _unsafeAreaParts[1] = new Rectangle(dw, height - 2 * dh, width - 2 * dw, dh);
            _unsafeAreaParts[2] = new Rectangle(dw, 2 * dh, dw, height - 4 * dh);
            _unsafeAreaParts[3] = new Rectangle(width - 2 * dw, 2 * dh, dw, height - 4 * dh);
        }

        public override void Draw(GameTime gameTime)
        {
            _spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.FrontToBack, SaveStateMode.None);
            Color[] c = new Color[4];
            foreach (Rectangle r in _noActionAreaParts)
            {
                _spriteBatch.Draw(_texture, r, NoActionAreaColor);
            }
            foreach (Rectangle r in _unsafeAreaParts)
            {
                _spriteBatch.Draw(_texture, r, UnsafeAreaColor);
            }
            _spriteBatch.End();
        }

        private Texture2D _texture;
        private SpriteBatch _spriteBatch;

        private Rectangle[] _noActionAreaParts;
        private Rectangle[] _unsafeAreaParts;
    }
}
