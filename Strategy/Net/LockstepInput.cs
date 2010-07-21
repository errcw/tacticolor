using System;

using Strategy.Interface;

namespace Strategy.Net
{
    /// <summary>
    /// Provides commands.
    /// </summary>
    public interface ICommandProvider
    {
        Command Update(int elapsed);
    }

    /// <summary>
    /// Gathers local input feeds it both to the match and to remote players.
    /// </summary>
    public class LockstepInput
    {
        public LockstepInput(LockstepMatch match, Player[] players)
        {
            _match = match;
            _players = players;
        }

        /// <summary>
        /// Updates the input for this frame.
        /// </summary>
        /// <param name="elapsed">The elapsed time, in milliseconds, since the last update.</param>
        public void Update(int elapsed)
        {

        }

        private LockstepMatch _match;
        private Player[] _players;
    }
}
