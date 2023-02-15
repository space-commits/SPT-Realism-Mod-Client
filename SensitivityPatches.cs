using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using static EFT.Profile;

namespace RealismMod
{

    public class UpdateSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("UpdateSensitivity", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref float ____aimingSens)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.isUniformAimPresent == false || Plugin.isBridgePresent == false)
                {
                    Plugin.StartingAimSens = ____aimingSens;
                    Plugin.CurrentAimSens = ____aimingSens;
                }
                else
                {
                    Plugin.CurrentAimSens = Plugin.StartingAimSens;
                }
            }
        }
    }

    public class AimingSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("get_AimingSensitivity", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref float __result)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                __result = Plugin.CurrentAimSens;
            }
        }
    }

    public class GetRotationMultiplierPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("GetRotationMultiplier", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        public static void PatchPostfix(ref Player __instance, ref float __result)
        {
            if (__instance.IsYourPlayer == true)
            {
                if (!(__instance.HandsController != null) || !__instance.HandsController.IsAiming)
                {
                    float sens = Singleton<GClass1776>.Instance.Control.Settings.MouseSensitivity;
                    Plugin.StartingHipSens = sens;
                    if (Plugin.CheckedForSens == false)
                    {
                        Plugin.CurrentHipSens = sens;
                        Plugin.CheckedForSens = true;
                    }
                    else
                    {
                        float _mouseSensitivityModifier = (float)AccessTools.Field(typeof(Player), "_mouseSensitivityModifier").GetValue(__instance);
                        __result = Plugin.CurrentHipSens * (1f + _mouseSensitivityModifier);
                    }

                }
            }
        }
    }
}
