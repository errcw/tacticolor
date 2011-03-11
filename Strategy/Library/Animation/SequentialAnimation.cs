using System;

namespace Strategy.Library.Animation
{
    /// <summary>
    /// Animates an ordered list of child animations.
    /// </summary>
    public class SequentialAnimation : IAnimation
    {
        /// <summary>
        /// If the entire sequence should repeat.
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Creates and starts a new sequential animation.
        /// </summary>
        /// <param name="animations">The animation sequence, in order from first to last.</param>
        public SequentialAnimation(params IAnimation[] animations)
        {
            _animations = animations;
            Start();
        }

        /// <summary>
        /// Starts the first animation in the sequence.
        /// </summary>
        public void Start()
        {
            _animationIndex = 0;
            _animations[_animationIndex].Start();
        }

        /// <summary>
        /// Updates the active animation in the sequence.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        /// <returns>True if the last animation in the sequence is finished; otherwise, false.</returns>
        public bool Update(float time)
        {
            if (_animationIndex < 0)
            {
                return false;
            }

            bool running = _animations[_animationIndex].Update(time);
            if (!running)
            {
                _animationIndex += 1;
                if (_animationIndex >= _animations.Length)
                {
                    _animationIndex = Loop ? 0 : -1;
                }
                if (_animationIndex >= 0)
                {
                    _animations[_animationIndex].Start();
                }
            }
            return _animationIndex >= 0;
        }

        private IAnimation[] _animations;
        private int _animationIndex;
    }
}