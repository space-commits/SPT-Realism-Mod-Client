using Aki.Reflection.Patching;
using BepInEx.Logging;
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
    public static class ReloadController
    {
        public static void ReloadStateCheck(Player player, EFT.Player.FirearmController fc, ManualLogSource logger)
        {
            PlayerProperties.IsInReloadOpertation = fc.IsInReloadOperation();

            if (PlayerProperties.IsInReloadOpertation)
            {
                StanceController.IsPatrolStance = false;
                StanceController.CancelShortStock = true;
                StanceController.CancelPistolStance = true;
                StanceController.CancelActiveAim = true;

                if (PlayerProperties.IsAttemptingToReloadInternalMag == true)
                {
                    StanceController.CancelHighReady = fc.Item.WeapClass != "shotgun" ? true : false;
                    StanceController.CancelLowReady = fc.Item.WeapClass == "shotgun" || fc.Item.WeapClass == "pistol" ? true : false;

                    float highReadyBonus = fc.Item.WeapClass == "shotgun" && StanceController.IsHighReady == true ? StanceController.HighReadyManipBuff : 1f;
                    float lowReadyBonus = fc.Item.WeapClass != "shotgun" && StanceController.IsLowReady == true ? StanceController.LowReadyManipBuff : 1f;
         
                    float IntenralMagReloadSpeed = Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * Plugin.InternalMagReloadMulti.Value * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * highReadyBonus * lowReadyBonus * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.55f, 1.4f);
                    player.HandsAnimator.SetAnimationSpeed(IntenralMagReloadSpeed);

                    if (Plugin.EnableLogging.Value == true)
                    {
                        logger.LogWarning("IsAttemptingToReloadInternalMag = " + IntenralMagReloadSpeed);
                    }
                }
            }
            else
            {
                PlayerProperties.IsAttemptingToReloadInternalMag = false;
                PlayerProperties.IsAttemptingRevolverReload = false;
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
            __instance.SetAnimationSpeed(1);

            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===SetSpeedParameters===");
                Logger.LogWarning("=============");
            }
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
                StanceController.DoResetStances = true;

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("SetAnimatorAndProceduralValues");
                }
            }
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

    public class CheckAmmoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("CheckAmmo", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer)
            {
                if (Plugin.EnableReloadPatches.Value) 
                {
                    float bonus = Plugin.GlobalCheckAmmoMulti.Value;
                    if (WeaponProperties._WeapClass == "pistol")
                    {
                        bonus = Plugin.GlobalCheckAmmoPistolSpeedMulti.Value;
                    }

                    float totalCheckAmmoPatch = Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * StanceController.HighReadyManipBuff * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)) * bonus, 0.6f, 1.3f);
                    __instance.FirearmsAnimator.SetAnimationSpeed(totalCheckAmmoPatch);

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("===CheckAmmo===");
                        Logger.LogWarning("Check Ammo =" + totalCheckAmmoPatch);
                        Logger.LogWarning("=============");
                    }
                }

                StanceController.CancelLowReady = true;
                StanceController.CancelShortStock = true;
                StanceController.CancelActiveAim = true;
                /*StanceController.CancelPistolStance = true;*/
            }
        }
    }

    public class CheckChamberPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("CheckChamber", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer)
            {
                if (Plugin.EnableReloadPatches.Value)
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

                    float totalCheckChamberSpeed = Mathf.Clamp(chamberSpeed * PlayerProperties.FixSkillMulti * PlayerProperties.ReloadInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.55f, 1.8f);
                    __instance.FirearmsAnimator.SetAnimationSpeed(totalCheckChamberSpeed);

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("===CheckChamber===");
                        Logger.LogWarning("Check Chamber = " + totalCheckChamberSpeed);
                        Logger.LogWarning("=============");
                    }
                }

                StanceController.CancelLowReady = true;
                StanceController.CancelHighReady = true;
                StanceController.CancelShortStock = true;
                StanceController.CancelActiveAim = true;
            }
        }
    }


    public class BoltActionReloadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("InitiateShot", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static void PatchPrefix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer && ((WeaponProperties._IsManuallyOperated || __instance.Item.IsGrenadeLauncher || __instance.Item.IsUnderBarrelDeviceActive)))
            {
                float chamberSpeed = WeaponProperties.TotalFiringChamberSpeed;
                float ammoRec = __instance.Item.CurrentAmmoTemplate.ammoRec;
                float ammoFactor = ammoRec < 0 ? 1f + (ammoRec / 100f) : 1f + (ammoRec / 150f);
                ammoFactor = 2f - ammoFactor;
                float stanceModifier = 1f;

                if (WeaponProperties._WeapClass == "shotgun")
                {
                    chamberSpeed *= Plugin.GlobalShotgunRackSpeedFactor.Value;
                    stanceModifier = StanceController.IsBracing ? 1.1f : StanceController.IsMounting ? 1.2f : StanceController.IsActiveAiming ? 1.35f : 1f;
                }
                if (__instance.Item.IsGrenadeLauncher || __instance.Item.IsUnderBarrelDeviceActive)
                {
                    chamberSpeed *= Plugin.GlobalUBGLReloadMulti.Value;
                }
                if (WeaponProperties._WeapClass == "sniperRifle")
                {
                    chamberSpeed *= Plugin.GlobalBoltSpeedMulti.Value;
                    stanceModifier = StanceController.IsBracing ? 1.2f : StanceController.IsMounting ? 1.4f : StanceController.IsActiveAiming ? 1.15f : 1f;
                }
                float totalChamberSpeed = Mathf.Clamp(chamberSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * stanceModifier * ammoFactor * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.55f, 1.3f);
                __instance.FirearmsAnimator.SetAnimationSpeed(totalChamberSpeed);

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===SetBoltActionReload===");
                    Logger.LogWarning("Set Bolt Action Reload = " + totalChamberSpeed);
                    Logger.LogWarning("=============");
                }
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
            float totalFixSpeed = Mathf.Clamp(fix * WeaponProperties.TotalFixSpeed * PlayerProperties.ReloadInjuryMulti * Plugin.GlobalFixSpeedMulti.Value * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.55f, 1.15f);
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

    public class RechamberPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController.GClass1510).GetMethod("Start", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController.GClass1510 __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController.GClass1472), "player_0").GetValue(__instance);
            if (player.IsYourPlayer)
            {
                if (Plugin.EnableReloadPatches.Value)
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

                    float totalRechamberSpeed = Mathf.Clamp(chamberSpeed * PlayerProperties.FixSkillMulti * PlayerProperties.ReloadInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.5f, 1.5f);

                    FirearmsAnimator fa = (FirearmsAnimator)AccessTools.Field(typeof(Player.FirearmController.GClass1472), "firearmsAnimator_0").GetValue(__instance);
                    fa.SetAnimationSpeed(totalRechamberSpeed);

                    if (Plugin.EnableLogging.Value == true)
                    {
                        Logger.LogWarning("===Rechamber===");
                        Logger.LogWarning("Rechamber = " + totalRechamberSpeed);
                        Logger.LogWarning("=============");
                    }
                }

                StanceController.CancelShortStock = true;
                StanceController.CancelPistolStance = true;
            }
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
                        PlayerProperties.NoCurrentMagazineReload = true;
                    }
                    else
                    {
                        PlayerProperties.NoCurrentMagazineReload = false;
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
                StatCalc.SetMagReloadSpeeds(__instance, magazine);

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("ReloadMag Patch");
                    Logger.LogWarning("magazine = " + magazine.LocalizedName());
                    Logger.LogWarning("magazine weight = " + magazine.GetSingleItemTotalWeight());
                }
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
                StatCalc.SetMagReloadSpeeds(__instance, magazine, true);
                PlayerProperties.IsQuickReloading = true;

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===QuickReloadMag===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    public class ReloadCylinderMagazinePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("ReloadCylinderMagazine", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                PlayerProperties.IsAttemptingToReloadInternalMag = true;
                PlayerProperties.IsAttemptingRevolverReload = true;

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadCylinderMagazine===");
                    Logger.LogWarning("=============");
                }
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
                PlayerProperties.IsAttemptingToReloadInternalMag = true;

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadWithAmmo===");
                    Logger.LogWarning("=============");
                }
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
                PlayerProperties.IsAttemptingToReloadInternalMag = true;

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadBarrels===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }


    public class SetMagTypeNewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController.GClass1498).GetMethod("Start", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController.GClass1498 __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController.GClass1472), "player_0").GetValue(__instance);
            if (player.IsYourPlayer) 
            {
                FirearmsAnimator fa = (FirearmsAnimator)AccessTools.Field(typeof(Player.FirearmController.GClass1472), "firearmsAnimator_0").GetValue(__instance);

                float totalReloadSpeed = Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.5f, 1.3f);
                fa.SetAnimationSpeed(totalReloadSpeed);

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===SetMagTypeNew===");
                    Logger.LogWarning("SetMagTypeNew = " + totalReloadSpeed);
                    Logger.LogWarning("=============");
                }
            }

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
            float totalReloadSpeed = Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.5f, 1.3f);
            __instance.SetAnimationSpeed(totalReloadSpeed);

            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("===SetMagTypeCurrent===");
                Logger.LogWarning("SetMagTypeCurrent = " + totalReloadSpeed);
                Logger.LogWarning("=============");
            }

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
                float totalReloadSpeed = Mathf.Clamp(WeaponProperties.NewMagReloadSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * PlayerProperties.GearReloadMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.5f, 1.3f);
                __instance.SetAnimationSpeed(totalReloadSpeed);

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===SetMagInWeapon===");
                    Logger.LogWarning("SetMagInWeapon = " + totalReloadSpeed);
                    Logger.LogWarning("ReloadSkillMulti = " + PlayerProperties.ReloadSkillMulti);
                    Logger.LogWarning("ReloadInjuryMulti = " + PlayerProperties.ReloadInjuryMulti);
                    Logger.LogWarning("GearReloadMulti = " + PlayerProperties.GearReloadMulti);
                    Logger.LogWarning("HighReadyManipBuff = " + StanceController.HighReadyManipBuff);
                    Logger.LogWarning("RemainingArmStamPercentage = " + (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)));
                    Logger.LogWarning("NewMagReloadSpeed = " + WeaponProperties.NewMagReloadSpeed);
                    Logger.LogWarning("=============");
                }
     
            }
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
                PlayerProperties.IsMagReloading = false;
                PlayerProperties.IsQuickReloading = false;
                player.HandsAnimator.SetAnimationSpeed(1);

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===OnMagInsertedPatch/method_47===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    /*    public class SetHammerArmedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetHammerArmed", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            if (RecoilController.IsFiring != true && PlayerProperties.IsInReloadOpertation)
            {
                float hammerSpeed = Mathf.Clamp(WeaponProperties.TotalChamberSpeed * PlayerProperties.ReloadSkillMulti * PlayerProperties.ReloadInjuryMulti * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.75f)), 0.4f, 2f) * Plugin.GlobalArmHammerSpeedMulti.Value;
                __instance.SetAnimationSpeed(hammerSpeed);
                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("SetHammerArmed = " + hammerSpeed);
                }
            }
        }
    }*/

}
