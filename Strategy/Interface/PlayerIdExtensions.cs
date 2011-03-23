using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Strategy.Gameplay;

namespace Strategy.Interface
{
    /// <summary>
    /// Interface-related extensions for PlayerId.
    /// </summary>
    public static class PlayerIdExtensions
    {
        public static Color GetPieceColor(this PlayerId playerId)
        {
            switch (playerId)
            {
                case PlayerId.A: return new Color(221, 104, 168);
                case PlayerId.B: return new Color(103, 180, 219);
                case PlayerId.C: return new Color(82, 165, 114);
                case PlayerId.D: return new Color(249, 235, 124);
                default: throw new ArgumentException("Invalid player id " + playerId);
            }
        }

        public static Color GetTerritoryColor(this PlayerId? playerId)
        {
            return playerId.HasValue ? playerId.Value.GetTerritoryColor() : Color.White;
        }

        public static Color GetTerritoryColor(this PlayerId playerId)
        {
            switch (playerId)
            {
                case PlayerId.A: return new Color(222, 35, 136);
                case PlayerId.B: return new Color(33, 157, 221);
                case PlayerId.C: return new Color(0, 168, 67);
                case PlayerId.D: return new Color(251, 223, 0);
                default: throw new ArgumentException("Invalid player id " + playerId);
            }
        }

        public static Color GetSelectionColor(this PlayerId playerId)
        {
            switch (playerId)
            {
                case PlayerId.A: return new Color(255, 109, 189);
                case PlayerId.B: return new Color(112, 207, 255);
                case PlayerId.C: return new Color(66, 206, 119);
                case PlayerId.D: return new Color(255, 242, 147);
                default: throw new ArgumentException("Invalid player id " + playerId);
            }
        }
    }
}
