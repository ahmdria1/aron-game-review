// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Threading.Tasks;
using Aron.Logic.Safety;

namespace Aron.Logic.Names
{
    /// <summary>The game's door to player names. The real one is the Cloud Code module; offline the game uses <see cref="PlayerNameService"/> with no remote.</summary>
    public interface IPlayerNameService
    {
        Task<NameResult> CheckAsync(string name);
        Task<NameResult> ReserveAsync(string name, bool allowTag);
        Task<NameResult> ChangeAsync(string name, bool allowTag);
        Task<NameResult> ReportAsync(string targetPlayerId, string reason);
    }

    /// <summary>Calls a <see cref="NameRegistry"/> directly for one player. This is the in-process "server" used by tests and by an offline demo.</summary>
    public class RegistryNameService : IPlayerNameService
    {
        private readonly NameRegistry registry;
        private readonly string playerId;
        public RegistryNameService(NameRegistry registry, string playerId) { this.registry = registry; this.playerId = playerId; }
        public Task<NameResult> CheckAsync(string name) => Task.FromResult(registry.Check(playerId, name));
        public Task<NameResult> ReserveAsync(string name, bool allowTag) => Task.FromResult(registry.Reserve(playerId, name, allowTag));
        public Task<NameResult> ChangeAsync(string name, bool allowTag) => Task.FromResult(registry.Change(playerId, name, allowTag));
        public Task<NameResult> ReportAsync(string target, string reason) => Task.FromResult(registry.Report(playerId, target, reason));
    }

    /// <summary>
    /// The client side. Checks the shape and the word lists on the device first (fast, no network), then asks the server.
    /// With no server (offline, or Unity Gaming Services not set up yet) the name is kept on the device and marked "not confirmed";
    /// online races need a confirmed name.
    /// </summary>
    public class PlayerNameService : IPlayerNameService
    {
        public const int MaxTries = 1;
        private readonly NameChecker checker;
        private readonly IPlayerNameService remote;

        public PlayerNameService(NameChecker checker, IPlayerNameService remote = null) { this.checker = checker; this.remote = remote; }

        public bool HasRemote => remote != null;

        private NameResult Local(string name, out bool bad)
        {
            var v = checker.Check(name);
            bad = !v.Ok;
            if (v.Ok) return new NameResult { Status = NameStatus.Ok, Name = v.Clean, Available = true };
            var st = v.Code == NameCode.Blocked ? NameStatus.Blocked : v.Code == NameCode.Reserved ? NameStatus.Reserved : NameStatus.Invalid;
            return new NameResult { Status = st, Code = v.Code, Name = v.Clean };
        }

        private async Task<NameResult> Ask(Func<Task<NameResult>> call, NameResult local)
        {
            if (remote == null) return Offline(local);
            try { return await call(); }
            catch (Exception) { return Offline(local); }
        }

        private static NameResult Offline(NameResult local) =>
            new NameResult { Status = NameStatus.Offline, Name = local.Name, Available = true };

        public async Task<NameResult> CheckAsync(string name)
        {
            var l = Local(name, out bool bad);
            if (bad) return l;
            return await Ask(() => remote.CheckAsync(name), l);
        }

        public async Task<NameResult> ReserveAsync(string name, bool allowTag)
        {
            var l = Local(name, out bool bad);
            if (bad) return l;
            return await Ask(() => remote.ReserveAsync(name, allowTag), l);
        }

        public async Task<NameResult> ChangeAsync(string name, bool allowTag)
        {
            var l = Local(name, out bool bad);
            if (bad) return l;
            return await Ask(() => remote.ChangeAsync(name, allowTag), l);
        }

        public async Task<NameResult> ReportAsync(string targetPlayerId, string reason)
        {
            if (!ReportReasons.IsValid(reason)) return new NameResult { Status = NameStatus.Error, Code = NameCode.Error };
            if (remote == null) return new NameResult { Status = NameStatus.Offline };
            try { return await remote.ReportAsync(targetPlayerId, reason); }
            catch (Exception) { return new NameResult { Status = NameStatus.Offline }; }
        }
    }

    /// <summary>How the name screen behaves for each age band. Children (and anyone whose age is not known) pick from safe names; teens and adults type.</summary>
    public static class NameFlow
    {
        public enum Mode { Pick, Type }
        public static Mode ModeFor(string ageBand) => AgeGate.CanTypeName(ageBand) ? Mode.Type : Mode.Pick;
        /// <summary>Online races need a name the server has confirmed, and no pending forced rename.</summary>
        public static bool CanRaceOnline(bool nameReserved, bool mustChange) => nameReserved && !mustChange;
        public static bool CooldownPassed(long changedTicks, long nowTicks) =>
            changedTicks <= 0 || nowTicks - changedTicks >= TimeSpan.TicksPerHour * NameRegistry.ChangeCooldownHours;
    }
}
