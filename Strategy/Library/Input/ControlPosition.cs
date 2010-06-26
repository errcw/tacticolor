using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Strategy.Library.Input
{
    /// <summary>
    /// The position of a single control (thumb stick, trigger, etc.).
    /// </summary>
    /// <typeparam name="T">The type of state reported by the control.</typeparam>
    public class ControlPosition
    {
        /// <summary>
        /// The current position of the control.
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
        }

        /// <summary>
        /// If the control changed position between the previous and current frame.
        /// </summary>
        public bool Changed
        {
            get { return _position != _prevPosition; }
        }

        /// <summary>
        /// Updates the current position of the control.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        /// <param name="position">The current position of the control.</param>
        internal void Update(float time, Vector2 position)
        {
            _prevPosition = _position;
            _position = position;
        }

        private Vector2 _position;
        private Vector2 _prevPosition;
    }
}
