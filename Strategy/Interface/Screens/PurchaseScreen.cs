using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    public class PurchaseScreen : MessageScreen
    {
        public PurchaseScreen(Game game, string messageText, Type popUntilScreen) : base(game, messageText, popUntilScreen)
        {
            // pop this screen if the player buys the game
            game.Services.GetService<TrialModeObserverComponent>().TrialModeEnded += (s, a) => Stack.Pop();

            //XXX add upsell graphics
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_input.Buy.Released)
            {
                _input.Controller.Value.PurchaseContent();
                return;
            }
            // defer to hitting continue
            base.UpdateActive(gameTime);
        }
    }
}
