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
            TextSprite labelSprite = new TextSprite(_font, labelText);
            TextSprite textSprite = new TextSprite(_font, initialState ? Resources.MenuOn : Resources.MenuOff);
            TextMenuEntry entry = new TextMenuEntry(labelSprite, textSprite);
            entry.SelectText = Resources.MenuToggle;
            entry.Selected += toggledHandler;

            bool state = initialState;
            entry.Selected += (s, a) =>
            {
                state = !state;
                textSprite.Text = state ? Resources.MenuOn : Resources.MenuOff;
            };

            _screen.AddEntry(entry);
            return this;
        }

        public MenuBuilder CreateCycleButtonEntry<T>(string labelText, EventHandler<EventArgs> cycledHandler, T initialState)
        {
            string[] states = Enum.GetNames(typeof(T));
            int state = Array.FindIndex(states, s => s == initialState.ToString());

            TextSprite labelSprite = new TextSprite(_font, labelText);
            TextSprite textSprite = new TextSprite(_font, states[state]);
            TextMenuEntry entry = new TextMenuEntry(labelSprite, textSprite);
            entry.SelectText = Resources.MenuCycle;
            entry.Selected += cycledHandler;

            entry.Selected += (s, a) =>
            {
                state = (state + 1) % states.Length;
                textSprite.Text = states[state];
            };

            _screen.AddEntry(entry);
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
    }
}
