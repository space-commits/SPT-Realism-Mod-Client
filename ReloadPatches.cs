using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static EFT.Player;

namespace RealismMod
{

    public class SetLauncherPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetLauncher", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(bool isLauncherEnabled)
        {
            Plugin.LauncherIsActive = isLauncherEnabled;
        }
    }

    public class SetWeaponLevelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetWeaponLevel", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance, float weaponLevel)
        {
            if (WeaponProperties._WeapClass == "shotgun")
            {
                if (weaponLevel < 3)
                {
                    weaponLevel += 1;
                }
                WeaponAnimationSpeedControllerClass.SetWeaponLevel(__instance.Animator, weaponLevel);
            }

        }
    }


    public class SetHammerArmedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetHammerArmed", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {

            if (Plugin.IsFiring != true && PlayerProperties.IsInReloadOpertation)
            {
                __instance.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.TotalChamberSpeed * Plugin.GlobalArmHammerSpeedMulti.Value * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.35f));
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("SetHammerArmed, firing and reload op");
                    Logger.LogWarning("SetHammerArmed = " + Mathf.Clamp(WeaponProperties.TotalChamberSpeed * Plugin.GlobalArmHammerSpeedMulti.Value * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.35f));
                }
            }

        }
    }

    public class CheckAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("CheckAmmo", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            float bonus = Plugin.GlobalCheckAmmoMulti.Value;
            if (WeaponProperties._WeapClass == "pistol")
            {
                bonus = Plugin.GlobalCheckAmmoPistolSpeedMulti.Value;
            }
            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===CheckAmmo===");
                Logger.LogWarning("Check Ammo =" + Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * bonus * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.3f));
                Logger.LogWarning("=============");
            }

            __instance.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * bonus * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.3f));
        }
    }

    public class SetAnimatorAndProceduralValuesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("SetAnimatorAndProceduralValues", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("SetAnimatorAndProceduralValues");
                }
  
                PlayerProperties.IsManipulatingWeapon = false;
            }
        }
    }

    public class CheckAmmoFirearmControllerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("CheckAmmo", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true) 
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("CheckAmmo");
                }

                PlayerProperties.IsManipulatingWeapon = true;
            }
 
        }
    }

    public class CheckChamberPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("CheckChamber", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            float chamberSpeed = WeaponProperties.TotalChamberCheckSpeed;
            if (WeaponProperties._WeapClass == "pistol")
            {
                chamberSpeed *= Plugin.GlobalCheckChamberPistolSpeedMulti.Value;
            }
            else if (WeaponProperties._WeapClass == "shotgun")
            {
                chamberSpeed *= Plugin.GlobalCheckChamberShotgunSpeedMulti.Value;
            }
            else
            {
                chamberSpeed *= Plugin.GlobalCheckChamberSpeedMulti.Value;
            }
            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===CheckChamber===");
                Logger.LogWarning("Check Chamber = " + Mathf.Clamp(chamberSpeed * PlayerProperties.FixSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.3f));
                Logger.LogWarning("=============");
            }

            __instance.SetAnimationSpeed(Mathf.Clamp(chamberSpeed * PlayerProperties.FixSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.3f));
        }
    }

    public class SetBoltActionReloadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetBoltActionReload", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {

            if (WeaponProperties._IsManuallyOperated == true || Plugin.LauncherIsActive == true)
            {

                float chamberSpeed = WeaponProperties.TotalFiringChamberSpeed;
                if (WeaponProperties._WeapClass == "shotgun")
                {
                    chamberSpeed *= Plugin.GlobalShotgunRackSpeedFactor.Value;
                }
                if (Plugin.LauncherIsActive == true)
                {
                    chamberSpeed *= Plugin.GlobalUBGLReloadMulti.Value;
                }
                if (WeaponProperties._WeapClass == "sniperRifle")
                {
                    chamberSpeed *= Plugin.GlobalBoltSpeedMulti.Value;
                }
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===SetBoltActionReload===");
                    Logger.LogWarning("Set Bolt Action Reload = " + Mathf.Clamp(chamberSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.3f));
                    Logger.LogWarning("=============");
                }
                __instance.SetAnimationSpeed(Mathf.Clamp(chamberSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.3f));
            }
        }
    }

    public class SetMalfRepairSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetMalfRepairSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static void Prefix(FirearmsAnimator __instance, float fix)
        {

            float totalFixSpeed = Mathf.Clamp(fix * WeaponProperties.TotalFixSpeed * PlayerProperties.ReloadInjuryMulti * Plugin.GlobalFixSpeedMulti.Value, 0.5f, 1.3f);
            WeaponAnimationSpeedControllerClass.SetSpeedFix(__instance.Animator, totalFixSpeed);
            __instance.SetAnimationSpeed(totalFixSpeed);
            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===SetMalfRepairSpeed===");
                Logger.LogWarning("SetMalfRepairSpeed = " + totalFixSpeed);
                Logger.LogWarning("=============");
            }

        }
    }

    public class RechamberSpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("Rechamber", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            float chamberSpeed = WeaponProperties.TotalFixSpeed;
            if (WeaponProperties._WeapClass == "pistol")
            {
                chamberSpeed *= Plugin.RechamberPistolSpeedMulti.Value;
            }

            else
            {
                chamberSpeed *= Plugin.GlobalRechamberSpeedMulti.Value;
            }

            __instance.SetAnimationSpeed(Mathf.Clamp(chamberSpeed * PlayerProperties.FixSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.35f));
            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===Rechamber===");
                Logger.LogWarning("Rechamber = " + Mathf.Clamp(chamberSpeed * PlayerProperties.FixSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.5f, 1.35f));
                Logger.LogWarning("=============");
            }

            PlayerProperties.IsManipulatingWeapon = true;

        }
    }

    public class CanStartReloadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("CanStartReload", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, bool __result)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (__result == true)
                {
                    if (__instance.Item.GetCurrentMagazine() == null)
                    {
                        PlayerProperties.NoMagazineReload = true;
                    }
                    else
                    {
                        PlayerProperties.NoMagazineReload = false;
                    }
                }
            }
        }
    }

    public class ReloadMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("ReloadMag", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, MagazineClass magazine)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {

                }
                StatCalc.SetMagReloadSpeeds(__instance, magazine);
            }
        }
    }


    public class QuickReloadMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("QuickReloadMag", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, MagazineClass magazine)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===QuickReloadMag===");
                    Logger.LogWarning("=============");
                }

                StatCalc.SetMagReloadSpeeds(__instance, magazine, true);
            }
        }
    }


    public class ReloadRevolverDrumPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("ReloadRevolverDrum", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadRevolverDrum===");
                    Logger.LogWarning("=============");
                }

                PlayerProperties.IsAttemptingToReloadInternalMag = true;
                PlayerProperties.IsAttemptingRevolverReload = true;
            }
        }
    }

    public class ReloadWithAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("ReloadWithAmmo", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadWithAmmo===");
                    Logger.LogWarning("=============");
                }

                PlayerProperties.IsAttemptingToReloadInternalMag = true;
            }
        }
    }

    public class ReloadBarrelsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("ReloadBarrels", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadBarrels===");
                    Logger.LogWarning("=============");
                }

                PlayerProperties.IsAttemptingToReloadInternalMag = true;
            }
        }
    }


    public class SetMagTypeNewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetMagTypeNew", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===SetMagTypeNew===");
                Logger.LogWarning("SetMagTypeNew = " + Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.25f));
                Logger.LogWarning("=============");
            }

            __instance.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.25f));
        }
    }

    public class SetMagTypeCurrentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetMagTypeCurrent", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===SetMagTypeCurrent===");
                Logger.LogWarning("SetMagTypeCurrent = " + Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.25f));
                Logger.LogWarning("=============");
            }

            __instance.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti, 0.6f, 1.25f));

        }
    }

    public class SetMagInWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetMagInWeapon", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            if (PlayerProperties.IsMagReloading == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===SetMagInWeapon===");
                    Logger.LogWarning("SetMagInWeapon = " + Mathf.Clamp(WeaponProperties.NewMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * PlayerProperties.GearReloadMulti, 0.6f, 1.25f));
                    Logger.LogWarning("=============");
                }
     
                __instance.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.NewMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * PlayerProperties.GearReloadMulti, 0.6f, 1.25f));
            }
        }
    }

    public class SetSpeedParametersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetSpeedParameters", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===SetSpeedParameters===");
                Logger.LogWarning("=============");
            }
            __instance.SetAnimationSpeed(1);
        }
    }



    public class OnMagInsertedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("method_47", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Player.FirearmController __instance)
        {
            //to find this again, look for private void method_47(){ this.CurrentOperation.OnMagInsertedToWeapon(); }
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===OnMagInsertedPatch/method_47===");
                    Logger.LogWarning("=============");
                }

                PlayerProperties.IsMagReloading = false;
                player.HandsAnimator.SetAnimationSpeed(1);
            }

        }
    }


    /*    public class SetBoltCatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetBoltCatch", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogWarning("SetBoltCatch");
            }
        }
    */

    /*    public class SetFireModePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetFireMode", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("SetFireMode");
                __instance.SetAnimationSpeed(2);
            }
        }*/


    /*    public class SetAnimatorAndProceduralValuesPatch : ModulePatch
{
   protected override MethodBase GetTargetMethod()
   {
       return typeof(Player.FirearmController).GetMethod("SetAnimatorAndProceduralValues", BindingFlags.Instance | BindingFlags.Public);
   }

   [PatchPostfix]
   private static void PatchPostfix(FirearmsAnimator __instance)
   {
       Logger.LogInfo("SetSpeedReload");
   }
}
*/


    /*    public class SetAmmoCompatiblePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetAmmoCompatible", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("SetAmmoCompatible");
            }
        }
    */
    /*    public class SetPatronInWeaponVisiblePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetPatronInWeaponVisible", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("SetPatronInWeaponVisible");
            }
        }

    */

    /*    public class SetAmmoInChamberPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetAmmoInChamber", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("SetAmmoInChamber");
            }
        }
    */
    /*    public class SetChamberIndexWithShellPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetChamberIndexWithShell", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("SetChamberIndexWithShell");
            }
        }
    */
    /*    public class SetShellsInWeapon : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetShellsInWeapon", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("SetShellsInWeapon");
            }
        }*/

    /*    public class ReloadPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("Reload", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("Reload");

            }
        }

        public class SetAmmoOnMagPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("SetAmmoOnMag", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                if (Helper.IsAttemptingToReloadInternalMag == true && Helper.IsInReloadOpertation)
                {
                   *//* __instance.SetAnimationSpeed(5);*//*
                    Logger.LogInfo("SetAmmoOnMag");
                }
            }
        }*/

    /*    public class ResetReloadPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(FirearmsAnimator).GetMethod("ResetReload", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPostfix]
            private static void PatchPostfix(FirearmsAnimator __instance)
            {
                Logger.LogInfo("ResetReload");
            }
        }*/

    /*    public class DischargePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("Discharge", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            Logger.LogInfo("Discharge");
        }
    }*/

    /*    public class SetSpeedReloadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponAnimationSpeedControllerClass).GetMethod("SetSpeedReload", BindingFlags.Static | BindingFlags.Public);

        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            Logger.LogError("=====================================SetSpeedReload===================================");

        }
    }

    public class PullOutMagInInventoryModePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("PullOutMagInInventoryMode", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            Logger.LogWarning("=====================================PullOutMagInInventoryMode===================================");

        }
    }

    public class ResetInsertMagInInventoryModePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("ResetInsertMagInInventoryMode", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            Logger.LogWarning("=====================================ResetInsertMagInInventoryMode===================================");

        }
    }*/

}
