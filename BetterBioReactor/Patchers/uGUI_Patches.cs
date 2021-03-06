﻿namespace BetterBioReactor.Patchers
{
    using Harmony;

    // The immediate access to the internals of the BaseBioReactor (without the use of Reflection) was made possible thanks to the AssemblyPublicizer
    // https://github.com/CabbageCrow/AssemblyPublicizer
    [HarmonyPatch(typeof(uGUI_InventoryTab))]
    [HarmonyPatch("OnOpenPDA")]
    public class UGUI_InventoryTab_OnOpenPDA_Patcher
    {
        [HarmonyPostfix]
        public static void Postfix(uGUI_InventoryTab __instance)
        {
            // This event happens whenever the player opens their PDA.
            // We will make a series of checks to see if what they have opened is the Base BioReactor item container.

            if (__instance is null)
                return; // Safety check

            if (!Player.main.IsInSub() || !Player.main.currentSub.isBase)
                return; // If not in Base then all is irrelevant

            if (__instance.storage is null)
                return; // Safety check

            ItemsContainer currentContainer = __instance.storage.container;

            if (currentContainer is null)
                return; // If this isn't a non-null ItemsContainer, then it's not what we want.

            if (currentContainer._label != "BaseBioReactorStorageLabel")
                return; // Not a BaseBioReactor storage

            BaseBioReactor[] reactors = Player.main.currentSub.GetAllComponentsInChildren<BaseBioReactor>();

            if (reactors is null || reactors.Length == 0)
                return; // Base has no bioreactors

            // Look for the reactor that matches the container we just opened.
            foreach (BaseBioReactor reactor in reactors)
            {
                if (reactor.container != currentContainer)
                    continue;

                CyBioReactorMini.GetMiniReactor(reactor).ConnectToInventory(__instance.storage.items);
            }
        }
    }
}
