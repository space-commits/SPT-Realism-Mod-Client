using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using BepInEx.Logging;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace RealismMod
{
    public class SyncWithCharacterSkillsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("SyncWithCharacterSkills", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                SkillsClass.GClass1743 skillsClass = (SkillsClass.GClass1743)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1743_0").GetValue(__instance);
                PlayerProperties.StrengthSkillAimBuff = player.Skills.StrengthBuffAimFatigue.Value;
                PlayerProperties.ReloadSkillMulti = Mathf.Max(1, ((skillsClass.ReloadSpeed - 1f) * 0.5f) + 1f);
                PlayerProperties.FixSkillMulti = skillsClass.FixSpeed;
                PlayerProperties.WeaponSkillErgo = skillsClass.DeltaErgonomics;
                PlayerProperties.AimSkillADSBuff = skillsClass.AimSpeed;
                PlayerProperties.StressResistanceFactor = player.Skills.StressPain.Value;
            }
        }
    }


    public class PlayerInitPatch : ModulePatch
    {
        private InventoryClass invClass;

        private void calcWeight(Player player)
        {
            InventoryControllerClass invController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
            this.invClass = invController.Inventory;
            invController.Inventory.TotalWeight = new GClass787<float>(new Func<float>(getTotalWeight));
            float weaponWeight = player?.HandsController != null && player?.HandsController?.Item != null ? player.HandsController.Item.GetSingleItemTotalWeight() : 1f;
            PlayerProperties.TotalModifiedWeightMinusWeapon = PlayerProperties.TotalModifiedWeight - weaponWeight;
        }

        private float getTotalWeight()
        {
            float modifiedWeight = 0f;
            float trueWeight = 0f;
            foreach (EquipmentSlot equipmentSlot in EquipmentClass.AllSlotNames) 
            {
                IEnumerable<Item> items = this.invClass.Equipment.GetSlot(equipmentSlot).Items;
                foreach (Item item in items) 
                {
                    float itemTotalWeight = item.GetSingleItemTotalWeight();
                    trueWeight += itemTotalWeight;
                    if (equipmentSlot == EquipmentSlot.Backpack || equipmentSlot == EquipmentSlot.TacticalVest)
                    {
                        float modifier = GearProperties.ComfortModifier(item);
                        float containedItemsModifiedWeight = (itemTotalWeight - item.Weight) * modifier;
                        modifiedWeight += item.Weight + containedItemsModifiedWeight;
                    }
                    else 
                    {
                        modifiedWeight += itemTotalWeight;
                    }
                }
            }
            PlayerProperties.TotalModifiedWeight = modifiedWeight;
            PlayerProperties.TotalUnmodifiedWeight = trueWeight;
            return modifiedWeight;
        }

        private void HandleAddItemEvent(GEventArgs2 args)
        {
            Player player = Utils.GetPlayer();
            PlayerInitPatch p = new PlayerInitPatch();
            p.calcWeight(player);
        }

        private void HandleRemoveItemEvent(GEventArgs3 args)
        {
            Player player = Utils.GetPlayer();
            PlayerInitPatch p = new PlayerInitPatch();
            p.calcWeight(player);
        }

        private void RefreshItemEvent(GEventArgs22 args)
        {
            Player player = Utils.GetPlayer();
            PlayerInitPatch p = new PlayerInitPatch();
            p.calcWeight(player);
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer == true)
            {
                PlayerInitPatch p = new PlayerInitPatch();

                StanceController.StanceBlender.Target = 0f;
                StatCalc.SetGearParamaters(__instance);
                StanceController.SelectedStance = 0;
                StanceController.IsLowReady = false;
                StanceController.IsHighReady = false;
                StanceController.IsActiveAiming = false;
                StanceController.WasHighReady = false;
                StanceController.WasLowReady = false;
                StanceController.IsShortStock = false;
                StanceController.WasShortStock = false;
                StanceController.IsPatrolStance = false;

                InventoryControllerClass invController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(__instance);
   
                invController.AddItemEvent += p.HandleAddItemEvent;
                invController.RemoveItemEvent += p.HandleRemoveItemEvent;
                invController.RefreshItemEvent += p.RefreshItemEvent;
            }
        }
    }

    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer == true)
            {
                StatCalc.SetGearParamaters(__instance);
            }
        }
    }

    public class PlayerLateUpdatePatch : ModulePatch
    {

        private static float sprintCooldownTimer = 0f;
        private static bool doSwayReset = false;
        private static float sprintTimer = 0f;
        private static bool didSprintPenalties = false;
        private static bool resetSwayAfterFiring = false;

        private static void doSprintTimer(ProceduralWeaponAnimation pwa, Player.FirearmController fc)
        {
            sprintCooldownTimer += Time.deltaTime;

            if (!didSprintPenalties) 
            {
                float sprintDurationModi = 1f + (sprintTimer / 7f);

                float breathIntensity = Mathf.Min(pwa.Breath.Intensity * sprintDurationModi, 3f);
                float inputIntensitry = Mathf.Min(pwa.HandsContainer.HandsRotation.InputIntensity * sprintDurationModi, 1.05f);
                pwa.Breath.Intensity = breathIntensity;
                pwa.HandsContainer.HandsRotation.InputIntensity = inputIntensitry;
                PlayerProperties.SprintTotalBreathIntensity = breathIntensity;
                PlayerProperties.SprintTotalHandsIntensity = inputIntensitry;
                PlayerProperties.SprintHipfirePenalty = Mathf.Min(1f + (sprintTimer / 100f), 1.2f);

                PlayerProperties.ADSSprintMulti = Mathf.Max(1f - (sprintTimer / 12f), 0.3f);

                didSprintPenalties = true;
                doSwayReset = false;
            }

            if (sprintCooldownTimer >= 0.35f)
            {
                PlayerProperties.SprintBlockADS = false;
                if (PlayerProperties.TriedToADSFromSprint)
                {
                    fc.ToggleAim();
                }
            }
            if (sprintCooldownTimer >= 4f)
            {
                PlayerProperties.WasSprinting = false;
                doSwayReset = true;
                sprintCooldownTimer = 0f;
                sprintTimer = 0f;
            }
        }

        private static void resetSwayParams(ProceduralWeaponAnimation pwa, float mountingBonus) 
        {
            float resetSwaySpeed = 0.05f;
            float resetSpeed = 0.5f;
            PlayerProperties.SprintTotalBreathIntensity = Mathf.Lerp(PlayerProperties.SprintTotalBreathIntensity, PlayerProperties.TotalBreathIntensity, resetSwaySpeed);
            PlayerProperties.SprintTotalHandsIntensity = Mathf.Lerp(PlayerProperties.SprintTotalHandsIntensity, PlayerProperties.TotalHandsIntensity, resetSwaySpeed);
            PlayerProperties.ADSSprintMulti = Mathf.Lerp(PlayerProperties.ADSSprintMulti, 1f, resetSpeed);
            PlayerProperties.SprintHipfirePenalty = Mathf.Lerp(PlayerProperties.SprintHipfirePenalty, 1f, resetSpeed);

            if (!RecoilController.IsFiring) 
            {
                pwa.Breath.Intensity = PlayerProperties.SprintTotalBreathIntensity * mountingBonus;
                pwa.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.SprintTotalHandsIntensity * mountingBonus;
            }

            if (Utils.AreFloatsEqual(1f, PlayerProperties.ADSSprintMulti) && Utils.AreFloatsEqual(pwa.Breath.Intensity, PlayerProperties.TotalBreathIntensity) && Utils.AreFloatsEqual(pwa.HandsContainer.HandsRotation.InputIntensity, PlayerProperties.TotalHandsIntensity))
            {
                doSwayReset = false;
            }
        }

        private static void DoSprintPenalty(Player player, Player.FirearmController fc, float mountingBonus) 
        {
            if (player.IsSprintEnabled)
            {
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= 1f)
                {
                    PlayerProperties.SprintBlockADS = true;
                    PlayerProperties.WasSprinting = true;
                    didSprintPenalties = false;
                }
            }
            else
            {
                if (PlayerProperties.WasSprinting)
                {
                    doSprintTimer(player.ProceduralWeaponAnimation, fc);
                }
                if (doSwayReset)
                {
                    resetSwayParams(player.ProceduralWeaponAnimation, mountingBonus);
                }
            }

            if (!doSwayReset && !PlayerProperties.WasSprinting)
            {
                PlayerProperties.HasFullyResetSprintADSPenalties = true;
            }
            else
            {
                PlayerProperties.HasFullyResetSprintADSPenalties = false;
            }

            if (RecoilController.IsFiring)
            {
                doSwayReset = false;
                resetSwayAfterFiring = false;
            }
            else if (!resetSwayAfterFiring)
            {
                resetSwayAfterFiring = true;
                doSwayReset = true;
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (Utils.IsReady && __instance.IsYourPlayer)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;

                PlayerProperties.IsSprinting = __instance.IsSprintEnabled;
                PlayerProperties.EnviroType = __instance.Environment;
                Plugin.IsInInventory = __instance.IsInventoryOpened;
                float mountingSwayBonus = StanceController.IsMounting ? StanceController.MountingSwayBonus : StanceController.BracingSwayBonus;
                PlayerProperties.IsMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

                if (Plugin.EnableSprintPenalty.Value) 
                {
                    DoSprintPenalty(__instance, fc, mountingSwayBonus);
                }
                if (!RecoilController.IsFiring && PlayerProperties.HasFullyResetSprintADSPenalties)
                {
                    __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerProperties.TotalBreathIntensity * mountingSwayBonus; //both aim sway and up and down breathing
                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity * mountingSwayBonus; //also breathing and sway but different, the hands doing sway motion but camera bobbing up and down. 
                }

                if (fc != null)
                {
                    if (RecoilController.IsFiring)
                    {
                        if (PlayerProperties.IsMoving)
                        {
                            __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerProperties.TotalBreathIntensity * mountingSwayBonus * 0.01f;
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity * mountingSwayBonus * 0.01f; // CHECK IF THIS AFFECTS RECOIL
                        }
                        else 
                        {
                            __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerProperties.TotalBreathIntensity * mountingSwayBonus * 0.25f;
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity * mountingSwayBonus * 0.25f; // CHECK IF THIS AFFECTS RECOIL
                        }

                        RecoilController.SetRecoilParams(__instance.ProceduralWeaponAnimation, fc.Item);

                        StanceController.IsPatrolStance = false;
                        __instance.HandsController.FirearmsAnimator.SetPatrol(false);
                    }

                    __instance.ProceduralWeaponAnimation.Shootingg.Intensity = (Plugin.IsInThirdPerson && !Plugin.IsAiming ? Plugin.RecoilIntensity.Value * 5f : Plugin.RecoilIntensity.Value);
                    ReloadController.ReloadStateCheck(__instance, fc, Logger);
                    AimController.ADSCheck(__instance, fc, Logger);

                    if (Plugin.EnableStanceStamChanges.Value)
                    {
                        StanceController.SetStanceStamina(__instance, fc);
                    }

                    float remainStamPercent = __instance.Physical.HandsStamina.Current / __instance.Physical.HandsStamina.TotalCapacity;
                    PlayerProperties.RemainingArmStamPercentage = 1f - ((1f - remainStamPercent) / 3f);
                }
                else if (Plugin.EnableStanceStamChanges.Value)
                {
                    StanceController.ResetStanceStamina(__instance);
                }

                __instance.Physical.HandsStamina.Current = Mathf.Max(__instance.Physical.HandsStamina.Current, 1f);

     /*           __instance.ProceduralWeaponAnimation.HandsContainer.CameraRotation.ReturnSpeed = WeaponProperties.TotalCameraReturnSpeed;*/ //not sure about this one
                /*__instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.ReturnSpeed = Plugin.test1.Value;*/

                if (!RecoilController.IsFiring)
                {
                    if (StanceController.CanResetDamping)
                    {
                        if (Plugin.IsAiming)
                        {
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = Mathf.Lerp(__instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping, Mathf.Clamp(0.45f * (1f + (WeaponProperties.ErgoFactor / 100f)), 0.4f, 0.6f), 0.1f);
                        }
                        else
                        {
                            float resetSpeed = 0.02f;
                            if (PlayerProperties.IsMoving && (StanceController.WasLowReady || StanceController.WasHighReady || StanceController.WasShortStock || StanceController.WasActiveAim))
                            {
                                resetSpeed = 1f;
                            }
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.ReturnSpeed = Mathf.Lerp(__instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.ReturnSpeed, 0.1f, resetSpeed);
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = Mathf.Lerp(__instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping, 0.45f, resetSpeed);
                        }

                    }
                    else
                    {
                        __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.75f;
                        __instance.ProceduralWeaponAnimation.Shootingg.ShotVals[3].Intensity = 0;
                        __instance.ProceduralWeaponAnimation.Shootingg.ShotVals[4].Intensity = 0;
                    }

                    __instance.ProceduralWeaponAnimation.HandsContainer.Recoil.ReturnSpeed = Mathf.Lerp(__instance.ProceduralWeaponAnimation.HandsContainer.Recoil.ReturnSpeed, 10f * StanceController.WiggleReturnSpeed, 0.05f);

      /*              __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.5f;
                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.ReturnSpeed = 0.4f;*/
                }
            }
        }
    }
}

