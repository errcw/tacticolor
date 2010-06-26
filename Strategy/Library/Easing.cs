using System;

namespace Strategy.Library
{
    /// <summary>
    /// Animates a value between a start and end point.
    /// </summary>
    /// <param name="start">The initial value.</param>
    /// <param name="delta">The difference between the start and end values.</param>
    /// <param name="progress">The quotient of elapsed time and total duration in [0, 1].</param>
    public delegate float Ease(float start, float delta, float progress);

    /// <summary>
    /// A collection of easing functions.
    /// </summary>
    public static class Easing
    {
        /// <summary>
        /// Animates at uniform speed between two values.
        /// </summary>
        public static float Uniform(float start, float delta, float progress)
        {
            return delta * progress + start;
        }

        /// <summary>
        /// Starts slowly and accelerates.
        /// </summary>
        public static float QuadraticIn(float start, float delta, float progress)
        {
            return delta * (progress * progress) + start;
        }

        /// <summary>
        /// Starts quickly and decelerates.
        /// </summary>
        public static float QuadraticOut(float start, float delta, float progress)
        {
            return -delta * (progress * (progress - 2f)) + start;
        }

        /// <summary>
        /// Accelerates at the start and decelerates at the end.
        /// </summary>
        public static float QuadraticInOut(float start, float delta, float progress)
        {
            if (progress < 0.5f)
            {
                float inProgress = progress * 2f;
                return (delta / 2f) * (inProgress * inProgress) + start;
            }
            float outProgress = (progress - 0.5f) * 2f;
            return -delta / 2f * (outProgress * (outProgress - 2f)) + start;
        }
    }
}