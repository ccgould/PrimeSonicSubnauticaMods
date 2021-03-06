﻿namespace MoreCyclopsUpgrades.Modules
{
    using Caching;
    using Common;
    using Enhancement;
    using PowerUpgrade;
    using Recharging.Nuclear;
    using Recharging.Solar;
    using Recharging.Thermal;
    using SMLHelper.V2.Assets;
    using SMLHelper.V2.Crafting;
    using SMLHelper.V2.Handlers;
    using System.Collections.Generic;
    using UnityEngine;

    internal abstract class CyclopsModule : ModPrefab
    {
        private static readonly List<CyclopsModule> ModulesToPatch = new List<CyclopsModule>();

        internal static readonly Dictionary<TechType, CyclopsModule> CyclopsModulesByTechType = new Dictionary<TechType, CyclopsModule>(8);

        internal static bool ModulesEnabled { get; private set; } = true;

        private const string MaxSolarReachedKey = "MaxSolarMsg";
        internal static string MaxSolarReached()
        {
            return Language.main.Get(MaxSolarReachedKey);
        }

        private const string MaxThermalReachedKey = "MaxThermalMsg";
        internal static string MaxThermalReached()
        {
            return Language.main.Get(MaxThermalReachedKey);
        }

        private const string MaxNuclearReachedKey = "MaxNuclearMsg";
        internal static string MaxNuclearReached()
        {
            return Language.main.Get(MaxNuclearReachedKey);
        }

        // Default value that shouldn't get hit. Only here for error testing.
        public static TechType SolarChargerID { get; protected set; } = TechType.UnusedOld;
        public static TechType SolarChargerMk2ID { get; protected set; } = TechType.UnusedOld;
        public static TechType ThermalChargerMk2ID { get; protected set; } = TechType.UnusedOld;
        public static TechType PowerUpgradeMk2ID { get; protected set; } = TechType.UnusedOld;
        public static TechType PowerUpgradeMk3ID { get; protected set; } = TechType.UnusedOld;
        public static TechType SpeedBoosterModuleID { get; protected set; } = TechType.UnusedOld;
        public static TechType NuclearChargerID { get; protected set; } = TechType.UnusedOld;
        public static TechType DepletedNuclearModuleID { get; protected set; } = TechType.UnusedOld;
        public static TechType RefillNuclearModuleID { get; protected set; } = TechType.UnusedOld;
        public static TechType BioReactorBoosterID { get; protected set; } = TechType.UnusedOld;

        public readonly string NameID;
        public readonly string FriendlyName;
        public readonly string Description;
        public readonly TechType RequiredForUnlock;
        public readonly CraftTree.Type Fabricator;
        public readonly string[] FabricatorTabs;

        protected readonly TechType PreFabTemplate;

        private readonly bool AddToCraftTree;

        protected CyclopsModule(string nameID, string friendlyName, string description, CraftTree.Type fabricator, string[] fabricatorTab, TechType requiredAnalysisItem = TechType.None, TechType preFabTemplate = TechType.CyclopsThermalReactorModule)
            : base(nameID, $"{nameID}PreFab")
        {
            NameID = nameID;
            FriendlyName = friendlyName;
            Description = description;
            RequiredForUnlock = requiredAnalysisItem;
            Fabricator = fabricator;
            FabricatorTabs = fabricatorTab;

            AddToCraftTree = FabricatorTabs != null;
            PreFabTemplate = preFabTemplate;
        }

        protected CyclopsModule(string nameID, string friendlyName, string description, TechType requiredAnalysisItem = TechType.None, TechType preFabTemplate = TechType.CyclopsThermalReactorModule)
            : base(nameID, $"{nameID}PreFab")
        {
            NameID = nameID;
            FriendlyName = friendlyName;
            Description = description;
            RequiredForUnlock = requiredAnalysisItem;

            AddToCraftTree = false;
            PreFabTemplate = preFabTemplate;
        }

        protected virtual void Patch()
        {
            this.TechType = TechTypeHandler.AddTechType(NameID, FriendlyName, Description, RequiredForUnlock == TechType.None);

            if (!ModulesEnabled) // Even if the options have this be disabled,
                return; // we still want to run through the AddTechType methods to prevent mismatched TechTypeIDs as these settings are switched

            if (RequiredForUnlock == TechType.None)
                KnownTechHandler.UnlockOnStart(this.TechType);
            else
                KnownTechHandler.SetAnalysisTechEntry(RequiredForUnlock, new TechType[1] { this.TechType }, $"{FriendlyName} blueprint discovered!");

            PrefabHandler.RegisterPrefab(this);

            SpriteHandler.RegisterSprite(this.TechType, $"./QMods/MoreCyclopsUpgrades/Assets/{NameID}.png");

            CraftDataHandler.SetTechData(this.TechType, GetRecipe());

            if (AddToCraftTree)
            {
                QuickLogger.Debug($"Setting crafting node for {this.ClassID}");
                CraftTreeHandler.AddCraftingNode(Fabricator, this.TechType, FabricatorTabs);
            }

            CraftDataHandler.SetEquipmentType(this.TechType, EquipmentType.CyclopsModule);
            CraftDataHandler.AddToGroup(TechGroup.Cyclops, TechCategory.CyclopsUpgrades, this.TechType);

            SetStaticTechTypeID(this.TechType);
        }

        protected abstract void SetStaticTechTypeID(TechType techTypeID);

        public override GameObject GetGameObject()
        {
            GameObject prefab = CraftData.GetPrefabForTechType(PreFabTemplate);
            var obj = GameObject.Instantiate(prefab);

            return obj;
        }

        protected abstract TechData GetRecipe();

        internal static void PatchAllModules(bool modulesEnabled)
        {
            ModulesEnabled = modulesEnabled;

            ModulesToPatch.Add(new ThermalChargerMk2());
            ModulesToPatch.Add(new SolarCharger(OtherMods.VehicleUpgradesInCyclops));
            ModulesToPatch.Add(new SolarChargerMk2());
            ModulesToPatch.Add(new PowerUpgradeMk2());
            ModulesToPatch.Add(new PowerUpgradeMk3());
            ModulesToPatch.Add(new CyclopsSpeedBooster(OtherMods.VehicleUpgradesInCyclops));
            ModulesToPatch.Add(new BioReactorBooster(OtherMods.VehicleUpgradesInCyclops));
            ModulesToPatch.Add(new NuclearCharger());
            ModulesToPatch.Add(new DepletedNuclearModule());

            foreach (CyclopsModule module in ModulesToPatch)
            {
                QuickLogger.Debug($"Patching {module.NameID}");
                module.Patch();
                CyclopsModulesByTechType.Add(module.TechType, module);
            }

            LanguageHandler.SetLanguageLine(MaxSolarReachedKey, "Max number of solar chargers reached.");
            LanguageHandler.SetLanguageLine(MaxThermalReachedKey, "Max number of thermal chargers reached.");
            LanguageHandler.SetLanguageLine(MaxNuclearReachedKey, "Max number of nuclear chargers.");
        }

        public static InventoryItem SpawnCyclopsModule(TechType techTypeID)
        {
            var gameObject = GameObject.Instantiate(CraftData.GetPrefabForTechType(techTypeID));

            Pickupable pickupable = gameObject.GetComponent<Pickupable>().Pickup(false);
            return new InventoryItem(pickupable);
        }
    }
}