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
    /// Displays instructions how to play the game.
    /// </summary>
    public class HowToPlayScreen : MenuScreen
    {
        public HowToPlayScreen(Game game) : base(game)
        {
            CompositeSprite page1 = new CompositeSprite();
            CompositeSprite page2 = new CompositeSprite();

            new MenuBuilder(this, game)
                .CreateImageEntry(page1)
                .CreateImageEntry(page2);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(170f, 160f);
            VisibleEntryCount = 1;
        }
    }
}
