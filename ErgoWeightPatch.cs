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
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (!player.IsAI)
            {
                __result = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * PlayerProperties.StrengthSkillAimBuff;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
