using System;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Strategy.Library.Components;

namespace Strategy.Library.Extensions
{
    /// <summary>
    /// Extensions to Game.
    /// </summary>
    public static class GameExtensions
    {
        /// <summary>
        /// Creates and runs a game. If a debugger is not attached the game will handle
        /// exceptions by displaying an appropriate screen.
        /// </summary>
        /// <typeparam name="T">The type of game to run.</typeparam>
        public static void Run<T>() where T : Game, new()
        {
            if (Debugger.IsAttached)
            {
                using (var game = new T())
                {
                    game.Run();
                }
            }
            else
            {
                try
                {
                    using (var game = new T())
                    {
                        game.Run();
                    }
                }
                catch (Exception e)
                {
                    using (var game = new ExceptionDebugGame(e))
                    {
                        game.Run();
                    }
                }
            }
        }
    }
}
