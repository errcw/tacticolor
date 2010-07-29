using System;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

namespace Strategy.Net
{
    /// <summary>
    /// Owns and manages the network session
    /// </summary>
    public class NetworkSessionComponent : GameComponent
    {
        public static NetworkSessionComponent Create(Game game, NetworkSession session)
        {
            NetworkSessionComponent component = new NetworkSessionComponent(game, session);
            game.Services.AddService(typeof(NetworkSession), session);
            game.Components.Add(component);
            return component;
        }

        private NetworkSessionComponent(Game game, NetworkSession session) : base(game)
        {
            _session = session;
        }

        public override void Update(GameTime gameTime)
        {
            if (_session == null)
            {
                return;
            }
            try
            {
                _session.Update();
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

        private NetworkSession _session;
    }
}