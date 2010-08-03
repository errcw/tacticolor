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
            string name = (player.Gamer != null) ? player.Gamer.Gamertag : "Computer " + _player.Id;
            _name = new TextSprite(font, name);
            _name.Color = Color.White;
            _name.OutlineColor = Color.Black;
            _name.OutlineWidth = 2;
            _name.Position = GetBasePosition(_player.Id);
        }

        public void Update(float time)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _name.Draw(spriteBatch);
        }

        /// <summary>
        /// Returns the position at which to start drawing.
        /// </summary>
        private Vector2 GetBasePosition(PlayerId player)
        {
            const int BaseX = 80;
            const int BaseY = 50;
            const int SpacingY = 100;
            switch (player)
            {
                case PlayerId.A: return new Vector2(BaseX, BaseY);
                case PlayerId.B: return new Vector2(BaseX, BaseY + SpacingY);
                case PlayerId.C: return new Vector2(BaseX, 720 - 25 - BaseY - SpacingY);
                case PlayerId.D: return new Vector2(BaseX, 720 - 25 - BaseY);
                default: throw new ArgumentException("Invalid player id " + player);
            }
        }

        private Player _player;
        private InterfaceContext _context;

        private TextSprite _name;
    }
}
