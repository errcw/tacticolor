using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Strategy.Interface.Gameplay
{
    /// <summary>
    /// Encapsulates shared interface state.
    /// </summary>
    public class InterfaceContext
    {
        public readonly Game Game;
        public readonly ContentManager Content;
        public readonly IsometricParameters IsoParams;
        public readonly ICollection<Player> Players;

        public InterfaceContext(Game game, ContentManager content, IsometricParameters isoParams, ICollection<Player> players)
        {
            Game = game;
            Content = content;
            IsoParams = isoParams;
            Players = players;
        }
    }
}
