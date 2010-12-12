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
    /// Displays the credits.
    /// </summary>
    public class CreditsScreen : MenuScreen
    {
        public CreditsScreen(Game game) : base(game)
        {
            SpriteFont font = game.Content.Load<SpriteFont>("Fonts/TextSmall");
            SpriteFont titleFont = game.Content.Load<SpriteFont>("Fonts/TextSmallItalic");
            Sprite main = new TextSprite(font, Resources.CreditsMain) { Position = new Vector2(0, 0) };
            Sprite musicTitle = new TextSprite(titleFont, Resources.CreditsMusic) { Position = new Vector2(0, 30) };
            Sprite musicList = new TextSprite(font, Resources.CreditsMusicList) { Position = new Vector2(15, 50) };
            Sprite soundTitle = new TextSprite(titleFont, Resources.CreditsSound) { Position = new Vector2(0, 75) };
            Sprite soundList = new TextSprite(font, Resources.CreditsSoundList) { Position = new Vector2(15, 95) };
            Sprite credits = new CompositeSprite(main, musicTitle, musicList, soundTitle, soundList);

            new MenuBuilder(this, game).CreateImageEntry(credits);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(170f, 160f);
        }
    }
}
