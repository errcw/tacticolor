using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Library.Extensions;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Shows instructions for the controls and how to interact with the game.
    /// </summary>
    public class InstructionsView
    {
        public InstructionsView(LocalInput input, InterfaceContext context)
        {
            _options = context.Game.Services.GetService<Options>();
        }

        public void Update(float time)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // hide the instructions if requested by the user
            if (!_options.InstructionsToggle)
            {
                return;
            }
        }

        private Options _options;
    }
}
