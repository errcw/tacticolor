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
        public AIInput(PlayerId player, Match match, AIDifficulty difficulty)
        {
            _player = player;
            _match = match;
            _difficulty = difficulty;

            _random = new Random();
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
                PotentialCommand potential = commands[_random.Next(commands.Count)];
                command = potential.GetCommand(_player);

                _commandCooldown = CommandCooldownTime;
            }

            return command;
        }

        private List<PotentialCommand> GetPotentialCommands()
        {
            List<PotentialCommand> commands = new List<PotentialCommand>(32);

            // placement
            if (CanPlacePiece())
            {
                var openTerritories = GetOwnedTerritories().Where(t => t.Pieces.Count < t.Capacity);
                foreach (Territory territory in openTerritories)
                {
                    commands.Add(new PotentialCommand(CommandType.Place, null, territory));
                }
            }

            var srcTerritories = GetOwnedTerritories().Where(t => t.Pieces.Count >= 2 && HasReadyPieces(t));

            // move
            foreach (Territory src in srcTerritories)
            {
                var dstTerritories = src.Neighbors.Where(t => t.Owner == _player && t.Pieces.Count < t.Capacity);
                foreach (Territory dst in dstTerritories)
                {
                    commands.Add(new PotentialCommand(CommandType.Move, src, dst));
                }
            }

            // attack
            foreach (Territory src in srcTerritories)
            {
                var dstTerritories = src.Neighbors.Where(t => t.Owner != _player);
                foreach (Territory dst in dstTerritories)
                {
                    commands.Add(new PotentialCommand(CommandType.Attack, src, dst));
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

        private PlayerId _player;
        private Match _match;
        private AIDifficulty _difficulty;

        private Random _random;

        private int _commandCooldown = CommandCooldownTime;
        private const int CommandCooldownTime = 3000;
    }
}
