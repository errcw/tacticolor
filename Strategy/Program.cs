using System;

namespace Strategy
{
    static class Program
    {
        /// <summary>
        /// Entry point for the game.
        /// </summary>
        static void Main(string[] args)
        {
            using (StrategyGame game = new StrategyGame())
            {
                game.Run();
            }
        }
    }
}

