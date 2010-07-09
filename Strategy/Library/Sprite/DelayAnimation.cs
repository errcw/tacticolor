using System;

namespace Strategy.Library.Sprite
{
    /// <summary>
    /// Does nothing for a given duration. Useful for padding sequential animations.
    /// </summary>
    public class DelayAnimation : IAnimation
    {
        /// <summary>
        /// Creates and starts a new delay animation.
        /// </summary>
        /// <param name="duration">The duration to wait, in seconds.</param>
        public DelayAnimation(float duration)
        {
            _duration = duration;
            Start();
        }

        /// <summary>
        /// Resets the elapsed time to zero.
        /// </summary>
        public void Start()
        {
            _elapsed = 0f;
        }

        /// <summary>
        /// Updates this delay animation.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        /// <returns>False after the delay has elapsed and true at all times before.</returns>
        public bool Update(float time)
        {
            _elapsed += time;
            if (_elapsed >= _duration)
            {
                return false;
            }
            return true;
        }

        private float _duration;
        private float _elapsed;
    }
}
