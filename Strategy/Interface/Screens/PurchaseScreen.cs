using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;
using Strategy.Properties;
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

            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextLarge");

            // build the purchase instructions
            TextSprite instructions = new TextSprite(font, Resources.MenuPurchase);
            instructions.Position = new Vector2(
                _boxRightX - instructions.Size.X,
                _boxBottomY + instructions.Size.Y + 15);
            instructions.Color = new Color(60, 60, 60);

            ImageSprite button = new ImageSprite(game.Content.Load<Texture2D>("Images/ButtonX"));
            button.Position = new Vector2(
                instructions.Position.X - button.Size.X - 5,
                instructions.Position.Y + (instructions.Size.Y - button.Size.Y) / 2);

            _sprite.Add(instructions);
            _sprite.Add(button);

            // build the logo
            ImageSprite logo = new ImageSprite(game.Content.Load<Texture2D>("Images/Title"));
            logo.Position = new Vector2((1280 - logo.Size.X) / 2, 100);
            _sprite.Add(logo);

            // build the reasons
            Texture2D pieceTexture = game.Content.Load<Texture2D>("Images/PieceAvailable");

            CompositeSprite reasonSprite = new CompositeSprite();
            float x = 0;
            for (int i = 0; i < 4; i++)
            {
                ImageSprite pieceSprite = new ImageSprite(pieceTexture);
                pieceSprite.Color = ((PlayerId)i).GetPieceColor();
                pieceSprite.Position = new Vector2(x, 0f);
                x += pieceSprite.Size.X + PieceSeparation;

                TextSprite textSprite = new TextSprite(font);
                textSprite.Text = Resources.ResourceManager.GetString("TrialUpsellReason" + i, Resources.Culture);
                textSprite.Color = new Color(90, 90, 90);
                textSprite.Position = new Vector2(x, 0f);
                x += textSprite.Size.X + TextSeparation;

                reasonSprite.Add(pieceSprite);
                reasonSprite.Add(textSprite);
            }
            reasonSprite.Position = new Vector2(
                (1280 - reasonSprite.Size.X) / 2,
                600f);

            _sprite.Add(reasonSprite);
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

        private const float PieceSeparation = 5f;
        private const float TextSeparation = 40f;
    }
}
