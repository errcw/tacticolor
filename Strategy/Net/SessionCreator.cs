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

        public SessionCreator(ScreenStack stack)
        {
            _stack = stack;
        }
        
        /// <summary>
        /// Creates a new network session.
        /// </summary>
        /// <param name="type">The type of session to create.</param>
        /// <param name="creator">The gamer creating the session.</param>
        public void CreateSession(NetworkSessionType type, SignedInGamer creator)
        {
            try
            {
                IAsyncResult result = NetworkSession.BeginCreate(
                    type,
                    Enumerable.Repeat(creator, 1),
                    Match.MaxPlayers,
                    0,
                    null,
                    null,
                    null);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                busyScreen.OperationCompleted += OnSessionCreated;
                _stack.Push(busyScreen);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        /// <summary>
        /// Joins an existing network session.
        /// </summary>
        /// <param name="type">The type of network session to join.</param>
        /// <param name="joiner">The gamer joining the session.</param>
        public void FindSession(NetworkSessionType type, SignedInGamer joiner)
        {
            try
            {
                IAsyncResult result = NetworkSession.BeginFind(
                    type,
                    Enumerable.Repeat(joiner, 1),
                    null,
                    null,
                    null);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                busyScreen.OperationCompleted += OnSessionsFound;
                _stack.Push(busyScreen);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void OnSessionsFound(object sender, AsyncOperationCompletedEventArgs args)
        {
            try
            {
                AvailableNetworkSessionCollection sessions = NetworkSession.EndFind(args.AsyncResult);
                IAsyncResult result = Selector.BeginInvoke(
                    sessions,
                    null,
                    null);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                busyScreen.OperationCompleted += OnSessionSelected;
                _stack.Push(busyScreen);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void OnSessionSelected(object sender, AsyncOperationCompletedEventArgs args)
        {
            try
            {
                AvailableNetworkSession session = Selector.EndInvoke(args.AsyncResult);
                if (session != null)
                {
                    IAsyncResult result = NetworkSession.BeginJoin(
                        session,
                        null,
                        null);
                    AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                    busyScreen.OperationCompleted += OnSessionCreated;
                    _stack.Push(busyScreen);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                SetSession(null);
            }
        }

        private void OnSessionCreated(object sender, AsyncOperationCompletedEventArgs args)
        {
            try
            {
                NetworkSession session = NetworkSession.EndCreate(args.AsyncResult);
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

        private static AvailableNetworkSession SelectSession(AvailableNetworkSessionCollection sessions)
        {
            var openSessions = sessions.Where(session => session.OpenPublicGamerSlots > 0);

            // if there are zero or one sessions return null or the first, respectively
            if (openSessions.Count() <= 1)
            {
                return openSessions.FirstOrDefault();
            }

            // wait for QOS data
            bool allAvailable = true;
            for (int tries = 0; tries < 10 || allAvailable; tries++)
            {
                allAvailable = true;
                foreach (AvailableNetworkSession session in openSessions)
                {
                    if (!session.QualityOfService.IsAvailable)
                    {
                        allAvailable = false;
                        break;
                    }
                }
                Thread.Sleep(200);
            }

            // select the session with the lowest ping
            TimeSpan lowestRoundtripTime = TimeSpan.MaxValue;
            AvailableNetworkSession bestSession = null;
            foreach (AvailableNetworkSession session in openSessions)
            {
                if (session.QualityOfService.IsAvailable && session.QualityOfService.AverageRoundtripTime < lowestRoundtripTime)
                {
                    lowestRoundtripTime = session.QualityOfService.AverageRoundtripTime;
                    bestSession = session;
                }
            }

            // no QOS data available
            if (bestSession == null)
            {
                bestSession = openSessions.FirstOrDefault();
            }

            return bestSession;
        }

        private delegate AvailableNetworkSession SessionSelector(AvailableNetworkSessionCollection collection);
        private SessionSelector Selector = new SessionSelector(SelectSession);

        private ScreenStack _stack;
    }
}
