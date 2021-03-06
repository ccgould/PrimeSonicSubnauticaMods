﻿namespace MoreCyclopsUpgrades.SaveData
{
    using System.Collections.Generic;
    using System.IO;
    using Common;
    using Common.EasyMarkup;
    using MoreCyclopsUpgrades.Caching;
    using MoreCyclopsUpgrades.Monobehaviors;
    using SMLHelper.V2.Utility;
    using UnityEngine;

    internal class CyBioReactorSaveData : EmPropertyCollection
    {
        private const string KeyName = "CBR";
        private const string ReactorBatterChargeKey = "BRP";
        private const string MaterialsKey = "MAT";
        private const string BoostCountKey = "BC";
        private readonly string ID;

        private readonly EmProperty<float> _batteryCharge;
        private readonly EmProperty<int> _boosterCount;
        private EmPropertyCollectionList<EmModuleSaveData> _materials;

        private static ICollection<EmProperty> GetDefinitions => new List<EmProperty>()
        {
            new EmProperty<float>(ReactorBatterChargeKey, 0),
            new EmProperty<int>(BoostCountKey, 0),
            new EmPropertyCollectionList<EmModuleSaveData>(MaterialsKey)
        };

        public CyBioReactorSaveData(ICollection<EmProperty> definitions) : base("CyBioReactor", definitions)
        {
            _batteryCharge = (EmProperty<float>)Properties[ReactorBatterChargeKey];
            _boosterCount = (EmProperty<int>)Properties[BoostCountKey];
            _materials = (EmPropertyCollectionList<EmModuleSaveData>)Properties[MaterialsKey];
        }

        public CyBioReactorSaveData(string preFabID) : this(GetDefinitions)
        {
            ID = preFabID;
        }

        public void SaveMaterialsProcessing(IEnumerable<BioEnergy> materialsInProcessor)
        {
            _materials.Values.Clear();

            foreach (BioEnergy item in materialsInProcessor)
            {
                _materials.Add(new EmModuleSaveData
                {
                    ItemID = (int)item.Pickupable.GetTechType(),
                    RemainingCharge = item.RemainingEnergy
                });
            }
        }

        public List<BioEnergy> GetMaterialsInProcessing()
        {
            var list = new List<BioEnergy>();

            foreach (EmModuleSaveData savedItem in _materials.Values)
            {
                var techTypeID = (TechType)savedItem.ItemID;
                var gameObject = GameObject.Instantiate(CraftData.GetPrefabForTechType(techTypeID));

                Pickupable pickupable = gameObject.GetComponent<Pickupable>().Pickup(false);

                list.Add(new BioEnergy(pickupable, savedItem.RemainingCharge));
            }

            return list;
        }

        public float ReactorBatterCharge
        {
            get => _batteryCharge.HasValue ? Mathf.Min(_batteryCharge.Value, CyBioReactorMono.MaxPowerBaseline) : 0;
            set => _batteryCharge.Value = Mathf.Min(value, CyBioReactorMono.MaxPowerBaseline);
        }

        public int BoosterCount
        {
            get => _boosterCount.HasValue ? _boosterCount.Value : 0;
            set => _boosterCount.Value = Mathf.Max(value, 0);
        }

        private string SaveDirectory => Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "CyBioReactor");
        private string SaveFile => Path.Combine(this.SaveDirectory, ID + ".txt");

        public void Save()
        {
            this.Save(this.SaveDirectory, this.SaveFile);
        }

        public bool Load()
        {
            return this.Load(this.SaveDirectory, this.SaveFile);
        }

        internal override EmProperty Copy()
        {
            return new CyBioReactorSaveData(this.CopyDefinitions);
        }
    }
}
