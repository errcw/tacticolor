using System;
using System.Collections.Generic;
using System.Linq;

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
        public event EventHandler<MatchEventArgs<List<Piece>>> PiecesMoved;

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
        /// Updates this match for the current frame.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public void Update(float time)
        {
            foreach (Territory territory in _map.Territories)
            {
                foreach (Piece piece in territory.Pieces)
                {
                    piece.Update(time);
                }
            }
        }

        /// <summary>
        /// Checks if pieces can attack from one territory to another.
        /// </summary>
        /// <param name="attacker">The territory attacking.</param>
        /// <param name="defender">The territory defending.</param>
        public bool CanAttack(Territory attacker, Territory defender)
        {
            // cannot attack if the attacker and defender are the same
            if (attacker == defender)
            {
                return false;
            }
            // cannot attack if the attacker and defender are both owned
            if (attacker.Owner == defender.Owner)
            {
                return false;
            }
            // cannot attack with fewer than two pieces
            if (attacker.Pieces.Count <= 1)
            {
                return false;
            }
            // cannot attack if no pieces are ready
            if (!attacker.Pieces.Any(piece => piece.Ready))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initiates an attack from one territory to another.
        /// </summary>
        /// <param name="attacker">The territory attacking.</param>
        /// <param name="defender">The territory defending.</param>
        public void Attack(Territory attacker, Territory defender)
        {
            //TODO
            defender.Pieces.Clear();
            defender.Owner = attacker.Owner;
            Move(attacker, defender);
        }

        /// <summary>
        /// Checkes if pieces can be moved one territory to another.
        /// </summary>
        /// <param name="source">The source of the pieces.</param>
        /// <param name="destination">The destination of the pieces.</param>
        public bool CanMove(Territory source, Territory destination)
        {
            // cannot move if the source and destination are the same
            if (source == destination)
            {
                return false;
            }
            // cannot move if the source and destination are not both owned
            if (source.Owner != destination.Owner)
            {
                return false;
            }
            // cannot move if there is no room
            if (destination.Capacity - destination.Pieces.Count == 0)
            {
                return false;
            }
            // cannot move if there are fewer than two pieces
            if (source.Pieces.Count <= 1)
            {
                return false;
            }
            // cannot move if no pieces are ready
            if (!source.Pieces.Any(piece => piece.Ready))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Moves pieces from one territory to another.
        /// </summary>
        /// <param name="source">The source of the pieces.</param>
        /// <param name="destination">The destination of the pieces.</param>
        public void Move(Territory source, Territory destination)
        {
            int toMove = Math.Min(
                destination.Capacity - destination.Pieces.Count,
                source.Pieces.Count - 1);
            List<Piece> moved = new List<Piece>(toMove);
            for (int i = 0; i < toMove; i++)
            {
                if (source.Pieces[i].Ready)
                {
                    moved.Add(source.Pieces[i]);
                }
            }
            foreach (Piece piece in moved)
            {
                piece.DidPerformAction();
                source.Pieces.Remove(piece);
                destination.Pieces.Add(piece);
            }
            if (moved.Count > 0 && PiecesMoved != null)
            {
                PiecesMoved(this, new MatchEventArgs<List<Piece>>(moved));
            }
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
