using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.AI;
using Strategy.Gameplay;

namespace Strategy.Net
{
    public class MatchConfigurationManager
    {
        /// <summary>
        /// Occurs when the match configuration changes.
        /// </summary>
        public event EventHandler<EventArgs> ConfigurationChanged;

        /// <summary>
        /// The random number generator seed for the match.
        /// </summary>
        public int Seed
        {
            get { return _seed; }
            set { _seed = value; OnConfigurationChanged(); }
        }

        /// <summary>
        /// The type of map for the match.
        /// </summary>
        public MapType MapType
        {
            get { return _mapType; }
            set { _mapType = value; OnConfigurationChanged(); }
        }

        /// <summary>
        /// The size of map for the match.
        /// </summary>
        public MapSize MapSize
        {
            get { return _mapSize; }
            set { _mapSize = value; OnConfigurationChanged(); }
        }

        /// <summary>
        /// The AI difficulty for the match.
        /// </summary>
        public AiDifficulty Difficulty
        {
            get { return _difficulty; }
            set { _difficulty = value; OnConfigurationChanged(); }
        }

        /// <summary>
        /// Determines if all players are ready to begin the match.
        /// </summary>
        public bool IsEveryoneReady { get; set; }

        public MatchConfigurationManager(NetworkSession session)
        {
            IsEveryoneReady = false;
        }

        public void Update()
        {
            foreach (LocalNetworkGamer gamer in _session.LocalGamers)
            {
                while (gamer.IsDataAvailable)
                {
                    NetworkGamer sender;
                    gamer.ReceiveData(_reader, out sender);
                    MatchConfigurationCommand command = _reader.ReadCommand() as MatchConfigurationCommand;
                    if (command != null)
                    {
                        if (!_session.IsHost)
                        {
                            // received new configuration from the host
                            if (HasNewConfiguration(command))
                            {
                                Seed = command.RandomSeed;
                                MapType = command.MapType;
                                MapSize = command.MapSize;
                                Difficulty = command.Difficulty;
                                OnConfigurationChanged();
                            }
                            AcknowledgeConfiguration(gamer);
                        }
                        else
                        {
                            // received acknowledgement from a player
                        }
                    }
                }
            }
        }

        public bool IsReady(NetworkGamer gamer)
        {
            return _ready[gamer];
        }

        private void AcknowledgeConfiguration(LocalNetworkGamer gamer)
        {
            _writer.Write(new MatchConfigurationCommand(Seed, MapType, MapSize, Difficulty));
            gamer.SendData(_writer, SendDataOptions.ReliableInOrder, gamer);
        }

        /// <summary>
        /// Returns true if the configuration in the command differs from the
        /// local configuration; otherwise, false.
        /// </summary>
        private bool HasNewConfiguration(MatchConfigurationCommand command)
        {
            return Seed != command.RandomSeed ||
                MapType != command.MapType ||
                MapSize != command.MapSize ||
                Difficulty != command.Difficulty;
        }

        /// <summary>
        /// Handles configuration changes.
        /// </summary>
        private void OnConfigurationChanged()
        {
            if (ConfigurationChanged != null)
            {
                ConfigurationChanged(this, EventArgs.Empty);
            }
        }

        private int _seed = 0;
        private MapType _mapType = MapType.LandRush;
        private MapSize _mapSize = MapSize.Normal;
        private AiDifficulty _difficulty = AiDifficulty.Easy;

        private NetworkSession _session;
        private CommandReader _reader = new CommandReader();
        private CommandWriter _writer = new CommandWriter();

        private Dictionary<NetworkGamer, bool> _ready;
    }
}
