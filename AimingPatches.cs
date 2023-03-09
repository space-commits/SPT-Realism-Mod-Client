using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;



namespace RealismMod
{



    public class SetAimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("set_IsAiming", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, bool value, ref bool ____isAiming)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
            if (__instance.Item.WeapClass == "pistol")
            {
                player.Physical.Aim((!____isAiming || !(player.MovementContext.StationaryWeapon == null)) ? 0f : __instance.ErgonomicWeight * 0.2f);
            }
        }
    }

    public class GetAimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {
            if (Utils.IsReady == true)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
                if (!player.IsAI)
                {
                    FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    bool fsIsON = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);
                    if ((Plugin.EnableNVGPatch.Value == true && nvgIsOn == true && Plugin.HasOptic) || (Plugin.EnableFSPatch.Value == true && ((fsIsON && !WeaponProperties.WeaponCanFSADS && !ArmorProperties.AllowsADS(fsComponent.Item)) || (!PlayerProperties.GearAllowsADS && !WeaponProperties.WeaponCanFSADS))))
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.MovementContext.SetAimingSlowdown(false, 0.33f);
                        player.ProceduralWeaponAnimation.IsAiming = false;
                    }
                    else
                    {
                        PlayerProperties.IsAllowedADS = true;
                    }

                    if (Plugin.IsActiveAiming == true)
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.ProceduralWeaponAnimation.IsAiming = false;
                        player.MovementContext.SetAimingSlowdown(true, 0.33f);
                    }
                    if (!Plugin.IsActiveAiming && !____isAiming)
                    {
                        player.MovementContext.SetAimingSlowdown(false, 0.33f);
                    }

                    Plugin.IsAiming = ____isAiming;
                }
            }
        }
    }

    public class ToggleAimPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("ToggleAim", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
            if (Plugin.EnableFSPatch.Value == true && !player.IsAI)
            {
                return PlayerProperties.IsAllowedADS;
            }
            return true;
        }
    }
}