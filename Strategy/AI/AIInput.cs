using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Input;

namespace Strategy.AI
{
    /// <summary>
    /// AI difficulty rating.
    /// </summary>
    public enum AiDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// Generates input from a computer player.
    /// </summary>
    public class AiInput : ICommandProvider
    {
        /// <summary>
        /// Creates a new AI input.
        /// </summary>
        /// <param name="difficulty">The difficulty of the player.</param>
        /// <param name="random">The random number generator to use.</param>
        public AiInput(PlayerId player, Match match, AiDifficulty difficulty, Random random)
        {
            _player = player;
            _match = match;
            _difficulty = difficulty;
            _random = new Random(random.Next());

            switch (_difficulty)
            {
                case AiDifficulty.Easy: _evaluator = new EasyCommandEvaluator(this); break;
                case AiDifficulty.Normal: _evaluator = new NormalCommandEvaluator(this); break;
                case AiDifficulty.Hard: _evaluator = new HardCommandEvaluator(this); break;
            }

            _lastCommandTime = 0;
            _shouldScheduleCommand = true;
        }

        /// <summary>
        /// Updates the input state.
        /// </summary>
        public MatchCommand Update(int time)
        {
            MatchCommand command = null;

            if (_shouldScheduleCommand)
            {
                command = new AiDecisionCommand(_player, OnAiDecision);
                command.Time = _lastCommandTime + GetCooldownTime();

                _lastCommandTime = command.Time;
                _shouldScheduleCommand = false;

                Debug.Assert(_lastCommandTime > _match.Time);
            }

            return command;
        }

        private void OnAiDecision(Match match)
        {
            Debug.Assert(match == _match);
            Debug.Assert(match.Time == _lastCommandTime);

            List<PotentialCommand> commands = GetPotentialCommands();
            if (commands.Count > 0)
            {
                commands.ForEach(cmd => cmd.Score = _evaluator.RateCommand(cmd));
                var sortedCommands = commands.OrderBy(cmd => cmd.Score).Reverse();
                var bestScore = sortedCommands.First().Score;
                if (bestScore > 0)
                {
                    var bestCommands = sortedCommands.TakeWhile(cmd => cmd.Score == bestScore);
                    int randomBest = _random.Next(bestCommands.Count());
                    MatchCommand command = bestCommands.ElementAt(randomBest).GetCommand(_player);
                    command.Execute(match);
                }
            }

            // having made a decision, make a new one in the future
            _shouldScheduleCommand = true;
        }

        private List<PotentialCommand> GetPotentialCommands()
        {
            List<PotentialCommand> commands = new List<PotentialCommand>(32);

            // moving and attacking
            foreach (Territory src in GetOwnedTerritories())
            {
                // placement
                if (_match.CanPlacePiece(_player, src))
                {
                    commands.Add(new PotentialCommand(CommandType.Place, null, src));
                }
                foreach (Territory dst in src.Neighbors)
                {
                    // movement
                    if (_match.CanMove(_player, src, dst))
                    {
                        commands.Add(new PotentialCommand(CommandType.Move, src, dst));
                    }
                    // attack
                    if (_match.CanAttack(_player, src, dst))
                    {
                        commands.Add(new PotentialCommand(CommandType.Attack, src, dst));
                    }
                }
            }

            return commands;
        }

        private bool CanPlacePiece()
        {
            return _match.PiecesAvailable[(int)_player] > 0;
        }

        private IEnumerable<Territory> GetOwnedTerritories()
        {
            return _match.Map.Territories.Where(t => t.Owner == _player);
        }

        private bool HasReadyPieces(Territory territory)
        {
            return territory.Pieces.Any(p => p.Ready);
        }

        private int GetCooldownTime()
        {
            int baseTime = 0;
            switch (_difficulty)
            {
                case AiDifficulty.Easy: baseTime = 2000; break;
                case AiDifficulty.Normal: baseTime = 1000; break;
                case AiDifficulty.Hard: baseTime = 750; break;
            }
            int variationTime = (int)(baseTime * 0.2f);
            return _random.Next(baseTime - variationTime, baseTime + variationTime);
        }

        /// <summary>
        /// Describes the types of commands available to the player.
        /// </summary>
        private enum CommandType
        {
            Place,
            Move,
            Attack
        }

        /// <summary>
        /// Describes a command available to the player.
        /// </summary>
        private class PotentialCommand
        {
            public CommandType CommandType { get; private set; }
            public Territory Source { get; private set; }
            public Territory Destination { get; private set; }
            public int Score { get; set; }

            public PotentialCommand(CommandType commandType, Territory source, Territory destination)
            {
                CommandType = commandType;
                Source = source;
                Destination = destination;
            }

            /// <summary>
            /// Returns the associated command for this potential command.
            /// </summary>
            /// <param name="player">The player to construct the command for.</param>
            public MatchCommand GetCommand(PlayerId player)
            {
                switch (CommandType)
                {
                    case CommandType.Place: return new PlaceCommand(player, Destination);
                    case CommandType.Move: return new MoveCommand(player, Source, Destination);
                    case CommandType.Attack: return new AttackCommand(player, Source, Destination);
                }
                return null;
            }
        }

        /// <summary>
        /// Assigns scores to potential commands.
        /// </summary>
        private abstract class CommandEvaluator
        {
            public CommandEvaluator(AiInput input)
            {
                _input = input;
            }

            public int RateCommand(PotentialCommand command)
            {
                switch (command.CommandType)
                {
                    case CommandType.Place: return RatePlacement(command.Destination);
                    case CommandType.Move: return RateMove(command.Source, command.Destination);
                    case CommandType.Attack: return RateAttack(command.Source, command.Destination);
                    default: return -1;
                }
            }

            protected virtual int RatePlacement(Territory place)
            {
                return BasePlacementRating;
            }

            protected virtual int RateAttack(Territory atk, Territory def)
            {
                return BaseAttackRating;
            }

            protected virtual int RateMove(Territory src, Territory dst)
            {
                return BaseMovementRating;
            }

            protected AiInput _input;

            protected int BasePlacementRating = 10;
            protected int BaseAttackRating = 20;
            protected int BaseMovementRating = 5;
        }

        /// <summary>
        /// Assigns a fixed value to all possible commands.
        /// </summary>
        private class RandomCommandEvaluator : CommandEvaluator
        {
            public RandomCommandEvaluator(AiInput input) : base(input)
            {
                BasePlacementRating = BaseAttackRating = BaseMovementRating = 1;
            }
        }

        /// <summary>
        /// Makes generally poor decisions.
        /// </summary>
        private class EasyCommandEvaluator : CommandEvaluator
        {
            public EasyCommandEvaluator(AiInput input) : base(input)
            {
                BaseAttackRating = BasePlacementRating;
            }

            protected override int RatePlacement(Territory place)
            {
                bool hasEnemyNeighbors = place.Neighbors.Any(t => t.Owner != place.Owner);
                if (hasEnemyNeighbors)
                {
                    int enemyNeighbors = place.Neighbors.Count(t => t.Owner != null && t.Owner != _input._player);
                    int missingSlots = place.Capacity - place.Pieces.Count;
                    return BasePlacementRating + enemyNeighbors + missingSlots;
                }
                else
                {
                    // only allow placement next to enemy territories
                    // (i.e., restrict reinforcing interior territories)
                    return 0;
                }
            }

            protected override int RateAttack(Territory atk, Territory def)
            {
                int diff = atk.Pieces.Count(p => p.Ready) - def.Pieces.Count;
                bool fullAttacker = atk.Pieces.Count == atk.Capacity;
                return (diff >= 0 || fullAttacker) ? BaseAttackRating + diff : 0;
            }

            protected override int RateMove(Territory src, Territory dst)
            {
                // never consider moving
                return 0;
            }
        }

        /// <summary>
        /// Makes decent decisions.
        /// </summary>
        private class NormalCommandEvaluator : CommandEvaluator
        {
            public NormalCommandEvaluator(AiInput input) : base(input)
            {
            }

            protected override int RatePlacement(Territory place)
            {
                int enemyNeighbors = place.Neighbors.Count(t => t.Owner != null && t.Owner != _input._player);
                int missingSlots = place.Capacity - place.Pieces.Count;
                return BasePlacementRating + enemyNeighbors + missingSlots;
            }

            protected override int RateAttack(Territory atk, Territory def)
            {
                int diff = atk.Pieces.Count(p => p.Ready) - def.Pieces.Count;
                bool fullAttacker = atk.Pieces.Count == atk.Capacity;
                return (diff > 0 || fullAttacker) ? BaseAttackRating + diff : 0;
            }

            protected override int RateMove(Territory src, Territory dst)
            {
                int srcEnemyNeighbors = src.Neighbors.Count(t => t.Owner != null && t.Owner != _input._player);
                int dstEnemyNeighbors = dst.Neighbors.Count(t => t.Owner != null && t.Owner != _input._player);
                int diffEnemyNeighbors = dstEnemyNeighbors - srcEnemyNeighbors;
                int diffNeighbors = dst.Neighbors.Count - src.Neighbors.Count;
                return BaseMovementRating + diffEnemyNeighbors + diffNeighbors;
            }
        }

        /// <summary>
        /// Makes the best decisions it can.
        /// </summary>
        private class HardCommandEvaluator : CommandEvaluator
        {
            public HardCommandEvaluator(AiInput input) : base(input)
            {
            }

            protected override int RatePlacement(Territory place)
            {
                int enemyNeighbors = place.Neighbors.Count(t => t.Owner != _input._player);
                int emptyNeighbors = place.Neighbors.Count(t => t.Owner == null);
                if (enemyNeighbors != 0 || emptyNeighbors != 0)
                {
                    // prefer placement only next to enemy territories
                    int emptySlots = place.Capacity - place.Pieces.Count;
                    int readyPiecesBonus = place.Pieces.Any(p => p.Ready) ? 1 : 0;
                    return BasePlacementRating + 2 * enemyNeighbors + emptyNeighbors + emptySlots + readyPiecesBonus;
                }
                else
                {
                    // internal placement if no other spaces are viable
                    return BasePlacementRating - 1;
                }
            }

            protected override int RateAttack(Territory atk, Territory def)
            {
                int attackDiff = atk.Pieces.Count(p => p.Ready) - def.Pieces.Count;
                if (attackDiff > 0)
                {
                    // attacker has the advantage
                    int connectionBonus = def.Neighbors.Count < atk.Neighbors.Count ? 1 : 0;
                    return BaseAttackRating + attackDiff + connectionBonus;
                }
                else
                {
                    // attacker does not have the advantage, only attack if territory is full
                    return atk.Pieces.Count == atk.Capacity ? BaseAttackRating - 1 : 0;
                }
            }

            protected override int RateMove(Territory src, Territory dst)
            {
                int srcEnemyNeighborPieces = src.Neighbors.Where(t => t.Owner != _input._player).Sum(t => t.Pieces.Count);
                int dstEnemyNeighborPieces = dst.Neighbors.Where(t => t.Owner != _input._player).Sum(t => t.Pieces.Count);

                // avoid moving away from the front lines
                if (srcEnemyNeighborPieces > 0 && dstEnemyNeighborPieces == 0)
                {
                    return 0;
                }

                // moving behind the front lines: move towards the enemy or open spaces
                if (srcEnemyNeighborPieces == 0 && dstEnemyNeighborPieces == 0)
                {
                    int srcDistToEnemy = DistanceTo(src, t => t.Neighbors.Any(n => n.Owner != null && n.Owner != _input._player));
                    int dstDistToEnemy = DistanceTo(dst, t => t.Neighbors.Any(n => n.Owner != null && n.Owner != _input._player));
                    if (dstDistToEnemy < srcDistToEnemy)
                    {
                        return BaseMovementRating + (4 - dstDistToEnemy);
                    }
                    else
                    {
                        // no sense moving further away
                        return 0;
                    }
                }

                // moving in front of the enemy: move to protect more pieces
                int srcAttackDiff = srcEnemyNeighborPieces - src.Pieces.Count;
                int dstAttackDiff = dstEnemyNeighborPieces - Math.Min((src.Pieces.Count - 1) + dst.Pieces.Count, dst.Capacity);
                int attackDiff = dstAttackDiff - srcAttackDiff;
                if (attackDiff > 0)
                {
                    return BaseMovementRating + Math.Max(attackDiff, 2);
                }

                // no benefit: do not consider this move
                return 0;
            }

            private int DistanceTo(Territory start, Predicate<Territory> destinationCheck)
            {
                Queue<Territory> visit = new Queue<Territory>();
                Dictionary<Territory, int> visited = new Dictionary<Territory, int>();

                visited.Add(start, 0);
                visit.Enqueue(start);

                while (visit.Count > 0)
                {
                    Territory current = visit.Dequeue();
                    int distance = visited[current];
                    if (destinationCheck(current))
                    {
                        return distance;
                    }
                    foreach (Territory neighbor in current.Neighbors)
                    {
                        if (neighbor.Owner == _input._player && !visited.ContainsKey(neighbor))
                        {
                            visit.Enqueue(neighbor);
                            visited.Add(neighbor, distance + 1);
                        }
                    }
                }

                return int.MaxValue;
            }
        }

        private PlayerId _player;
        private Match _match;
        private AiDifficulty _difficulty;

        private Random _random;

        private CommandEvaluator _evaluator;

        private bool _shouldScheduleCommand;
        private long _lastCommandTime;
    }
}
