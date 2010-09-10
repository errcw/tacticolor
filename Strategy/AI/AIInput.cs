using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Input;

namespace Strategy.AI
{
    /// <summary>
    /// AI difficulty rating.
    /// </summary>
    public enum AIDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    /// <summary>
    /// Generates input from a computer player.
    /// </summary>
    public class AIInput : ICommandProvider
    {
        /// <summary>
        /// Creates a new AI input.
        /// </summary>
        /// <param name="difficulty">The difficulty of the player.</param>
        public AIInput(PlayerId player, Match match, AIDifficulty difficulty)
        {
            _player = player;
            _match = match;
            _difficulty = difficulty;
        }

        /// <summary>
        /// Updates the input state.
        /// </summary>
        public Command Update(int time)
        {
            if (CanPlacePiece())
            {
                Territory place = GetOwnedTerritories().FirstOrDefault();
                if (place != null && _match.CanPlacePiece(_player, place))
                {
                    return new PlaceCommand(_player, place);
                }
            }
            return null;
        }

        private bool CanPlacePiece()
        {
            return _match.PiecesAvailable[(int)_player] > 0;
        }

        private IEnumerable<Territory> GetOwnedTerritories()
        {
            return _match.Map.Territories.Where(t => t.Owner == _player);
        }

        private PlayerId _player;
        private Match _match;
        private AIDifficulty _difficulty;
    }
}
