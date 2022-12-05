using Aki.Reflection.Patching;
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
        private static void PatchPostfix(ref EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {
            if (Helper.IsReady == true)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
  
                if (!player.IsAI)
                {
                    CheckReloading(__instance, player);
                    FaceShieldComponent component = player.FaceShieldObserver.Component;
                    bool isOn = component != null && (component.Togglable == null || component.Togglable.On);
                    if (isOn && !WeaponProperties.WeaponCanFSADS && !FaceShieldProperties.AllowsADS(component.Item))
                    {
                        Helper.IsAllowedAim = false;
                    }
                    else
                    {
                        Helper.IsAllowedAim = true;
                    }
                    Plugin.isAiming = ____isAiming;
                }
            }
        }

        public static void CheckReloading(EFT.Player.FirearmController __instance, Player player)
        {
            Helper.IsInReloadOpertation = __instance.IsInReloadOperation();
            bool rightArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.RightArmDamaged);
            bool leftArmDamaged = player.MovementContext.PhysicalConditionIs(EPhysicalCondition.LeftArmDamaged);

            if (rightArmDamaged == false && leftArmDamaged == false)
            {
                PlayerProperties.AimMoveSpeedBase = 0.42f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1f;
                PlayerProperties.ADSInjuryMulti = 1f;
                PlayerProperties.ReloadInjuryMulti = 1f;
            }
            else if (rightArmDamaged == true && leftArmDamaged == false)
            {
                PlayerProperties.AimMoveSpeedBase = 0.39f;
                PlayerProperties.ErgoDeltaInjuryMulti = 2f;
                PlayerProperties.ADSInjuryMulti = 0.8f;
                PlayerProperties.ReloadInjuryMulti = 0.85f;
            }
            else if (rightArmDamaged == false && leftArmDamaged == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.35f;
                PlayerProperties.ErgoDeltaInjuryMulti = 3.5f;
                PlayerProperties.ADSInjuryMulti = 0.7f;
                PlayerProperties.ReloadInjuryMulti = 0.8f;
            }
            else if (rightArmDamaged == true && leftArmDamaged == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.3f;
                PlayerProperties.ErgoDeltaInjuryMulti = 5;
                PlayerProperties.ADSInjuryMulti = 0.6f;
                PlayerProperties.ReloadInjuryMulti = 0.75f;
            }

            if (Helper.IsInReloadOpertation == true)
            {
                if (Helper.IsAttemptingToReloadInternalMag == true)
                {
                    float reloadBonus = 0.17f;
/*                    if (Helper.isAttemptingRevolverReload == true)
                    {
                        reloadBonus += 0.05f;
                    }*/
                    player.HandsAnimator.SetAnimationSpeed(reloadBonus + (WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti));
                }
            }
            else
            {
                Helper.IsAttemptingToReloadInternalMag = false;
                Helper.IsAttemptingRevolverReload = false;
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
        private static bool Prefix()
        {

            if (Plugin.enableFSPatch.Value == true)
            {
                if (Helper.IsAllowedAim)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}