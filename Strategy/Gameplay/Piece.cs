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
        /// The owner of this piece.
        /// </summary>
        public readonly PlayerId Owner;

        /// <summary>
        /// True if this piece can perform an action; otherwise, false.
        /// </summary>
        public bool Ready { get { return _timer >= ReadyTime; } }

        /// <summary>
        /// The progress towards being ready in [0, 1].
        /// </summary>
        public float ReadyProgress { get; private set; }

        /// <summary>
        /// Creates a new piece that starts unready.
        /// </summary>
        public Piece(PlayerId owner) : this(owner, false)
        {
        }

        /// <summary>
        /// Creates a new piece.
        /// </summary>
        /// <param name="owner">The owner of this piece</param>
        /// <param name="startReady">If the piece should start ready.</param>
        public Piece(PlayerId owner, bool startReady)
        {
            Owner = owner;
            if (startReady)
            {
                _timer = ReadyTime;
                ReadyProgress = 1f;
            }
            else
            {
                _timer = 0;
                ReadyProgress = 0f;
            }
        }

        /// <summary>
        /// Updates the timer for this piece.
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        public void Update(int time)
        {
            _timer += time;
            ReadyProgress = Math.Min((float)_timer / ReadyTime, 1f);
        }

        /// <summary>
        /// Notifies this piece it was involved in an action.
        /// </summary>
        public void DidPerformAction()
        {
            _timer = 0;
            ReadyProgress = 0f;
        }

        private int _timer;
        private const int ReadyTime = 5000;
    }
}
