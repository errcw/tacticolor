using System;

using Microsoft.Xna.Framework;

namespace Strategy.Library.Extensions
{
    /// <summary>
    /// Extensions to MathHelper.
    /// </summary>
    public static class MathHelperExtensions
    {
        /// <summary>
        /// Returns abs(a - b) <= epsilon.
        /// </summary>
        /// <param name="a">The first number to compare.</param>
        /// <param name="b">The second number to compare.</param>
        /// <param name="epsilon">The epsilon tolerance.</param>
        /// <returns>If a and b are within epsilon of each other.</returns>
        public static bool EpsilonEquals(float a, float b, float epsilon)
        {
            return Math.Abs(a - b) <= epsilon;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value. If value is less than min, min will be returned.</param>
        /// <param name="max">The maximum value. If value is greater than max, max will be returned.</param>
        /// <returns>The clamped value.</returns>
        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : ((value > max) ? max : value);
        }
    }
}
