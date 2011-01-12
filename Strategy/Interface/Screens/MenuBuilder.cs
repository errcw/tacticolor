using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Properties;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
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
            _game = game;
            _font = _game.Content.Load<SpriteFont>("Fonts/TextLarge");
            _fontSmall = _game.Content.Load<SpriteFont>("Fonts/TextSmall");
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

        public MenuBuilder CreatePurchaseButtonEntry(string buttonText)
        {
            if (Guide.IsTrialMode)
            {
                MenuInput input = _game.Services.GetService<MenuInput>();
                MenuEntry purchaseEntry = null;
                CreateButtonEntry(buttonText, (s, a) => input.Controller.Value.PurchaseContent(), out purchaseEntry);

                TrialModeObserverComponent trialObserver = _game.Services.GetService<TrialModeObserverComponent>();
                trialObserver.TrialModeEnded += (s, a) => _screen.RemoveEntry(purchaseEntry);
            }
            return this;
        }

        public MenuBuilder CreateToggleButtonEntry(string labelText, EventHandler<EventArgs> toggledHandler, bool initialState)
        {
            string[] states = { Resources.MenuOn, Resources.MenuOff };
            int state = initialState ? 0 : 1;

            TextSprite labelSprite = new TextSprite(_font, labelText);
            TextSprite textSprite = new TextSprite(_fontSmall);
            CyclingTextMenuEntry entry = new CyclingTextMenuEntry(labelSprite, textSprite, states, state);
            entry.SelectText = Resources.MenuToggle;
            entry.Selected += toggledHandler;

            _screen.AddEntry(entry);
            return this;
        }

        public MenuBuilder CreateCycleButtonEntry<T>(string labelText, EventHandler<EventArgs> cycledHandler, T initialState)
        {
            CyclingTextMenuEntry dummy;
            return CreateCycleButtonEntry(labelText, cycledHandler, initialState, out dummy);
        }

        public MenuBuilder CreateCycleButtonEntry<T>(string labelText, EventHandler<EventArgs> cycledHandler, T initialState, out CyclingTextMenuEntry createdEntry)
        {
            string[] states = Enum.GetNames(typeof(T));
            int state = Array.IndexOf(states, initialState.ToString());

            TextSprite labelSprite = new TextSprite(_font, labelText);
            TextSprite textSprite = new TextSprite(_fontSmall);
            CyclingTextMenuEntry entry = new CyclingTextMenuEntry(labelSprite, textSprite, states, state);
            entry.Selected += cycledHandler;

            _screen.AddEntry(entry);
            createdEntry = entry;
            return this;
        }

        public MenuBuilder CreateImageEntry(Sprite sprite)
        {
            MenuEntry entry = new MenuEntry(sprite);
            entry.IsSelectable = false;
            _screen.AddEntry(entry);
            return this;
        }

        private Game _game;
        private MenuScreen _screen;

        private SpriteFont _font;
        private SpriteFont _fontSmall;
    }
}
