using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace Strategy.Library.Input
{
    /// <summary>
    /// A vibration function.
    /// </summary>
    /// <param name="time">The elapsed time, in seconds, since the last update.</param>
    /// <returns>The amount of vibration, or null if this vibration has finished.</returns>
    public delegate Vector2? Vibration(float time);

    /// <summary>
    /// A set of vibration functions.
    /// </summary>
    public static class Vibrations
    {
        /// <summary>
        /// Creates a constant vibration.
        /// </summary>
        /// <param name="leftMotor">The speed of the left, low-frequency motor in [0, 1].</param>
        /// <param name="rightMotor">The speed of the right, high-frequency motor in [0, 1].</param>
        /// <param name="duration">The duration, in seconds, to vibrate for.</param>
        /// <returns>A vibration function.</returns>
        public static Vibration Constant(float leftMotor, float rightMotor, float duration)
        {
            return InterpolateVibration(new Vector2(leftMotor, rightMotor), new Vector2(leftMotor, rightMotor), Easing.Uniform, duration);
        }

        /// <summary>
        /// Creates a vibration that fades in.
        /// </summary>
        /// <param name="leftMotor">The speed of the left, low-frequency motor in [0, 1].</param>
        /// <param name="rightMotor">The speed of the right, high-frequency motor in [0, 1].</param>
        /// <param name="duration">The duration, in seconds, to vibrate for.</param>
        /// <param name="easing">The easing function between the vibration values.</param>
        /// <returns>A vibration function.</returns>
        public static Vibration FadeIn(float leftMotor, float rightMotor, float duration, Ease easing)
        {
            return InterpolateVibration(Vector2.Zero, new Vector2(leftMotor, rightMotor), easing, duration);
        }

        /// <summary>
        /// Creates a vibration that fades out.
        /// </summary>
        /// <param name="leftMotor">The speed of the left, low-frequency motor in [0, 1].</param>
        /// <param name="rightMotor">The speed of the right, high-frequency motor in [0, 1].</param>
        /// <param name="duration">The duration, in seconds, to vibrate for.</param>
        /// <param name="easing">The easing function between the vibration values.</param>
        /// <returns>A vibration function.</returns>
        public static Vibration FadeOut(float leftMotor, float rightMotor, float duration, Ease easing)
        {
            return InterpolateVibration(new Vector2(leftMotor, rightMotor), Vector2.Zero, easing, duration);
        }

        /// <summary>
        /// Creates a composite vibration function.
        /// </summary>
        /// <param name="vibrations">The vibration functions to put in sequence.</param>
        /// <returns>A vibration function.</returns>
        public static Vibration Sequence(params Vibration[] vibrations)
        {
            int position = 0;
            return delegate(float time)
            {
                if (position >= vibrations.Length)
                {
                    return null;
                }
                // find the next non-null amount
                Vector2? amount;
                for (amount = vibrations[position](time);
                     amount == null && position < vibrations.Length;
                     position++) ;
                return amount;
            };
        }

        /// <summary>
        /// Creates a vibration that interpolates between two values.
        /// </summary>
        /// <param name="start">The initial vibration amount.</param>
        /// <param name="target">The final vibration amount.</param>
        /// <param name="easing">The easing function for the vibration amounts.</param>
        /// <param name="duration">The duration of the vibration.</param>
        private static Vibration InterpolateVibration(Vector2 start, Vector2 target, Ease easing, float duration)
        {
            Interpolate<Vector2> interpolate = Interpolation.InterpolateVector2(easing);
            float elapsed = 0f;
            return delegate(float time)
            {
                elapsed += time;
                return (elapsed < duration) ? interpolate(start, target, elapsed / duration) : (Vector2?)null;
            };
        }
    }
}
