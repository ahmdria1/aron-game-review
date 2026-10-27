// (c) Ahmed Alamrani. All rights reserved.
using System.IO;
using System.Linq;
using System.Text.Json;
using Aron.Logic.Regions;
using NUnit.Framework;

namespace Aron.Logic.Tests
{
    /// <summary>Shared loaders for the shipped Resources.</summary>
    public static class TestData
    {
        public static readonly JsonSerializerOptions Opt = new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true };

        public static string Resources()
        {
            for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
            {
                var p = Path.Combine(d.FullName, "Assets", "_Aron", "Resources");
                if (Directory.Exists(Path.Combine(p, "Regions"))) return p;
            }
            Assert.Fail("Resources not found"); return null;
        }

        public static T Read<T>(params string[] parts) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(new[] { Resources() }.Concat(parts).ToArray())), Opt);

        public static RegionCatalog Catalog() => RegionCatalog.FromRegions(
            Directory.GetFiles(Path.Combine(Resources(), "Regions"), "*.json").Where(f => Path.GetFileName(f) != "RegionCatalog.json")
                .Select(f => JsonSerializer.Deserialize<RegionDef>(File.ReadAllText(f), Opt)));
    }
}
