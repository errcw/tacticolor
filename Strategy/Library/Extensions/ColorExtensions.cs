using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Strategy.Library.Extensions
{
    /// <summary>
    /// Extension methods for the Colors.
    /// </summary>
    public static class ColorExtensions
    {
        public static Color FromNonPremultiplied(Color color, float alpha)
        {
            return Color.FromNonPremultiplied(new Vector4(color.ToVector3(), alpha));
        }
    }
}
