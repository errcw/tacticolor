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
        /// Creates a new lockstep match for a given game.
        /// </summary>
        /// <param name="match"></param>
        public LockstepMatch(Match match)
        {
            _match = match;
            _commands = new List<Command>(match.PlayerCount * 3 * 10);

            StepTime = 100;
            StepStart = 0;
            _stepEndTime = StepTime;

            SchedulingOffset = 2 * StepTime;

            _readyStepStartTime = _stepEndTime + 2 * SchedulingOffset;
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
            Log("Scheduling " + command);
            if (command is SynchronizationCommand)
            {
                HandleSynchronization((SynchronizationCommand)command);
            }
            else
            {
                _commands.Add(command);
            }
        }

        /// <summary>
        /// Updates the match for this time step. The match is only updated
        /// when all the commands for a given 
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        public void Update(int time)
        {
            if (_match.Time + time >= _stepEndTime)
            {
                long nextStepStart = _stepEndTime;
                if (nextStepStart <= _readyStepStartTime)
                {
                    if (StepEnded != null)
                    {
                        StepEnded(this, EventArgs.Empty);
                    }
                    StepStart = nextStepStart;
                    Log("Starting step " + StepStart);
                    _stepEndTime = StepStart + StepTime;
                }
                else
                {
                    Log("Blocked starting step " + _stepEndTime);
                    return;
                }
            }

            foreach (Command command in _commands)
            {
                if (command.Time >= _match.Time && command.Time < _match.Time + time)
                {
                    bool executed = command.Execute(_match);
                    if (!executed)
                    {
                        // do not consider this a fatal error because a player may issue
                        // more piece placement commands than is possible because she
                        // does not see the piece assigment reflected immediately
                        Log("Tried to execute invalid command " + command);
                    }
                }
            }
            _commands.Where(c => c.Time >= _match.Time && c.Time < _match.Time + time).ToList();

            _match.Update(time);
            Debug.Assert(_match.Time <= _readyStepStartTime + StepTime);
        }

        private void HandleSynchronization(SynchronizationCommand command)
        {
            // find the next step that is ready to be simulated
            int playerIdx = (int)command.Player;
            _readyStepStartTimes[playerIdx] = Math.Max(command.Time, _readyStepStartTimes[playerIdx]);
            _readyStepStartTime = _readyStepStartTimes.Min();
            Log("Setting ready time to " + _readyStepStartTime);

            // verify that the other players are running as expected
            if (command.Time > _match.Time + SchedulingOffset + StepTime)
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

        private List<Command> _commands;

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
