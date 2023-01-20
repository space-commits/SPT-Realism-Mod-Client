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
        private static void PatchPostfix(EFT.Player.FirearmController __instance, ref bool ____isAiming)
        {
            if (Helper.IsReady == true)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
                if (!player.IsAI)
                {
                    Plugin.IsAiming = ____isAiming;
                }
            }
        }

        //This is a bad place to put it, need to find another method that runs in update
        public static void PlayerUpdate(EFT.Player.FirearmController __instance, Player player)
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
                PlayerProperties.RecoilInjuryMulti = 1f;
    }
            else if (rightArmDamaged == true && leftArmDamaged == false)
            {
                PlayerProperties.AimMoveSpeedBase = 0.39f;
                PlayerProperties.ErgoDeltaInjuryMulti = 1.5f;
                PlayerProperties.ADSInjuryMulti = 0.8f;
                PlayerProperties.ReloadInjuryMulti = 0.85f;
                PlayerProperties.RecoilInjuryMulti = 1.05f;
            }
            else if (rightArmDamaged == false && leftArmDamaged == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.35f;
                PlayerProperties.ErgoDeltaInjuryMulti = 2f;
                PlayerProperties.ADSInjuryMulti = 0.7f;
                PlayerProperties.ReloadInjuryMulti = 0.8f;
                PlayerProperties.RecoilInjuryMulti = 1.1f;
            }
            else if (rightArmDamaged == true && leftArmDamaged == true)
            {
                PlayerProperties.AimMoveSpeedBase = 0.3f;
                PlayerProperties.ErgoDeltaInjuryMulti = 3.5f;
                PlayerProperties.ADSInjuryMulti = 0.6f;
                PlayerProperties.ReloadInjuryMulti = 0.75f;
                PlayerProperties.RecoilInjuryMulti = 1.15f;
            }

            if (Helper.IsInReloadOpertation == true)
            {
                if (Helper.IsAttemptingToReloadInternalMag == true)
                {
                    float reloadBonus = 0.17f;
/*                 if (Helper.isAttemptingRevolverReload == true)
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
        private static bool Prefix(EFT.Player.FirearmController __instance)
        {
            Logger.LogWarning("ToggleAim");
            if (Plugin.enableFSPatch.Value == true)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
                FaceShieldComponent fsComponent = player.FaceShieldObserver.Component;
                bool isOn = fsComponent != null && (fsComponent.Togglable == null || fsComponent.Togglable.On);
                if (isOn && !WeaponProperties.WeaponCanFSADS && !FaceShieldProperties.AllowsADS(fsComponent.Item))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }
}