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
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
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
        private Player player;

        //this is fucking curesd: it gets called twice, and both times calcWeightPenalties() will somehow be called after getTotalWeight() and the event that called it.
        //remove event will have calcWeightPenalties be called twice, but the add event will only have it called once despite getTotalWeight being called twice.
        //DO NOT RELY ON SETTING VALUES IN getTotalWeight()! Only set them inside calcWeightPenalties()!
        private void getTotalWeight()
        {
            this.player = Utils.GetPlayer();
            InventoryControllerClass invController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
            this.invClass = invController.Inventory;
            invController.Inventory.TotalWeight = new GClass787<float>(new Func<float>(calcWeightPenalties));
        }

        private float calcWeightPenalties()
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
            PlayerProperties.TotalMousePenalty = (-modifiedWeight / 10f);
            float weaponWeight = player?.HandsController != null && player?.HandsController?.Item != null ? player.HandsController.Item.GetSingleItemTotalWeight() : 1f;
            PlayerProperties.TotalModifiedWeightMinusWeapon = PlayerProperties.TotalModifiedWeight - weaponWeight;

            if (Plugin.EnableMouseSensPenalty.Value)
            {
                player.RemoveMouseSensitivityModifier(Player.EMouseSensitivityModifier.Armor);
                if (PlayerProperties.TotalMousePenalty < 0f)
                {
                    player.AddMouseSensitivityModifier(Player.EMouseSensitivityModifier.Armor, PlayerProperties.TotalMousePenalty / 100f);
                }
            }

            return modifiedWeight;
        }

        private void HandleAddItemEvent(GEventArgs2 args)
        {
            PlayerInitPatch p = new PlayerInitPatch();
            p.getTotalWeight();
        }

        private void HandleRemoveItemEvent(GEventArgs3 args)
        {
            PlayerInitPatch p = new PlayerInitPatch();
            p.getTotalWeight();
        }

        private void RefreshItemEvent(GEventArgs22 args)
        {
            PlayerInitPatch p = new PlayerInitPatch();
            p.getTotalWeight();
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer)
            {
                //for some reason this makes stances NOT reset
                /*              StanceController.StanceBlender.Speed = 1f;
                                StanceController.StanceBlender.Target = 0f;
                                StanceController.StanceTargetPosition = Vector3.zero;
                                StanceController.IsLowReady = false;
                                StanceController.WasLowReady = false;
                                StanceController.IsHighReady = false;
                                StanceController.WasHighReady = false;
                                StanceController.IsActiveAiming = false;
                                StanceController.WasActiveAim = false;
                                StanceController.IsShortStock = false;
                                StanceController.WasShortStock = false;
                                StanceController.IsPatrolStance = false;
                                StanceController.IsMounting = false;
                                StanceController.IsBracing = false;
                                StanceController.DidStanceWiggle = false;

                                Plugin.PlayerSpawnedIn = true;*/

                PlayerInitPatch p = new PlayerInitPatch();
                StatCalc.SetGearParamaters(__instance);
                InventoryControllerClass invController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(__instance);
                invController.AddItemEvent += p.HandleAddItemEvent;
                invController.RemoveItemEvent += p.HandleRemoveItemEvent;
                invController.RefreshItemEvent += p.RefreshItemEvent;
                p.getTotalWeight();
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


    public class BreathProcessPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BreathEffector).GetMethod("Process", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BreathEffector __instance, float deltaTime, float ____breathIntensity, float ____shakeIntensity, float ____breathFrequency, 
        float ____cameraSensetivity, Vector2 ____baseHipRandomAmplitudes, Spring ____recoilRotationSpring, Spring ____handsRotationSpring, AnimationCurve ____lackOfOxygenStrength, GClass2139[] ____processors)
        {
            float amplGain = Mathf.Sqrt(__instance.AmplitudeGain.Value);
            __instance.HipXRandom.Amplitude = Mathf.Clamp(____baseHipRandomAmplitudes.x + amplGain, 0f, 3f);
            __instance.HipZRandom.Amplitude = Mathf.Clamp(____baseHipRandomAmplitudes.y + amplGain, 0f, 3f);
            __instance.HipXRandom.Hardness = (__instance.HipZRandom.Hardness = __instance.Hardness.Value);
            ____shakeIntensity = 1f;
            bool isInjured = __instance.TremorOn || __instance.Fracture;
            float intensityHolder = 1f;

            if (__instance.Physical.HoldingBreath)
            {
                ____breathIntensity = 0.15f;
                ____shakeIntensity = 0.15f;
            }
            else if (Time.time < __instance.StiffUntill)
            {
                float intensity = Mathf.Clamp(-__instance.StiffUntill + Time.time + 1f, isInjured ? 0.5f : 0.3f, 1f);
                ____breathIntensity = intensity * __instance.Intensity;
                ____shakeIntensity = intensity;
                intensityHolder = intensity;
            }
            else
            {
                float t = ____lackOfOxygenStrength.Evaluate(__instance.OxygenLevel);
                float b = __instance.IsAiming ? 0.75f : 1f;
                ____breathIntensity = Mathf.Clamp(Mathf.Lerp(4f, b, t), 1f, 1.5f) * __instance.Intensity;
                ____breathFrequency = Mathf.Clamp(Mathf.Lerp(4f, 1f, t), 1f, 2.5f) * deltaTime;
                ____cameraSensetivity = Mathf.Lerp(2f, 0f, t) * __instance.Intensity;
            }

            GClass786<float> staminaLevel = __instance.StaminaLevel;
            __instance.YRandom.Amplitude = __instance.BreathParams.AmplitudeCurve.Evaluate(staminaLevel);
            float stamFactor = __instance.BreathParams.Delay.Evaluate(staminaLevel);
            __instance.XRandom.MinMaxDelay = (__instance.YRandom.MinMaxDelay = new Vector2(stamFactor / 2f, stamFactor));
            __instance.YRandom.Hardness = __instance.BreathParams.Hardness.Evaluate(staminaLevel);
            float randomY = __instance.YRandom.GetValue(deltaTime);
            float randomX = __instance.XRandom.GetValue(deltaTime);
            ____handsRotationSpring.AddAcceleration(new Vector3(Mathf.Max(0f, -randomY) * (1f - staminaLevel) * 2f, randomY, randomX) * (____shakeIntensity * __instance.Intensity));
            Vector3 breathVector = Vector3.zero;

            if (isInjured)
            {
                float tremorSpeed = __instance.TremorOn ? deltaTime : (deltaTime / 2f);
                tremorSpeed *= intensityHolder;
                float tremorXRandom = __instance.TremorXRandom.GetValue(tremorSpeed);
                float tremorYRandom = __instance.TremorYRandom.GetValue(tremorSpeed);
                float tremorZRnadom = __instance.TremorZRandom.GetValue(tremorSpeed);
                if (__instance.Fracture && !__instance.IsAiming)
                {
                    tremorXRandom += Mathf.Max(0f, randomY) * Mathf.Lerp(1f, 100f / __instance.EnergyFractureLimit, staminaLevel);
                }
                breathVector = new Vector3(tremorXRandom, tremorYRandom, tremorZRnadom) * __instance.Intensity;
            }
            else if (!__instance.IsAiming)
            {
                breathVector = new Vector3(__instance.HipXRandom.GetValue(deltaTime), 0f, __instance.HipZRandom.GetValue(deltaTime)) * (__instance.Intensity * __instance.HipPenalty);
            }

            if (Vector3.SqrMagnitude(breathVector - ____recoilRotationSpring.Zero) > 0.01f)
            {
                ____recoilRotationSpring.Zero = Vector3.Lerp(____recoilRotationSpring.Zero, breathVector, 0.1f);
            }
            else
            {
                ____recoilRotationSpring.Zero = breathVector;
            }
            ____processors[0].ProcessRaw(____breathFrequency, PlayerProperties.TotalBreathIntensity * 0.15f);
            ____processors[1].ProcessRaw(____breathFrequency, PlayerProperties.TotalBreathIntensity * 0.15f * ____cameraSensetivity);
            return false;
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
                PlayerProperties.SprintHipfirePenalty = Mathf.Min(1f + (sprintTimer / 100f), 1.25f);
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
                    __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerProperties.TotalBreathIntensity * mountingSwayBonus;
                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity * mountingSwayBonus;
                }

                if (fc != null)
                {
                    if (RecoilController.IsFiring)
                    {
                        if (Plugin.IsAiming)
                        {
                            __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerProperties.TotalBreathIntensity * mountingSwayBonus * 0.01f;
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity * mountingSwayBonus * 0.01f;
                        }
                        else 
                        {
                            __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerProperties.TotalBreathIntensity * mountingSwayBonus;
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerProperties.TotalHandsIntensity * mountingSwayBonus;
                        }

                        RecoilController.SetRecoilParams(__instance.ProceduralWeaponAnimation, fc.Item);

                        StanceController.IsPatrolStance = false;
                    }

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

/*                __instance.ProceduralWeaponAnimation.HandsContainer.CameraRotation.ReturnSpeed = WeaponProperties.TotalCameraReturnSpeed; //not sure about this one
                __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.ReturnSpeed = Plugin.test1.Value;
*/
                if (!RecoilController.IsFiring)
                {
                    if (StanceController.CanResetDamping)
                    {
                        float resetSpeed = 0.02f;
                        if (PlayerProperties.IsMoving && (StanceController.WasLowReady || StanceController.WasHighReady || StanceController.WasShortStock || StanceController.WasActiveAim))
                        {
                            resetSpeed = 1f;
                        }

                        __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = Mathf.Lerp(__instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping, 0.45f, resetSpeed);
                    }
                    else
                    {
                        __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.75f;
                        __instance.ProceduralWeaponAnimation.Shootingg.ShotVals[3].Intensity = 0;
                        __instance.ProceduralWeaponAnimation.Shootingg.ShotVals[4].Intensity = 0;
                    }
                    __instance.ProceduralWeaponAnimation.HandsContainer.Recoil.ReturnSpeed = Mathf.Lerp(__instance.ProceduralWeaponAnimation.HandsContainer.Recoil.ReturnSpeed, 10f * StanceController.WiggleReturnSpeed, 0.05f);
                    /*                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.5f;
                                        __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.ReturnSpeed = 0.4f;*/
                }
                __instance.HandsController.FirearmsAnimator.SetPatrol(StanceController.IsPatrolStance);
            }

        }
    }
}

