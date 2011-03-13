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
    public class ExitGameConfirmationScreen : MenuScreen
    {
        public ExitGameConfirmationScreen(Game game) : base(game)
        {
            MenuEntry returnEntry, exitEntry;

            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuExitNo, OnReturnSelected, out returnEntry)
                .CreateButtonEntry(Resources.MenuExitYes, OnExitSelected, out exitEntry);

            returnEntry.SuppressSelectSound = true;
            exitEntry.SuppressSelectSound = true;

            _upsellPanel = new SlidingPanel(Resources.TrialUpsellExit, game.Content.Load<Texture2D>("Images/PieceAvailable"), 720 - 40 - 75, game.Content);
            TrialModeObserverComponent trialObserver = game.Services.GetService<TrialModeObserverComponent>();
            trialObserver.TrialModeEnded += (s, a) => _upsellPanel.Hide();

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(150f, 120f);
        }

        private void OnReturnSelected(object sender, EventArgs args)
        {
            Stack.Pop();
        }

        private void OnExitSelected(object sender, EventArgs args)
        {
            Stack.PopAll();
        }

        protected internal override void Show(bool pushed)
        {
            if (Guide.IsTrialMode)
            {
                _upsellPanel.Show();
            }
            base.Show(pushed);
        }

        protected internal override void Hide(bool popped)
        {
            _upsellPanel.Hide();
            base.Hide(popped);
        }

        protected override void OnDraw()
        {
            _upsellPanel.Draw(_spriteBatch);
            base.OnDraw();
        }

        protected override void UpdateTransitionOn(GameTime gameTime, float progress, bool pushed)
        {
            _upsellPanel.Update(gameTime.GetElapsedSeconds());
            base.UpdateTransitionOn(gameTime, progress, pushed);
        }
        protected override void UpdateTransitionOff(GameTime gameTime, float progress, bool popped)
        {
            _upsellPanel.Update(gameTime.GetElapsedSeconds());
            base.UpdateTransitionOff(gameTime, progress, popped);
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            _upsellPanel.Update(gameTime.GetElapsedSeconds());
            base.UpdateActive(gameTime);
        }

        private SlidingPanel _upsellPanel;
    }
}
