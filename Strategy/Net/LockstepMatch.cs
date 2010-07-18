using System;

using Strategy.Gameplay;

namespace Strategy.Net
{
    public class LockstepMatch
    {
        public LockstepMatch(Match match)
        {
            _match = match;
        }

        public void ScheduleCommand(Command command)
        {
        }

        public void Update(int elapsed)
        {
        }

        private Match _match;
    }
}
