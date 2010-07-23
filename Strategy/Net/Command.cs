using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;

namespace Strategy.Net
{
    /// <summary>
    /// A game action communicated between players.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// The unique identifier for the type of command.
        /// </summary>
        public byte Id { get; private set; }

        /// <summary>
        /// The player issuing this command.
        /// </summary>
        public PlayerId Player { get; private set; }

        /// <summary>
        /// The game time when this command should execute.
        /// </summary>
        public long Time { get; set; }

        /// <summary>
        /// A simple constructor for packet reading.
        /// </summary>
        public Command(byte id)
        {
            Id = id;
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="id">The identifier for the type of command.</param>
        /// <param name="player">The player issuing this command.</param>
        /// <param name="time">The game time when the command should execute.</param>
        public Command(byte id, PlayerId player)
        {
            Id = id;
            Player = player;
            Time = 0; // filled out later
        }

        /// <summary>
        /// Execute this action on the given match.
        /// </summary>
        /// <param name="match">The match to operate on.</param>
        /// <returns>True if the command is valid; otherwise, false.</returns>
        public virtual bool Execute(Match match)
        {
            // by default a command cannot be executed
            return false;
        }

        /// <summary>
        /// Reads the data for this command from the given packet reader.
        /// </summary>
        public void Read(PacketReader reader)
        {
            Player = (PlayerId)reader.ReadByte();
            Time = reader.ReadInt64();
            ReadImpl(reader);
        }

        /// <summary>
        /// Writes the data for this command.
        /// </summary>
        public void Write(PacketWriter writer)
        {
            writer.Write((byte)Player);
            writer.Write(Time);
            WriteImpl(writer);
        }

        public override string ToString()
        {
            return String.Format("Command[Id={0},Player={1},Time={2}]", Id, Player, Time);
        }

        protected virtual void ReadImpl(PacketReader reader) { }
        protected virtual void WriteImpl(PacketWriter writer) { }
    }

    /// <summary>
    /// Contains data to setup the initial match state.
    /// </summary>
    public class InitializeMatchCommand : Command
    {
        public const byte Code = 0;

        public int RandomSeed { get; private set; }

        public InitializeMatchCommand() : base(Code)
        {
        }

        public InitializeMatchCommand(int randomSeed) : base(Code, PlayerId.A)
        {
            RandomSeed = randomSeed;
        }

        protected override void ReadImpl(PacketReader reader)
        {
            RandomSeed = reader.ReadInt32();
        }

        protected override void WriteImpl(PacketWriter writer)
        {
            writer.Write(RandomSeed);
        }
    }

    /// <summary>
    /// Notifies players that the match should start.
    /// </summary>
    public class StartMatchCommand : Command
    {
        public const byte Code = 1;

        public StartMatchCommand() : base(Code)
        {
        }

        public StartMatchCommand(PlayerId player) : base(Code, player)
        {
        }
    }

    /// <summary>
    /// Marks a synchronization point at the end of a step.
    /// </summary>
    public class SynchronizationCommand : Command
    {
        public const byte Code = 2;

        /// <summary>
        /// A hash of the game state at the end of the current step.
        /// </summary>
        public int Hash { get; private set; }

        /// <summary>
        /// Timing data to tune the step length.
        /// </summary>
        public int Timing { get; private set; }

        public SynchronizationCommand() : base(Code)
        {
        }

        public SynchronizationCommand(PlayerId player, int hash, int timing) : base(Code, player)
        {
            Hash = hash;
            Timing = timing;
        }

        protected override void ReadImpl(PacketReader reader)
        {
            Hash = reader.ReadInt32();
            Timing = reader.ReadInt32();
        }

        protected override void WriteImpl(PacketWriter writer)
        {
            writer.Write(Hash);
            writer.Write(Timing);
        }
    }

    /// <summary>
    /// Places a piece on a territory.
    /// </summary>
    public class PlaceCommand : Command
    {
        public const byte Code = 3;

        public PlaceCommand() : base(Code)
        {
        }

        public PlaceCommand(PlayerId player, Territory territory) : base(Code, player)
        {
            _placementCell = territory.Location;
        }

        public override bool Execute(Match match)
        {
            Territory territory = match.Map.GetTerritoryAt(_placementCell);
            if (territory == null || !match.CanPlacePiece(Player, territory))
            {
                return false;
            }
            match.PlacePiece(territory);
            return true;
        }

        protected override void ReadImpl(PacketReader reader)
        {
            int r = reader.ReadInt16();
            int c = reader.ReadInt16();
            _placementCell = new Cell(r, c);
        }

        protected override void WriteImpl(PacketWriter writer)
        {
            writer.Write((short)_placementCell.Row);
            writer.Write((short)_placementCell.Col);
        }

        private Cell _placementCell;
    }

    /// <summary>
    /// Moves pieces between two territories.
    /// </summary>
    public class MoveCommand : Command
    {
        public const byte Code = 4;

        public MoveCommand() : base(Code)
        {
        }

        public MoveCommand(PlayerId player, Territory source, Territory destination) : base(Code, player)
        {
            _sourceCell = source.Location;
            _destinationCell = destination.Location;
        }

        public override bool Execute(Match match)
        {
            Territory source = match.Map.GetTerritoryAt(_sourceCell);
            Territory destination = match.Map.GetTerritoryAt(_destinationCell);
            if (source == null || destination == null || !match.CanMove(Player, source, destination))
            {
                return false;
            }
            match.Move(source, destination);
            return true;
        }

        protected override void ReadImpl(PacketReader reader)
        {
            int r, c;
            r = reader.ReadInt16();
            c = reader.ReadInt16();
            _sourceCell = new Cell(r, c);
            r = reader.ReadInt16();
            c = reader.ReadInt16();
            _destinationCell = new Cell(r, c);
        }

        protected override void WriteImpl(PacketWriter writer)
        {
            writer.Write((short)_sourceCell.Row);
            writer.Write((short)_sourceCell.Col);
            writer.Write((short)_destinationCell.Row);
            writer.Write((short)_destinationCell.Col);
        }

        private Cell _sourceCell;
        private Cell _destinationCell;
    }

    /// <summary>
    /// Attacks from one territory to another.
    /// </summary>
    public class AttackCommand : Command
    {
        public const byte Code = 5;

        public AttackCommand() : base(Code)
        {
        }

        public AttackCommand(PlayerId player, Territory attacker, Territory defender) : base(Code, player)
        {
            _attackerCell = attacker.Location;
            _defenderCell = defender.Location;
        }

        public override bool Execute(Match match)
        {
            Territory attacker = match.Map.GetTerritoryAt(_attackerCell);
            Territory defender = match.Map.GetTerritoryAt(_defenderCell);
            if (attacker == null || defender == null || !match.CanAttack(Player, attacker, defender))
            {
                return false;
            }
            match.Attack(attacker, defender);
            return true;
        }

        protected override void ReadImpl(PacketReader reader)
        {
            int r, c;
            r = reader.ReadInt16();
            c = reader.ReadInt16();
            _attackerCell = new Cell(r, c);
            r = reader.ReadInt16();
            c = reader.ReadInt16();
            _defenderCell = new Cell(r, c);
        }

        protected override void WriteImpl(PacketWriter writer)
        {
            writer.Write((short)_attackerCell.Row);
            writer.Write((short)_attackerCell.Col);
            writer.Write((short)_defenderCell.Row);
            writer.Write((short)_defenderCell.Col);
        }

        private Cell _attackerCell;
        private Cell _defenderCell;
    }

    /// <summary>
    /// No action.
    /// </summary>
    public class NoOpCommand : Command
    {
        public const byte Code = 6;

        public NoOpCommand() : base(Code)
        {
        }

        public NoOpCommand(PlayerId player) : base(Code, player)
        {
        }

        public override bool Execute(Match match)
        {
            return true;
        }

        protected override void ReadImpl(PacketReader reader)
        {
        }

        protected override void WriteImpl(PacketWriter writer)
        {
        }
    }

    /// <summary>
    /// Reads commands from packets.
    /// </summary>
    public class CommandReader : PacketReader
    {
        public Command ReadCommand()
        {
            Command command = null;

            int code = ReadByte();
            switch (code)
            {
                case InitializeMatchCommand.Code:
                    command = new InitializeMatchCommand();
                    break;
                case StartMatchCommand.Code:
                    command = new StartMatchCommand();
                    break;
                case SynchronizationCommand.Code:
                    command = new SynchronizationCommand();
                    break;
                case PlaceCommand.Code:
                    command = new PlaceCommand();
                    break;
                case MoveCommand.Code:
                    command = new MoveCommand();
                    break;
                case AttackCommand.Code:
                    command = new AttackCommand();
                    break;
                case NoOpCommand.Code:
                    command = new NoOpCommand();
                    break;
                default:
                    // invalid/unknown command type
                    return null;
            }

            command.Read(this);

            return command;
        }
    }

    /// <summary>
    /// Writes commands to packets.
    /// </summary>
    public class CommandWriter : PacketWriter
    {
        public void Write(Command command)
        {
            Write((byte)command.Id);
            command.Write(this);
        }
    }
}
