using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.Interface;

namespace Strategy.Net
{
    /// <summary>
    /// Provides commands.
    /// </summary>
    public interface ICommandProvider
    {
        Command Update(int elapsed);
    }

    /// <summary>
    /// Gathers local input feeds it both to the match and to remote players.
    /// </summary>
    public class LockstepInput
    {
        public LockstepInput(LockstepMatch match, Player[] players)
        {
            _match = match;
            _match.StepEnded += OnStepEnded;
            _players = players;

            // find a local player we can use to send and receive messages
            foreach (Player player in _players)
            {
                if (player.Gamer != null && player.Gamer.IsLocal)
                {
                    _sendReceiveGamer = (LocalNetworkGamer)player.Gamer;
                    break;
                }
            }

            _writer = new CommandWriter();
            _reader = new CommandReader();
        }

        /// <summary>
        /// Updates the input for this frame.
        /// </summary>
        /// <param name="time">The elapsed time, in milliseconds, since the last update.</param>
        public void Update(int time)
        {
            foreach (Player player in _players)
            {
                if (player.Input != null) // local player providing input
                {
                    Command command = player.Input.Update(time);
                    if (command != null)
                    {
                        command.Time = _match.Match.Time + _match.SchedulingOffset;
                        BroadcastCommand(command);
                    }
                }
            }

            ReadNetworkCommands();
        }

        /// <summary>
        /// Notifies the input the step has ended.
        /// </summary>
        private void OnStepEnded(object matchObj, EventArgs args)
        {
            // send a synchronization command for each player
            for (int i = 0; i < _players.Length; i++)
            {
                SynchronizationCommand command = new SynchronizationCommand(_players[i].Id, _match.Match.GetStateHash(), _match.StepStart, 0);
                command.Time = _match.StepStart + _match.SchedulingOffset;
                BroadcastCommand(command);
            }

            SendNetworkCommands();
        }

        /// <summary>
        /// Sends a command to all remote players.
        /// </summary>
        private void BroadcastCommand(Command command)
        {
            _match.ScheduleCommand(command);
            System.Diagnostics.Debug.WriteLine("Broadcasting " + command);
            if (_sendReceiveGamer != null)
            {
                foreach (Player player in _players)
                {
                    if (player.Gamer != null && !player.Gamer.IsLocal)
                    {
                        _writer.Write(command);
                        _sendReceiveGamer.SendData(_writer, SendDataOptions.Reliable, player.Gamer);
                    }
                }
            }
        }

        /// <summary>
        /// Sends all pending commands.
        /// </summary>
        private void SendNetworkCommands()
        {
        }

        /// <summary>
        /// Reads incoming commands from the network.
        /// </summary>
        private void ReadNetworkCommands()
        {
            foreach (Player player in _players)
            {
                if (player.Gamer != null && player.Gamer.IsLocal)
                {
                    LocalNetworkGamer receiver = (LocalNetworkGamer)player.Gamer;
                    NetworkGamer sender;

                    while (receiver.IsDataAvailable)
                    {
                        receiver.ReceiveData(_reader, out sender);
                        if (receiver == _sendReceiveGamer)
                        {
                            Command command = _reader.ReadCommand();
                            if (command != null)
                            {
                                _match.ScheduleCommand(command);
                            }
                        }
                    }
                }
            }
        }

        private LockstepMatch _match;
        private Player[] _players;

        private LocalNetworkGamer _sendReceiveGamer;
        private CommandWriter _writer;
        private CommandReader _reader;
    }
}
