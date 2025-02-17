using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using RealismMod.Weapons;
using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using static EFT.Player;
using ChamberWeaponClass = EFT.Player.FirearmController.GClass1789;
using MagReloadClass = EFT.Player.FirearmController.GClass1773;
using ReloadWeaponClass = EFT.Player.FirearmController.GClass1751;
using StatusStruct = GStruct446<GInterface385>;
using WeaponEventClass = EFT.Player.FirearmController.GClass1741;
using WeaponEventHandlerClass = EFT.Player.FirearmController.GClass1740;
using WeaponStatSubclass = EFT.Player.FirearmController.GClass1784;
using EFT.Animations;

namespace RealismMod
{
    //enable reloading while aiming
    public class TacticalReloadPatch : ModulePatch
    {
        private static FieldInfo _playerField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            return typeof(ProceduralWeaponAnimation).GetMethod("get_TacticalReload", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Patch(ProceduralWeaponAnimation __instance, ref bool __result)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            if (firearmController == null) return false;
            Player player = (Player)_playerField.GetValue(firearmController);
            if (player != null && player.IsYourPlayer) 
            {
                if (StanceController.IsMounting && WeaponStats.BipodIsDeployed && StanceController.BracingDirection == EBracingDirection.Top) 
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

    //part of manual chambering
    public class PreChamberLoadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmController).GetMethod("method_18", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(FirearmController __instance)
        {
            if (__instance.Weapon.HasChambers && __instance.Weapon.Chambers.Length == 1 && __instance.Weapon.ChamberAmmoCount == 0 && !__instance.IsStationaryWeapon)
            {
                Plugin.BlockChambering = true;
            }
        }
    }

    //part of manual chambering
    public class StartEquipWeapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ChamberWeaponClass).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(m =>
            m.GetParameters().Length == 1
            && m.GetParameters()[0].Name == "onWeaponAppear");
        }

        [PatchPrefix]
        private static bool Prefix(ChamberWeaponClass __instance, Action onWeaponAppear)
        {
            var fc = (FirearmController)AccessTools.Field(typeof(ChamberWeaponClass), "firearmController_0").GetValue( __instance);
            var player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(fc);
            if (player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary) 
            {
                if (fc.Weapon.HasChambers && fc.Weapon.Chambers.Length == 1) 
                {
                    var magazine = (MagazineItemClass)AccessTools.Field(typeof(ChamberWeaponClass), "magazineItemClass").GetValue(__instance);
                    var ammoIsCompatible = (bool)AccessTools.Field(typeof(ChamberWeaponClass), "bool_1").GetValue(__instance);
                    var bulletClass = (AmmoItemClass)AccessTools.Field(typeof(ChamberWeaponClass), "ammoItemClass").GetValue(__instance);
                    var weaponManagerClass = (WeaponManagerClass)AccessTools.Field(typeof(ChamberWeaponClass), "weaponManagerClass").GetValue(__instance);

                    AccessTools.Field(typeof(ChamberWeaponClass), "action_0").SetValue(__instance, onWeaponAppear);
                    __instance.Start();
                    fc.FirearmsAnimator.SetActiveParam(true, true);
                    fc.FirearmsAnimator.SetLayerWeight(fc.FirearmsAnimator.LACTIONS_LAYER_INDEX, 0);
                    player.BodyAnimatorCommon.SetFloat(PlayerAnimator.WEAPON_SIZE_MODIFIER_PARAM_HASH, (float)fc.Weapon.CalculateCellSize().X);

                    int chamberAmmoCount = fc.Weapon.ChamberAmmoCount;
                    int currentMagazineCount = fc.Weapon.GetCurrentMagazineCount();

                    magazine = fc.Weapon.GetCurrentMagazine();
                    AccessTools.Field(typeof(ChamberWeaponClass), "magazineItemClass").SetValue(__instance, magazine);
                   
                    fc.AmmoInChamberOnSpawn = chamberAmmoCount;

                    if (fc.Weapon.ChamberAmmoCount == 0)
                    {
                        Plugin.CanLoadChamber = false;
                        Plugin.BlockChambering = true;
                    }
    
                    if (fc.Weapon.HasChambers)
                    {
                        fc.FirearmsAnimator.SetAmmoInChamber((float)chamberAmmoCount);
                    }

                    fc.FirearmsAnimator.SetAmmoOnMag(currentMagazineCount);

                    player.BodyAnimatorCommon.SetFloat(PlayerAnimator.RELOAD_FLOAT_PARAM_HASH, 1f);
                    player.Skills.OnWeaponDraw(fc.Weapon);
                    ammoIsCompatible = magazine == null || magazine.IsAmmoCompatible(fc.Weapon.Chambers);
                    AccessTools.Field(typeof(ChamberWeaponClass), "bool_1").SetValue(__instance, ammoIsCompatible);

                    fc.FirearmsAnimator.SetAmmoCompatible(ammoIsCompatible);
                    if (ammoIsCompatible && magazine != null && magazine.Count > 0 && fc.Weapon.Chambers.Length != 0 && fc.Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
                    {
                        fc.FirearmsAnimator.SetLayerWeight(fc.FirearmsAnimator.MALFUNCTION_LAYER_INDEX, 0);
                    }
                    if (Plugin.CanLoadChamber && magazine != null && chamberAmmoCount == 0 && currentMagazineCount > 0 && ammoIsCompatible && fc.Item.Chambers.Length != 0)
                    {
                        Weapon.EMalfunctionState malfState = fc.Item.MalfState.State;
                        if (malfState == Weapon.EMalfunctionState.Misfire)
                        {
                            fc.Weapon.MalfState.ChangeStateSilent(Weapon.EMalfunctionState.None);
                        }
                        StatusStruct gstruct = magazine.Cartridges.PopTo(player.InventoryController, fc.Item.Chambers[0].CreateItemAddress());
                        fc.Item.MalfState.ChangeStateSilent(malfState);
                        if (gstruct.Value == null)
                        {
                            return false;
                        }
                        weaponManagerClass.RemoveAllShells();
                        player.UpdatePhones();
                        bulletClass = (AmmoItemClass)gstruct.Value.ResultItem;
                        AccessTools.Field(typeof(ChamberWeaponClass), "bulletClass").SetValue(__instance, bulletClass);
                    }
                    return false;
                }
            }
            return true;
        }
    }

    //part of manual chambering
    public class StartReloadPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ReloadWeaponClass).GetMethods(BindingFlags.Public | BindingFlags.Instance).First(m =>
            m.GetParameters().Length == 2
            && m.GetParameters()[0].Name == "reloadExternalMagResult"
            && m.GetParameters()[1].Name == "callback");
        }

        [PatchPrefix]
        private static void Prefix(ReloadWeaponClass __instance, WeaponEventClass reloadExternalMagResult)
        {
            var fc = (FirearmController)AccessTools.Field(typeof(ReloadWeaponClass), "firearmController_0").GetValue(__instance);
            var player = (Player)AccessTools.Field(typeof(FirearmController), "_player").GetValue(fc);

            if (player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                Plugin.CanLoadChamber = true;
                Plugin.BlockChambering = false;
            }
        }
    }

    //part of manual chambering
    public class SetAmmoOnMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetAmmoOnMag", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(FirearmsAnimator __instance, int count)
        {
            Player player = Utils.GetYourPlayer();
            if (player == null || player.MovementContext.CurrentState.Name == EPlayerState.Stationary) return true;
            FirearmController fc = player.HandsController as FirearmController;
            if (player.HandsAnimator == __instance as ObjectInHandsAnimator || fc == null)
            {
                bool blocked = !Plugin.BlockChambering;
                Plugin.BlockChambering = false;
                return blocked;
            }
            return true;

        }
    }

    //part of manual chambering
    public class SetAmmoCompatiblePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetAmmoCompatible", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(FirearmsAnimator __instance, ref bool compatible)
        {
            Player player = Utils.GetYourPlayer();
            if (player == null || player.MovementContext.CurrentState.Name == EPlayerState.Stationary) return;
            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc == null) return;
            if (fc.FirearmsAnimator == __instance)
            {
                if (!Plugin.CanLoadChamber)
                {
                    compatible = false;
                }
            }
        }
    }

    //adds UI showing ammo in chamber when inspecting
    public class ChamberCheckUIPatch : ModulePatch
    {
        private static FieldInfo ammoCountPanelField;
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            ammoCountPanelField = AccessTools.Field(typeof(EftBattleUIScreen), "_ammoCountPanel");
            return typeof(Player.FirearmController).GetMethod("CheckChamber", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer) 
            {
                Slot slot = __instance.Weapon.Chambers.FirstOrDefault<Slot>();
                AmmoItemClass bulletClass = (slot == null) ? null : (slot.ContainedItem as AmmoItemClass);

                if (bulletClass != null)
                {
                    string name = bulletClass.LocalizedName();
                    Singleton<CommonUI>.Instance.EftBattleUIScreen.ShowAmmoDetails(1, 10, 10, name, false);
                }
                else
                {
                    if (__instance.Weapon.Chambers.Length == 1)
                    {
                        Singleton<CommonUI>.Instance.EftBattleUIScreen.ShowAmmoDetails(0, 10, 10, null, false);
                    }
                }
            }
        }
    }


    public class SetSpeedParametersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetSpeedParameters", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            Player player = Utils.GetYourPlayer();
            if (player == null) return;
            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc == null) return;
            if (fc.FirearmsAnimator == __instance)
            {
                __instance.SetAnimationSpeed(1);
                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===SetSpeedParameters===");
                }
            }
        }
    }

    public class SetAnimatorAndProceduralValuesPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("SetAnimatorAndProceduralValues", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                StanceController.ShouldResetStances = true;

                if (PluginConfig.EnableReloadLogging.Value == true)
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
            return typeof(FirearmsAnimator).GetMethod("SetWeaponLevel");
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance, float weaponLevel)
        {
            if (WeaponStats._WeapClass == "shotgun")
            {
                weaponLevel = Mathf.Clamp(weaponLevel + 1, 1, 3);
                WeaponAnimationSpeedControllerClass.SetWeaponLevel(__instance.Animator, weaponLevel);
            }
        }
    }

    public class CheckAmmoPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("CheckAmmo", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                if (Plugin.ServerConfig.reload_changes)
                {
                    float bonus = PluginConfig.GlobalCheckAmmoMulti.Value;
                    if (WeaponStats._WeapClass == "pistol")
                    {
                        bonus = PluginConfig.GlobalCheckAmmoPistolSpeedMulti.Value;
                    }

                    float totalCheckAmmoPatch = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerValues.ReloadSkillMulti * 
                        PlayerValues.ReloadInjuryMulti * StanceController.HighReadyManipBuff * 
                        PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus * bonus,
                        0.7f, 1.35f);
                    __instance.FirearmsAnimator.SetAnimationSpeed(totalCheckAmmoPatch);

                    if (PluginConfig.EnableReloadLogging.Value == true)
                    {
                        Logger.LogWarning("===CheckAmmo===");
                        Logger.LogWarning("Check Ammo =" + totalCheckAmmoPatch);
                        Logger.LogWarning("=============");
                    }
                }

                StanceController.CancelLeftShoulder = true;
                StanceController.CancelLowReady = true;
                StanceController.CancelShortStock = true;
                StanceController.CancelActiveAim = true;
                StanceController.ModifyHighReady = true;
                StanceController.ManipTimer = 0f;
            }
        }
    }

    public class CheckChamberPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("CheckChamber", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player == null) return;
            if (player.IsYourPlayer)
            {
                if (Plugin.ServerConfig.reload_changes)
                {
                    float chamberSpeed = WeaponStats.TotalChamberCheckSpeed;
                    if (WeaponStats._WeapClass == "pistol")
                    {
                        chamberSpeed *= PluginConfig.GlobalCheckChamberPistolSpeedMulti.Value;
                    }
                    else if (WeaponStats._WeapClass == "shotgun")
                    {
                        chamberSpeed *= PluginConfig.GlobalCheckChamberShotgunSpeedMulti.Value;
                    }
                    else
                    {
                        chamberSpeed *= PluginConfig.GlobalCheckChamberSpeedMulti.Value;
                    }

                    float totalCheckChamberSpeed = Mathf.Clamp(chamberSpeed * PlayerValues.FixSkillMulti * PlayerValues.ReloadInjuryMulti * 
                        PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus,
                        0.55f, 1.8f);

                    if (__instance?.FirearmsAnimator != null) __instance.FirearmsAnimator.SetAnimationSpeed(totalCheckChamberSpeed);

                    if (player?.Skills?.WeaponFixAction != null) player.ExecuteSkill(new Action(() => player.Skills.WeaponFixAction.Complete(1f)));
   
                    if (PluginConfig.EnableReloadLogging.Value == true)
                    {
                        Logger.LogWarning("===CheckChamber===");
                        Logger.LogWarning("Check Chamber = " + totalCheckChamberSpeed);
                        Logger.LogWarning("=============");
                    }
                }

                StanceController.CancelLowReady = true;
                StanceController.CancelHighReady = true;
                StanceController.CancelShortStock = true;
                StanceController.CancelLeftShoulder = true;
            }
        }
    }


    public class BoltActionReloadPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("InitiateShot", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void PatchPrefix(Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer && ((WeaponStats._IsManuallyOperated || __instance.Item.IsGrenadeLauncher || __instance.Item.IsUnderBarrelDeviceActive)))
            {
                float chamberSpeed = WeaponStats.TotalFiringChamberSpeed;
                float ammoRec = __instance.Item.CurrentAmmoTemplate.ammoRec;
                float ammoFactor = ammoRec < 0 ? 1f + (ammoRec / 100f) : 1f + (ammoRec / 150f);
                ammoFactor = 2f - ammoFactor;
                float stanceModifier = 1f;
                float maxSpeed = 1.4f;
                float shoulderFactor = StanceController.IsLeftShoulder ? 0.75f : 1f;

                if (WeaponStats._WeapClass == "shotgun")
                {
                    maxSpeed = 1.5f;
                    chamberSpeed *= PluginConfig.GlobalShotgunRackSpeedFactor.Value;
                    stanceModifier = StanceController.IsMounting ? 1.2f : StanceController.IsBracing ? 1.1f : StanceController.CurrentStance == EStance.ActiveAiming ? 1.32f : 1f;
                }
                if (__instance.Item.IsGrenadeLauncher || __instance.Item.IsUnderBarrelDeviceActive)
                {
                    chamberSpeed *= PluginConfig.GlobalUBGLReloadMulti.Value;
                }
                if (WeaponStats._WeapClass == "sniperRifle")
                {
                    chamberSpeed *= PluginConfig.GlobalBoltSpeedMulti.Value;
                    stanceModifier = StanceController.IsMounting && WeaponStats.BipodIsDeployed ? 1.3f : StanceController.IsMounting ? 1.2f : StanceController.IsBracing ? 1.14f : StanceController.CurrentStance == EStance.ActiveAiming ? 1.1f : 1f;
                }
               
                float totalChamberSpeed = Mathf.Clamp(chamberSpeed * PlayerValues.ReloadSkillMulti * PlayerValues.ReloadInjuryMulti * stanceModifier
                    * ammoFactor * PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus * shoulderFactor,
                    0.75f, maxSpeed);
                __instance.FirearmsAnimator.SetAnimationSpeed(totalChamberSpeed);

                if (PluginConfig.EnableReloadLogging.Value == true)
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
            return typeof(FirearmsAnimator).GetMethod("SetMalfRepairSpeed", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(FirearmsAnimator __instance, float fix)
        {
            Player player = Utils.GetYourPlayer();
            if (player == null) return;
            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc == null) return;
            if (fc.FirearmsAnimator == __instance)
            {
                float totalFixSpeed = Mathf.Clamp(fix * WeaponStats.TotalFixSpeed * PlayerValues.ReloadInjuryMulti * 
                    PluginConfig.GlobalFixSpeedMulti.Value * PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus,
                    0.7f, 1.15f);
                WeaponAnimationSpeedControllerClass.SetSpeedFix(__instance.Animator, totalFixSpeed);
                __instance.SetAnimationSpeed(totalFixSpeed);         
                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===SetMalfRepairSpeed===");
                    Logger.LogWarning("SetMalfRepairSpeed = " + totalFixSpeed);
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    public class RechamberPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("Rechamber");
        }

        [PatchPrefix]
        private static void PatchPrefix(FirearmsAnimator __instance)
        {
            Player player = Utils.GetYourPlayer();
            if (player == null) return;
            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc == null) return;
            if (fc.FirearmsAnimator == __instance)
            {
                if (Plugin.ServerConfig.reload_changes)
                {
                    float chamberSpeed = WeaponStats.TotalFixSpeed;
                    if (WeaponStats._WeapClass == "pistol")
                    {
                        chamberSpeed *= PluginConfig.RechamberPistolSpeedMulti.Value;
                    }
                    else
                    {
                        chamberSpeed *= PluginConfig.GlobalRechamberSpeedMulti.Value;
                    }

                    float totalRechamberSpeed = Mathf.Clamp(chamberSpeed * PlayerValues.FixSkillMulti * PlayerValues.ReloadInjuryMulti * 
                        PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus,
                        0.7f, 1.75f);

                    player.ExecuteSkill(new Action(() => player.Skills.WeaponFixAction.Complete(1f)));

                    fc.FirearmsAnimator.SetAnimationSpeed(totalRechamberSpeed);

                    if (PluginConfig.EnableReloadLogging.Value == true)
                    {
                        Logger.LogWarning("===Rechamber===");
                        Logger.LogWarning("Rechamber = " + totalRechamberSpeed);
                        Logger.LogWarning("=============");
                    }
                }

                StanceController.CancelShortStock = true;
                StanceController.CancelLeftShoulder = true;
            }
        }
    }

    public class CanStartReloadPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("CanStartReload", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, bool __result)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (__result == true)
                {
                    if (__instance.Item.GetCurrentMagazine() == null)
                    {
                        PlayerValues.NoCurrentMagazineReload = true;
                    }
                    else
                    {
                        PlayerValues.NoCurrentMagazineReload = false;
                    }
                }
            }
        }
    }

    public class ReloadMagPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("ReloadMag", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, MagazineItemClass magazine)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer && Plugin.ServerConfig.reload_changes)
            {
                ReloadController.SetMagReloadSpeeds(__instance, magazine);

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("ReloadMag Patch");
                    Logger.LogWarning("magazine = " + magazine.LocalizedName());
                    Logger.LogWarning("magazine weight = " + magazine.TotalWeight);
                }
            }
        }
    }


    public class QuickReloadMagPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("QuickReloadMag", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, MagazineItemClass magazine)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer )
            {
                if (Plugin.ServerConfig.reload_changes)
                {
                    ReloadController.SetMagReloadSpeeds(__instance, magazine, true);
                }

                PlayerValues.IsQuickReloading = true;

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===QuickReloadMag===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    public class ReloadCylinderMagazinePatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("ReloadCylinderMagazine", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                PlayerValues.IsAttemptingToReloadInternalMag = true;
                PlayerValues.IsAttemptingRevolverReload = true;

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadCylinderMagazine===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    public class ReloadWithAmmoPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("ReloadWithAmmo", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                PlayerValues.IsAttemptingToReloadInternalMag = true;

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadWithAmmo===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    public class ReloadBarrelsPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("ReloadBarrels", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                PlayerValues.IsAttemptingToReloadInternalMag = true;

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===ReloadBarrels===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    //edge cases
    public class SetMagTypeNewPatch : ModulePatch
    {
        private static FieldInfo _playerField;
        private static FieldInfo faField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(WeaponStatSubclass), "player_0");
            faField = AccessTools.Field(typeof(WeaponStatSubclass), "firearmsAnimator_0");
            return typeof(MagReloadClass).GetMethod("Start", new Type[] { typeof(WeaponEventHandlerClass), typeof(Callback) });
        }

        [PatchPostfix]
        private static void PatchPostfix(MagReloadClass __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                FirearmsAnimator fa = (FirearmsAnimator)faField.GetValue(__instance);

                float totalReloadSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerValues.ReloadSkillMulti * 
                    PlayerValues.ReloadInjuryMulti * StanceController.HighReadyManipBuff * 
                    StanceController.ActiveAimManipBuff * PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus, 
                    ReloadController.MinimumReloadSpeed, ReloadController.MaxReloadSpeed);
                fa.SetAnimationSpeed(totalReloadSpeed);

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===SetMagTypeNew===");
                    Logger.LogWarning("SetMagTypeNew = " + totalReloadSpeed);
                    Logger.LogWarning("=============");
                }
            }

        }
    }

    //reload speed of current mag
    public class SetMagTypeCurrentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetMagTypeCurrent", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            Player player = Utils.GetYourPlayer();
            if (player == null) return;
            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc == null) return;
            if (fc.FirearmsAnimator == __instance)
            {
                float totalReloadSpeed = Mathf.Clamp(WeaponStats.CurrentMagReloadSpeed * PlayerValues.ReloadSkillMulti *
                    PlayerValues.ReloadInjuryMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff *
                    PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus,
                    ReloadController.MinimumReloadSpeed, ReloadController.MaxReloadSpeed);
                __instance.SetAnimationSpeed(totalReloadSpeed);

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===SetMagTypeCurrent===");
                    Logger.LogWarning("SetMagTypeCurrent = " + totalReloadSpeed);
                    Logger.LogWarning("=============");
                }
            }
        }
    }

    //speed of putting mag in weapon
    public class SetMagInWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FirearmsAnimator).GetMethod("SetMagInWeapon", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(FirearmsAnimator __instance)
        {
            if (PlayerValues.IsMagReloading == true)
            {
                float totalReloadSpeed = Mathf.Clamp(WeaponStats.NewMagReloadSpeed * PlayerValues.ReloadSkillMulti *
                    PlayerValues.ReloadInjuryMulti * PlayerValues.GearReloadMulti * StanceController.HighReadyManipBuff *
                    StanceController.ActiveAimManipBuff * PlayerValues.RemainingArmStamReloadFactor * Plugin.RealHealthController.AdrenalineReloadBonus,
                    ReloadController.MinimumReloadSpeed, ReloadController.MaxReloadSpeed);
                __instance.SetAnimationSpeed(totalReloadSpeed);

                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===SetMagInWeapon===");
                    Logger.LogWarning("Total Reload Speed = " + totalReloadSpeed);
                    Logger.LogWarning("ReloadSkillMulti = " + PlayerValues.ReloadSkillMulti);
                    Logger.LogWarning("ReloadInjuryMulti = " + PlayerValues.ReloadInjuryMulti);
                    Logger.LogWarning("GearReloadMulti = " + PlayerValues.GearReloadMulti);
                    Logger.LogWarning("HighReadyManipBuff = " + StanceController.HighReadyManipBuff);
                    Logger.LogWarning("RemainingArmStamPercReload = " + (Mathf.Max(PlayerValues.RemainingArmStamReloadFactor, 0.75f)));
                    Logger.LogWarning("NewMagReloadSpeed = " + WeaponStats.NewMagReloadSpeed);
                    Logger.LogWarning("=============");
                }

            }
        }
    }


    //reset mag reload speed
    public class OnMagInsertedPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("method_50", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Player.FirearmController __instance)
        {
            //to find this again, look for private void method_47(){ this.CurrentOperation.OnMagInsertedToWeapon(); }
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                PlayerValues.IsMagReloading = false;
                PlayerValues.IsQuickReloading = false;
                player.HandsAnimator.SetAnimationSpeed(1);
      
                if (PluginConfig.EnableReloadLogging.Value == true)
                {
                    Logger.LogWarning("===OnMagInsertedPatch/method_50===");
                    Logger.LogWarning("=============");
                }
            }
        }
    }
}
