﻿namespace CustomCraft2SML.Serialization.Components
{
    using System.Collections.Generic;
    using Common;
    using Common.EasyMarkup;
    using CustomCraft2SML.Interfaces;
    using CustomCraft2SML.PublicAPI;
    using SMLHelper.V2.Handlers;

    public abstract class EmTechTyped : EmPropertyCollection, ITechTyped
    {
        protected const string ItemIdKey = "ItemID";

        protected readonly EmProperty<string> emTechType;

        protected static List<EmProperty> TechTypedProperties => new List<EmProperty>(1)
        {
            new EmProperty<string>(ItemIdKey),
        };

        public EmTechTyped() : this("TechTyped", TechTypedProperties)
        {
        }

        protected EmTechTyped(string key) : this(key, TechTypedProperties)
        {
        }

        protected EmTechTyped(string key, ICollection<EmProperty> definitions) : base(key, definitions)
        {
            emTechType = (EmProperty<string>)Properties[ItemIdKey];
        }

        public virtual string ItemID
        {
            get => emTechType.Value;
            set => emTechType.Value = value;
        }

        public TechType TechType { get; set; } = TechType.None;

        public virtual bool PassesPreValidation()
        {
            // Now we can safely do the prepass check in case we need to create a new modded TechType
            this.TechType = GetTechType(this.ItemID);

            if (this.TechType == TechType.None)
            {
                QuickLogger.Warning($"Could not resolve {ItemIdKey} value of '{this.ItemID}' for {this.Key}. Discarded entry.");
                return false;
            }

            return true;
        }

        protected static TechType GetTechType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return TechType.None;
            }

            // Look for a known TechType
            if (TechTypeExtensions.FromString(value, out TechType tType, true))
            {
                return tType;
            }

            //  Not one of the known TechTypes - is it registered with SMLHelper?
            if (TechTypeHandler.TryGetModdedTechType(value, out TechType custom))
            {
                return custom;
            }

            return TechType.None;
        }

        protected void AddCraftNode(CraftingPath newPath, TechType techType)
        {
            if (newPath.IsAtRoot)
                CraftTreeHandler.AddCraftingNode(newPath.Scheme, techType);
            else
                CraftTreeHandler.AddCraftingNode(newPath.Scheme, techType, newPath.Steps);

            QuickLogger.Debug($"New crafting node for {this.Key} '{this.ItemID}' from added at '{newPath.Path}'");
        }
    }
}
