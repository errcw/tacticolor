using System;

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

        public InterfaceContext(Game game, ContentManager content, IsometricParameters isoParams)
        {
            Game = game;
            Content = content;
            IsoParams = isoParams;
        }
    }
}
