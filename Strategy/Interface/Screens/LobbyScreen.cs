using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;

using Strategy.Gameplay;
using Strategy.Net;
using Strategy.Library.Extensions;
using Strategy.Library.Input;
using Strategy.Library.Screen;

namespace Strategy.Interface.Screens
{
    /// <summary>
    /// Sets up the networking.
    /// </summary>
    public class LobbyScreen : Screen
    {
        public LobbyScreen(StrategyGame game, bool hosting)
        {
            _game = game;
            _input = game.Services.GetService<MenuInput>();

            _players = new List<Player>();
        }

        protected override void UpdateActive(GameTime gameTime)
        {
            if (_session == null)
            {
#if XBOX
                CreateSession();
#else
                FindSession();
#endif
            }
            else
            {
                _session.Update();

                ReceiveSeeds();

                _input.Update(gameTime.GetElapsedSeconds());
                HandleInput();

                if (_session.IsHost && _session.IsEveryoneReady)
                {
                    _session.StartGame();
                }
            }
        }

        private void CreateSession()
        {
            try
            {
                IAsyncResult result = NetworkSession.BeginCreate(
                    NetworkSessionType.SystemLink,
                    Match.MaxPlayers,
                    Match.MaxPlayers,
                    null,
                    null);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                busyScreen.OperationCompleted += OnSessionCreated;
                Stack.Push(busyScreen);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void FindSession()
        {
            try
            {
                IAsyncResult result = NetworkSession.BeginFind(
                    NetworkSessionType.SystemLink,
                    Match.MaxPlayers,
                    null,
                    null,
                    null);
                AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                busyScreen.OperationCompleted += OnSessionsFound;
                Stack.Push(busyScreen);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void OnSessionCreated(object sender, AsyncOperationCompletedEventArgs args)
        {
            try
            {
                NetworkSession session = NetworkSession.EndCreate(args.AsyncResult);
                InitSession(session);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void OnSessionsFound(object sender, AsyncOperationCompletedEventArgs args)
        {
            try
            {
                AvailableNetworkSessionCollection sessions =  NetworkSession.EndFind(args.AsyncResult);
                AvailableNetworkSession session = SelectSession(sessions);
                if (session != null)
                {
                    IAsyncResult result = NetworkSession.BeginJoin(
                        session,
                        null,
                        null);
                    AsyncBusyScreen busyScreen = new AsyncBusyScreen(result);
                    busyScreen.OperationCompleted += OnSessionJoined;
                    Stack.Push(busyScreen);
                }
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private AvailableNetworkSession SelectSession(AvailableNetworkSessionCollection sessions)
        {
            Debug.WriteLine("Available sessions:");
            foreach (AvailableNetworkSession session in sessions)
            {
                Debug.WriteLine(session.HostGamertag);
            }
            return sessions.FirstOrDefault();
        }

        private void OnSessionJoined(object sender, AsyncOperationCompletedEventArgs args)
        {
            try
            {
                NetworkSession session = NetworkSession.EndJoin(args.AsyncResult);
                InitSession(session);
            }
            catch (Exception e)
            {
                Debug.Write(e);
            }
        }

        private void InitSession(NetworkSession session)
        {
            _session = session;
            if (_session.IsHost)
            {
                _session.AllowHostMigration = true;
                _session.AllowJoinInProgress = false;
            }
            _session.GamerJoined += OnGamerJoined;
            _session.GamerLeft += OnGamerLeft;
            _session.HostChanged += OnHostChanged;
            _session.GameStarted += OnGameStarted;
            _session.GameEnded += OnGameEnded;
            _session.SessionEnded += OnSessionEnded;
        }

        private void OnGamerJoined(object sender, GamerJoinedEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " joined");

            AddPlayer(args.Gamer);

            // if we're the host then send the initialization data to the new player
            if (_session.IsHost)
            {
                if (_seed == 0)
                {
                    _seed = _random.Next(1, int.MaxValue);
                }
                SendSeed(_seed, args.Gamer);
            }
        }

        private void OnGamerLeft(object sender, GamerLeftEventArgs args)
        {
            Debug.WriteLine(args.Gamer.Gamertag + " left");
            RemovePlayer(args.Gamer);
        }

        private void OnHostChanged(object sender, HostChangedEventArgs args)
        {
            Debug.WriteLine(args.NewHost.Gamertag + " is now host");

            // the host might have backed out before every player received
            // the seed data so broadcast a new seed to every player
            if (_session.IsHost)
            {
                _seed = _random.Next(1, int.MaxValue);
                foreach (NetworkGamer gamer in _session.AllGamers)
                {
                    SendSeed(_seed, gamer);
                }
            }
        }

        private void OnGameStarted(object sender, GameStartedEventArgs args)
        {
            Debug.Assert(_seed != 0);
            Debug.WriteLine("Game starting");

            // assign ids to players by sorting based on unique id
            // this assignment guarantees identical assignments across machines
            _players.Sort((a, b) => a.Gamer.Id.CompareTo(b.Gamer.Id));
            for (int p = 0; p < _players.Count; p++)
            {
                _players[p].Id = (PlayerId)p;
            }
        }

        private void OnGameEnded(object sender, GameEndedEventArgs args)
        {
            Debug.WriteLine("Game ended");
        }

        private void OnSessionEnded(object sender, NetworkSessionEndedEventArgs args)
        {
            Debug.WriteLine("Session ended");
        }

        private void AddPlayer(NetworkGamer gamer)
        {
            Player player = new Player();
            player.Gamer = gamer;
            _players.Add(player);
        }

        private void RemovePlayer(NetworkGamer gamer)
        {
            _players.RemoveAll(player => player.Gamer == gamer);
        }

        private Player FindPlayerByController(PlayerIndex index)
        {
            return _players.Find(player => player.Controller == index);
        }

        private void SendSeed(int seed, NetworkGamer gamer)
        {
            LocalNetworkGamer sender = (LocalNetworkGamer)_session.Host;
            CommandWriter writer = new CommandWriter();
            writer.Write(new InitializeMatchCommand(seed));
            sender.SendData(writer, SendDataOptions.Reliable);
        }

        private void ReceiveSeeds()
        {
            CommandReader reader = new CommandReader();
            foreach (LocalNetworkGamer gamer in _session.LocalGamers)
            {
                while (gamer.IsDataAvailable)
                {
                    NetworkGamer sender;
                    gamer.ReceiveData(reader, out sender);
                    InitializeMatchCommand command = reader.ReadCommand() as InitializeMatchCommand;
                    if (command != null)
                    {
                        _seed = command.RandomSeed;
                    }
                }
            }
        }

        private void HandleInput()
        {
            for (PlayerIndex p = PlayerIndex.One; p <= PlayerIndex.Four; p++)
            {
                Player player = FindPlayerByController(p);
                if (_input.Join[(int)p].Released)
                {
                    if (player != null)
                    {
                        // mark this player as ready
                        if (_seed != 0)
                        {
                            player.Gamer.IsReady = true;
                        }
                    }
                    else
                    {
                        // first time we saw this player
                        if (!p.IsSignedIn())
                        {
                            Guide.ShowSignIn(1, false);
                        }
                        if (p.IsSignedIn())
                        {
                            // join the existing session
                        }
                    }
                }
                else if (_input.Leave[(int)p].Released)
                {
                    // ignore leave requests from non-players
                    if (player != null)
                    {
                        if (player.Gamer.IsReady)
                        {
                            // back out of the ready state
                            player.Gamer.IsReady = false;
                        }
                        else
                        {
                            // leave the session if the request came from the main controller
                            if (p == _input.Controller)
                            {
                                _session.Dispose();
                                Stack.Pop();
                            }
                        }
                    }
                }
            }
        }

        private StrategyGame _game;
        private NetworkSession _session = null;

        private List<Player> _players;

        private MenuInput _input;

        private int _seed = 0;
        private Random _random = new Random();
    }
}