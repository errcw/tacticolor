using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Interface;
using Strategy.Interface.Screens;
using Strategy.Library.Screen;

namespace Strategy.Net
{
    /// <summary>
    /// Creates and joins NetworkSessions.
    /// </summary>
    public class SessionCreator
    {
        /// <summary>
        /// The created network session, or null if session creation failed.
        /// </summary>
        public NetworkSession Session { get; private set; }

        /// <summary>
        /// Notifies listeners that the session creation is finished.
        /// </summary>
        public event EventHandler<EventArgs> SesssionCreated;
        
        /// <summary>
        /// Creates a new network session.
        /// </summary>
        /// <param name="type">The type of session to create.</param>
        /// <param name="creator">The gamer creating the session.</param>
        public void CreateSession(NetworkSessionType type, SignedInGamer creator)
        {
            try
            {
                NetworkSession.BeginCreate(
                    type,
                    Enumerable.Repeat(creator, 1),
                    Match.MaxPlayers,
                    0,
                    null,
                    OnSessionCreated,
                    null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        /// <summary>
        /// Finds an existing network session to join.
        /// </summary>
        /// <param name="type">The type of network session to join.</param>
        /// <param name="joiner">The gamer joining the session.</param>
        public void FindSession(NetworkSessionType type, SignedInGamer joiner)
        {
            try
            {
                NetworkSession.BeginFind(
                    type,
                    Enumerable.Repeat(joiner, 1),
                    null,
                    OnSessionsFound,
                    null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void OnSessionCreated(IAsyncResult result)
        {
            try
            {
                NetworkSession session = NetworkSession.EndCreate(result);
                SetSession(session);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void OnSessionsFound(IAsyncResult result)
        {
            try
            {
                AvailableNetworkSessionCollection sessions = NetworkSession.EndFind(result);
                NetworkSessionSelector.BeginSelect(
                    sessions,
                    2000,
                    OnSessionSelected,
                    null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void OnSessionSelected(IAsyncResult result)
        {
            try
            {
                AvailableNetworkSession session = NetworkSessionSelector.EndSelect(result);
                if (session != null)
                {
                    NetworkSession.BeginJoin(
                        session,
                        OnSessionJoined,
                        null);
                }
                else
                {
                    SetSession(null);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void OnSessionJoined(IAsyncResult result)
        {
            try
            {
                NetworkSession session = NetworkSession.EndJoin(result);
                SetSession(session);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void SetSession(NetworkSession session)
        {
            Session = session;
            if (SesssionCreated != null)
            {
                SesssionCreated(this, EventArgs.Empty);
            }
        }
    }
}
