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

    public class SensPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo mouseSensField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(GClass1667), "player_0");
            mouseSensField = AccessTools.Field(typeof(Player), "_mouseSensitivityModifier");

            return typeof(GClass1667).GetMethod("ApplyExternalSense", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        public static bool PatchPrefix(ref Player.FirearmController __instance, Vector2 deltaRotation, ref Vector2 __result)
        {

            if (Plugin.IsFiring)
            {
                Player player = (Player)playerField.GetValue(__instance);
                float mouseSensitivityModifier = (float)mouseSensField.GetValue(player);
                float xLimit = Plugin.IsAiming ? Plugin.StartingAimSens : Plugin.StartingHipSens;
                Vector2 newSens = deltaRotation;
                newSens.y *= player.GetRotationMultiplier();
                newSens.x *= Mathf.Min(player.GetRotationMultiplier() * 1.5f, xLimit * (1f + mouseSensitivityModifier));
                __result = newSens;
                return false;
            }
            return true;
        }
    }


    public class UpdateSensitivityPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");

            return typeof(Player.FirearmController).GetMethod("UpdateSensitivity", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref float ____aimingSens)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                if (Plugin.FovFixIsPresent)
                {
                    Plugin.CurrentAimSens = Plugin.StartingAimSens;
                }
                else
                {
                    Plugin.StartingAimSens = ____aimingSens;
                    Plugin.CurrentAimSens = ____aimingSens;
                }
            }
        }
    }

    public class AimingSensitivityPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");

            return typeof(Player.FirearmController).GetMethod("get_AimingSensitivity", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref float __result)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                __result = Plugin.CurrentAimSens;
            }
        }
    }

    public class GetRotationMultiplierPatch : ModulePatch
    {
        private static FieldInfo mouseSensField;

        protected override MethodBase GetTargetMethod()
        {
            mouseSensField = AccessTools.Field(typeof(Player), "_mouseSensitivityModifier");

            return typeof(Player).GetMethod("GetRotationMultiplier", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        public static void PatchPostfix(ref Player __instance, ref float __result)
        {
            if (__instance.IsYourPlayer)
            {
                if (!(__instance.HandsController != null) || !__instance.HandsController.IsAiming)
                {
                    float sens = Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseSensitivity;
                    Plugin.StartingHipSens = sens;
                    if (!Plugin.CheckedForSens)
                    {
                        Plugin.CurrentHipSens = sens;
                        Plugin.CheckedForSens = true;
                    }
                    else if (Plugin.EnableHipfireRecoilClimb.Value)
                    {
                        float mouseSensitivityModifier = (float)mouseSensField.GetValue(__instance);
                        __result = Plugin.CurrentHipSens * (1f + mouseSensitivityModifier);
                    }

                }
            }
        }
    }
}
