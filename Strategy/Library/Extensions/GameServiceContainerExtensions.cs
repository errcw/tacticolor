using System;

using Microsoft.Xna.Framework;

namespace Strategy.Library.Extensions
{
    /// <summary>
    /// Provides generic versions of the GameServiceContainer methods.
    /// </summary>
    public static class GameServiceContainerExtensions
    {
        public static void AddService<T>(this GameServiceContainer container, T provider)
        {
            container.AddService(typeof(T), provider);
        }

        public static T GetService<T>(this GameServiceContainer container)
        {
            return (T)container.GetService(typeof(T));
        }

        public static void RemoveService<T>(this GameServiceContainer container)
        {
            container.RemoveService(typeof(T));
        }
    }
}
