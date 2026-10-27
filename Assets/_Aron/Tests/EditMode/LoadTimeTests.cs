// (c) Ahmed Alamrani. All rights reserved.
using System.Linq;
using Aron.Logic;
using Aron.Logic.Culture;
using Aron.Logic.Regions;
using Aron.Save;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Aron.Tests.EditMode
{
    /// <summary>Load time of the data every launch reads: all region and stage files, the store catalog, the culture files. Samples go to the Performance Testing report.</summary>
    public class LoadTimeTests
    {
        // Generous ceilings: they catch a data file that suddenly takes seconds, not tune the machine.
        private const double MaxRegionsMs = 1500, MaxCatalogMs = 500, MaxCultureMs = 1500;

        [Test, Performance]
        public void AllRegionAndStageFilesLoadFast()
        {
            int regions = 0, stages = 0;
            Measure.Method(() =>
            {
                var defs = Resources.LoadAll<TextAsset>("Regions").Select(f => JsonUtility.FromJson<RegionDef>(f.text)).ToList();
                var cat = RegionCatalog.FromRegions(defs);
                regions = defs.Count; stages = defs.Sum(d => d.stages.Length);
                Assert.IsEmpty(cat.Validate());
            }).WarmupCount(1).MeasurementCount(8).IterationsPerMeasurement(1).SampleGroup("Regions+stages load").Run();
            Assert.Greater(regions, 5, "region files are missing from Resources/Regions");
            Assert.Greater(stages, regions, "regions have no stages");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var f in Resources.LoadAll<TextAsset>("Regions")) JsonUtility.FromJson<RegionDef>(f.text);
            Assert.Less(sw.Elapsed.TotalMilliseconds, MaxRegionsMs);
            TestContext.Out.WriteLine($"regions={regions} stages={stages} load={sw.Elapsed.TotalMilliseconds:0.0} ms");
        }

        [Test, Performance]
        public void StoreCatalogLoadsFast()
        {
            Measure.Method(() => Aron.Store.StoreService.LoadCatalog()).WarmupCount(1).MeasurementCount(10).SampleGroup("Store catalog load").Run();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var c = Aron.Store.StoreService.LoadCatalog();
            Assert.Less(sw.Elapsed.TotalMilliseconds, MaxCatalogMs);
            Assert.Greater(c.items.Length, 20);
        }

        [Test, Performance]
        public void CultureFilesLoadFast()
        {
            int files = 0;
            Measure.Method(() => { files = Resources.LoadAll<TextAsset>("Culture").Length; }).WarmupCount(1).MeasurementCount(8).SampleGroup("Culture files load").Run();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var f in Resources.LoadAll<TextAsset>("Culture")) Assert.IsNotNull(JsonUtility.FromJson<CultureFile>(f.text), f.name);
            Assert.Less(sw.Elapsed.TotalMilliseconds, MaxCultureMs);
            Assert.Greater(files, 5);
        }
    }
}
