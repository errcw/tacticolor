﻿using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Extensions;

namespace Strategy.Library.Components
{
    /// <summary>
    /// Displays the number of frames rendered per second.
    /// </summary>
    /// <remarks>Adapted from http://blogs.msdn.com/shawnhar/archive/2007/06/08/displaying-the-framerate.aspx </remarks>
    public class FpsOverlay : DrawableGameComponent
    {
        /// <summary>
        /// The name of the font to use to draw the FPS counter.
        /// </summary>
        public string FontName { get; set; }

        public FpsOverlay(Game game) : base(game)
        {
            DrawOrder = Int32.MaxValue; // draw last
            FontName = "Fonts/Text";
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Game.Content.Load<SpriteFont>(FontName);
            _position = new Vector2(
                (int)(GraphicsDevice.Viewport.Width * 0.05),
                (int)(GraphicsDevice.Viewport.Height * 0.05));
        }

        public override void Update(GameTime gameTime)
        {
            _elapsed += gameTime.GetElapsedSeconds();
            if (_elapsed >= 1f)
            {
                _elapsed -= 1f;
                _frameRate = _frameCounter;
                _frameCounter = 0;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            _frameCounter += 1;
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _spriteBatch.DrawString(_font, _frameRate.ToString(), _position, Color.White);
            _spriteBatch.End();
        }

        private SpriteFont _font;
        private Vector2 _position;
        private SpriteBatch _spriteBatch;

        private float _elapsed;
        private int _frameCounter;
        private int _frameRate;
    }
}