using System;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Extensions;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Sets up the networking.
    /// </summary>
    public class NetworkScreen : Screen
    {
        public NetworkScreen(StrategyGame game, bool hosting)
        {
            _game = game;
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (!_hasSession)
            {
                CreateSession();
            }
        }

        private void CreateSession()
        {
            try
            {
                PlayerIndex playerIdx = PlayerIndex.One;
                if (!playerIdx.IsSignedIn())
                {
                    Guide.ShowSignIn(1, false);
                }
                if (playerIdx.IsSignedIn() && !_creatingSession)
                {
                    _creatingSession = true;
                    IAsyncResult async = NetworkSession.BeginCreate(
                        NetworkSessionType.Local,
                        Match.MaxPlayers,
                        Match.MaxPlayers,
                        OnSessionCreated,
                        null);
                }
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void OnSessionCreated(IAsyncResult result)
        {
            try
            {
                NetworkSession session = NetworkSession.EndCreate(result);
                InitSession(session);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void InitSession(NetworkSession session)
        {
            NetworkSessionComponent.Create(_game, session);
            if (session.IsHost)
            {
                session.AllowHostMigration = true;
                session.AllowJoinInProgress = false;
            }
            session.GamerJoined += OnGamerJoined;
            session.GamerLeft += OnGamerLeft;
            session.HostChanged += OnHostChanged;
            session.GameStarted += OnGameStarted;
            session.GameEnded += OnGameEnded;
            session.SessionEnded += OnSessionEnded;
            _hasSession = true;
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs args)
        {
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
        }

        private void OnHostChanged(object sender, HostChangedEventArgs args)
        {
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
        }

        private void OnGameEnded(object sender, GameEndedEventArgs args)
        {
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
        }

        private StrategyGame _game;
        private bool _creatingSession = false;
        private bool _hasSession = false;
    }
}