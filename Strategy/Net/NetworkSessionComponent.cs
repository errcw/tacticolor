using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;

namespace Strategy.Net
{
    /// <summary>
    /// Owns and manages the network session
    /// </summary>
    public class NetworkSessionComponent : GameComponent
    {
        public NetworkSession Session { get { return _session; } }

        public static NetworkSessionComponent Create(Game game, NetworkSession session)
        {
            NetworkSessionComponent component = new NetworkSessionComponent(game, session);
            game.Services.AddService(typeof(NetworkSession), session);
            game.Components.Add(component);
            return component;
        }

        public static void CreateInvited(Game game, InviteAcceptedEventArgs args)
        {
            NetworkSessionComponent component = game.Components.OfType<NetworkSessionComponent>().FirstOrDefault();
            if (component != null)
            {
                if (args.IsCurrentSession)
                {
                    component.Session.AddLocalGamer(args.Gamer);
                }
                else
                {
                    component.Dispose();
                    component = null;
                }
            }
            if (component == null)
            {
                try
                {
                    NetworkSession.BeginJoinInvited(1, OnJoinInvitedOperationCompleted, game);
                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
            }
        }

        private NetworkSessionComponent(Game game, NetworkSession session) : base(game)
        {
            _session = session;
        }

        public override void Update(GameTime gameTime)
        {
            try
            {
                if (_session != null)
                {
                    _session.Update();
                }
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
            base.Update(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Game.Components.Remove(this);
                Game.Services.RemoveService(typeof(NetworkSession));
                if (_session != null)
                {
                    _session.Dispose();
                    _session = null;
                }
            }
            base.Dispose(disposing);
        }

        private static void OnJoinInvitedOperationCompleted(IAsyncResult result)
        {
            try
            {
                NetworkSession session = NetworkSession.EndJoinInvited(result);
                Game game = (Game)result.AsyncState;
                Create(game, session);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private NetworkSession _session;
    }
}