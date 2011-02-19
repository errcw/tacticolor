using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Allows the player to confirm exiting the game.
    /// </summary>
    public class InGameMenuScreen : MenuScreen
    {
        public InGameMenuScreen(Game game, PlayerIndex controller) : base(game)
        {
            MenuEntry returnEntry;

            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuReturnMatch, OnReturnSelected, out returnEntry)
                .CreatePurchaseButtonEntry(Resources.MenuPurchase)
                .CreateButtonEntry(Resources.MenuHelpOptions, OnHelpOptionsSelected)
                .CreateButtonEntry(Resources.MenuLeaveMatch, OnLeaveSelected);

            returnEntry.SuppressSelectSound = true;

            // transfer control to the player opening the menu
            _input = game.Services.GetService<MenuInput>();
            _previousController = _input.Controller.Value;
            _input.Controller = controller;

            _background = new ImageSprite(game.Content.Load<Texture2D>("Images/Colourable"));
            _background.Scale = new Vector2(1280, 720);
            _background.Color = Color.FromNonPremultiplied(64, 64, 64, 190);
            _background.Position = Vector2.Zero;

            TransitionOnTime = 0.5f;
            TransitionOffTime = 0.5f;
            BasePosition = new Vector2(130f, 80f);
        }

        public override void Draw()
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            _background.Draw(_spriteBatch);
            _spriteBatch.End();

            base.Draw();
        }

        protected internal override void Hide(bool popped)
        {
            if (popped)
            {
                // restore control to the menu owner
                _input.Controller = _previousController;
            }
            base.Hide(popped);
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            if (pushed)
            {
                _background.Color = ColorExtensions.FromNonPremultiplied(new Color(64, 64, 64), progress * 0.75f);
            }
            base.UpdateTransitionOn(gameTime, progress, pushed);
        }

        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            if (popped)
            {
                _background.Color = ColorExtensions.FromNonPremultiplied(new Color(64, 64, 64), (1 - progress) * 0.75f);
            }
            base.UpdateTransitionOff(gameTime, progress, popped);
        }

        private void OnReturnSelected(object sender, EventArgs args)
        {
            Stack.Pop();
        }

        private void OnHelpOptionsSelected(object sender, EventArgs args)
        {
            HelpOptionsScreen helpOptionsScreen = new HelpOptionsScreen(Stack.Game);
            Stack.Push(helpOptionsScreen);
        }

        private void OnLeaveSelected(object sender, EventArgs args)
        {
            LeaveMatchConfirmationScreen confirmationScreen = new LeaveMatchConfirmationScreen(Stack.Game);
            Stack.Push(confirmationScreen);
        }

        private MenuInput _input;
        private PlayerIndex _previousController;

        private Sprite _background; 
    }
}
