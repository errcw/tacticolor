using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public MenuBuilder(MenuScreen screen)
        {
            _screen = screen;
        }

        public MenuBuilder CreateButtonEntry(string buttonText, EventHandler<EventArgs> selectedHandler)
        {
            MenuEntry dummy;
            return CreateButtonEntry(buttonText, selectedHandler, out dummy);
        }

        public MenuBuilder CreateButtonEntry(string buttonText, EventHandler<EventArgs> selectedHandler, out MenuEntry createdEntry)
        {
            TextMenuEntry entry = new TextMenuEntry(null);
            _screen.AddEntry(entry);
            createdEntry = entry;
            return this;
        }

        private MenuScreen _screen;
    }
}
