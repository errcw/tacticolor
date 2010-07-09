using System;

namespace Strategy.Library.Sprite
{
    /// <summary>
    /// Animates multiple child animations simultaneously.
    /// </summary>
    public class CompositeAnimation : IAnimation
    {
        /// <summary>
        /// If each individual child animation should repeat.
        /// </summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Creates and starts a new composite animation.
        /// </summary>
        /// <param name="animations">The child animations.</param>
        public CompositeAnimation(params IAnimation[] animations)
        {
            _animations = animations;
            _running = new bool[_animations.Length];
            Start();
        }

        /// <summary>
        /// Starts each of the child animations.
        /// </summary>
        public void Start()
        {
            for (int i = 0; i < _animations.Length; i++)
            {
                _animations[i].Start();
                _running[i] = true;
            }
        }

        /// <summary>
        /// Updates each of the child animations.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        /// <returns>True if any child animation is running; otherwise, false.</returns>
        public bool Update(float time)
        {
            bool anyRunning = false;
            for (int i = 0; i < _animations.Length; i++)
            {
                if (_running[i])
                {
                    _running[i] = _animations[i].Update(time);
                    if (!_running[i] && Loop)
                    {
                        _animations[i].Start();
                        _running[i] = true;
                    }
                    if (_running[i])
                    {
                        anyRunning = true;
                    }
                }
            }
            return anyRunning;
        }

        private IAnimation[] _animations;
        private bool[] _running;
    }
}
