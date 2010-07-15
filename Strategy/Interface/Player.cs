using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// A player in a match.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// The game id of this player.
        /// </summary>
        public PlayerId Id { get; set; }

        /// <summary>
        /// The network id of this player.
        /// </summary>
        public NetworkGamer Gamer { get; set; }

        /// <summary>
        /// The input handler for this player.
        /// </summary>
        public LocalInput Input { get; set; }
    }
}
