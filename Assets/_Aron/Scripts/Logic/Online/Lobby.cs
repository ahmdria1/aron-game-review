// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.Text;

namespace Aron.Logic.Online
{
    public class LobbyPlayer
    {
        public string id;
        public string name;
        /// <summary>aron or noura, with their race traits.</summary>
        public string hero = "aron";
        /// <summary>The costume others see (cosmetic only).</summary>
        public string costume = "";
        /// <summary>A computer racer. Always shown with the "computer" label, and never counted as a real racer for online points.</summary>
        public bool ai;
        public bool host;
        /// <summary>For computer racers: the rival character (see Rivals.json) and its mount.</summary>
        public string rivalId = "", mount = "";
    }

    public class LobbyInfo
    {
        public string id;
        /// <summary>Six-letter code for private rooms; empty for quick match.</summary>
        public string code = "";
        public string region = "";
        public string track = "";
        public bool isPrivate;
        public readonly List<LobbyPlayer> players = new List<LobbyPlayer>();
    }

    /// <summary>
    /// What the game needs from Unity Gaming Services (Authentication, Lobby, Relay). The real one wraps the
    /// UGS SDK; tests use a fake. Calls finish straight away or through the callbacks, never by blocking.
    /// </summary>
    public interface IMatchServices
    {
        bool SignedIn { get; }
        string PlayerId { get; }
        void SignInAnonymously(Action<bool> done);
        void QuickJoin(string region, string track, Action<LobbyInfo> done);
        void CreateLobby(string region, string track, bool isPrivate, int maxPlayers, Action<LobbyInfo> done);
        void JoinByCode(string code, Action<LobbyInfo> done);
        void Kick(LobbyInfo lobby, string playerId);
        void Report(string playerId, string reason);
        void Leave(LobbyInfo lobby);
        void StartRelay(LobbyInfo lobby, Action<bool> done);
    }

    public enum LobbyState { Idle, SigningIn, Searching, Waiting, Starting, Racing, Failed }

    /// <summary>
    /// Quick match or private room, up to eight racers. When the room isn't full after the wait, computer rivals
    /// take the empty seats so a match always starts. Only the host of a private room can kick.
    /// </summary>
    public class LobbyFlow
    {
        public const int MaxPlayers = 8;
        public const float FillAfterSeconds = 15f;

        private readonly IMatchServices svc;
        public LobbyState State { get; private set; } = LobbyState.Idle;
        public LobbyInfo Lobby { get; private set; }
        public string Error { get; private set; } = "";
        private float waited;

        public LobbyFlow(IMatchServices services) { svc = services; }

        private void SignIn(Action then)
        {
            if (svc.SignedIn) { then(); return; }
            State = LobbyState.SigningIn;
            svc.SignInAnonymously(ok => { if (ok) then(); else Fail("sign_in"); });
        }

        private void Fail(string why) { State = LobbyState.Failed; Error = why; }

        private void Joined(LobbyInfo l)
        {
            if (l == null) { Fail("no_lobby"); return; }
            Lobby = l; waited = 0f; State = LobbyState.Waiting;
        }

        public void QuickMatch(string region, string track)
        {
            SignIn(() =>
            {
                State = LobbyState.Searching;
                svc.QuickJoin(region, track, l =>
                {
                    if (l != null) Joined(l);
                    else svc.CreateLobby(region, track, false, MaxPlayers, Joined);
                });
            });
        }

        public void CreatePrivate(string region, string track)
        {
            SignIn(() => { State = LobbyState.Searching; svc.CreateLobby(region, track, true, MaxPlayers, Joined); });
        }

        public void JoinPrivate(string code)
        {
            if (!RoomCode.Valid(code)) { Fail("bad_code"); return; }
            SignIn(() => { State = LobbyState.Searching; svc.JoinByCode(RoomCode.Normalize(code), Joined); });
        }

        public bool IsHost => Lobby != null && Lobby.players.Exists(p => p.host && p.id == svc.PlayerId);

        /// <summary>Kicking is for private rooms, by their host, and never yourself.</summary>
        public bool Kick(string playerId)
        {
            if (Lobby == null || !Lobby.isPrivate || !IsHost || playerId == svc.PlayerId) return false;
            if (Lobby.players.RemoveAll(p => p.id == playerId && !p.ai) == 0) return false;
            svc.Kick(Lobby, playerId);
            return true;
        }

        public void Report(string playerId, string reason) => svc.Report(playerId, reason);

        /// <summary>Call every frame while waiting. After the wait, computer racers fill the room and the race starts.</summary>
        public void Tick(float dt)
        {
            if (State != LobbyState.Waiting || Lobby == null) return;
            waited += dt;
            bool full = Lobby.players.Count >= MaxPlayers;
            if (!full && waited < FillAfterSeconds) return;
            AiFill.Fill(Lobby, MaxPlayers);
            State = LobbyState.Starting;
            svc.StartRelay(Lobby, ok => State = ok ? LobbyState.Racing : LobbyState.Failed);
        }

        public void Leave()
        {
            if (Lobby != null) svc.Leave(Lobby);
            Lobby = null; State = LobbyState.Idle;
        }
    }

    public static class AiFill
    {
        /// <summary>Real racers in the room: computer racers never count (online points need at least two).</summary>
        public static int RealRacers(LobbyInfo lobby)
        {
            int n = 0;
            foreach (var p in lobby.players) if (!p.ai) n++;
            return n;
        }

        /// <summary>
        /// Fills the empty seats with the same rival characters used offline (the host country's rival first). They are marked
        /// <see cref="LobbyPlayer.ai"/> so every screen shows the computer label.
        /// </summary>
        public static int Fill(LobbyInfo lobby, int seats, Races.RivalFile rivals, Races.AiRacerFile cfg, string difficulty, string hostCountry, Random rnd)
        {
            int missing = seats - lobby.players.Count;
            if (missing <= 0) return 0;
            var field = Races.AiRoster.Build(rivals, cfg, Math.Min(missing, rivals.rivals.Length), difficulty, hostCountry, rnd);
            int added = 0;
            foreach (var e in field)
            {
                if (lobby.players.Count >= seats) break;
                int i = lobby.players.Count;
                var r = rivals.Find(e.rivalId);
                lobby.players.Add(new LobbyPlayer { id = "ai_" + e.rivalId, name = e.name, hero = r != null && r.female ? "noura" : "aron", ai = true, rivalId = e.rivalId, mount = e.mount });
                added++;
            }
            return added;
        }

        /// <summary>Adds computer racers (alternating Aron and Noura looks) until the room has <paramref name="seats"/>. Returns how many.</summary>
        public static int Fill(LobbyInfo lobby, int seats)
        {
            int added = 0;
            int n = lobby.players.Count;
            while (lobby.players.Count < seats)
            {
                int i = lobby.players.Count;
                lobby.players.Add(new LobbyPlayer { id = "ai_" + i, name = "Racer " + (i - n + 1), hero = i % 2 == 0 ? "aron" : "noura", ai = true });
                added++;
            }
            return added;
        }
    }

    /// <summary>Six characters, no look-alike letters (no 0/O, 1/I/L).</summary>
    public static class RoomCode
    {
        public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        public const int Length = 6;

        public static string Make(Random rnd)
        {
            var sb = new StringBuilder(Length);
            for (int i = 0; i < Length; i++) sb.Append(Alphabet[rnd.Next(Alphabet.Length)]);
            return sb.ToString();
        }

        public static string Normalize(string code) => (code ?? "").Trim().ToUpperInvariant();

        public static bool Valid(string code)
        {
            string c = Normalize(code);
            if (c.Length != Length) return false;
            foreach (char ch in c) if (Alphabet.IndexOf(ch) < 0) return false;
            return true;
        }
    }

    /// <summary>
    /// Display names for online play. No free chat in the game, so the name is the only text other players see:
    /// The rules live in Logic/Names (docs/player_names.md); this class is the short door the lobby uses.
    /// </summary>
    public static class NameFilter
    {
        public const int Min = Names.PlayerNameRules.Min, Max = Names.PlayerNameRules.Max;

        /// <summary>The name checker the game built from Resources/Names (set once at start-up). Null in tests that only need the shape rules.</summary>
        public static Names.NameChecker Checker;

        /// <summary>Same rules as the name screen and the server: shape first, then the word lists of all 12 languages and the reserved names.</summary>
        public static bool Allowed(string name)
        {
            if (Checker != null) return Checker.Check(name).Ok;
            return Names.PlayerNameRules.CheckShape(name, out _) == null;
        }

        /// <summary>A safe default name when the player hasn't picked one or picked one the filter refused.</summary>
        public static string Default(string playerId)
        {
            return "Racer" + (Names.SafeNames.StableHash(playerId) % 10000).ToString("0000");
        }
    }

    /// <summary>Preset emotes and quick phrases: the only way players talk to each other (no text chat).</summary>
    public static class QuickPhrases
    {
        public static readonly string[] Keys =
        {
            "qp_hello", "qp_good_race", "qp_yalla", "qp_mashallah", "qp_again", "qp_thanks", "qp_wow", "qp_bye",
        };
        public static readonly string[] Emotes = { "wave", "thumbs_up", "clap", "laugh", "bisht_spin" };
    }
}
