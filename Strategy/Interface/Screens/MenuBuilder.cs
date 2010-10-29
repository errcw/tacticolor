using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Builds menu structures.
    /// </summary>
    public class MenuBuilder
    {
        /// <summary>
        /// Creates a new builder for the specified screen.
        /// </summary>
        /// <param name="screen">The screen to build in.</param>
        /// <param name="game">The game.</param>
        public MenuBuilder(MenuScreen screen, Game game)
        {
            _screen = screen;
            _font = game.Content.Load<SpriteFont>("Fonts/TextLarge");
        }

        public MenuBuilder CreateButtonEntry(string buttonText, EventHandler<EventArgs> selectedHandler)
        {
            MenuEntry dummy;
            return CreateButtonEntry(buttonText, selectedHandler, out dummy);
        }

        public MenuBuilder CreateButtonEntry(string buttonText, EventHandler<EventArgs> selectedHandler, out MenuEntry createdEntry)
        {
            TextSprite textSprite = new TextSprite(_font, buttonText);
            TextMenuEntry entry = new TextMenuEntry(textSprite);
            entry.Selected += selectedHandler;
            _screen.AddEntry(entry);
            createdEntry = entry;
            return this;
        }

        private MenuScreen _screen;

        private SpriteFont _font;
    }
}
