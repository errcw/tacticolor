using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace Strategy.Net
{
    /// <summary>
    /// Wraps a network session.
    /// </summary>
    public class StrategyNetworkSession
    {
        /// <summary>
        /// Occurs when the underlying session receives a GameStarted event.
        /// </summary>
        public event EventHandler<GameStartedEventArgs> GameStarted;

        /// <summary>
        /// Occurs when the underlying session receives a GameEnded event.
        /// </summary>
        public event EventHandler<GameEndedEventArgs> GameEnded;

        /// <summary>
        /// Occurs when the underlying session receives a GamerHostChangedent.
        /// </summary>
        public event EventHandler<GamerJoinedEventArgs> GamerJoined;

        /// <summary>
        /// Occurs when the underlying session receives a GamerJoined event
        /// and the session is in the Lobby state.
        /// </summary>
        public event EventHandler<GamerJoinedEventArgs> GamerJoinedInLobby;

        /// <summary>
        /// Occurs when the underlying session receives a GamerJoined event
        /// and the session is in the Playing state.
        /// </summary>
        public event EventHandler<GamerJoinedEventArgs> GamerJoinedInGame;

        /// <summary>
        /// Occurs when the underlying session receives a GamerLeft event.
        /// </summary>
        public event EventHandler<GamerLeftEventArgs> GamerLeft;

        /// <summary>
        /// Occurs when the underlying session receives a GamerLeft event
        /// and the session is in the Lobby state.
        /// </summary>
        public event EventHandler<GamerLeftEventArgs> GamerLeftInLobby;

        /// <summary>
        /// Occurs when the underlying session receives a GamerLeft event
        /// and the session is in the Playing state.
        /// </summary>
        public event EventHandler<GamerLeftEventArgs> GamerLeftInGame;

        /// <summary>
        /// Occurs when the underlying session receives a HostChanged event.
        /// </summary>
        public event EventHandler<HostChangedEventArgs> HostChanged;

        /// <summary>
        /// Occurs when the underlying session receives a HostChanged event
        /// and the session is in the Lobby state.
        /// </summary>
        public event EventHandler<HostChangedEventArgs> HostChangedInLobby;

        /// <summary>
        /// Occurs when the underlying session receives a HostChanged event
        /// and the session is in the Playing state.
        /// </summary>
        public event EventHandler<HostChangedEventArgs> HostChangedInGame;

        /// <summary>
        /// Occurs when the underlying session receives a SessionEnded event.
        /// </summary>
        public event EventHandler<NetworkSessionEndedEventArgs> SessionEnded;

        /// <summary>
        /// Occurs when the underlying session receives a SessionEnded event
        /// and the session is in the Lobby state.
        /// </summary>
        public event EventHandler<NetworkSessionEndedEventArgs> SessionEndedInLobby;

        /// <summary>
        /// Occurs when the underlying session receives a SessionEnded event
        /// and the session is in the Playing state.
        /// </summary>
        public event EventHandler<NetworkSessionEndedEventArgs> SessionEndedInGame;

        /// <summary>
        /// The underlying network session.
        /// </summary>
        public NetworkSession Session { get; private set; }

        /// <summary>
        /// Creates a new component wrapping the given session.
        /// </summary>
        public StrategyNetworkSession(NetworkSession session)
        {
            Session = session;
            Session.GameStarted += OnGameStarted;
            Session.GameEnded += OnGameEnded;
            Session.GamerJoined += OnGamerJoined;
            Session.GamerLeft += OnGamerLeft;
            Session.HostChanged += OnHostChanged;
            Session.SessionEnded += OnSessionEnded;
            UpdateDeferUntilState();
        }

        /// <summary>
        /// Updates the underlying network session.
        /// </summary>
        public void Update()
        {
            Session.Update();
        }

        /// <summary>
        /// Sends a command to a specific gamer.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="sender">The sender gamer of the command.</param>
        /// <param name="recipient">The recipient gamer of the command.</param>
        /// <param name="options">The options to send with.</param>
        public void SendCommand(Command command, LocalNetworkGamer sender, NetworkGamer recipient, SendDataOptions options)
        {
            _writer.Write(command);
            sender.SendData(_writer, options, recipient);
        }

        /// <summary>
        /// Broadcasts a command to all gamers in the session.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="sender">The sender gamer of the command.</param>
        /// <param name="recipient">The recipient gamer of the command.</param>
        /// <param name="options">The options to send with.</param>
        public void BroadcastCommand(Command command, LocalNetworkGamer sender, SendDataOptions options)
        {
            _writer.Write(command);
            sender.SendData(_writer, SendDataOptions.ReliableInOrder);
        }

        /// <summary>
        /// Receives commands.
        /// </summary>
        /// <returns>An enumeration of the commands received.</returns>
        public IEnumerable<Command> ReceiveCommands()
        {
            if (ShouldReleaseDeferredCommands())
            {
                foreach (Command command in _deferredCommands)
                {
                    yield return command;
                }
                _deferredCommands.Clear();
            }

            foreach (LocalNetworkGamer gamer in Session.LocalGamers)
            {
                while (gamer.IsDataAvailable)
                {
                    NetworkGamer sender;
                    gamer.ReceiveData(_reader, out sender);
                    Command command = _reader.ReadCommand();
                    if (ShouldDeferCommand(command))
                    {
                        _deferredCommands.Add(command);
                    }
                    else
                    {
                        yield return command;
                    }
                }
            }
            yield break;
        }

        /// <summary>
        /// Checks if the given command can be executed given the current state
        /// of the network session, or if it should be deferred for execution
        /// once the state changes appropriately.
        /// </summary>
        private bool ShouldDeferCommand(Command command)
        {
            NetworkSessionState executionState = Session.SessionState; // by default allow execution
            if (command is MatchConfigurationCommand)
            {
                executionState = NetworkSessionState.Lobby;
            }
            else if (command is MatchCommand)
            {
                executionState = NetworkSessionState.Playing;
            }
            return (executionState != Session.SessionState);
        }

        /// <summary>
        /// Checks if the collection of deferred commands may now be executed.
        /// </summary>
        private bool ShouldReleaseDeferredCommands()
        {
            return (_deferUntilState == Session.SessionState);
        }

        /// <summary>
        /// Sets the state commands should be deferred for.
        /// </summary>
        private void UpdateDeferUntilState()
        {
            _deferUntilState = (Session.SessionState == NetworkSessionState.Lobby)
                ? NetworkSessionState.Playing
                : NetworkSessionState.Lobby;
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
            UpdateDeferUntilState();
            if (GameStarted != null)
            {
                GameStarted(this, args);
            }
        }

        private void OnGameEnded(object sender, GameEndedEventArgs args)
        {
            UpdateDeferUntilState();
            if (GameEnded != null)
            {
                GameEnded(this, args);
            }
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs args)
        {
            if (GamerJoined != null)
            {
                GamerJoined(this, args);
            }
            if (GamerJoinedInLobby != null && Session.SessionState == NetworkSessionState.Lobby)
            {
                GamerJoinedInLobby(this, args);
            }
            if (GamerJoinedInGame != null && Session.SessionState == NetworkSessionState.Playing)
            {
                GamerJoinedInGame(this, args);
            }
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            if (GamerLeft != null)
            {
                GamerLeft(this, args);
            }
            if (GamerLeftInLobby != null && Session.SessionState == NetworkSessionState.Lobby)
            {
                GamerLeftInLobby(this, args);
            }
            if (GamerLeftInGame != null && Session.SessionState == NetworkSessionState.Playing)
            {
                GamerLeftInGame(this, args);
            }
        }

        private void OnHostChanged(object sender, HostChangedEventArgs args)
        {
            if (HostChanged != null)
            {
                HostChanged(this, args);
            }
            if (HostChangedInLobby != null && Session.SessionState == NetworkSessionState.Lobby)
            {
                HostChangedInLobby(this, args);
            }
            if (HostChangedInGame != null && Session.SessionState == NetworkSessionState.Playing)
            {
                HostChangedInGame(this, args);
            }
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            if (SessionEnded != null)
            {
                SessionEnded(this, args);
            }
            if (SessionEndedInLobby != null && Session.SessionState == NetworkSessionState.Lobby)
            {
                SessionEndedInLobby(this, args);
            }
            if (SessionEndedInGame != null && Session.SessionState == NetworkSessionState.Playing)
            {
                SessionEndedInGame(this, args);
            }
        }

        private ICollection<Command> _deferredCommands = new List<Command>();
        private NetworkSessionState _deferUntilState;

        private CommandReader _reader = new CommandReader();
        private CommandWriter _writer = new CommandWriter();
    }
}
