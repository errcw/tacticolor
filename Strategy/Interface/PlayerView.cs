using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Library;
using Strategy.Library.Sprite;

namespace Strategy.Interface
{
    /// <summary>
    /// Shows a player.
    /// </summary>
    public class PlayerView
    {
        public PlayerView(Player player, InterfaceContext context)
        {
            _player = player;
            _context = context;

            SpriteFont font = context.Content.Load<SpriteFont>("Fonts/Gamertag");
            _name = new TextSprite(font, "errcw");
            _name.Color = Color.White;
            _name.OutlineColor = Color.Black;
            _name.OutlineWidth = 2;
            _name.Position = new Vector2(0, 0);
        }

        public void Update(float time)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
        }

        private Player _player;
        private InterfaceContext _context;

        private TextSprite _name;
    }
}
