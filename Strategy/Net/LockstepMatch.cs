using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Strategy.Gameplay;

namespace Strategy.Net
{
    /// <summary>
    /// Manages running a match in lockstep across all player input.
    /// </summary>
    public class LockstepMatch
    {
        /// <summary>
        /// Occurs when the current step is finished simulating.
        /// </summary>
        public event EventHandler<EventArgs> StepEnded;

        /// <summary>
        /// The underlying match running in lockstep.
        /// </summary>
        public Match Match { get { return _match; } }

        /// <summary>
        /// The time at which the current step started.
        /// </summary>
        public long StepStart { get; private set; }

        /// <summary>
        /// The duration of the current step. The step runs for [StepStart,StepStart+StepTime).
        /// </summary>
        public int StepTime { get; set; }

        /// <summary>
        /// How far in the future commands issued now should expect to be executed.
        /// </summary>
        public int SchedulingOffset { get; set; }

        /// <summary>
        /// Returns, if the last update did not complete because one or more players was not
        /// synchronized, the players blocking the update; otherwise, an empty collection.
        /// </summary>
        public IEnumerable<PlayerId> BlockingPlayers { get; private set;  }

        /// <summary>
        /// Creates a new lockstep match for a given game.
        /// </summary>
        /// <param name="match"></param>
        public LockstepMatch(Match match)
        {
            _match = match;
            _commands = new CommandList();

            StepStart = -100;
            StepTime = 100;
            _stepEndTime = StepStart + StepTime;

            SchedulingOffset = 2 * StepTime;

            _readyStepStartTime = -1; // wait for start game command
            _readyStepStartTimes = new long[match.PlayerCount];
            for (int i = 0; i < _readyStepStartTimes.Length; i++)
            {
                _readyStepStartTimes[i] = _readyStepStartTime;
            }

            _stepHashes = new Dictionary<long, long>(2);
        }

        /// <summary>
        /// Schedules a command to be executed in the match.
        /// </summary>
        public void ScheduleCommand(Command command)
        {
            if (command is SynchronizationCommand)
            {
                HandleSynchronization((SynchronizationCommand)command);
            }
            else
            {
                Log("Scheduling " + command);
                _commands.Add(command);
            }
        }

        /// <summary>
        /// Updates the match for this frame. The match is only updated
        /// when all the commands for a given step are available.
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        public void Update(int time)
        {
            long updateEndTime = _match.Time + time;

            while (_commands.Count > 0)
            {
                // grab the next command to execute
                Command command = _commands.Peek();

                // bail if we have executed all the commands for this update
                if (command.Time > updateEndTime)
                {
                    break;
                }

                // consume the time leading up to the command
                int dtCommandTime = (int)(command.Time - _match.Time);
                if (!UpdateMatch(dtCommandTime))
                {
                    return; // blocked waiting for the next step
                }

                // execute the command at its specified time
                bool executed = command.Execute(_match);
                if (!executed)
                {
                    // do not consider this a fatal error because a player may issue
                    // commands that are invalidated by earlier remote commands
                    Log("Tried to execute invalid command " + command);
                }

                // remove the command now that we have successfully executed it
                _commands.Pop();
            }

            // consume the remaining time in the update
            int dtUpdateEndTime = (int)(updateEndTime - _match.Time);
            UpdateMatch(dtUpdateEndTime);
        }

        private bool UpdateMatch(int deltaTime)
        {
            Debug.Assert(deltaTime >= 0);

            // handle step transitions
            if (_match.Time + deltaTime >= _stepEndTime)
            {
                // update to right before the end of the step
                int dtStepEnd = (int)(_stepEndTime - _match.Time - 1);
                deltaTime -= dtStepEnd;
                _match.Update(dtStepEnd);

                long nextStepStart = _stepEndTime;
                if (nextStepStart <= _readyStepStartTime)
                {
                    if (StepEnded != null)
                    {
                        StepEnded(this, EventArgs.Empty);
                    }
                    StepStart = nextStepStart;
                    _stepEndTime = StepStart + StepTime;
                }
                else
                {
                    // bail and do not consume the remaining time
                    Log("Blocked starting step " + _stepEndTime);

                    BlockingPlayers = _readyStepStartTimes
                        .Select((time, id) => new { Time = time, Id = (PlayerId)id })
                        .Where((desc) => desc.Time < nextStepStart)
                        .Select((desc) => desc.Id);

                    return false;
                }
            }

            // update the (remaining) match time
            _match.Update(deltaTime);
            Debug.Assert(_match.Time <= _readyStepStartTime + StepTime);

            BlockingPlayers = Enumerable.Empty<PlayerId>();

            return true;
        }

        private void HandleSynchronization(SynchronizationCommand command)
        {
            // find the next step that is ready to be simulated
            int playerIdx = (int)command.Player;
            _readyStepStartTimes[playerIdx] = Math.Max(command.Time, _readyStepStartTimes[playerIdx]);
            _readyStepStartTime = _readyStepStartTimes.Min();
            Log("Setting ready time to " + _readyStepStartTime);

            // verify that the other players are running as expected
            // explicitly allow for infinite synchronization special case
            if (command.Time > _match.Time + SchedulingOffset + StepTime && command.Time < long.MaxValue)
            {
                Log("Got a sync command from " + command.Player + " for time " + command.Time + " but match time is only " + _match.Time);
                throw new OutOfSyncException("Got a sync command from too far in the future");
            }

            long expectedHash;
            if (_stepHashes.TryGetValue(command.HashTime, out expectedHash))
            {
                if (command.Hash != expectedHash)
                {
                    Log("Got hash " + command.Hash + " from " + command.Player + " but expected hash " + expectedHash);
                    throw new OutOfSyncException("Match simulation out of sync");
                }
            }
            else
            {
                _stepHashes[command.HashTime] = command.Hash;
            }
        }

        private void Log(string message)
        {
#if NET_DEBUG
#if XBOX
            Debug.WriteLine("X " + message);
#else
            Debug.WriteLine("C " + message);
#endif
#endif
        }

        private Match _match;

        private CommandList _commands;

        private long _stepEndTime;
        private long _readyStepStartTime;
        private long[] _readyStepStartTimes;
        private Dictionary<long, long> _stepHashes;
    }

    /// <summary>
    /// Signals that the game simulation has drifted out of sync with other clients.
    /// </summary>
    public class OutOfSyncException : Exception
    {
        public OutOfSyncException(string reason) : base(reason) { }
    }
}
