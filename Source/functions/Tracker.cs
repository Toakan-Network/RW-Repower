using RimWorld;
using RW_Repower.functions;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RW_Repower
{
    public class Tracker
    {
        public static Dictionary<Building, int> BuildingsInUse = new Dictionary<Building, int>();
        public static Dictionary<Building, int> BuildingsInUseLastTick = new Dictionary<Building, int>();
        public static HashSet<Building> BuildingsToModify = new HashSet<Building>();

        public static HashSet<ThingDef> BuildingDefsReservable = new HashSet<ThingDef>();
        public static HashSet<Building> ReservableBuildings = new HashSet<Building>();

        public static HashSet<ThingDef> ScheduledBuildingsDefs = new HashSet<ThingDef>();
        public static HashSet<Building> ScheduledBuildings = new HashSet<Building>();

        public static HashSet<Building_Bed> MedicalBeds = new HashSet<Building_Bed>();
        public static HashSet<Building> HiTechResearchBenches = new HashSet<Building>();

        public static HashSet<Building_Door> Autodoors = new HashSet<Building_Door>();
        public static HashSet<Building> DeepDrills = new HashSet<Building>();
        public static HashSet<Building> HydroponcsBasins = new HashSet<Building>();

        private static ThingDef MedicalBedDef;
        private static ThingDef HiTechResearchBenchDef;
        private static ThingDef AutodoorDef;
        private static ThingDef DeepDrillDef;
        private static ThingDef HydroponicsBasinDef;

        public static void LoadThingDefs()
        {
            MedicalBedDef = ThingDef.Named("HospitalBed");
            HiTechResearchBenchDef = ThingDef.Named("HiTechResearchBench");
            AutodoorDef = ThingDef.Named("Autodoor");
            DeepDrillDef = ThingDef.Named("DeepDrill");
            HydroponicsBasinDef = ThingDef.Named("HydroponicsBasin");
        }

        public static void AddBuildingUsed(Building building)
        {
            BuildingsInUse[building] = Find.TickManager.TicksGame;
            RePower.SetPower(building, IsActive: true);
        }

        public static void EvalAll()
        {
            EvalBeds();
            EvalResearchTables();
            EvalAutodoors();
            EvalDeepDrills();
            EvalHydroponicsBasins();
            EvalScheduledBuildings();
        }

        public static void ScanExternalReservable()
        {
            ReservableBuildings.Clear();
            foreach (ThingDef def in BuildingDefsReservable)
            {
                foreach (var map in Find.Maps)
                {
                    if (map == null) continue;
                    var buildings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                    foreach (var building in buildings)
                    {
                        if (building == null) continue;
                        ReservableBuildings.Add(building);
                    }
                }
            }
        }

        public static void ScanScheduledBuildings()
        {
            ScheduledBuildings.Clear();
            foreach (ThingDef def in ScheduledBuildingsDefs)
            {
                foreach (var map in Find.Maps)
                {
                    if (map == null) continue;
                    var buildings = map.listerBuildings.AllBuildingsColonistOfDef(def);
                    foreach (var building in buildings)
                    {
                        if (building == null) continue;
                        Log.Message($"Scheduled Building found: {building.def.defName} at {building.Position}");
                        ScheduledBuildings.Add(building);
                    }
                }
            }
        }

        public static void EvalScheduledBuildings()
        {
            foreach (var building in ScheduledBuildings)
            {
                if (building == null) continue;
                if (building.Map == null) continue;

                var comp = building.GetComp<CompSchedule>();
                if (comp == null) continue; // Doesn't actually have a schedule

                if (comp.Allowed)
                {
                    AddBuildingUsed(building);
                }
            }
        }

        // Evaluate medical beds for medical beds in use, to register that the vitals monitors should be in high power mode
        public static void EvalBeds()
        {
            if (MedicalBeds == null || MedicalBeds.Count == 0)
            {
                Log.Warning("No medical beds found to evaluate.");
                return;
            }

            foreach (var mediBed in MedicalBeds)
            {
                if (mediBed == null) 
                    continue;
                if (mediBed.Map == null) 
                    continue;
                if (mediBed.CurOccupants.Count() == 0)
                    continue;

                var facilityAffector = mediBed.GetComp<CompAffectedByFacilities>();
                if (facilityAffector == null) continue; // No facilities 
                AddBuildingUsed(facilityAffector.LinkedFacilitiesListForReading[0] as Building); // Add the first facility linked to the bed           
            }
        }

        public static void EvalDeepDrills()
        {
            foreach (var deepDrill in DeepDrills)
            {
                if (deepDrill == null) continue;
                if (deepDrill.Map == null) continue;

                var inUse = deepDrill.Map.reservationManager.IsReservedByAnyoneOf(deepDrill, deepDrill.Faction);

                if (!inUse) continue;

                AddBuildingUsed(deepDrill);
            }
        }

        // How to tell if a research table is in use?
        // I can't figure it out. Instead let's base it on being reserved for use
        public static void EvalResearchTables()
        {
            if (HiTechResearchBenches == null || HiTechResearchBenches.Count == 0)
            {
                return;
            }
            foreach (var researchTable in HiTechResearchBenches)
            {
                if (researchTable == null) continue;
                if (researchTable.Map == null) continue;

                // Determine if we are reserved:
                var inUse = researchTable.Map.reservationManager.IsReservedByAnyoneOf(researchTable, researchTable.Faction);
                if (!inUse) continue;
                AddBuildingUsed(researchTable);

                var facilityAffector = researchTable.GetComp<CompAffectedByFacilities>();
                if (facilityAffector == null) continue; 
                AddBuildingUsed(facilityAffector.LinkedFacilitiesListForReading[0] as Building); // Add the first facility linked to the research table as there's no benefit for more anyway.             
            }
        }

        public static void EvalAutodoors()
        {
            foreach (var autodoor in Autodoors)
            {
                if (autodoor == null) continue;
                if (autodoor.Map == null) continue;

                // If the door allows passage and isn't blocked by an object
                if (autodoor.Open && (!autodoor.BlockedOpenMomentary))
                {
                    AddBuildingUsed(autodoor);
                }
            }
        }

        public static void EvalHydroponicsBasins()
        {
            foreach (var basin in HydroponcsBasins)
            {
                if (basin == null) continue;
                if (basin.Map == null) continue;

                foreach (var tile in basin.OccupiedRect())
                {
                    var thingsOnTile = basin.Map.thingGrid.ThingsListAt(tile);
                    foreach (var thing in thingsOnTile)
                    {
                        if (thing is Plant)
                        {
                            AddBuildingUsed(basin);
                            break;
                        }
                    }
                }
            }
        }

        public static HashSet<ThingDef> thingDefsToLookFor;
        public static void ScanForThings()
        {
            // Build the set of def names to look for if we don't have it

            ScanExternalReservable(); // Handle the scanning of external reservable objects
            ScanScheduledBuildings(); // Search for buildings with scheduled activation

            BuildingsToModify.Clear();
            MedicalBeds.Clear();
            HiTechResearchBenches.Clear();
            Autodoors.Clear();
            DeepDrills.Clear();
            HydroponcsBasins.Clear();

            var maps = Find.Maps;
            foreach (Map map in maps)
            {
                foreach (ThingDef def in RW_Repower.functions.RePower.Things)
                {
                    // Merge in all matching things
                    var matchingThings = map.listerBuildings.AllBuildingsColonistOfDef(def).Where(t => t is Building).Cast<Building>();
                    BuildingsToModify.UnionWith(matchingThings);
                }

                // Register the medical beds in the watch list
                var mediBeds = map.listerBuildings.AllBuildingsColonistOfDef(MedicalBedDef);
                if (mediBeds.Count > 0 || mediBeds != null) {
                    foreach (var mediBed in mediBeds)
                    {
                        var medicalBed = mediBed as Building_Bed;
                        MedicalBeds.Add(medicalBed);
                        continue;
                    }
                }

                var doors = map.listerBuildings.AllBuildingsColonistOfDef(AutodoorDef);
                if (doors.Count > 0 || doors != null)
                {
                    foreach (var door in doors)
                    {
                        var autodoor = door as Building_Door;
                        Autodoors.Add(autodoor);
                        continue;
                    }
                }

                var hydroponics = map.listerBuildings.AllBuildingsColonistOfDef(HydroponicsBasinDef);
                if (hydroponics.Count > 0 || hydroponics != null)
                {
                    foreach (var hydroponic in hydroponics)
                    {
                        var hydroponicsBasin = hydroponic as Building;
                        HydroponcsBasins.Add(hydroponicsBasin);
                        continue;
                    }
                }

                // Register Hightech research tables too
                var researchTables = map.listerBuildings.AllBuildingsColonistOfDef(HiTechResearchBenchDef);
                HiTechResearchBenches.UnionWith(researchTables);

                var deepDrills = map.listerBuildings.AllBuildingsColonistOfDef(DeepDrillDef);
                DeepDrills.UnionWith(deepDrills);
            }
        }
    }
}
