using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.Library.Extensions;

namespace Strategy.Net
{
    /// <summary>
    /// Wraps a network session.
    /// </summary>
    /// <remarks>
    /// Handles the ugly realities of peer-to-peer distributed game state
    /// changes. Because different machines will transition between the Playing
    /// and Lobby states at different times, a machine may receive a command
    /// for the next or previous state. Commands for the next state must be
    /// saved and executed when appropriate; commands for the previous state
    /// must be discarded. Here we use a system of state sequence numbers to
    /// track to which state a command belongs then handle it appropriately.
    /// </remarks>
    public class StrategyNetworkSession : IDisposable
    {
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
        /// Disposes the underlying network session.
        /// </summary>
        public void Dispose()
        {
            Session.Dispose();
        }

        /// <summary>
        /// Starts the match and moves from Lobby to Playing state.
        /// </summary>
        public void StartGame()
        {
            Debug.Assert(Session.IsHost);
            if (Session.SessionState == NetworkSessionState.Lobby)
            {
                Session.StartGame();
            }
        }

        /// <summary>
        /// Ends the match and moves from Playing to Lobby state.
        /// </summary>
        public void EndGame()
        {
            Debug.Assert(Session.IsHost);
            if (Session.SessionState == NetworkSessionState.Playing)
            {
                Session.EndGame();
            }
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
        public IEnumerable<ReceivedCommand> ReceiveCommands()
        {
            if (_releaseDeferredCommands)
            {
                foreach (ReceivedCommand command in _deferredCommands)
                {
                    yield return command;
                }
                _deferredCommands.Clear();
                _releaseDeferredCommands = false;
            }

            foreach (LocalNetworkGamer receiver in Session.LocalGamers)
            {
                while (receiver.IsDataAvailable)
                {
                    NetworkGamer sender;
                    receiver.ReceiveData(_reader, out sender);
                    foreach (Command command in _reader.ReadCommands())
                    {
                        ReceivedCommand receivedCommand = new ReceivedCommand(command, sender, receiver);
                        switch (GetCommandAction(command, sender))
                        {
                            case CommandAction.Execute:
                                yield return receivedCommand;
                                break;
                            case CommandAction.Defer:
                                _deferredCommands.Add(receivedCommand);
                                break;
                            case CommandAction.Discard:
                                break;
                        }
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
            // nudge all the sequences forward by one
            _sequence += 1;

            var newExpectedSequences = new Dictionary<NetworkGamer, long>();
            foreach (var entry in _expectedSequences)
            {
                newExpectedSequences[entry.Key] = entry.Value + 1;
            }
            _expectedSequences = newExpectedSequences;

            // release the commands deferred for this new state
            _releaseDeferredCommands = true;
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
            OnStateChanged();
        }

        private void OnGameEnded(object sender, GameEndedEventArgs args)
        {
            OnStateChanged();
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

        private ICollection<ReceivedCommand> _deferredCommands = new List<ReceivedCommand>();
        private bool _releaseDeferredCommands;

        private long _sequence = 1;
        private IDictionary<NetworkGamer, long> _expectedSequences = new Dictionary<NetworkGamer, long>();

        private CommandReader _reader = new CommandReader();
        private CommandWriter _writer = new CommandWriter();
    }

    /// <summary>
    /// Describes a received command.
    /// </summary>
    public struct ReceivedCommand
    {
        public readonly Command Command;
        public readonly NetworkGamer Sender;
        public readonly LocalNetworkGamer Receiver;

        public ReceivedCommand(Command command, NetworkGamer sender, LocalNetworkGamer receiver)
        {
            Command = command;
            Sender = sender;
            Receiver = receiver;
        }
    }
}
