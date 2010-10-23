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
        /// <summary>
        /// Creates a new color using a base color and alpha value.
        /// </summary>
        public static Color FromPremultiplied(Color color, float alpha)
        {
            return new Color(new Vector4(color.ToVector3(), alpha));
        }

        /// <summary>
        /// Converts a nonpremultiplied base color and alpha value into a
        /// premultiplied color.
        /// </summary>
        public static Color FromNonPremultiplied(Color color, float alpha)
        {
            return Color.FromNonPremultiplied(new Vector4(color.ToVector3(), alpha));
        }
    }
}
