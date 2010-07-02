using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows the input cursor.
    /// </summary>
    public class LocalInputView
    {
        public LocalInputView(LocalInput input, InterfaceContext context)
        {
            _input = input;
            _context = context;

            Texture2D cursorTex = context.Content.Load<Texture2D>("PieceSmall");
            _cursor = new IsometricSprite(cursorTex);
            _cursor.Color = GetPlayerColor(input.Player);
            _cursor.Origin = new Vector2(0, 14);
        }

        public void Update(float time)
        {
            Cell cell = _input.Hovered.Area.First();
            Point point = _context.IsoParams.GetPoint(cell);
            _cursor.X = point.X;
            _cursor.Y = point.Y;
            _cursor.Position += new Vector2(10, 10); // offset in tile
        }

        public void Draw(IsometricBatch spriteBatch)
        {
            spriteBatch.Draw(_cursor);
        }

        /// <summary>
        /// Returns the color of the given player.
        /// </summary>
        private Color GetPlayerColor(PlayerId player)
        {
            switch (player)
            {
                case PlayerId.A: return Color.SlateGray;
                case PlayerId.B: return Color.Brown;
                case PlayerId.C: return Color.Coral;
                case PlayerId.D: return Color.CornflowerBlue;
                default: return Color.White;
            }
        }

        private LocalInput _input;
        private InterfaceContext _context;
        private IsometricSprite _cursor;
    }
}
