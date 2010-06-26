using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Library
{
    /// <summary>
    /// Interpolates between an initial and target value.
    /// </summary>
    /// <typeparam name="T">The type of value to interpolate.</typeparam>
    /// <param name="start">The initial value.</param>
    /// <param name="target">The target value.</param>
    /// <param name="progress">The progress in [0, 1].</param>
    /// <returns>The interpolated value.</returns>
    public delegate T Interpolate<T>(T start, T target, float progress);

    /// <summary>
    /// A set of interpolation function constructors.
    /// </summary>
    public static class Interpolation
    {
        /// <summary>
        /// Interpolates floats.
        /// </summary>
        public static Interpolate<float> InterpolateFloat(Ease easing)
        {
            return (start, target, progress) =>
                easing(start, target - start, progress);
        }

        /// <summary>
        /// Interpolates two-dimensional vectors.
        /// </summary>
        public static Interpolate<Vector2> InterpolateVector2(Ease easing)
        {
            return (start, target, progress) =>
                new Vector2(easing(start.X, target.X - start.X, progress),
                            easing(start.Y, target.Y - start.Y, progress));
        }

        /// <summary>
        /// Interpolates colours.
        /// </summary>
        public static Interpolate<Color> InterpolateColor(Ease easing)
        {
            return (start, target, progress) =>
                new Color(easing(start.R / 255f, (target.R - start.R) / 255f, progress),
                          easing(start.G / 255f, (target.G - start.G) / 255f, progress),
                          easing(start.B / 255f, (target.B - start.B) / 255f, progress),
                          easing(start.A / 255f, (target.A - start.A) / 255f, progress));
        }
    }
}
