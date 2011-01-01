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

            SpriteFont titleFont = game.Content.Load<SpriteFont>("Fonts/TextSmallItalic");
            SpriteFont descFont = game.Content.Load<SpriteFont>("Fonts/TextSmall");

            // build an entry for every awardment
            MenuBuilder builder = new MenuBuilder(this, game);
            foreach (Awardment awardment in gamerAwardments)
            {
                builder.CreateImageEntry(BuildAwardmentSprite(awardment, titleFont, descFont));
            }

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(150f, 120f);
            VisibleEntryCount = 1;
        }

        private Sprite BuildAwardmentSprite(Awardment awardment, SpriteFont titleFont, SpriteFont descFont)
        {
            TextSprite title = new TextSprite(titleFont, awardment.Name);

            TextSprite description = new TextSprite(descFont, awardment.Description);
            description.Position = new Vector2(
                title.Position.X,
                title.Position.Y + title.Size.Y + 5);

            return new CompositeSprite(title, description);
        }
    }
}
