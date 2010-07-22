using System;
using System.Collections.Generic;

using Strategy.Gameplay;

namespace Strategy.Net
{
    /// <summary>
    /// Manages running a match in lockstep across all player input.
    /// </summary>
    public class LockstepMatch
    {
        /// <summary>
        /// The underlying match running in lockstep.
        /// </summary>
        public Match Match { get { return _match; } }

        /// <summary>
        /// The time step time.
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

            StepTime = 50;
            SchedulingOffset = 50;

            // initial step is long to wait for the first player input
            _stepStartTime = 0;
            _stepEndTime = 2 * StepTime;
        }

        /// <summary>
        /// Schedules a command to be executed in the match.
        /// </summary>
        public void ScheduleCommand(Command command)
        {
            _commands.Add(command);
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
                if (HaveCommandsForStep(_stepEndTime))
                {
                    _stepStartTime = _stepEndTime;
                    _stepEndTime = _stepStartTime + StepTime;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Blocked starting step " + _stepEndTime);
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
                        System.Diagnostics.Debug.WriteLine("Tried to execute invalid command " + command);
                    }
                }
            }
            _commands.RemoveAll(c => c.Time >= _match.Time && c.Time < _match.Time + time);

            _match.Update(time);
        }

        /// <summary>
        /// Returns true if we received commands from every player for a time step.
        /// </summary>
        private bool HaveCommandsForStep(long stepStartTime)
        {
            bool[] received = new bool[_match.PlayerCount];
            long stepEndTime = stepStartTime + StepTime;

            foreach (Command command in _commands)
            {
                if (command.Time >= stepStartTime && command.Time < stepEndTime)
                {
                    received[(int)command.Player] = true;
                }
            }

            bool receivedAll = true;
            for (int i = 0; i < received.Length; i++)
            {
                if (!received[i])
                {
                    receivedAll = false;
                    break;
                }
            }

            return receivedAll;
        }

        private Match _match;

        private List<Command> _commands;
        private long _stepStartTime, _stepEndTime;
    }
}
