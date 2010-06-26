using System;

using Microsoft.Xna.Framework;

namespace Strategy.Library.Extensions
{
    /// <summary>
    /// Extensions to GameTime.
    /// </summary>
    public static class GameTimeExtensions
    {
        /// <summary>
        /// Gets the total number of elapsed seconds since the last update.
        /// </summary>
        public static float GetElapsedSeconds(this GameTime gameTime)
        {
            return (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
