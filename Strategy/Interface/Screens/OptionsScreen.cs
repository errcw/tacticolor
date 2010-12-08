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
            _options = game.Services.GetService<Options>();

            new MenuBuilder(this, game)
                .CreateToggleButtonEntry(Resources.MenuSounds, OnSoundEffectsToggled, _options.SoundEffectsToggle)
                .CreateToggleButtonEntry(Resources.MenuMusic, OnMusicToggled, _options.MusicToggle)
                .CreateToggleButtonEntry(Resources.MenuInstructions, OnInstructionsToggled, _options.InstructionsToggle);

            TransitionOnTime = 0.01f;
            BasePosition = new Vector2(170f, 160f);
        }

        private void OnSoundEffectsToggled(object sender, EventArgs args)
        {
            _options.SoundEffectsToggle = !_options.SoundEffectsToggle;
        }

        private void OnMusicToggled(object sender, EventArgs args)
        {
            _options.MusicToggle = !_options.MusicToggle;
        }

        private void OnInstructionsToggled(object sender, EventArgs args)
        {
            _options.InstructionsToggle = !_options.InstructionsToggle;
        }

        private Options _options;
    }
}
