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
    /// Allows players to modify global options.
    /// </summary>
    public class OptionsScreen : MenuScreen
    {
        public OptionsScreen(Game game) : base(game)
        {
            new MenuBuilder(this, game)
                .CreateButtonEntry(Resources.MenuSounds, OnOptionToggled)
                .CreateButtonEntry(Resources.MenuMusic, OnOptionToggled)
                .CreateButtonEntry(Resources.MenuInstructions, OnOptionToggled);

            _options = game.Services.GetService<Options>();

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(170f, 160f);
        }

        private void OnOptionToggled(object sender, EventArgs args)
        {
        }

        private Options _options;
    }
}
