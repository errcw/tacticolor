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
        public LockstepInput(LockstepMatch match, ICollection<Player> players)
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
                        if (command.Time <= 0) // some commands may ask for a specific execution time
                        {
                            command.Time = _match.Match.Time + _match.SchedulingOffset;
                        }
                        BroadcastCommand(command, player);
                    }
                }
            }

            ReadNetworkCommands();
        }

        /// <summary>
        /// Notifies the input the specified player left the match.
        /// </summary>
        public void OnPlayerLeft(Player player)
        {
            // the match should no longer wait for commands for this player
            // so tell it that it has all the commands for all time
            SynchronizationCommand command = new StepSynchronizationCommand(player.Id, -1, -1);
            command.Time = long.MaxValue;
            _match.ScheduleCommand(command);
        }

        /// <summary>
        /// Notifies the input the step has ended.
        /// </summary>
        private void OnStepEnded(object matchObj, EventArgs args)
        {
            // send a synchronization command for each local player
            foreach (Player player in _players)
            {
                if (player.Gamer == null || player.Gamer.IsLocal || player.Gamer.HasLeftSession)
                {
                    SynchronizationCommand command = new StepSynchronizationCommand(player.Id, _match.Match.GetStateHash(), _match.Match.Time);
                    command.Time = _match.StepStart + _match.SchedulingOffset;
                    BroadcastCommand(command, player);
                }
            }
        }

        /// <summary>
        /// Sends a command to all remote players.
        /// </summary>
        private void BroadcastCommand(Command command, Player sender)
        {
            _match.ScheduleCommand(command);
            if (_sendReceiveGamer != null && sender.Gamer != null) // no need to send AI commands
            {
                foreach (Player player in _players)
                {
                    if (player.Gamer != null && !player.Gamer.IsLocal && !player.Gamer.HasLeftSession)
                    {
                        _writer.Write(command);
                        _sendReceiveGamer.SendData(_writer, SendDataOptions.Reliable, player.Gamer);
                    }
                }
            }
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
        private ICollection<Player> _players;

        private LocalNetworkGamer _sendReceiveGamer;
        private CommandWriter _writer;
        private CommandReader _reader;
    }
}
