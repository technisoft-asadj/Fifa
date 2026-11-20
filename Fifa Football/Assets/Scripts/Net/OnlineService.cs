using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>
    /// Internet play without a server of our own: Unity Authentication (anonymous, nothing to type), Unity Relay (carries the match between two
    /// phones anywhere) and Unity Lobby (lists open public rooms for quick match). A room is a Relay allocation; its short join code IS the room code.
    /// Needs the project linked to a Unity cloud project with Relay and Lobby switched on; without that every call fails with a readable message
    /// (<see cref="Describe"/>) and the rest of the game is unaffected. One guest per room, as the match supports one friend.
    /// </summary>
    public static class OnlineService
    {
        const string RelayCodeKey = "relay";
        const int MaxGuests = 1;

        static Task signIn;

        /// <summary>Initialises Unity Services and signs in anonymously, once; later calls share the same task.</summary>
        public static Task EnsureSignedIn()
        {
            if (signIn == null || signIn.IsFaulted || signIn.IsCanceled) signIn = SignIn();
            return signIn;
        }

        static async Task SignIn()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>A room this phone is hosting: the code to share and the relay transport its match server runs on.</summary>
        public sealed class HostedRoom
        {
            public string Code;
            public RelayTransport Transport;
            public string LobbyId; // null for a private room
        }

        /// <summary>Opens a room. <paramref name="publicRoom"/> also lists it for quick match; a private one is reachable only by its code.</summary>
        public static async Task<HostedRoom> CreateRoom(bool publicRoom)
        {
            await EnsureSignedIn();
            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxGuests);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            var transport = RelayTransport.Host(allocation);
            var room = new HostedRoom { Code = code, Transport = transport };
            if (publicRoom)
            {
                try
                {
                    var lobby = await LobbyService.Instance.CreateLobbyAsync("Soccer Fight " + code, MaxGuests + 1, new CreateLobbyOptions
                    {
                        IsPrivate = false,
                        Data = new Dictionary<string, DataObject> { { RelayCodeKey, new DataObject(DataObject.VisibilityOptions.Public, code) } },
                    });
                    room.LobbyId = lobby.Id;
                }
                catch { transport.Dispose(); throw; }
            }
            return room;
        }

        /// <summary>Joins the room with this code. Returns the guest's relay transport; it is connected once <see cref="RelayTransport.Connected"/> turns true.</summary>
        public static async Task<RelayTransport> JoinRoom(string code)
        {
            code = NormaliseCode(code);
            if (code.Length == 0) throw new ArgumentException("Type the room code first.");
            await EnsureSignedIn();
            var allocation = await RelayService.Instance.JoinAllocationAsync(code);
            return RelayTransport.Join(allocation);
        }

        /// <summary>The result of quick match: either a guest transport (joined someone's open room) or a freshly opened public room to host.</summary>
        public sealed class QuickResult
        {
            public RelayTransport Guest;
            public HostedRoom Hosted;
        }

        /// <summary>Looks for an open public room and joins it; when there is none, opens one and waits for the next player.</summary>
        public static async Task<QuickResult> QuickMatch()
        {
            await EnsureSignedIn();
            try
            {
                var lobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions
                {
                    Filter = new List<QueryFilter> { new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) },
                });
                if (lobby.Data != null && lobby.Data.TryGetValue(RelayCodeKey, out var data))
                    return new QuickResult { Guest = await JoinRoom(data.Value) };
            }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.NoOpenLobbies) { }
            return new QuickResult { Hosted = await CreateRoom(true) };
        }

        /// <summary>Keeps a public room listed while it waits and takes it off the list once a friend has joined or the host leaves.</summary>
        public static GameObject KeepListed(HostedRoom room)
        {
            var go = new GameObject("OnlineRoomKeeper");
            go.AddComponent<RoomKeeper>().Init(room);
            return go;
        }

        sealed class RoomKeeper : MonoBehaviour
        {
            HostedRoom room; float nextBeat; bool removed;
            public void Init(HostedRoom r) { room = r; nextBeat = Time.unscaledTime + 15f; }

            void Update()
            {
                if (room == null || room.LobbyId == null || removed) return;
                if (room.Transport.GuestCount > 0) { Remove(); return; }
                if (Time.unscaledTime < nextBeat) return;
                nextBeat = Time.unscaledTime + 15f; // a lobby with no heartbeat disappears after about 30 s
                Beat(room.LobbyId);
            }

            static async void Beat(string id) { try { await LobbyService.Instance.SendHeartbeatPingAsync(id); } catch { } }

            void Remove()
            {
                if (removed || room == null || room.LobbyId == null) return;
                removed = true;
                Delete(room.LobbyId);
            }

            static async void Delete(string id) { try { await LobbyService.Instance.DeleteLobbyAsync(id); } catch { } }

            void OnDestroy() { Remove(); }
        }

        /// <summary>Room codes are typed by people: trim, upper-case, drop spaces and dashes.</summary>
        public static string NormaliseCode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in raw) if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            return sb.ToString();
        }

        /// <summary>A sentence a player can act on for whatever went wrong.</summary>
        public static string Describe(Exception e)
        {
            if (e is ArgumentException) return e.Message;
            if (e is AggregateException ag && ag.InnerException != null) return Describe(ag.InnerException);
            if (e is RelayServiceException rs)
                return rs.Reason == RelayExceptionReason.JoinCodeNotFound ? "No room with that code. Check it and try again." : "The relay is not available right now. Try again in a moment.";
            if (e is LobbyServiceException) return "Matchmaking is not available right now. Try again in a moment.";
            if (e is RequestFailedException) return "Could not reach the online service. Check the internet connection.";
            return "Online play could not start: " + e.Message;
        }
    }
}
