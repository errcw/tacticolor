using System;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Sets up the networking.
    /// </summary>
    public class NetworkScreen : Screen
    {
        public NetworkScreen(bool hosting)
        {
        }

        protected override void UpdateActive(GameTime gameTime)
        {
        }

        private void CreateSession()
        {
            try
            {
                IAsyncResult async = NetworkSession.BeginCreate(
                    NetworkSessionType.Local,
                    Match.MaxPlayers,
                    Match.MaxPlayers,
                    OnSessionCreated,
                    null);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void OnSessionCreated(IAsyncResult result)
        {
            NetworkSession session = NetworkSession.EndCreate(result);
        }
    }
}