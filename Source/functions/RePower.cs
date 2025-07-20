using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RW_Repower.functions
{
    [StaticConstructorOnStartup]
    public static class Main
    {
        static Main()
        {
            Log.Message($"{General.ModName()} :: Initializing...");
            try
            {
                RePower.DefsLoaded();
            }
            catch (Exception e)
            {
                Log.Error(string.Format("Error while loading RePower: {0}", e.Message));
                Log.Error(e.StackTrace);
            }
        }
    }

    // Add a constructor to RePower that takes a ModContentPack parameter and passes it to the base Mod class.
    public class RePower : Mod
    {

        public RePower(ModContentPack content) : base(content)
        {
        }
        private static int ticksToRescan = 0; // Tick tracker for rescanning

        public static void Tick()
        {
            int currentTick = Find.TickManager.TicksGame;
            // Rescan Logic
            if (ticksToRescan == 20000)
            {
                Tracker.ScanForThings();
                ticksToRescan = 0;
            }
            else
                ticksToRescan++;

            // 1. Update buildings in use (Tracker.EvalAll should call AddBuildingUsed for active buildings)
            Tracker.EvalAll();

            // 2. Set power to high for buildings in use this tick
            foreach (var kvp in Tracker.BuildingsInUse.ToList())
            {
                Building building = kvp.Key;
                bool isActive = false;
                int lastTick = kvp.Value != 0 ? kvp.Value : 0;

                if (building == null) continue;

                if (lastTick == currentTick && Tracker.BuildingsToModify.Contains(building))
                {
                    Tracker.AddBuildingUsed(building);
                    continue;
                }

                Tracker.BuildingsToModify.Add(building);
                Tracker.BuildingsInUse.Remove(building);
                SetPower(building, isActive);
            }
        }

        public static void SetPower(Building building, bool IsActive)
        {
            if (building == null || !building.Spawned || !building.def.IsBuildingArtificial)
                return;

            var powerComp = building.TryGetComp<CompPowerTrader>();
            if (powerComp == null || powerComp.Props == null)
                return;

            powerComp.PowerOutput = GetPowerSetting(building, IsActive);
        }

        // public static Dictionary<string, float> PowerLevels { get; private set; } = new Dictionary<string, float>();
        public static List<ThingDef> Things = new List<ThingDef>();
        public static void DefsLoaded()
        {
            Things.Clear();
            var defs = DefDatabase<RePowerDef>.AllDefs;
            Things = DefDatabase<ThingDef>.AllDefsListForReading
               .Where(d => d.IsBuildingArtificial && d.HasComp(typeof(CompPowerTrader))
               && defs.Any(rpd => (rpd.className == d.thingClass?.Name) || (rpd.targetDef == d.defName))
               ).ToList();

            var loadedDefs = new List<string>();
            int num = 0, loaded = 0;

            foreach (ThingDef def in Things)
            {
                ++num;
                var target = def.defName;
                var namedDef = DefDatabase<ThingDef>.GetNamedSilentFail(target);
                var defPower = namedDef.GetCompProperties<CompProperties_Power>().PowerConsumption;
                // PowerLevels.Add(namedDef.defName, defPower);
                ++loaded;
                loadedDefs.Add(target);
            }

            var names = String.Join(", ", loadedDefs.ToArray()).Trim();
            Log.Message($"{General.ModName()} :: {string.Format("Loaded {1} of {0} building defs: {2}", num, loaded, names)}");

            Tracker.LoadThingDefs();
        }

        private static float basePower;
        private static float idlePower;

        public static float GetPowerSetting(Building building, bool isActive)
        {
            var powerComp = building?.TryGetComp<CompPowerTrader>();
            if (powerComp == null || powerComp.Props == null)
                return 0f;

            basePower = powerComp.Props.PowerConsumption;

            if (!(basePower < 0f && (basePower > 0f)))
                basePower = basePower * -1f; // Ensure basePower is negative for power DRAW

            if (isActive)
                return basePower;

            idlePower = powerComp.Props.idlePowerDraw;
            var calculatedIdlePower = basePower / 10f;

            if (idlePower == -1f) // RimWorld defaults idlePower to -1f, if its not set on the Def, we use a calculated value.
                return calculatedIdlePower;

            if (idlePower > 0f)
                idlePower = idlePower * -1f; // Ensure idlePower is negative for power DRAW

            if (idlePower < 0f) // Should already be negative
                return idlePower;

            // Something went wrong, return base power
            return basePower;
        }
    }
}
