using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            OnStateChanged();
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
            command.Sequence = _sequence;
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
            command.Sequence = _sequence;
            _writer.Write(command);
            sender.SendData(_writer, options);
        }

        /// <summary>
        /// Receives commands.
        /// </summary>
        /// <returns>An enumeration of the commands received.</returns>
        public IEnumerable<Command> ReceiveCommands()
        {
            if (_releaseDeferredCommands)
            {
                foreach (Command command in _deferredCommands)
                {
                    yield return command;
                }
                _deferredCommands.Clear();
                _releaseDeferredCommands = false;
            }

            foreach (LocalNetworkGamer gamer in Session.LocalGamers)
            {
                while (gamer.IsDataAvailable)
                {
                    NetworkGamer sender;
                    gamer.ReceiveData(_reader, out sender);
                    Command command = _reader.ReadCommand();
                    switch (GetCommandAction(command, sender))
                    {
                        case CommandAction.Execute:
                            yield return command;
                            break;
                        case CommandAction.Defer:
                            _deferredCommands.Add(command);
                            break;
                        case CommandAction.Discard:
                            break;
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
        private CommandAction GetCommandAction(Command command, NetworkGamer sender)
        {
            long expectedSequence;

            if (!_expectedSequences.TryGetValue(sender, out expectedSequence))
            {
                // if this is the first command we have seen from this gamer
                // then set the initial expectation to be the command sequence
                expectedSequence = command.Sequence;
                _expectedSequences[sender] = expectedSequence;
            }

            if (command.Sequence == expectedSequence)
            {
                // the sender has the same sequence as we expect and so must be
                // in the same state as us, so execute the command now
                return CommandAction.Execute;
            }
            else if (command.Sequence > expectedSequence)
            {
                Debug.Assert(command.Sequence == expectedSequence + 1);

                // the sender has a sequence in the future from what we expect
                // so defer the command until the local state changes to match
                return CommandAction.Defer;
            }
            else
            {
                // the sender has a sequence in the past from what we expect
                // so the command is no longer relevant and may be discarded
                return CommandAction.Discard;
            }
        }

        /// <summary>
        /// Sets the state commands should be deferred for.
        /// </summary>
        private void OnStateChanged()
        {
            // nudge all the expected sequences forward by one
            _sequence += 1;
            foreach (var entry in _expectedSequences)
            {
                //TODO: concurrent modification exception?
                _expectedSequences[entry.Key] = entry.Value + 1;
            }

            // release the commands deferred for this new state
            _releaseDeferredCommands = true;
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
            OnStateChanged();
            if (GameStarted != null)
            {
                GameStarted(this, args);
            }
        }

        private void OnGameEnded(object sender, GameEndedEventArgs args)
        {
            OnStateChanged();
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

        /// <summary>
        /// Describes how a received command should be handled.
        /// </summary>
        private enum CommandAction
        {
            Defer,
            Execute,
            Discard
        }

        private ICollection<Command> _deferredCommands = new List<Command>();
        private bool _releaseDeferredCommands;

        private long _sequence = 1;
        private IDictionary<NetworkGamer, long> _expectedSequences = new Dictionary<NetworkGamer, long>();

        private CommandReader _reader = new CommandReader();
        private CommandWriter _writer = new CommandWriter();
    }
}
