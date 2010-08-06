using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;

namespace Strategy.Net
{
    /// <summary>
    /// Provides network sessions.
    /// </summary>
    public class NetworkSessionProvider
    {
        /// <summary>
        /// The current active network session, if any.
        /// </summary>
        public static NetworkSession CurrentSession { get; private set; }

        /// <summary>
        /// Starts an asynchronous operation to create a network session.
        /// </summary>
        /// <param name="type">The type of network session to create.</param>
        /// <param name="creator">The gamer creating the session.</param>
        /// <param name="callback">The method to be called once the asynchronous operation has finished.</param>
        /// <param name="asyncState">State of the asynchronous operation.</param>
        /// <returns>An IAsyncResult used to track the progress of the operation.</returns>
        public static IAsyncResult BeginCreate(NetworkSessionType type, SignedInGamer creator, AsyncCallback callback, object asyncState)
        {
            SessionAsyncResult result = new SessionAsyncResult(callback, asyncState);
            ThreadPool.QueueUserWorkItem(CreateSessionWorker(type, creator, result));
            return result;
        }

        /// <summary>
        /// Gets the result from a BeginCreate asynchronous call.
        /// </summary>
        /// <param name="result">An IAsyncResult used to track the progress of the operation.</param>
        public static NetworkSession EndCreate(IAsyncResult result)
        {
            return HandleResult(result);
        }

        /// <summary>
        /// Starts an asynchronous operation to find and join a network session.
        /// </summary>
        /// <param name="type">The type of network session to join.</param>
        /// <param name="joiner">The gamer joinig the session.</param>
        /// <param name="callback">The method to be called once the asynchronous operation has finished.</param>
        /// <param name="asyncState">State of the asynchronous operation.</param>
        /// <returns>An IAsyncResult used to track the progress of the operation.</returns>
        public static IAsyncResult BeginFindAndJoin(NetworkSessionType type, SignedInGamer joiner, AsyncCallback callback, object asyncState)
        {
            SessionAsyncResult result = new SessionAsyncResult(callback, asyncState);
            ThreadPool.QueueUserWorkItem(FindJoinSessionWorker(type, joiner, result));
            return result;
        }

        /// <summary>
        /// Gets the result from a BeginFindAndJoin asynchronous call.
        /// </summary>
        /// <param name="result">An IAsyncResult used to track the progress of the operation.</param>
        public static NetworkSession EndFindAndJoin(IAsyncResult result)
        {
            return HandleResult(result);
        }

        /// <summary>
        /// Starts an asynchronous operation to join an invited network session.
        /// </summary>
        /// <param name="accepter">The gamer accepting the invite for the session.</param>
        /// <param name="callback">The method to be called once the asynchronous operation has finished.</param>
        /// <param name="asyncState">State of the asynchronous operation.</param>
        /// <returns>An IAsyncResult used to track the progress of the operation.</returns>
        public static IAsyncResult BeginJoinInvited(SignedInGamer accepter, AsyncCallback callback, object asyncState)
        {
            SessionAsyncResult result = new SessionAsyncResult(callback, asyncState);
            ThreadPool.QueueUserWorkItem(JoinInvitedWorker(accepter, result));
            return result;
        }

        /// <summary>
        /// Gets the result from a BeginJoinInvited asynchronous call.
        /// </summary>
        /// <param name="result">An IAsyncResult used to track the progress of the operation.</param>
        public static NetworkSession EndJoinInvited(IAsyncResult result)
        {
            return HandleResult(result);
        }

        /// <summary>
        /// Handles an asynchronous result passed back from an End call.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static NetworkSession HandleResult(IAsyncResult result)
        {
            SessionAsyncResult selectionResult = (SessionAsyncResult)result;
            if (!selectionResult.IsCompleted)
            {
                selectionResult.AsyncWaitHandle.WaitOne();
            }
            return selectionResult.Result;
        }

        /// <summary>
        /// Sets the current global session.
        /// </summary>
        private static void SetCurrentSession(NetworkSession session)
        {
            CurrentSession = session;
            CurrentSession.SessionEnded += (s,a) => CurrentSession = null;
        }

        /// <summary>
        /// Creates a worker thread function for creating a session.
        /// </summary>
        private static WaitCallback CreateSessionWorker(NetworkSessionType type, SignedInGamer creator, SessionAsyncResult result)
        {
            return delegate(object dummyParam)
            {
                try
                {
                    NetworkSession session = NetworkSession.Create(
                        type,
                        Enumerable.Repeat(creator, 1),
                        Match.MaxPlayers,
                        0,
                        null);
                    SetCurrentSession(session);
                    result.Complete(session, false);
                }
                catch
                {
                    result.Complete(null, false);
                }
            };
        }

        /// <summary>
        /// Creates a worker thread function for finding and joining a session.
        /// </summary>
        private static WaitCallback FindJoinSessionWorker(NetworkSessionType type, SignedInGamer joiner, SessionAsyncResult result)
        {
            return delegate(object dummyParam)
            {
                try
                {
                    AvailableNetworkSessionCollection availableSessions = NetworkSession.Find(
                        type,
                        Enumerable.Repeat(joiner, 1),
                        null);

                    // wait for QOS data if we need to choose between sessions
                    if (availableSessions.Count > 1)
                    {
                        int timeoutMs = 3000;
                        bool allAvailable = true;
                        while (true)
                        {
                            foreach (AvailableNetworkSession session in availableSessions)
                            {
                                if (!session.QualityOfService.IsAvailable)
                                {
                                    allAvailable = false;
                                    break;
                                }
                            }
                            if (allAvailable || timeoutMs <= 0)
                            {
                                break;
                            }
                            else
                            {
                                Thread.Sleep(200);
                                timeoutMs -= 200;
                            }
                        }
                    }

                    // try the first three sessions with the best average ping
                    var orderedSessions = availableSessions.OrderBy(s => s.QualityOfService.IsAvailable ? s.QualityOfService.AverageRoundtripTime : TimeSpan.MaxValue);
                    var attemptedJoinSessions = orderedSessions.Take(3);
                    NetworkSession joinedSession = null;
                    foreach (AvailableNetworkSession availableSession in attemptedJoinSessions)
                    {
                        try
                        {
                            joinedSession = NetworkSession.Join(availableSession);
                            SetCurrentSession(joinedSession);
                            break;
                        }
                        catch
                        {
                            // this session might already be full, try the next one
                        }
                    }

                    result.Complete(joinedSession, false);
                }
                catch
                {
                    result.Complete(null, false);
                }
            };
        }

        /// <summary>
        /// Creates a worker thread function for accepting an invite.
        /// </summary>
        private static WaitCallback JoinInvitedWorker(SignedInGamer accepter, SessionAsyncResult result)
        {
            return delegate(object dummyParam)
            {
                try
                {
                    NetworkSession session = NetworkSession.JoinInvited(Enumerable.Repeat(accepter, 1));
                    SetCurrentSession(session);
                    result.Complete(session, false);
                }
                catch
                {
                    result.Complete(null, false);
                }
            };
        }

        /// <summary>
        /// Asynchronous result from selecting a session.
        /// </summary>
        private class SessionAsyncResult : IAsyncResult
        {
            public object AsyncState
            {
                get { return _asyncState; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _waitHandle; }
            }

            public bool CompletedSynchronously
            {
                get { lock (_syncRoot) { return _completedSynchronously; } }
            }

            public bool IsCompleted
            {
                get { lock (_syncRoot) { return _completed; } }
            }

            public NetworkSession Result
            {
                get { lock (_syncRoot) { return _result; } }
            }

            public SessionAsyncResult(AsyncCallback cb, object state)
            {
                _callback = cb;
                _asyncState = state;
                _completed = false;
                _completedSynchronously = false;

                _waitHandle = new ManualResetEvent(false);
                _syncRoot = new object();
            }

            public void Complete(NetworkSession result, bool completedSynchronously)
            {
                lock (_syncRoot)
                {
                    _completed = true;
                    _completedSynchronously = completedSynchronously;
                    _result = result;
                }
                SignalCompletion();
            }

            private void SignalCompletion()
            {
                _waitHandle.Set();
                ThreadPool.QueueUserWorkItem(new WaitCallback(InvokeCallback));
            }

            private void InvokeCallback(object state)
            {
                if (_callback != null)
                {
                    _callback(this);
                }
            }

            private readonly AsyncCallback _callback;
            private bool _completed;
            private bool _completedSynchronously;
            private readonly object _asyncState;
            private readonly ManualResetEvent _waitHandle;
            private NetworkSession _result;
            private readonly object _syncRoot;
        }
    }
}
