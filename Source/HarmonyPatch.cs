using HarmonyLib;
using RimWorld;
using RW_Repower.functions;
using System;
using System.Linq;
using Verse;

namespace RW_Repower
{
    public class General
    {
        public static string ModName()
        {
            return "RW_Repower";
        }
    }

    [StaticConstructorOnStartup]
    internal class HarmonyPatch
    {
        public static Type patchtype = typeof(HarmonyPatch);
        public static string patchname = "RW_Repower.HarmonyPatch";
    
        static HarmonyPatch()
        {
            var harmony = new HarmonyLib.Harmony(patchname);
            try
            {
                Log.Message($"{General.ModName()} :: Applying Harmony patches for {patchname}...");

                harmony.Patch(
                    AccessTools.Method(typeof(ThingWithComps), "Tick"),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch.PatchMethods), nameof(PatchMethods.TickPostfix))
                    );

                harmony.Patch(
                    AccessTools.Method(typeof(Building_WorkTable), "UsedThisTick"),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch.PatchMethods), nameof(PatchMethods.UsedThisTickPostfix))
                    );

                harmony.Patch(AccessTools.Method(typeof(JobDriver_WatchBuilding), "WatchTickAction"),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch.PatchMethods), nameof(PatchMethods.WatchTickAction))
                    );


                Log.Message("RW_Repower :: Tick applied successfully.");

                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to apply Harmony patches for {patchname}: {ex.Message}");
                Log.Error(ex.StackTrace);
            }
        }

        public class PatchMethods
        {
            public static void TickPostfix(ThingWithComps __instance)
            {
                if (__instance == null)
                    return;
                RePower.Tick();
            }

            public static void UsedThisTickPostfix(Building_WorkTable __instance)
            {
                if (__instance != null)
                    Tracker.AddBuildingUsed(__instance);
            }
            public static void WatchTickAction(JobDriver_WatchBuilding __instance)
            {
                if (__instance?.job?.targetA.Thing is Building building && building != null)
                {
                    Tracker.AddBuildingUsed(building);
                }

            }
        }
    }
}
