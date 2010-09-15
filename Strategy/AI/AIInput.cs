using System;
using System.Collections.Generic;
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
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// Generates input from a computer player.
    /// </summary>
    public class AIInput : ICommandProvider
    {
        /// <summary>
        /// Creates a new AI input.
        /// </summary>
        /// <param name="difficulty">The difficulty of the player.</param>
        /// <param name="random">The random number generator to use.</param>
        public AIInput(PlayerId player, Match match, AIDifficulty difficulty, Random random)
        {
            _player = player;
            _match = match;
            _difficulty = difficulty;
            _random = random;

            _evaluator = new CommandEvaluator(this);
            _commandCooldown = GetCooldownTime();
        }

        /// <summary>
        /// Updates the input state.
        /// </summary>
        public Command Update(int time)
        {
            Command command = null;

            _commandCooldown -= time;
            if (_commandCooldown <= 0)
            {
                List<PotentialCommand> commands = GetPotentialCommands();
                if (commands.Count != 0)
                {
                    commands.ForEach(cmd => cmd.Score = _evaluator.RateCommand(cmd));
                    var sortedCommands = commands.OrderBy(cmd => cmd.Score).Reverse();
                    var bestScore = sortedCommands.First().Score;
                    var bestCommands = sortedCommands.TakeWhile(cmd => cmd.Score == bestScore);

                    int randomBest = _random.Next(bestCommands.Count());
                    command = bestCommands.ElementAt(randomBest).GetCommand(_player);

                    _commandCooldown = GetCooldownTime();
                }
            }

            return command;
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
                case AIDifficulty.Easy: baseTime = 2000; break;
                case AIDifficulty.Normal: baseTime = 1000; break;
                case AIDifficulty.Hard: baseTime = 750; break;
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
            public Command GetCommand(PlayerId player)
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
        private class CommandEvaluator
        {
            public CommandEvaluator(AIInput input)
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
                int evilNeighbors = place.Neighbors.Count(t => t.Owner != null && t.Owner != _input._player);
                int missingSlots = place.Capacity - place.Pieces.Count;
                return 10 + evilNeighbors + missingSlots;
            }

            private int RateAttack(Territory atk, Territory def)
            {
                int diff = atk.Pieces.Count(p => p.Ready) - def.Pieces.Count;
                return (diff > 0) ? 20 + diff : diff;
            }

            private int RateMove(Territory src, Territory dst)
            {
                int srcEvilNeighbors = src.Neighbors.Count(t => t.Owner != null && t.Owner != _input._player);
                int dstEvilNeighbors = dst.Neighbors.Count(t => t.Owner != null && t.Owner != _input._player);
                int diffEvilNeighbors = dstEvilNeighbors - srcEvilNeighbors;
                int diffNeighbors = dst.Neighbors.Count - src.Neighbors.Count;
                return 5 + diffEvilNeighbors + diffNeighbors;
            }

            private AIInput _input;
        }

        private PlayerId _player;
        private Match _match;
        private AIDifficulty _difficulty;

        private Random _random;

        private CommandEvaluator _evaluator;
        private int _commandCooldown = CommandCooldownTime;
        private const int CommandCooldownTime = 1000;
    }
}
