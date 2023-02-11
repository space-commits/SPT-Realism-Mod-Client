using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;



namespace RealismMod
{
    public class AimingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("get_IsAiming", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {
            if (Helper.IsReady == true)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
                if (!player.IsAI)
                {
                    FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                    bool isOn = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                    if ((isOn && !WeaponProperties.WeaponCanFSADS && !ArmorProperties.AllowsADS(fsComponent.Item)) || !PlayerProperties.GearAllowsADS && !WeaponProperties.WeaponCanFSADS)
                    {
                        PlayerProperties.IsAllowedADS = false;
                        player.MovementContext.SetAimingSlowdown(false, 0.33f);
                        player.ProceduralWeaponAnimation.IsAiming = false;
                    }
                    else
                    {
                        PlayerProperties.IsAllowedADS = true;
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
            Logger.LogWarning("ToggleAim");
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
            if (Plugin.enableFSPatch.Value == true && !player.IsAI)
            {
                return PlayerProperties.IsAllowedADS;
            }
            return true;
        }
    }
}