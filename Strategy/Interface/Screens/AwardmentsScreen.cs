using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Properties;
using Strategy.Library.Components;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;
using Strategy.Library.Sprite;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Lets the player view awardments.
    /// </summary>
    public class AwardmentsScreen : MenuScreen
    {
        public AwardmentsScreen(Game game) : base(game)
        {
            // grab the awardments for the gamer that opened the menu
            MenuInput input = game.Services.GetService<MenuInput>();
            string gamer = input.Controller.Value.GetSignedInGamer().Gamertag;
            Awardments awardments = game.Services.GetService<Awardments>();
            List<Awardment> gamerAwardments = awardments.GetAwardments(gamer);

            Texture2D earnedTex = game.Content.Load<Texture2D>("Images/AwardmentEarned");
            Texture2D unearnedTex = game.Content.Load<Texture2D>("Images/AwardmentNotEarned");
            SpriteFont titleFont = game.Content.Load<SpriteFont>("Fonts/TextSmallItalic");
            SpriteFont descFont = game.Content.Load<SpriteFont>("Fonts/TextSmall");

            // build an entry for every awardment
            MenuBuilder builder = new MenuBuilder(this, game);
            foreach (Awardment awardment in gamerAwardments)
            {
                Sprite image = new ImageSprite(awardment.IsEarned ? earnedTex : unearnedTex);
                image.Position = new Vector2(0, 15);

                Sprite title = new TextSprite(titleFont, awardment.Name);
                title.Position = new Vector2(image.Size.X + 10, 0);

                Sprite description = new TextSprite(descFont, awardment.Description);
                description.Position = new Vector2(
                    title.Position.X,
                    title.Position.Y + title.Size.Y + 5);

                Sprite awardmentSprite = new CompositeSprite(image, title, description);
                builder.CreateImageEntry(awardmentSprite);
            }

            // build an upsell panel
            _upsellPanel = new SlidingPanel(Resources.TrialUpsellAwardments, game.Content.Load<Texture2D>("Images/TrialUpsellIcon"), 720 - 40 - 75, game.Content);
            TrialModeObserverComponent trialObserver = game.Services.GetService<TrialModeObserverComponent>();
            trialObserver.TrialModeEnded += (s, a) => _upsellPanel.Hide();

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(150f, 120f);
            VisibleEntryCount = 1;
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
