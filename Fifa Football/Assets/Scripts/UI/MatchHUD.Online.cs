using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FootballSim.Core;
using FootballSim.Net;
using FootballSim.Players;
using TMPro;

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
        GameObject pingPill; Image pingDot; TextMeshProUGUI pingText;
        int lastGuestHome = -1, lastGuestAway = -1;
        string guestAddress = "";

        public bool OnlineGuestActive => guest != null && guest.Active;
        public bool OnlineHosting => hosting != null && hosting.root != null;
        public string OnlineStatusText => onlineStatus != null ? onlineStatus.text : "";
        public string OnlineBannerText => onlineBanner != null ? onlineBanner.text : "";
        public NetSession.HostHandle OnlineHost => hosting;
        public GuestMode OnlineGuest => guest;

        void BuildOnlinePanel(RectTransform root, Color btnCol)
        {
            onlinePanel = MatchHUD.Glassify(Rect("OnlinePanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560, 760), new Color(0.05f, 0.08f, 0.12f, 1f)).gameObject);
            var op = onlinePanel.transform;
            var onlineFill = onlinePanel.GetComponent<Image>(); var oc = onlineFill.color; oc.a = 1f; onlineFill.color = oc; // opaque: it sits over the main menu
            var title = Label("OnlineTitle", op, 66, TextAnchor.MiddleCenter, new Vector2(0, 312), new Vector2(900, 90)); title.text = "PLAY ONLINE"; title.fontStyle = FontStyle.Bold; title.color = UITheme.Gold;
            var note = Label("OnlineNote", op, 28, TextAnchor.MiddleCenter, new Vector2(0, 250), new Vector2(1000, 44));
            note.text = "Play a friend or a stranger anywhere in the world."; note.color = UITheme.Muted;
            Button(op, "BACK", new Vector2(-650, 312), new Vector2(200, 64), CloseOnline, btnCol).GetComponentInChildren<Text>().fontSize = 28;
            var divider = UIKit.Image(op, "Divider", UITheme.Rounded, new Color(1f, 1f, 1f, 0.14f), false, false);
            divider.rectTransform.anchorMin = divider.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            divider.rectTransform.anchoredPosition = new Vector2(0f, -30f); divider.rectTransform.sizeDelta = new Vector2(3f, 400f);

            // left: over the internet. Quick match with anyone, or a room code shared with a friend
            var netLabel = Label("NetLabel", op, 32, TextAnchor.MiddleCenter, new Vector2(-400, 190), new Vector2(700, 44)); netLabel.text = "Over the internet"; netLabel.color = UITheme.Gold;
            Button(op, "QUICK MATCH", new Vector2(-400, 110), new Vector2(560, 100), QuickMatchOnline, Accent);
            Button(op, "CREATE ROOM", new Vector2(-400, 0), new Vector2(560, 88), CreateRoomOnline, btnCol);
            var codeLabel = Label("CodeLabel", op, 26, TextAnchor.MiddleCenter, new Vector2(-400, -76), new Vector2(700, 36)); codeLabel.text = "Have a room code?";
            codeLabel.color = new Color(1, 1, 1, 0.75f);
            roomCodeField = BuildAddressField(op, new Vector2(-545, -142), new Vector2(290, 80), "ROOM CODE", 8, TouchScreenKeyboardType.ASCIICapable, false);
            Button(op, "JOIN ROOM", new Vector2(-255, -142), new Vector2(270, 80), () => JoinRoomOnline(roomCodeField.text), btnCol);
            // a created room: the code shows here until the host starts the match
            onlineInfo = Label("OnlineInfo", op, 40, TextAnchor.MiddleCenter, new Vector2(-400, -225), new Vector2(700, 56));
            onlineInfo.color = UITheme.Gold; onlineInfo.fontStyle = FontStyle.Bold;
            roomStartButton = Button(op, "START MATCH", new Vector2(-400, -300), new Vector2(420, 66), StartRoomMatch, Accent).gameObject;
            roomStartButton.SetActive(false);

            // right: same Wi-Fi, no internet needed
            var lanLabel = Label("LanLabel", op, 32, TextAnchor.MiddleCenter, new Vector2(400, 190), new Vector2(700, 44)); lanLabel.text = "Same Wi-Fi";
            lanLabel.color = UITheme.Gold;
            var lanNote = Label("LanNote", op, 24, TextAnchor.MiddleCenter, new Vector2(400, 150), new Vector2(700, 34)); lanNote.text = "No internet needed";
            lanNote.color = new Color(1, 1, 1, 0.65f);
            Button(op, "HOST", new Vector2(400, 80), new Vector2(480, 84), HostOnline, btnCol);
            var joinHint = Label("JoinHint", op, 26, TextAnchor.MiddleCenter, new Vector2(400, 0), new Vector2(700, 36)); joinHint.text = "Or join: type the address your friend sees";
            joinHint.color = new Color(1, 1, 1, 0.75f);
            addressField = BuildAddressField(op, new Vector2(400, -62), new Vector2(480, 80));
            Button(op, "JOIN", new Vector2(400, -160), new Vector2(480, 84), () => JoinOnline(addressField.text), btnCol);

            onlineStatus = Label("OnlineStatus", op, 28, TextAnchor.MiddleCenter, new Vector2(400, -250), new Vector2(700, 90));
            onlineStatus.horizontalOverflow = HorizontalWrapMode.Wrap;
            onlineStatus.color = new Color(1f, 0.9f, 0.4f);

            // a one-line status in the corner while hosting or playing as a guest
            onlineBanner = Label("OnlineBanner", root, 26, TextAnchor.MiddleLeft, new Vector2(20, -30), new Vector2(900, 40));
            var brt = onlineBanner.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f); brt.pivot = new Vector2(0f, 1f); brt.anchoredPosition = new Vector2(20, -20);
            onlineBanner.color = new Color(1f, 0.9f, 0.4f);
            onlineBanner.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2, -2);
            onlinePanel.SetActive(false);

            // a small ping pill under the banner while playing as a guest
            var pill = UIKit.Image(root, "PingPill", UITheme.Rounded, new Color(0f, 0.02f, 0.08f, 0.75f), true, false);
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = pill.rectTransform.pivot = UIKit.TopLeft;
            pill.rectTransform.anchoredPosition = new Vector2(20f, -66f); pill.rectTransform.sizeDelta = new Vector2(250f, 54f);
            pingDot = UIKit.Image(pill.transform, "Dot", UITheme.Circle, UITheme.Green, false, false);
            pingDot.rectTransform.anchorMin = pingDot.rectTransform.anchorMax = new Vector2(0f, 0.5f); pingDot.rectTransform.pivot = new Vector2(0f, 0.5f);
            pingDot.rectTransform.anchoredPosition = new Vector2(16f, 0f); pingDot.rectTransform.sizeDelta = new Vector2(22f, 22f);
            pingText = UIKit.Text(pill.transform, "Text", "Ping ...", 28f, Color.white, TextAlignmentOptions.Center, true);
            pingText.rectTransform.offsetMin = new Vector2(48f, 0f); pingText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            pingPill = pill.gameObject; pingPill.SetActive(false);
        }

        /// <summary>Green below 60 ms, gold below 120, red above; the words say the same for anyone who cannot tell the colours apart.</summary>
        public static string PingQuality(float rttMs) { return rttMs < 60f ? "good" : rttMs < 120f ? "fair" : "poor"; }
        public string OnlinePingText => pingText != null ? pingText.text : "";
        public bool OnlinePingShown => pingPill != null && pingPill.activeSelf;

        InputField roomCodeField;
        GameObject roomStartButton;

        InputField BuildAddressField(Transform parent, Vector2 pos, Vector2 size, string hint = "192.168.1.20", int limit = 21, TouchScreenKeyboardType keyboard = TouchScreenKeyboardType.NumbersAndPunctuation, bool rememberLast = true)
        {
            var go = new GameObject("AddressField", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>(); img.sprite = UITheme.Rounded; img.type = Image.Type.Sliced; img.color = new Color(0.14f, 0.19f, 0.30f);
            var inner = size - new Vector2(30, 10);
            var text = Label("Text", go.transform, 38, TextAnchor.MiddleLeft, Vector2.zero, inner); text.supportRichText = false;
            var placeholder = Label("Placeholder", go.transform, 38, TextAnchor.MiddleLeft, Vector2.zero, inner);
            placeholder.text = hint; placeholder.fontStyle = FontStyle.Italic; placeholder.color = new Color(1, 1, 1, 0.35f);
            var field = go.GetComponent<InputField>();
            field.targetGraphic = img; field.textComponent = text; field.placeholder = placeholder;
            field.lineType = InputField.LineType.SingleLine;
            field.characterLimit = limit; // 255.255.255.255:65535 for an address
            field.keyboardType = keyboard;
            if (!rememberLast) { field.contentType = InputField.ContentType.Alphanumeric; return field; }
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
            if (!OnlineHosting && pendingRoom == null) { onlineInfo.text = ""; roomStartButton.SetActive(false); }
            onlinePanel.SetActive(true);
        }

        void CloseOnline()
        {
            onlineRequest++; // an answer still on its way from the online service is ignored
            if (pendingRoom != null && !OnlineHosting) DiscardPendingRoom();
            onlinePanel.SetActive(false);
        }

        // ---- internet play: rooms and quick match (see OnlineService) ----
        OnlineService.HostedRoom pendingRoom;
        GameObject roomKeeper;
        string roomCode = "";
        bool guestIsRelay;
        int onlineRequest;

        /// <summary>The code of the internet room being hosted (empty when not hosting one).</summary>
        public string OnlineRoomCode => roomCode;

        void DiscardPendingRoom()
        {
            pendingRoom?.Transport?.Dispose();
            pendingRoom = null; roomStartButton.SetActive(false); onlineInfo.text = "";
        }

        /// <summary>True and the away player a friend can take over when this match can be hosted; otherwise says why in the status line.</summary>
        bool TryFindGuestSlot(out int slot)
        {
            slot = -1;
            if (OnlineHosting) { onlineStatus.text = "Already hosting. Press BACK to return to the match."; return false; }
            if (homeTeam == null || awayTeam == null || match == null) { onlineStatus.text = "No match to host."; return false; }
            if (TrainingMode.Active || MiniLeague.Active || Tournament.Active || Season.Active)
            {
                onlineStatus.text = "Online needs Single match mode (turn off league, cup, season, training).";
                return false;
            }
            for (int i = awayTeam.players.Count - 1; i >= 0; i--) if (!awayTeam.players[i].isGoalkeeper) { slot = homeTeam.players.Count + i; break; }
            if (slot < 0) { onlineStatus.text = "No free player for a guest."; return false; }
            return true;
        }

        /// <summary>Opens a private internet room and shows its code; the match starts when the host presses START.</summary>
        public async void CreateRoomOnline()
        {
            if (OnlineGuestActive || !TryFindGuestSlot(out _)) return;
            if (pendingRoom != null) DiscardPendingRoom();
            int request = ++onlineRequest;
            onlineStatus.text = "Opening a room...";
            try
            {
                var room = await OnlineService.CreateRoom(false);
                if (request != onlineRequest) { room.Transport.Dispose(); return; }
                pendingRoom = room;
                onlineInfo.text = $"ROOM CODE   {room.Code}";
                roomStartButton.SetActive(true);
                onlineStatus.text = "Tell your friend this code. Press START when you are ready.";
            }
            catch (System.Exception e) { if (request == onlineRequest) onlineStatus.text = OnlineService.Describe(e); }
        }

        /// <summary>Starts the match for the room that was just opened.</summary>
        public void StartRoomMatch()
        {
            if (pendingRoom == null || OnlineHosting || !TryFindGuestSlot(out int slot)) return;
            var room = pendingRoom; pendingRoom = null;
            var players = new List<PlayerController>(homeTeam.players);
            players.AddRange(awayTeam.players);
            var ball = FindFirstObjectByType<FootballSim.Ball.BallController>();
            BeginMatch();
            try { hosting = NetSession.HostOn(room.Transport, match, ball, players, slot); }
            catch (System.Exception e) { Debug.LogWarning("Could not host: " + e.Message); room.Transport.Dispose(); hosting = null; onlineStatus.text = "Could not start the room."; Time.timeScale = 0f; startPanel.SetActive(true); return; }
            roomCode = room.Code;
            if (room.LobbyId != null) roomKeeper = OnlineService.KeepListed(room);
            roomStartButton.SetActive(false);
            onlinePanel.SetActive(false);
        }

        /// <summary>Joins the internet room with the typed code.</summary>
        public async void JoinRoomOnline(string code)
        {
            if (OnlineGuestActive) return;
            string clean = OnlineService.NormaliseCode(code);
            if (clean.Length == 0) { onlineStatus.text = "Type the room code first."; return; }
            int request = ++onlineRequest;
            onlineStatus.text = "Joining the room...";
            try
            {
                var transport = await OnlineService.JoinRoom(clean);
                if (request != onlineRequest) { transport.Dispose(); return; }
                EnterRelayGuest(transport, "room " + clean);
            }
            catch (System.Exception e) { if (request == onlineRequest) onlineStatus.text = OnlineService.Describe(e); }
        }

        /// <summary>Finds an open public room and joins it, or opens one and plays the AI until someone arrives.</summary>
        public async void QuickMatchOnline()
        {
            if (OnlineGuestActive || !TryFindGuestSlot(out _)) return;
            int request = ++onlineRequest;
            onlineStatus.text = "Looking for a player...";
            try
            {
                var result = await OnlineService.QuickMatch();
                if (request != onlineRequest) { result.Guest?.Dispose(); result.Hosted?.Transport.Dispose(); return; }
                if (result.Guest != null) { EnterRelayGuest(result.Guest, "a player"); return; }
                pendingRoom = result.Hosted;
                StartRoomMatch(); // nobody waiting: the match starts at once and a stranger can join it
            }
            catch (System.Exception e) { if (request == onlineRequest) onlineStatus.text = OnlineService.Describe(e); }
        }

        void EnterRelayGuest(RelayTransport transport, string what)
        {
            FootballSim.Inputs.IPlayerInput source = homeTeam != null && homeTeam.humanInput != null ? (FootballSim.Inputs.IPlayerInput)homeTeam.humanInput : Touch();
            guest = GuestMode.Enter(RelayTransport.HostAddress, source, true, transport);
            guestIsRelay = true;
            guestStartedAt = Time.unscaledTime; guestAddress = what;
            HideAllMenus();
            Time.timeScale = 1f;
        }

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
            guestIsRelay = false;
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
                else if (Time.unscaledTime - guestStartedAt > (guestIsRelay ? 15f : 8f))
                    onlineBanner.text = guestIsRelay ? "No answer from the room. Is the host still there?  -  Back to leave" : $"No answer from {guestAddress}. Same Wi-Fi? Host pressed HOST?  -  Back to leave";
                else onlineBanner.text = $"ONLINE: connecting to {guestAddress}...";
                bool receiving = client.Received > 0;
                if (client.Joined && receiving && client.SecondsSinceSnapshot > 2.5f)
                    onlineBanner.text = $"No data from the host for {client.SecondsSinceSnapshot:0} s  -  Back to leave";
                if (client.HasMatchInfo)
                {
                    int h = client.LatestHomeScore, a = client.LatestAwayScore, mins = Mathf.FloorToInt(client.LatestGameMinute);
                    scoreText.text = $"HOME  {h} - {a}  AWAY";
                    clockText.text = $"{mins:00}:00";
                    if (lastGuestHome >= 0 && (h != lastGuestHome || a != lastGuestAway)) Banner("GOAL!", 3f);
                    lastGuestHome = h; lastGuestAway = a;
                }
                else { scoreText.text = "ONLINE GUEST"; clockText.text = ""; }
                pingPill.SetActive(client.Joined);
                bool stalled = client.Joined && receiving && client.SecondsSinceSnapshot > 2.5f;
                if (stalled) { pingText.text = "No signal"; pingDot.color = UITheme.Red; }
                else if (client.HasRtt)
                {
                    float rtt = client.RttMs;
                    pingText.text = $"Ping {rtt:0} ms";
                    pingDot.color = rtt < 60f ? UITheme.Green : rtt < 120f ? UITheme.Gold : UITheme.Red;
                }
                else { pingText.text = "Ping ..."; pingDot.color = UITheme.Slate; }
            }
            else if (OnlineHosting)
            {
                lastGuestHome = lastGuestAway = -1; pingPill.SetActive(false);
                int n = hosting.server.ClientCount;
                if (roomCode.Length > 0) onlineBanner.text = n == 0 ? $"ROOM {roomCode}  -  waiting for a player" : $"ROOM {roomCode}  -  player connected";
                else onlineBanner.text = n == 0 ? $"HOSTING at {AddressOfHost()}  -  waiting for a friend" : $"HOSTING at {AddressOfHost()}  -  friend connected";
            }
            else
            {
                if (roomCode.Length > 0) { roomCode = ""; if (roomKeeper != null) Destroy(roomKeeper); roomKeeper = null; }
                lastGuestHome = lastGuestAway = -1; pingPill.SetActive(false);
                if (onlineBanner.text.Length > 0) onlineBanner.text = "";
            }
        }
    }
}
