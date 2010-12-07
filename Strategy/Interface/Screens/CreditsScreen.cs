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
            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(170f, 160f);
        }
    }
}
