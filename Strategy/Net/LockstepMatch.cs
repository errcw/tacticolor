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
        /// The time step time.
        /// </summary>
        public int StepTime { get; set; }

        /// <summary>
        /// How far in the future commands issued now should expect to be executed.
        /// </summary>
        public int SchedulingOffset { get { return 2 * StepTime; } }

        /// <summary>
        /// Creates a new lockstep match for a given game.
        /// </summary>
        /// <param name="match"></param>
        public LockstepMatch(Match match)
        {
            _match = match;
            _commands = new List<Command>(match.PlayerCount * 10);
        }

        /// <summary>
        /// Schedules a command to be executed in the match.
        /// </summary>
        public void ScheduleCommand(Command command)
        {
            int index;
            for (index = 0; index < _commands.Count; index++)
            {
                if (_commands[index].Time > command.Time)
                {
                    break;
                }
            }
            _commands.Insert(index, command);
        }

        /// <summary>
        /// Updates the match for this time step. The match is only updated
        /// when all the commands for a given 
        /// </summary>
        /// <param name="elapsed">The elapsed time, in milliseconds, since the last update.</param>
        public void Update(int elapsed)
        {
            // early out when there is no input expected
            if (_match.Time < SchedulingOffset)
            {
                _match.Update(elapsed);
                return;
            }

            long stepTime = _match.Time;
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
                if (command.Time > stepEndTime)
                {
                    break;
                }
            }
        }

        private Match _match;

        private List<Command> _commands;
    }
}
