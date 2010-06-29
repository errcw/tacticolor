using System;
using System.Collections.Generic;
using System.Linq;

using Strategy.Library.Extensions;

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
        public event EventHandler<PiecePlacedEventArgs> PiecePlaced;

        /// <summary>
        /// Occurs when pieces are moved between territories.
        /// </summary>
        public event EventHandler<PiecesMovedEventArgs> PiecesMoved;

        /// <summary>
        /// Occurs when one territory attacks another.
        /// </summary>
        public event EventHandler<TerritoryAttackedEventArgs> TerritoryAttacked;

        /// <summary>
        /// Occurs when the game is won by a player.
        /// </summary>
        public event EventHandler<MatchEndedEventArgs> Ended;

        /// <summary>
        /// The map this match is played on.
        /// </summary>
        public Map Map { get { return _map; } }

        /// <summary>
        /// The number of pieces available to be placed by each player.
        /// </summary>
        public int[] PiecesAvailable { get; private set; }

        /// <summary>
        /// Creates a new match.
        /// </summary>
        /// <param name="map">The map on which to play.</param>
        /// <param name="random">The random number generator used to resolve battles.</param>
        public Match(Map map, Random random)
        {
            _map = map;
            _random = random;
            PiecesAvailable = new int[4];
        }

        /// <summary>
        /// Updates this match for the current frame.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public void Update(float time)
        {
            // update the piece action timers
            foreach (Territory territory in _map.Territories)
            {
                foreach (Piece piece in territory.Pieces)
                {
                    piece.Update(time);
                }
            }
            // update the piece counts
            _pieceCreationDelta += time;
            if (_pieceCreationDelta > PieceCreationTime)
            {
                for (int p = 0; p < PiecesAvailable.Length; p++)
                {
                    PiecesAvailable[p] = Math.Min(PiecesAvailable[p] + 1, MaxPiecesAvailable);
                }
                _pieceCreationDelta -= PieceCreationTime;
            }
        }

        /// <summary>
        /// Checks if pieces can attack from one territory to another.
        /// </summary>
        /// <param name="actor">The player initiating the attack.</param>
        /// <param name="attacker">The territory attacking.</param>
        /// <param name="defender">The territory defending.</param>
        public bool CanAttack(PlayerId actor, Territory attacker, Territory defender)
        {
            // cannot attack if the attacker and defender are the same
            if (attacker == defender)
            {
                return false;
            }
            // cannot attack from an unowned territory
            if (attacker.Owner != actor)
            {
                return false;
            }
            // cannot attack if the attacker and defender are both owned
            if (attacker.Owner == defender.Owner)
            {
                return false;
            }
            // cannot attack with fewer than two pieces
            if (attacker.Pieces.Count < 2)
            {
                return false;
            }
            // cannot attack if no pieces are ready
            if (!attacker.Pieces.Any(piece => piece.Ready))
            {
                return false;
            }
            // cannot attack non-adjacent territories
            if (!attacker.Neighbors.Contains(defender))
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
            int attackerTotal = attacker.Pieces.Count;
            int defenderTotal = defender.Pieces.Count;

            // build the set of attacking pieces
            List<PieceAttackData> attackers = new List<PieceAttackData>(attackerTotal);
            int attackerSum = 0;
            foreach (Piece piece in attacker.Pieces)
            {
                if (piece.Ready)
                {
                    PieceAttackData data = new PieceAttackData();
                    data.Piece = piece;
                    data.Roll = _random.Next(1, 6 + 1);
                    data.Survived = true;
                    attackers.Add(data);

                    attackerSum += data.Roll;

                    piece.DidPerformAction();
                }
            }

            // build the set of defending pieces
            List<PieceAttackData> defenders = new List<PieceAttackData>(defenderTotal);
            int defenderSum = 0;
            foreach(Piece piece in defender.Pieces)
            {
                PieceAttackData data = new PieceAttackData();
                data.Piece = piece;
                data.Roll = _random.Next(1, 6 + 1);
                data.Survived = true;
                defenders.Add(data);

                defenderSum += data.Roll;
            }

            Console.WriteLine("Attack: {0} Defense: {1}", attackerSum, defenderSum);

            if (attackerSum - defenderSum > 0) // attack succeeded
            {
                // remove all the killed defenders
                defender.Pieces.Clear();
                for (int i = 0; i < defenders.Count; i++)
                {
                    defenders[i].Survived = false;
                }

                // can lose all the attackers but must have a piece ready to move
                int attackersLost = (int)(3f * (float)defenderSum / attackerSum);
                attackersLost = Math.Min(attackerTotal - 1, attackersLost); 
                for (int i = 0; i < attackers.Count; i++)
                {
                    if (i < attackersLost) // remove the killed pieces
                    {
                        attackers[i].Survived = false;
                        attacker.Pieces.Remove(attackers[i].Piece);
                    }
                    else // move the surviving pieces
                    {
                        // if every piece was attacking then we must leave one piece behind
                        if (attackerTotal > attackers.Count || i != attackers.Count - 1)
                        {
                            attacker.Pieces.Remove(attackers[i].Piece);
                            defender.Pieces.Add(attackers[i].Piece);
                        }
                    }
                }

                // change the owner
                defender.Owner = attacker.Owner;
            }
            else // attack failed
            {
                // can lose all attackers or defenders but must leave a single piece
                int attackersLost = attackerTotal;
                attackersLost = Math.Min(attacker.Pieces.Count - 1, attackersLost);
                int defendersLost = (int)(3f * (float)attackerSum / defenderSum);
                defendersLost = Math.Min(defender.Pieces.Count - 1, defendersLost);

                // remove the killed defenders
                for (int i = 0; i < defendersLost; i++)
                {
                    defenders[i].Survived = false;
                    defender.Pieces.Remove(defenders[i].Piece);
                }

                // remove the killed attackers
                for (int i = 0; i < attackersLost; i++)
                {
                    attackers[i].Survived = false;
                    attacker.Pieces.Remove(attackers[i].Piece);
                }
            }

            if (TerritoryAttacked != null)
            {
                TerritoryAttacked(this, new TerritoryAttackedEventArgs(attacker, attackers, defender, defenders));
            }
        }

        /// <summary>
        /// Checkes if pieces can be moved one territory to another.
        /// </summary>
        /// <param name="actor">The player moving the pieces.</param>
        /// <param name="source">The source of the pieces.</param>
        /// <param name="destination">The destination of the pieces.</param>
        public bool CanMove(PlayerId actor, Territory source, Territory destination)
        {
            // cannot move if the source and destination are the same
            if (source == destination)
            {
                return false;
            }
            // cannot move pieces from an unowned territory
            if (source.Owner != actor)
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
            if (source.Pieces.Count < 2)
            {
                return false;
            }
            // cannot move if no pieces are ready
            if (!source.Pieces.Any(piece => piece.Ready))
            {
                return false;
            }
            // cannot move to a non-adjacent territory
            if (!source.Neighbors.Contains(destination))
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
            int toMove = Math.Min(destination.Capacity - destination.Pieces.Count, source.Pieces.Count - 1);
            List<Piece> moved = new List<Piece>(source.Pieces.Where(piece => piece.Ready).Take(toMove));
            foreach (Piece piece in moved)
            {
                source.Pieces.Remove(piece);
                destination.Pieces.Add(piece);
                piece.DidPerformAction();
            }
            if (moved.Count > 0 && PiecesMoved != null)
            {
                PiecesMoved(this, new PiecesMovedEventArgs(source, destination, moved));
            }
        }

        /// <summary>
        /// Checks if a piece can be added to a territory.
        /// </summary>
        /// <param name="actor">The player placing the piece.</param>
        /// <param name="location">The territory where the piece is placed.</param>
        public bool CanPlacePiece(PlayerId actor, Territory location)
        {
            // cannot place a piece on unowned territories
            if (actor != location.Owner)
            {
                return false;
            }
            // cannot place a piece if there is no room
            if (location.Pieces.Count >= location.Capacity)
            {
                return false;
            }
            // cannot place a piece if none are available
            if (PiecesAvailable[(int)actor] <= 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Adds a piece to a territory.
        /// </summary>
        /// <param name="location">The territory where the piece is placed.</param>
        public void PlacePiece(Territory location)
        {
            Piece added = new Piece();
            location.Pieces.Add(added);
            PiecesAvailable[(int)location.Owner] -= 1;

            if (PiecePlaced != null)
            {
                PiecePlaced(this, new PiecePlacedEventArgs(added, location));
            }
        }

        private Map _map;
        private Random _random;

        private float _pieceCreationDelta;

        private const float PieceCreationTime = 3f;
        private const int MaxPiecesAvailable = 5;
    }

    /// <summary>
    /// Event data for when a piece is placed on the map.
    /// </summary>
    public class PiecePlacedEventArgs : EventArgs
    {
        public Piece Piece { get; private set; }
        public Territory Location { get; private set; }

        public PiecePlacedEventArgs(Piece piece, Territory location)
        {
            Piece = piece;
            Location = location;
        }
    }

    /// <summary>
    /// Event data for when pieces are moved on the map.
    /// </summary>
    public class PiecesMovedEventArgs : EventArgs
    {
        public Territory Source { get; private set; }
        public Territory Destination { get; private set; }
        public ICollection<Piece> Pieces { get; private set; }

        public PiecesMovedEventArgs(Territory source, Territory destination, ICollection<Piece> pieces)
        {
            Source = source;
            Destination = destination;
            Pieces = pieces;
        }
    }

    /// <summary>
    /// Details about an attacking piece.
    /// </summary>
    public class PieceAttackData
    {
        public Piece Piece;
        public int Roll;
        public bool Survived;
    }

    /// <summary>
    /// Event data for when one territory attacks another.
    /// </summary>
    public class TerritoryAttackedEventArgs : EventArgs
    {
        public Territory Attacker { get; private set; }
        public ICollection<PieceAttackData> Attackers { get; private set; }
        public Territory Defender { get; private set; }
        public ICollection<PieceAttackData> Defenders { get; private set; }

        public TerritoryAttackedEventArgs(Territory attacker, ICollection<PieceAttackData> attackers, Territory defender, ICollection<PieceAttackData> defenders)
        {
            Attacker = attacker;
            Attackers = attackers;
            Defender = defender;
            Defenders = defenders;
        }
    }

    /// <summary>
    /// Event data for when the match ends.
    /// </summary>
    public class MatchEndedEventArgs : EventArgs
    {
        public PlayerId Winner { get; private set; }

        public MatchEndedEventArgs(PlayerId winner)
        {
            Winner = winner;
        }
    }
}
