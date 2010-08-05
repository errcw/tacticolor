using System;
using System.Linq;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace Strategy.Net
{
    /// <summary>
    /// Selects network sessions from a list.
    /// </summary>
    public class NetworkSessionSelector
    {
        /// <summary>
        /// Selects the best network session to connect to from a collection.
        /// </summary>
        /// <param name="sessions">The sesssions to select from.</param>
        /// <param name="timeoutMs">The timeout in milliseconds to wait for quality of service data.</param>
        /// <param name="callback">The method to be called once the asynchronous operation has finished.</param>
        /// <param name="asyncState">State of the asynchronous operation.</param>
        /// <returns>An IAsyncResult used to track the progress of the operation.</returns>
        public static IAsyncResult BeginSelect(AvailableNetworkSessionCollection sessions, int timeoutMs, AsyncCallback callback, object asyncState)
        {
            SelectionAsyncResult result = new SelectionAsyncResult(callback, asyncState);
            if (sessions.Count <= 1)
            {
                // just return the zero or one available session
                result.Complete(sessions.FirstOrDefault(), true);
            }
            else
            {
                // wait for data to make a selection in a separate thread
                ThreadPool.QueueUserWorkItem(SelectSessionWorker(sessions, result, timeoutMs));
            }
            return result;
        }
        
        /// <summary>
        /// Gets the result from a BeginSelect asynchronous call.
        /// </summary>
        /// <param name="result">An IAsyncResult used to track the progress of the operation.</param>
        public static AvailableNetworkSession EndSelect(IAsyncResult result)
        {
            SelectionAsyncResult selectionResult = (SelectionAsyncResult)result;
            if (!selectionResult.IsCompleted)
            {
                selectionResult.AsyncWaitHandle.WaitOne();
            }
            return selectionResult.Result;
        }

        /// <summary>
        /// Worker thread to select a session.
        /// </summary>
        private static WaitCallback SelectSessionWorker(AvailableNetworkSessionCollection sessions, SelectionAsyncResult result, int timeoutMs)
        {
            return delegate(object dummyParam)
            {
                var openSessions = sessions.Where(session => session.OpenPublicGamerSlots > 0);

                // wait for QOS data
                bool allAvailable = true;
                while (true)
                {
                    foreach (AvailableNetworkSession session in openSessions)
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

                result.Complete(bestSession, false);
            };
        }

        /// <summary>
        /// Asynchronous result from selecting a session.
        /// </summary>
        private class SelectionAsyncResult : IAsyncResult
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
                get 
                {
                    lock (_syncRoot)
                    {
                        return _completedSynchronously;
                    }
                }
            }
         
            public bool IsCompleted
            {
                get 
                {
                    lock (_syncRoot)
                    {
                        return _completed;
                    }
                }
            }

            public AvailableNetworkSession Result
            {
                get
                {
                    lock (_syncRoot)
                    {
                        return _result;
                    }
                }
            }

            public SelectionAsyncResult(AsyncCallback cb, object state)
            {
                _callback = cb;
                _asyncState = state;
                _completed = false;
                _completedSynchronously = false;
         
                _waitHandle = new ManualResetEvent(false);
                _syncRoot = new object();
            }
         
            public void Complete(AvailableNetworkSession result, bool completedSynchronously)
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
            private AvailableNetworkSession _result;
            private readonly object _syncRoot;
         
        }
    }
}
