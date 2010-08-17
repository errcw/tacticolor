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
        /// Occurs when one player is eliminated from this match.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerEliminated;

        /// <summary>
        /// Occurs when the game is won by a player.
        /// </summary>
        public event EventHandler<PlayerEventArgs> Ended;

        /// <summary>
        /// The maximum number of players in a match.
        /// </summary>
        public const int MaxPlayerCount = 4;

        /// <summary>
        /// The maximum number of pieces available to place by a player.
        /// </summary>
        public readonly int MaxPiecesAvailable = 5;

        /// <summary>
        /// The map this match is played on.
        /// </summary>
        public Map Map { get { return _map; } }

        /// <summary>
        /// The number of players in this match (counts losers).
        /// </summary>
        public int PlayerCount { get; private set; }

        /// <summary>
        /// The number of pieces available to be placed by each player.
        /// </summary>
        public int[] PiecesAvailable { get; private set; }

        /// <summary>
        /// The progress towards the next piece (in [0, 1]) for each player. If
        /// the player has the maximum number of pieces this value is invalid.
        /// </summary>
        public float[] PieceCreationProgress { get; private set; }

        /// <summary>
        /// The current game time.
        /// </summary>
        public long Time { get; private set; }


        /// <summary>
        /// Creates a new match.
        /// </summary>
        /// <param name="map">The map on which to play.</param>
        /// <param name="random">The random number generator used to resolve battles.</param>
        public Match(Map map, Random random)
        {
            _map = map;
            _random = random;

            PlayerCount = GetPlayerCount();
            _playersRemaining = PlayerCount;

            SetInitialTerritoryState();
            SetInitialPieceState();

            Time = 0;
        }

        /// <summary>
        /// Updates this match for the current frame.
        /// </summary>
        /// <param name="time">The elapsed time, in seconds, since the last update.</param>
        public void Update(int time)
        {
            Time += time;
            UpdateTerritories(time);
            UpdatePieces(time);
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
            // cannot attack if the attacker or defender are cooling down
            if (attacker.Cooldown > 0 || defender.Cooldown > 0)
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
            PlayerId? previousOwner = defender.Owner;

            // treat this attack like a move if the defender is unowned
            if (previousOwner == null)
            {
                defender.Owner = attacker.Owner;
                Move(attacker, defender);
                TerritoryDidChangeOwners(defender, previousOwner);
                return;
            }

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
                    data.Moved = false;
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
                data.Moved = false;
                defenders.Add(data);

                defenderSum += data.Roll;
            }

            bool success = attackerSum - defenderSum > 0;
            if (success) // attack succeeded
            {
                // remove all the killed defenders
                defender.Pieces.Clear();
                for (int i = 0; i < defenders.Count; i++)
                {
                    defenders[i].Survived = false;
                }

                // can lose all the attackers but must have a piece ready to move
                int attackersLost = (int)(3f * (float)defenderSum / attackerSum);
                attackersLost = Math.Min(attackers.Count - (attackerTotal > attackers.Count ? 1 : 2), attackersLost);
                int attackersMoved = attackers.Count - attackersLost - (attackerTotal > attackers.Count ? 0 : 1);
                attackersMoved = Math.Min(defender.Capacity, attackersMoved);
                for (int i = 0; i < attackers.Count; i++)
                {
                    if (i < attackersLost) // remove the killed pieces
                    {
                        attackers[i].Survived = false;
                        attacker.Pieces.Remove(attackers[i].Piece);
                    }
                    else if (i < attackersLost + attackersMoved)
                    {
                        attackers[i].Moved = true;
                        attacker.Pieces.Remove(attackers[i].Piece);
                        defender.Pieces.Add(attackers[i].Piece);
                    }
                }

                // change the owner
                defender.Owner = attacker.Owner;
                TerritoryDidChangeOwners(defender, previousOwner);
            }
            else // attack failed
            {
                // can lose all attackers or defenders but must leave a single piece
                int attackersLost = attackers.Count;
                attackersLost = Math.Min(attacker.Pieces.Count - 1, attackersLost);
                int defendersLost = (int)(3f * (float)attackerSum / defenderSum);
                defendersLost = Math.Max(1, defendersLost);
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

            // set the cooldowns
            int cooldown = CooldownAttackBase + CooldownAttackPerPiece * (attackers.Count + defenders.Count);
            attacker.Cooldown = cooldown;
            defender.Cooldown = cooldown;

            if (TerritoryAttacked != null)
            {
                TerritoryAttacked(this, new TerritoryAttackedEventArgs(attacker, attackers, defender, defenders, success));
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
            // cannot move if the source or destination are cooling down
            if (source.Cooldown > 0 || destination.Cooldown > 0)
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
            destination.Cooldown = CooldownMove;
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
            // cannot place a piece if territory is cooling down
            if (location.Cooldown > 0)
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
            Piece added = new Piece(location.Owner.Value, false);
            location.Pieces.Add(added);
            PiecesAvailable[(int)location.Owner] -= 1;

            if (PiecePlaced != null)
            {
                PiecePlaced(this, new PiecePlacedEventArgs(added, location));
            }
        }

        /// <summary>
        /// Calculates a hash of the match state.
        /// </summary>
        public long GetStateHash()
        {
            return 0;
        }

        /// <summary>
        /// Configures the initial territory counts and state.
        /// </summary>
        private void SetInitialTerritoryState()
        {
            _numTerritoriesOwned = new int[PlayerCount];
            foreach (Territory territory in _map.Territories)
            {
                territory.Cooldown = 0;
                if (territory.Owner.HasValue)
                {
                    _numTerritoriesOwned[(int)territory.Owner] += 1;
                }
            }
        }

        /// <summary>
        /// Configures the initial piece counts and state.
        /// </summary>
        private void SetInitialPieceState()
        {
            _pieceCreationElapsed = new int[PlayerCount];
            _pieceCreationSpeed = new float[PlayerCount];
            PieceCreationProgress = new float[PlayerCount];
            PiecesAvailable = new int[PlayerCount];

            for (int p = 0; p < PlayerCount; p++)
            {
                _pieceCreationElapsed[p] = 0;
                _pieceCreationSpeed[p] = GetPieceCreationSpeed(_numTerritoriesOwned[p]);
                PieceCreationProgress[p] = 0f;
                PiecesAvailable[p] = 0;
            }
        }

        /// <summary>
        /// Updates the state of all the territories.
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        private void UpdateTerritories(int time)
        {
            foreach (Territory territory in _map.Territories)
            {
                territory.Cooldown = Math.Max(territory.Cooldown - time, 0);
            }
        }

        /// <summary>
        /// Updates the state of all the pieces, including generating new ones.
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        private void UpdatePieces(int time)
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
            for (int p = 0; p < PlayerCount; p++)
            {
                if (PiecesAvailable[p] >= MaxPiecesAvailable)
                {
                    continue;
                }

                _pieceCreationElapsed[p] += (int)Math.Floor(time * _pieceCreationSpeed[p]);
                PieceCreationProgress[p] = Math.Min(_pieceCreationElapsed[p] / (float)PieceCreationTime, 1f);

                if (_pieceCreationElapsed[p] >= PieceCreationTime)
                {
                    PiecesAvailable[p] += 1;

                    _pieceCreationElapsed[p] = 0;
                    PieceCreationProgress[p] = 0f;
                }
            }
        }

        /// <summary>
        /// Handles a territory changing owners.
        /// </summary>
        private void TerritoryDidChangeOwners(Territory territory, PlayerId? previousOwner)
        {
            int ownerIdx = (int)territory.Owner;
            _numTerritoriesOwned[ownerIdx] += 1;
            _pieceCreationSpeed[ownerIdx] = GetPieceCreationSpeed(_numTerritoriesOwned[ownerIdx]);

            // might not have a value if territory was unowned
            if (previousOwner.HasValue)
            {
                int prevIdx = (int)previousOwner;
                _numTerritoriesOwned[prevIdx] -= 1;
                if (_numTerritoriesOwned[prevIdx] <= 0)
                {
                    PlayerWasEliminated(previousOwner.Value);
                }
                _pieceCreationSpeed[prevIdx] = GetPieceCreationSpeed(_numTerritoriesOwned[prevIdx]);
            }
        }

        /// <summary>
        /// Notifies this match that a player was eliminated.
        /// </summary>
        private void PlayerWasEliminated(PlayerId player)
        {
            _playersRemaining -= 1;
            if (PlayerEliminated != null)
            {
                PlayerEliminated(this, new PlayerEventArgs(player));
            }
            if (_playersRemaining == 1 && Ended != null)
            {
                Ended(this, new PlayerEventArgs(player));
            }
        }

        /// <summary>
        /// Returns a territory creation speed.
        /// </summary>
        private float GetPieceCreationSpeed(int numTerritoriesOwned)
        {
            return Math.Min(PieceCreationBaseSpeed + numTerritoriesOwned * 0.1f, PieceCreationMaxSpeed);
        }

        /// <summary>
        /// Returns the number of players owning territories on the map.
        /// </summary>
        private int GetPlayerCount()
        {
            bool[] sawOwner = new bool[4];
            int players = 0;
            foreach (Territory territory in _map.Territories)
            {
                if (territory.Owner.HasValue && !sawOwner[(int)territory.Owner])
                {
                    players += 1;
                    sawOwner[(int)territory.Owner] = true;
                }
            }
            return players;
        }

        private Map _map;
        private Random _random;

        private int _playersRemaining;

        private int[] _pieceCreationElapsed;
        private float[] _pieceCreationSpeed;

        private int[] _numTerritoriesOwned;

        private const int PieceCreationTime = 5000;
        private const float PieceCreationBaseSpeed = 1f;
        private const float PieceCreationMaxSpeed = 2f;

        private const int CooldownMove = 500;
        private const int CooldownAttackBase = 600;
        private const int CooldownAttackPerPiece = 100;
    }

    /// <summary>
    /// Event data for when a piece is placed on the map.
    /// </summary>
    public class PiecePlacedEventArgs : EventArgs
    {
        public readonly Piece Piece;
        public readonly Territory Location;

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
        public readonly Territory Source;
        public readonly Territory Destination;
        public readonly ICollection<Piece> Pieces;

        public PiecesMovedEventArgs(Territory source, Territory destination, ICollection<Piece> pieces)
        {
            Source = source;
            Destination = destination;
            Pieces = pieces;
        }
    }

    /// <summary>
    /// Details about an attacking or defending piece.
    /// </summary>
    public class PieceAttackData
    {
        public Piece Piece;
        public int Roll;
        public bool Survived;
        public bool Moved;
    }

    /// <summary>
    /// Event data for when one territory attacks another.
    /// </summary>
    public class TerritoryAttackedEventArgs : EventArgs
    {
        public readonly Territory Attacker;
        public readonly ICollection<PieceAttackData> Attackers;
        public readonly Territory Defender;
        public readonly ICollection<PieceAttackData> Defenders;
        public readonly bool Successful;

        public TerritoryAttackedEventArgs(Territory attacker, ICollection<PieceAttackData> attackers, Territory defender, ICollection<PieceAttackData> defenders, bool successful)
        {
            Attacker = attacker;
            Attackers = attackers;
            Defender = defender;
            Defenders = defenders;
            Successful = successful;
        }
    }

    /// <summary>
    /// Event data for when a player is eliminated or the match ends.
    /// </summary>
    public class PlayerEventArgs : EventArgs
    {
        public readonly PlayerId Player;

        public PlayerEventArgs(PlayerId player)
        {
            Player = player;
        }
    }
}
