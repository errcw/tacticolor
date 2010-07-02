using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Strategy.Interface
{
    /// <summary>
    /// Encapsulates shared interface state.
    /// </summary>
    public class InterfaceContext
    {
        public readonly StrategyGame Game;
        public readonly ContentManager Content;
        public readonly IsometricParameters IsoParams;

        public InterfaceContext(StrategyGame game, ContentManager content, IsometricParameters isoParams)
        {
            Game = game;
            Content = content;
            IsoParams = isoParams;
        }
    }
}
