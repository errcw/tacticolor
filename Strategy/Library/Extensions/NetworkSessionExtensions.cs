using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace Strategy.Library.Extensions
{
    /// <summary>
    /// Extensions to NetworkSession.
    /// </summary>
    public static class NetworkSessionExtensions
    {
        /// <summary>
        /// Checks if SessionType is NetworkSessionType.Local.
        /// </summary>
        public static bool IsLocalSession(this NetworkSession session)
        {
            return session.SessionType == NetworkSessionType.Local;
        }

        /// <summary>
        /// Checks if SessionType is NetworkSessionType.PlayerMatch or NetworkSessionType.Ranked.
        /// </summary>
        public static bool IsOnlineSession(this NetworkSession session)
        {
            return session.SessionType == NetworkSessionType.PlayerMatch ||
                   session.SessionType == NetworkSessionType.Ranked;
        }
    }
}