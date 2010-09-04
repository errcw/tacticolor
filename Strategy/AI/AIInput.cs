using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Input;

namespace Strategy.AI
{
    /// <summary>
    /// AI difficulty rating.
    /// </summary>
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// Generates input from a computer player.
    /// </summary>
    public class AIInput : ICommandProvider
    {
        public AIInput()
        {
        }

        /// <summary>
        /// Updates the input state.
        /// </summary>
        public Command Update(int time)
        {
            return null;
        }
    }
}
