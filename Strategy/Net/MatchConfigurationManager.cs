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
        /// Occurs when the ready state of a gamer changes.
        /// </summary>
        public event EventHandler<ReadyChangedEventArgs> ReadyChanged;

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
        public bool IsEveryoneReady
        {
            get { return _ready.All(kv => kv.Value); }
        }

        public MatchConfigurationManager(StrategyNetworkSession net)
        {
            _net = net;
            _net.Session.GamerJoined += OnGamerJoined;
            _net.Session.GamerLeft += OnGamerLeft;
        }

        /// <summary>
        /// Sends and receives network data for configuration.
        /// </summary>
        public void Update()
        {
            foreach (ReceivedCommand received in _net.ReceiveCommands())
            {
                MatchConfigurationCommand command = received.Command as MatchConfigurationCommand;
                if (command != null)
                {
                    if (command.IsConfiguration)
                    {
                        // received new configuration from the host
                        OnConfigurationReceived(command);
                    }
                    else
                    {
                        // received ready signal from a gamer
                        OnReadyReceived(received.Sender, command);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the ready state of the specified gamer.
        /// </summary>
        public void SetIsReady(LocalNetworkGamer gamer, bool isReady)
        {
            bool changedReadyState = SetIsReadyInternal(gamer, isReady);
            if (changedReadyState)
            {
                if (isReady)
                {
                    BroadcastReady(gamer);
                }
                else
                {
                    BroadcastUnready(gamer);
                }
            }
        }

        /// <summary>
        /// Queries the ready state of the specified gamer.
        /// </summary>
        public bool IsReady(NetworkGamer gamer)
        {
            return _ready[gamer];
        }

        /// <summary>
        /// Resets the configuration for a new match.
        /// </summary>
        public void ResetForNextMatch()
        {
            // deterministically choose a new seed so that no negotiation
            // is necessary for the gamers to use the same configuration
            // across matches
            _seed += 1;

            foreach (NetworkGamer gamer in _net.Session.AllGamers)
            {
                SetIsReadyInternal(gamer, false);
            }
        }

        /// <summary>
        /// Sets the entire configuration. A mechanism for avoiding multiple
        /// change notifications (and the corresponding network traffic) when
        /// multiple parameters are changed at a time.
        /// </summary>
        public void SetConfiguration(int seed, MapType mapType, MapSize mapSize, AiDifficulty difficulty)
        {
            _seed = seed;
            _mapType = mapType;
            _mapSize = mapSize;
            _difficulty = difficulty;
            OnConfigurationChanged();
        }

        private bool SetIsReadyInternal(NetworkGamer gamer, bool isReady)
        {
            bool wasReady = _ready[gamer];
            if (wasReady != isReady)
            {
                _ready[gamer] = isReady;
                if (ReadyChanged != null)
                {
                    ReadyChanged(this, new ReadyChangedEventArgs(gamer, isReady));
                }
                return true;
            }
            return false;
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs args)
        {
            _ready[args.Gamer] = false;
            _lastReadied[args.Gamer] = null;

            // only remote gamers are missing information
            if (!args.Gamer.IsLocal)
            {
                // the newly added gamer needs to get the current configuration
                if (_net.Session.IsHost)
                {
                    SendConfigurationFromHost(args.Gamer);
                }

                // the gamer also needs to know about the local ready state
                foreach (var entry in _ready)
                {
                    if (entry.Key.IsLocal && entry.Value)
                    {
                        SendReady((LocalNetworkGamer)entry.Key, args.Gamer);
                    }
                }
            }
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            _ready.Remove(args.Gamer);
            _lastReadied.Remove(args.Gamer);
        }

        /// <summary>
        /// Handles configuration changes.
        /// </summary>
        private void OnConfigurationChanged()
        {
            // host broadcasts the new configuration to every gamer
            if (_net.Session.IsHost)
            {
                BroadcastConfigurationFromHost();
            }

            // set the ready state based on the last readied configuration
            foreach (NetworkGamer gamer in _net.Session.AllGamers)
            {
                MatchConfigurationCommand lastReady = _lastReadied[gamer];
                SetIsReadyInternal(gamer, MatchesLocalConfiguration(lastReady));
            }

            if (ConfigurationChanged != null)
            {
                ConfigurationChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles authoritative configuration commands.
        /// </summary>
        private void OnConfigurationReceived(MatchConfigurationCommand command)
        {
            // only trigger the on change logic if appropriate, otherwise the
            // host will continually trigger change broadcasts when it receives
            // its own configuration back as a command
            if (!MatchesLocalConfiguration(command))
            {
                _seed = command.RandomSeed;
                _mapType = command.MapType;
                _mapSize = command.MapSize;
                _difficulty = command.Difficulty;
                OnConfigurationChanged();
            }
        }

        /// <summary>
        /// Handles ready commands.
        /// </summary>
        private void OnReadyReceived(NetworkGamer gamer, MatchConfigurationCommand command)
        {
            _lastReadied[gamer] = command;
            SetIsReadyInternal(gamer, MatchesLocalConfiguration(command));
        }

        private bool MatchesLocalConfiguration(MatchConfigurationCommand command)
        {
            return command != null &&
                Seed == command.RandomSeed &&
                MapType == command.MapType &&
                MapSize == command.MapSize &&
                Difficulty == command.Difficulty;
        }

        private void BroadcastReady(LocalNetworkGamer gamer)
        {
            _net.BroadcastCommand(
                new MatchConfigurationCommand(Seed, MapType, MapSize, Difficulty, false),
                gamer,
                SendDataOptions.ReliableInOrder);
        }

        private void SendReady(LocalNetworkGamer gamer, NetworkGamer receiver)
        {
            _net.SendCommand(
                new MatchConfigurationCommand(Seed, MapType, MapSize, Difficulty, false),
                gamer,
                receiver,
                SendDataOptions.ReliableInOrder);
        }

        private void BroadcastUnready(LocalNetworkGamer gamer)
        {
            _net.BroadcastCommand(UnreadyCommand, gamer, SendDataOptions.ReliableInOrder);
        }

        private void SendConfigurationFromHost(NetworkGamer receiver)
        {
            _net.SendCommand(
                new MatchConfigurationCommand(Seed, MapType, MapSize, Difficulty, true),
                (LocalNetworkGamer)_net.Session.Host,
                receiver,
                SendDataOptions.ReliableInOrder);
        }

        private void BroadcastConfigurationFromHost()
        {
            _net.BroadcastCommand(
                new MatchConfigurationCommand(Seed, MapType, MapSize, Difficulty, true),
                (LocalNetworkGamer)_net.Session.Host,
                SendDataOptions.ReliableInOrder);
        }

        private int _seed = 0;
        private MapType _mapType = MapType.LandRush;
        private MapSize _mapSize = MapSize.Normal;
        private AiDifficulty _difficulty = AiDifficulty.Easy;

        private StrategyNetworkSession _net;

        private Dictionary<NetworkGamer, bool> _ready = new Dictionary<NetworkGamer, bool>();
        private Dictionary<NetworkGamer, MatchConfigurationCommand> _lastReadied = new Dictionary<NetworkGamer, MatchConfigurationCommand>();

        private readonly MatchConfigurationCommand UnreadyCommand = new MatchConfigurationCommand(0, 0, 0, 0, false);
    }

    public class ReadyChangedEventArgs : EventArgs
    {
        public readonly NetworkGamer Gamer;
        public readonly bool IsReady;

        public ReadyChangedEventArgs(NetworkGamer gamer, bool isReady)
        {
            Gamer = gamer;
            IsReady = isReady;
        }
    }
}
