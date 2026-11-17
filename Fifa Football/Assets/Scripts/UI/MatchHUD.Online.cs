using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.Net;
using FootballSim.Players;

namespace FootballSim.UI
{
    /// <summary>
    /// The "Online" screen: host a match for a friend on the same Wi-Fi, or join one by typing the host's address. Plain UDP between two
    /// devices: no account, relay or paid service. The host plays the home team as usual and the guest takes one away outfield player;
    /// the guest sees the host's match from snapshots (<see cref="GuestMode"/>). Back or the pause button leaves a guest game.
    /// </summary>
    public partial class MatchHUD
    {
        public const int OnlinePort = LanAddress.DefaultPort;
        public const string LastAddressKey = "online_last_address";

        GameObject onlinePanel;
        InputField addressField;
        Text onlineInfo, onlineStatus, onlineBanner;
        GuestMode guest;
        NetSession.HostHandle hosting;
        float guestStartedAt;
        string guestAddress = "";

        public bool OnlineGuestActive => guest != null && guest.Active;
        public bool OnlineHosting => hosting != null && hosting.root != null;
        public string OnlineStatusText => onlineStatus != null ? onlineStatus.text : "";
        public string OnlineBannerText => onlineBanner != null ? onlineBanner.text : "";
        public NetSession.HostHandle OnlineHost => hosting;
        public GuestMode OnlineGuest => guest;

        void BuildOnlinePanel(RectTransform root, Color btnCol)
        {
            onlinePanel = Rect("OnlinePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900), new Color(0.05f, 0.08f, 0.12f, 1f)).gameObject;
            var op = onlinePanel.transform;
            var title = Label("OnlineTitle", op, 64, TextAnchor.MiddleCenter, new Vector2(0, 340), new Vector2(860, 90)); title.text = "PLAY ONLINE"; title.fontStyle = FontStyle.Bold;
            var note = Label("OnlineNote", op, 26, TextAnchor.MiddleCenter, new Vector2(0, 270), new Vector2(860, 40));
            note.text = "Both devices on the same Wi-Fi. No account or server needed."; note.color = new Color(1, 1, 1, 0.8f);

            onlineInfo = Label("OnlineInfo", op, 30, TextAnchor.MiddleCenter, new Vector2(0, 170), new Vector2(860, 50));
            Button(op, "HOST MATCH", new Vector2(0, 80), new Vector2(460, 100), HostOnline, Accent);

            var joinLabel = Label("JoinLabel", op, 30, TextAnchor.MiddleCenter, new Vector2(0, -50), new Vector2(860, 50)); joinLabel.text = "Or join a friend: type the address they see";
            addressField = BuildAddressField(op, new Vector2(0, -130), new Vector2(560, 80));
            Button(op, "JOIN", new Vector2(0, -245), new Vector2(460, 100), () => JoinOnline(addressField.text), Accent);

            onlineStatus = Label("OnlineStatus", op, 26, TextAnchor.MiddleCenter, new Vector2(0, -335), new Vector2(860, 50));
            onlineStatus.color = new Color(1f, 0.9f, 0.4f);
            Button(op, "BACK", new Vector2(0, -400), new Vector2(260, 70), CloseOnline, btnCol);

            // a one-line status in the corner while hosting or playing as a guest
            onlineBanner = Label("OnlineBanner", root, 26, TextAnchor.MiddleLeft, new Vector2(20, -30), new Vector2(900, 40));
            var brt = onlineBanner.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f); brt.pivot = new Vector2(0f, 1f); brt.anchoredPosition = new Vector2(20, -20);
            onlineBanner.color = new Color(1f, 0.9f, 0.4f);
            onlineBanner.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2, -2);
            onlinePanel.SetActive(false);
        }

        InputField BuildAddressField(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("AddressField", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.color = new Color(0.14f, 0.19f, 0.27f);
            var inner = size - new Vector2(30, 10);
            var text = Label("Text", go.transform, 38, TextAnchor.MiddleLeft, Vector2.zero, inner); text.supportRichText = false;
            var placeholder = Label("Placeholder", go.transform, 38, TextAnchor.MiddleLeft, Vector2.zero, inner);
            placeholder.text = "192.168.1.20"; placeholder.fontStyle = FontStyle.Italic; placeholder.color = new Color(1, 1, 1, 0.35f);
            var field = go.GetComponent<InputField>();
            field.targetGraphic = img; field.textComponent = text; field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            field.characterLimit = 21; // 255.255.255.255:65535
            field.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
            string last = "";
            try { last = PlayerPrefs.GetString(LastAddressKey, ""); } catch { }
            field.text = last;
            return field;
        }

        void OpenOnline()
        {
            if (OnlineGuestActive) return;
            string mine = LanAddress.LocalAddress();
            onlineInfo.text = OnlineHosting ? $"Hosting at {AddressOfHost()}"
                : mine != null ? $"Your address: {mine}:{OnlinePort}" : "No Wi-Fi address found - connect to Wi-Fi first";
            onlineStatus.text = "";
            onlinePanel.SetActive(true);
        }

        void CloseOnline() { onlinePanel.SetActive(false); }

        string AddressOfHost()
        {
            string ip = LanAddress.LocalAddress() ?? "this device";
            return OnlineHosting ? $"{ip}:{hosting.server.LocalEndPoint.Port}" : ip;
        }

        /// <summary>Starts the match as usual and lets one friend join as an away outfield player. Online play is for single matches.</summary>
        public void HostOnline()
        {
            if (OnlineHosting) { onlineStatus.text = "Already hosting. Press BACK to return to the match."; return; }
            if (homeTeam == null || awayTeam == null || match == null) { onlineStatus.text = "No match to host."; return; }
            if (TrainingMode.Active || MiniLeague.Active || Tournament.Active || Season.Active)
            {
                onlineStatus.text = "Online needs Single match mode (turn off league, cup, season, training).";
                return;
            }
            int slot = -1;
            for (int i = awayTeam.players.Count - 1; i >= 0; i--) if (!awayTeam.players[i].isGoalkeeper) { slot = homeTeam.players.Count + i; break; }
            if (slot < 0) { onlineStatus.text = "No free player for a guest."; return; }
            var players = new List<PlayerController>(homeTeam.players);
            players.AddRange(awayTeam.players);
            var ball = FindFirstObjectByType<FootballSim.Ball.BallController>();
            BeginMatch();
            try { hosting = NetSession.HostOnLan(match, ball, players, OnlinePort, slot); }
            catch (System.Exception e) { Debug.LogWarning("Could not host: " + e.Message); hosting = null; onlineStatus.text = "Could not start hosting on this network."; Time.timeScale = 0f; startPanel.SetActive(true); return; }
            onlinePanel.SetActive(false);
        }

        /// <summary>Joins the match at the typed address. A bad address only shows a message.</summary>
        public void JoinOnline(string text)
        {
            if (OnlineGuestActive) return;
            if (!LanAddress.TryParse(text, out var endPoint))
            {
                onlineStatus.text = "That is not an address. Example: 192.168.1.20";
                return;
            }
            try { PlayerPrefs.SetString(LastAddressKey, text.Trim()); PlayerPrefs.Save(); } catch { }
            FootballSim.Inputs.IPlayerInput source = homeTeam != null && homeTeam.humanInput != null ? (FootballSim.Inputs.IPlayerInput)homeTeam.humanInput : Touch();
            guest = GuestMode.Enter(endPoint, source);
            guestStartedAt = Time.unscaledTime; guestAddress = endPoint.ToString();
            HideAllMenus();
            Time.timeScale = 1f;
        }

        /// <summary>Leaves an online game as a guest and returns to the start screen with the local match as it was.</summary>
        public void LeaveOnline()
        {
            if (guest != null) guest.Leave();
            guest = null;
            ShowMainMenu();
            onlineBanner.text = OnlineHosting ? onlineBanner.text : "";
        }

        void UpdateOnlineLabels()
        {
            if (onlineBanner == null) return;
            if (OnlineGuestActive)
            {
                var client = guest.Session.client;
                if (client.ServerFull) onlineBanner.text = "ONLINE: that match is full  -  Back to leave";
                else if (client.Joined) onlineBanner.text = $"ONLINE GUEST  -  you play as player {client.PlayerIndex + 1}  -  Back to leave";
                else if (Time.unscaledTime - guestStartedAt > 8f) onlineBanner.text = $"No answer from {guestAddress}. Same Wi-Fi? Host pressed HOST?  -  Back to leave";
                else onlineBanner.text = $"ONLINE: connecting to {guestAddress}...";
                scoreText.text = "ONLINE GUEST"; clockText.text = "";
            }
            else if (OnlineHosting)
            {
                int n = hosting.server.ClientCount;
                onlineBanner.text = n == 0 ? $"HOSTING at {AddressOfHost()}  -  waiting for a friend" : $"HOSTING at {AddressOfHost()}  -  friend connected";
            }
            else if (onlineBanner.text.Length > 0) onlineBanner.text = "";
        }
    }
}
