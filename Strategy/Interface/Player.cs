using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Properties;

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
        /// The network id of this player. Null for AI players.
        /// </summary>
        public NetworkGamer Gamer { get; set; }

        /// <summary>
        /// The input handler for this player.
        /// </summary>
        public ICommandProvider Input { get; set; }

        /// <summary>
        /// The controller used for this player. Valid only for local players.
        /// </summary>
        public PlayerIndex? Controller { get; set; }

        /// <summary>
        /// The name used to display this player.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (Gamer != null)
                {
                    return Gamer.Gamertag;
                }
                else
                {
                    switch (Id)
                    {
                        case PlayerId.A: return Resources.ComputerA;
                        case PlayerId.B: return Resources.ComputerB;
                        case PlayerId.C: return Resources.ComputerC;
                        case PlayerId.D: return Resources.ComputerD;
                        default: return Resources.ComputerA;
                    }
                }
            }
        }
    }
}
