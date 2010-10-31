using System;
using System.Collections.Generic;

namespace Strategy.Net
{
    /// <summary>
    /// Maintains a sorted list of commands. Commands are sorted first
    /// chronologically and second by player in the case of conflicts.
    /// </summary>
    public class CommandList
    {
        /// <summary>
        /// The number of commands in the list.
        /// </summary>
        public int Count
        {
            get { return _commands.Count; }
        }

        /// <summary>
        /// Creates a new, empty command list.
        /// </summary>
        public CommandList()
        {
            _commands = new LinkedList<Command>();
        }

        /// <summary>
        /// Adds the specified command to the list.
        /// </summary>
        /// <param name="command">The command to add.</param>
        public void Add(Command command)
        {
            LinkedListNode<Command> node = _commands.Last;
            while (node != null && HappensBefore(command, node.Value))
            {
                node = node.Previous;
            }
            if (node != null)
            {
                _commands.AddAfter(node, command);
            }
            else
            {
                _commands.AddFirst(command);
            }
        }

        /// <summary>
        /// Returns the earliest command in the list without removing it.
        /// </summary>
        public Command Peek()
        {
            if (_commands.Count == 0)
            {
                throw new InvalidOperationException("List contains no commands");
            }
            return _commands.First.Value;
        }

        /// <summary>
        /// Removes and returns the earliest command in the list.
        /// </summary>
        public Command Pop()
        {
            if (_commands.Count == 0)
            {
                throw new InvalidOperationException("List contains no commands");
            }
            Command first = Peek();
            _commands.RemoveFirst();
            return first;
        }

        private bool HappensBefore(Command ca, Command cb)
        {
            long dt = ca.Time - cb.Time;
            if (dt != 0)
            {
                return dt < 0;
            }
            else
            {
                return ca.Player < cb.Player;
            }
        }

        private LinkedList<Command> _commands;
    }
}
