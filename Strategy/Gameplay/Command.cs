using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Strategy.Gameplay
{
    /// <summary>
    /// An operation applied to a match.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Executes this command in the context of the given match.
        /// </summary>
        void Execute(Match match);
    }
}
