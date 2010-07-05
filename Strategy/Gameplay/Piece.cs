using System;

using Strategy.Library.Extensions;

namespace Strategy.Gameplay
{
    /// <summary>
    /// A piece on the map.
    /// </summary>
    public class Piece
    {
        /// <summary>
        /// The current value of the action timer.
        /// </summary>
        public int TimerValue { get; private set; }

        /// <summary>
        /// The maximum value of the action timer, when the piece will be ready.
        /// </summary>
        public int TimerMax { get; private set; }

        /// <summary>
        /// True if this piece can perform an action; otherwise, false.
        /// </summary>
        public bool Ready { get { return TimerValue >= TimerMax; } }

        /// <summary>
        /// Creates a new piece that starts unready.
        /// </summary>
        public Piece() : this(false)
        {
        }

        /// <summary>
        /// Creates a new piece.
        /// </summary>
        /// <param name="startReady">If the piece should start ready.</param>
        public Piece(bool startReady)
        {
            _timer = 0;
            TimerMax = TimerStandardMax;
            TimerValue = startReady ? TimerMax : 0;
        }

        /// <summary>
        /// Updates the timer for this piece.
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        public void Update(int time)
        {
            _timer += time;
            if (_timer >= TimerIncrementTime)
            {
                TimerValue = Math.Min(TimerValue + 1, TimerMax);
                _timer -= TimerIncrementTime;
            }
        }

        /// <summary>
        /// Notifies this piece it was involved in an action.
        /// </summary>
        public void DidPerformAction()
        {
            _timer = 0;
            TimerValue = 0;
        }

        private int _timer;

        private const int TimerStandardMax = 4;
        private const int TimerIncrementTime = 2000;
    }
}
