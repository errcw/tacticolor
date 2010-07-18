using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace Strategy.Net
{
    public class LockstepManager
    {
        public LockstepManager()
        {
            _reader = new CommandReader();
            _writer = new CommandWriter();
            _commands = new Command[4];
        }

        public void Update()
        {
            NetworkGamer sender;
            Command command;

            // read all the data we have
            while (_gamer.IsDataAvailable)
            {
                _gamer.ReceiveData(_reader, out sender);
                command = _reader.ReadCommand();
                if (command == null)
                {
                    System.Diagnostics.Debug.WriteLine("Discarding invalid packet");
                }
                _commands[(int)command.Player] = command;
            }

            // check if we have received all the commands for this step
            bool receivedAll = true;
            for (int i = 0; i < _commands.Length; i++)
            {
                if (_commands[i] == null)
                {
                    receivedAll = false;
                    break;
                }
            }

            if (receivedAll)
            {
            }
        }

        private LocalNetworkGamer _gamer;
        private CommandReader _reader;
        private CommandWriter _writer;

        private Command[] _commands;
    }
}
