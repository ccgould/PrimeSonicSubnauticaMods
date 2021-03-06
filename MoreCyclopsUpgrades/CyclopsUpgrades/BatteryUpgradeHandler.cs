﻿namespace MoreCyclopsUpgrades.CyclopsUpgrades
{
    using Managers;
    using System.Collections.Generic;
    using UnityEngine;

    internal class BatteryUpgradeHandler : ChargingUpgradeHandler
    {
        public delegate void BatteryDrainedEvent(BatteryDetails details);
        public BatteryDrainedEvent OnBatteryDrained;

        internal IList<BatteryDetails> Batteries { get; } = new List<BatteryDetails>();

        internal readonly bool BatteryRecharges;
        private float tempTotalCharge;
        public float TotalBatteryCharge = 0f;
        public float TotalBatteryCapacity = 0f;

        internal bool BatteryHasCharge => this.Count > 0 && TotalBatteryCharge > PowerManager.MinimalPowerValue;

        public BatteryUpgradeHandler(TechType techType, bool canRecharge) : base(techType)
        {
            BatteryRecharges = canRecharge;
            OnClearUpgrades += (SubRoot cyclops) =>
            {
                TotalBatteryCharge = 0f;
                TotalBatteryCapacity = 0f;
                this.Batteries.Clear();
            };

            OnUpgradeCounted += (SubRoot cyclops, Equipment modules, string slot) =>
            {
                var details = new BatteryDetails(modules, slot, modules.GetItemInSlot(slot).item.GetComponent<Battery>());
                this.Batteries.Add(details);
                TotalBatteryCharge += details.BatteryRef._charge;
                TotalBatteryCapacity += details.BatteryRef._capacity;
            };
        }

        public float GetBatteryPower(float drainingRate, float requestedPower)
        {
            if (requestedPower < PowerManager.MinimalPowerValue) // No power deficit left to charge
                return 0f; // Exit

            tempTotalCharge = 0f;
            float totalDrainedAmt = 0f;
            foreach (BatteryDetails details in this.Batteries)
            {
                if (requestedPower <= 0f)
                    continue; // No more power requested

                Battery battery = details.BatteryRef;

                if (battery._charge < PowerManager.MinimalPowerValue) // The battery has no charge left
                    continue; // Skip this battery

                // Mathf.Min is to prevent accidentally taking too much power from the battery
                float amtToDrain = Mathf.Min(requestedPower, drainingRate);

                if (battery._charge > amtToDrain)
                {
                    battery._charge -= amtToDrain;
                }
                else // Battery about to be fully drained
                {
                    amtToDrain = battery._charge; // Take what's left
                    battery._charge = 0f; // Set battery to empty

                    OnBatteryDrained?.Invoke(details);
                }

                tempTotalCharge += battery._charge;
                requestedPower -= amtToDrain; // This is to prevent draining more than needed if the power cells were topped up mid-loop

                totalDrainedAmt += amtToDrain;
            }
            TotalBatteryCharge = tempTotalCharge;

            return totalDrainedAmt;
        }

        public void RechargeBatteries(float surplusPower)
        {
            if (!BatteryRecharges)
                return;

            tempTotalCharge = 0f;
            bool batteryCharged = false;
            foreach (BatteryDetails details in this.Batteries)
            {
                tempTotalCharge += details.BatteryRef._charge;

                if (batteryCharged)
                    continue;

                if (surplusPower < PowerManager.MinimalPowerValue)
                    continue;

                if (details.IsFull)
                    continue;

                Battery batteryToCharge = details.BatteryRef;
                batteryToCharge._charge = Mathf.Min(batteryToCharge._capacity, batteryToCharge._charge + surplusPower);
                surplusPower -= (batteryToCharge._capacity - batteryToCharge._charge);
                batteryCharged = true;
            }

            TotalBatteryCharge = tempTotalCharge;
        }
    }
}
