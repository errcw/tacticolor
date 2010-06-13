using System;
using System.Collections.Generic;

namespace Strategy.Gameplay
{
    /// <summary>
    /// The state of match.
    /// </summary>
    public class Match
    {
        /// <summary>
        /// Occurs when a piece is placed on the map.
        /// </summary>
        public event EventHandler<MatchEventArgs<Territory>> PiecePlaced;

        /// <summary>
        /// Occurs when pieces are moved between territories.
        /// </summary>
        public event EventHandler<EventArgs> PiecesMoved;

        /// <summary>
        /// Occurs when one territory attacks another.
        /// </summary>
        public event EventHandler<EventArgs> TerritoryAttacked;

        /// <summary>
        /// Occurs when the game is won by a player.
        /// </summary>
        public event EventHandler<MatchEventArgs<PlayerId>> GameFinished;

        /// <summary>
        /// Creates a new match.
        /// </summary>
        /// <param name="map">The map on which to play.</param>
        /// <param name="random">The random number generator used to resolve battles.</param>
        public Match(Map map, Random random)
        {
            _map = map;
            _random = random;
        }

        /// <summary>
        /// Updates this match for one tick of the game clock.
        /// </summary>
        public void Tick()
        {
        }

        /// <summary>
        /// Initiates an attack from one territory to another.
        /// </summary>
        /// <param name="attacker">The territory attacking.</param>
        /// <param name="defender">The territory defending.</param>
        public void Attack(Territory attacker, Territory defender)
        {
        }

        /// <summary>
        /// Moves pieces from one territory to another.
        /// </summary>
        /// <param name="source">The source of the pieces.</param>
        /// <param name="destination">The destination of the pieces.</param>
        public void Move(Territory source, Territory destination)
        {
        }

        private Map _map;
        private Random _random;
    }

    /// <summary>
    /// Event data for match events.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the args.</typeparam>
    public class MatchEventArgs<T> : EventArgs
    {
        /// <summary>
        /// The event data.
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// Creates a new match event.
        /// </summary>
        /// <param name="data">The event data.</param>
        public MatchEventArgs(T data)
        {
            Data = data;
        }
    }
}
