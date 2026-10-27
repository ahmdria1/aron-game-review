// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Aron.Logic.Names;
using Microsoft.Extensions.DependencyInjection;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

// UNVERIFIED: written against the Cloud Code module and Cloud Save documentation, never compiled or run (no Unity Gaming Services project yet).
// The rules, word lists, registry and store logic are in Assets/_Aron/Scripts/Logic/Names and are covered by the test suite;
// this file only connects them to Cloud Code and Cloud Save.

namespace Aron.Cloud
{
    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.Dependencies.AddSingleton(GameApiClient.Create());
        }
    }

    /// <summary>Cloud Save custom data as an <see cref="IVersionedKv"/>: the write lock is the token, a 409 is a conflict.</summary>
    public class CloudSaveKv : IVersionedKv
    {
        private const string StoreId = "ARON_NAMES";
        private readonly IExecutionContext ctx;
        private readonly IGameApiClient api;
        public CloudSaveKv(IExecutionContext ctx, IGameApiClient api) { this.ctx = ctx; this.api = api; }

        public bool Get(string key, out string value, out string lockToken)
        {
            var r = api.CloudSaveData.GetCustomItemsAsync(ctx, ctx.ServiceToken, ctx.ProjectId, StoreId, new List<string> { key }).GetAwaiter().GetResult();
            var item = r.Data.Results.Count > 0 ? r.Data.Results[0] : null;
            if (item == null) { value = null; lockToken = null; return false; }
            value = item.Value as string ?? item.Value?.ToString();
            lockToken = item.WriteLock;
            return true;
        }

        public bool Set(string key, string value, string lockToken)
        {
            try
            {
                api.CloudSaveData.SetCustomItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, StoreId, new SetItemBody(key, value, lockToken)).GetAwaiter().GetResult();
                return true;
            }
            catch (ApiException e) when (e.Response.StatusCode == HttpStatusCode.Conflict) { return false; }
        }
    }

    public class AronNamesModule
    {
        private static NameChecker checker;

        private static NameChecker Checker()
        {
            if (checker != null) return checker;
            var dir = Path.Combine(AppContext.BaseDirectory, "Names");
            var opt = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };
            var lists = new List<BlocklistFile>();
            foreach (var f in Directory.GetFiles(dir, "blocklist_*.json")) lists.Add(JsonSerializer.Deserialize<BlocklistFile>(File.ReadAllText(f), opt));
            var reserved = JsonSerializer.Deserialize<ReservedFile>(File.ReadAllText(Path.Combine(dir, "reserved.json")), opt);
            return checker = new NameChecker(lists, reserved);
        }

        private static NameRegistry Registry(IExecutionContext ctx, IGameApiClient api) =>
            new NameRegistry(Checker(), new CloudNameStore(new CloudSaveKv(ctx, api)), () => DateTime.UtcNow.Ticks, () => Guid.NewGuid().GetHashCode());

        // The player is always the caller (ctx.PlayerId), never a value sent by the client.

        [CloudCodeFunction("CheckName")]
        public NameResult CheckName(IExecutionContext ctx, IGameApiClient api, string name) => Registry(ctx, api).Check(ctx.PlayerId, name);

        [CloudCodeFunction("ReserveName")]
        public NameResult ReserveName(IExecutionContext ctx, IGameApiClient api, string name, bool allowTag) => Registry(ctx, api).Reserve(ctx.PlayerId, name, allowTag);

        [CloudCodeFunction("ChangeName")]
        public NameResult ChangeName(IExecutionContext ctx, IGameApiClient api, string name, bool allowTag) => Registry(ctx, api).Change(ctx.PlayerId, name, allowTag);

        [CloudCodeFunction("ReportName")]
        public NameResult ReportName(IExecutionContext ctx, IGameApiClient api, string targetPlayerId, string reason) => Registry(ctx, api).Report(ctx.PlayerId, targetPlayerId, reason);

        [CloudCodeFunction("GetDisplayName")]
        public string GetDisplayName(IExecutionContext ctx, IGameApiClient api, string playerId) => Registry(ctx, api).DisplayName(playerId);

        /// <summary>Run once by Ahmed (Cloud Code console or dashboard) before the first player registers. Safe to run again.</summary>
        [CloudCodeFunction("InitNameShards")]
        public int InitNameShards(IExecutionContext ctx, IGameApiClient api) => CloudNameStore.InitShards(new CloudSaveKv(ctx, api));
    }
}
