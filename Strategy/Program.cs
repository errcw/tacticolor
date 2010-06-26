using System;
using Strategy.Library.Extensions;

namespace Strategy
{
    static class Program
    {
        /// <summary>
        /// Entry point for the game.
        /// </summary>
        static void Main(string[] args)
        {
            GameExtensions.Run<StrategyGame>();
        }
    }
}

