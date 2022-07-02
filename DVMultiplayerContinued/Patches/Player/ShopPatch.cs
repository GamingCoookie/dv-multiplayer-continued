using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DVMultiplayer.Networking;
using DV.Shops;
using DV.CashRegister;
using HarmonyLib;
using UnityEngine;

namespace DVMultiplayerContinued.Patches.Player
{
    internal static class ShopPatch
    {
        [HarmonyPatch(typeof(ScanItemResourceModule), nameof(ScanItemResourceModule.AddItemsToBuy))]
        internal static class AddItemsToBuyPatch
        {
            internal static bool Prefix(ScanItemResourceModule __instance, ref bool __result)
            {
                if (NetworkManager.IsClient() && int.Parse(__instance.itemPriceText.text.TrimStart('$')) > 250)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}
