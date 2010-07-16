using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;

namespace Strategy.Net
{
    /// <summary>
    /// A game action communicated between players.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The player issuing this command.
        /// </summary>
        public PlayerId Player { get; private set; }

        /// <summary>
        /// The game time when this command should execute.
        /// </summary>
        public long Time { get; private set; }

        /// <summary>
        /// No-argument constructor for packet reading.
        /// </summary>
        public Command()
        {
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="player">The player issuing this command.</param>
        /// <param name="time">The game time when the command should execute.</param>
        public Command(PlayerId player, long time)
        {
            Player = player;
            Time = time;
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

        private virtual void ReadImpl(PacketReader reader);
        private virtual void WriteImpl(PacketWriter writer);
    }

    /// <summary>
    /// A command that executes a player action in the match.
    /// </summary>
    public interface IGameCommand
    {
        /// <summary>
        /// Execute this action on the given match.
        /// </summary>
        /// <param name="match">The match to operate on.</param>
        /// <returns>True if the command is valid; otherwise, false.</returns>
        public bool Execute(Match match);
    }

    /// <summary>
    /// Contains data to setup the initial match state.
    /// </summary>
    public class InitializeMatchCommand : Command
    {
        public const byte CODE = 0;

        public int RandomSeed { get; private set; }

        public InitializeMatchCommand(int randomSeed)
        {
            RandomSeed = randomSeed;
        }

        private override void ReadImpl(PacketReader reader)
        {
            RandomSeed = reader.ReadInt32();
        }

        private override void WriteImpl(PacketWriter writer)
        {
            writer.Write(RandomSeed);
        }
    }

    /// <summary>
    /// Notifies players that the match should start.
    /// </summary>
    public class StartMatchCommand : Command
    {
        public const byte CODE = 1;

        public StartMatchCommand(int randomSeed)
        {
        }

        private override void ReadImpl(PacketReader reader)
        {
        }

        private override void WriteImpl(PacketWriter writer)
        {
        }
    }

    /// <summary>
    /// Places a piece on a territory.
    /// </summary>
    public class PlaceCommand : Command
    {
        public const byte CODE = 2;

        private override void ReadImpl(PacketReader reader)
        {
        }

        private override void WriteImpl(PacketWriter writer)
        {
        }
    }

    /// <summary>
    /// Moves pieces between two territories.
    /// </summary>
    public class MoveCommand : Command
    {
        public const byte CODE = 3;

        private override void ReadImpl(PacketReader reader)
        {
        }

        private override void WriteImpl(PacketWriter writer)
        {
        }
    }

    /// <summary>
    /// Attacks from one territory to another.
    /// </summary>
    public class AttackCommand : Command
    {
        public const byte CODE = 4;

        private override void ReadImpl(PacketReader reader)
        {
        }

        private override void WriteImpl(PacketWriter writer)
        {
        }
    }
}
