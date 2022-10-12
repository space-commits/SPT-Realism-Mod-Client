using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static RealismMod.Helper;

namespace RealismMod
{
    public class ErgoWeightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("get_ErgonomicWeight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Player.FirearmController __instance, ref float __result)
        {
            if (__instance?.Item?.Owner?.ID != null && __instance.Item.Owner.ID.StartsWith("pmc"))
            {
                __result = WeaponProperties.ErgnomicWeight;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
