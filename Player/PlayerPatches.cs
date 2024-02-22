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
using WeaponSkills = EFT.SkillManager.GClass1768;
using StaminaLevelClass = GClass750<float>;
using WeightClass = GClass751<float>;
using Comfort.Common;
using ProcessorClass = GClass2210;

namespace RealismMod
{
    public class SyncWithCharacterSkillsPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("SyncWithCharacterSkills", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(EFT.Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                WeaponSkills weaponInfo = player.Skills.GetWeaponInfo(__instance.Item);
                PlayerStats.StrengthSkillAimBuff = player.Skills.StrengthBuffAimFatigue.Value;
                PlayerStats.ReloadSkillMulti = Mathf.Max(1, ((weaponInfo.ReloadSpeed - 1f) * 0.5f) + 1f);
                PlayerStats.FixSkillMulti = weaponInfo.FixSpeed;
                PlayerStats.WeaponSkillErgo = weaponInfo.DeltaErgonomics;
                PlayerStats.AimSkillADSBuff = weaponInfo.AimSpeed;
                PlayerStats.StressResistanceFactor = player.Skills.StressPain.Value;
            }
        }
    }

    public class TotalWeightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Inventory).GetMethod("method_0", BindingFlags.Instance | BindingFlags.Public);
        }

        private static float getTotalWeight(Inventory invClass)
        {
            float modifiedWeight = 0f;
            float trueWeight = 0f;
            foreach (EquipmentSlot equipmentSlot in EquipmentClass.AllSlotNames)
            {
                Slot slot = invClass.Equipment.GetSlot(equipmentSlot);
                IEnumerable<Item> items = slot.Items;
                foreach (Item item in items)
                {
                    float itemTotalWeight = item.GetSingleItemTotalWeight();
                    trueWeight += itemTotalWeight;
                    if (equipmentSlot == EquipmentSlot.Backpack || equipmentSlot == EquipmentSlot.TacticalVest || equipmentSlot == EquipmentSlot.ArmorVest || equipmentSlot == EquipmentSlot.Headwear)
                    {
                        modifiedWeight += itemTotalWeight * GearStats.ComfortModifier(item);
                    }
                    else
                    {
                        modifiedWeight += itemTotalWeight;
                    }
                }
            }

            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("Total Modified Weight " + modifiedWeight);
                Logger.LogWarning("Total Unmodified Weight " + trueWeight);
            }

            return modifiedWeight;
        }

        [PatchPrefix]
        private static bool PatchPrefix(Inventory __instance, ref float __result)
        {
            __result = getTotalWeight(__instance);
            return false;
        }
    }

    public class PlayerInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer)
            {
                StatCalc.CalcPlayerWeightStats(__instance);
                StatCalc.SetGearParamaters(__instance);
            }
        }
    }

    public class OnItemAddedOrRemovedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnItemAddedOrRemoved", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {

            if (__instance.IsYourPlayer)
            {
                StatCalc.CalcPlayerWeightStats(__instance);
                StatCalc.SetGearParamaters(__instance);
            }
        }
    }


    public class BreathProcessPatch : ModulePatch
    {
        private static FieldInfo breathIntensityField;
        private static FieldInfo shakeIntensityField;
        private static FieldInfo breathFrequencyField;
        private static FieldInfo cameraSensetivityField;
        private static FieldInfo baseHipRandomAmplitudesField; 
        private static FieldInfo shotEffectorField;
        private static FieldInfo handsRotationSpringField;
        private static FieldInfo processorsField;
        private static FieldInfo lackOfOxygenStrengthField;

        protected override MethodBase GetTargetMethod()
        {
            breathIntensityField = AccessTools.Field(typeof(BreathEffector), "_breathIntensity");
            shakeIntensityField = AccessTools.Field(typeof(BreathEffector), "_shakeIntensity");
            breathFrequencyField = AccessTools.Field(typeof(BreathEffector), "_breathFrequency");
            cameraSensetivityField = AccessTools.Field(typeof(BreathEffector), "_cameraSensetivity");
            baseHipRandomAmplitudesField = AccessTools.Field(typeof(BreathEffector), "_baseHipRandomAmplitudes");
            shotEffectorField = AccessTools.Field(typeof(BreathEffector), "_shotEffector");
            handsRotationSpringField = AccessTools.Field(typeof(BreathEffector), "_handsRotationSpring");
            lackOfOxygenStrengthField = AccessTools.Field(typeof(BreathEffector), "_lackOfOxygenStrength");
            processorsField = AccessTools.Field(typeof(BreathEffector), "_processors");

            return typeof(BreathEffector).GetMethod("Process", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BreathEffector __instance, float deltaTime)
        {
           float breathFrequency = (float)AccessTools.Field(typeof(BreathEffector), "_breathFrequency").GetValue(__instance);
           float cameraSensetivity = (float)AccessTools.Field(typeof(BreathEffector), "_cameraSensetivity").GetValue(__instance);
           Vector2 baseHipRandomAmplitudes = (Vector2)AccessTools.Field(typeof(BreathEffector), "_baseHipRandomAmplitudes").GetValue(__instance);
           ShotEffector  shotEffector = (ShotEffector)AccessTools.Field(typeof(BreathEffector), "_shotEffector").GetValue(__instance);
           Spring handsRotationSpring = (Spring)AccessTools.Field(typeof(BreathEffector), "_handsRotationSpring").GetValue(__instance);
           AnimationCurve lackOfOxygenStrength = (AnimationCurve)AccessTools.Field(typeof(BreathEffector), "_lackOfOxygenStrength").GetValue(__instance);
           ProcessorClass[] processors = (ProcessorClass[])AccessTools.Field(typeof(BreathEffector), "_processors").GetValue(__instance);

            float amplGain = Mathf.Sqrt(__instance.AmplitudeGain.Value);
            __instance.HipXRandom.Amplitude = Mathf.Clamp(baseHipRandomAmplitudes.x + amplGain, 0f, 3f);
            __instance.HipZRandom.Amplitude = Mathf.Clamp(baseHipRandomAmplitudes.y + amplGain, 0f, 3f);
            __instance.HipXRandom.Hardness = (__instance.HipZRandom.Hardness = __instance.Hardness.Value);
            shakeIntensityField.SetValue(__instance, 1f);
            bool isInjured = __instance.TremorOn || __instance.Fracture || Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed;
            float intensityHolder = 1f;

            if (Time.time < __instance.StiffUntill)
            {
                float intensity = Mathf.Clamp(-__instance.StiffUntill + Time.time + 1f, isInjured ? 0.75f : 0.3f, 1f);
                breathIntensityField.SetValue(__instance, intensity * __instance.Intensity);
                shakeIntensityField.SetValue(__instance, intensity);
                intensityHolder = intensity;
            }
            else
            {
                float holdBreathBonus = __instance.Physical.HoldingBreath ? 0.55f : 1f;
                float t = lackOfOxygenStrength.Evaluate(__instance.OxygenLevel);
                float b = __instance.IsAiming ? 0.75f : 1f;
                breathIntensityField.SetValue(__instance, Mathf.Clamp(Mathf.Lerp(4f, b, t), 1f, 1.5f) * __instance.Intensity * holdBreathBonus);
                breathFrequencyField.SetValue(__instance, Mathf.Clamp(Mathf.Lerp(4f, 1f, t), 1f, 2.5f) * deltaTime * holdBreathBonus);
                shakeIntensityField.SetValue(__instance, holdBreathBonus);
                cameraSensetivityField.SetValue(__instance, Mathf.Lerp(2f, 0f, t) * __instance.Intensity);
                breathFrequency = (float)AccessTools.Field(typeof(BreathEffector), "_breathFrequency").GetValue(__instance);
                cameraSensetivity = (float)AccessTools.Field(typeof(BreathEffector), "_cameraSensetivity").GetValue(__instance);
            }

            StaminaLevelClass staminaLevel = __instance.StaminaLevel;
            __instance.YRandom.Amplitude = __instance.BreathParams.AmplitudeCurve.Evaluate(staminaLevel);
            float stamFactor = __instance.BreathParams.Delay.Evaluate(staminaLevel);
            __instance.XRandom.MinMaxDelay = (__instance.YRandom.MinMaxDelay = new Vector2(stamFactor / 2f, stamFactor));
            __instance.YRandom.Hardness = __instance.BreathParams.Hardness.Evaluate(staminaLevel);
            float randomY = __instance.YRandom.GetValue(deltaTime);
            float randomX = __instance.XRandom.GetValue(deltaTime);
            handsRotationSpring.AddAcceleration(new Vector3(Mathf.Max(0f, -randomY) * (1f - staminaLevel) * 2f, randomY, randomX) * ((float)shakeIntensityField.GetValue(__instance) * __instance.Intensity));
            Vector3 breathVector = Vector3.zero;
          
            if (isInjured)
            {
                float tremorSpeed = __instance.TremorOn ? deltaTime : (deltaTime / 2f);
                tremorSpeed *= intensityHolder;
                float tremorXRandom = __instance.TremorXRandom.GetValue(tremorSpeed);
                float tremorYRandom = __instance.TremorYRandom.GetValue(tremorSpeed);
                float tremorZRnadom = __instance.TremorZRandom.GetValue(tremorSpeed);
                if ((__instance.Fracture || Plugin.RealHealthController.ArmsAreIncapacitated || Plugin.RealHealthController.HasOverdosed) && !__instance.IsAiming)
                {
                    tremorXRandom += Mathf.Max(0f, randomY) * Mathf.Lerp(1f, 100f / __instance.EnergyFractureLimit, staminaLevel);
                }
                breathVector = new Vector3(tremorXRandom, tremorYRandom, tremorZRnadom) * __instance.Intensity;
            }
            else if (!__instance.IsAiming && RecoilController.IsFiring)
            {
                breathVector = new Vector3(__instance.HipXRandom.GetValue(deltaTime), 0f, __instance.HipZRandom.GetValue(deltaTime)) * (__instance.Intensity * __instance.HipPenalty);
            }

            if (Vector3.SqrMagnitude(breathVector - shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset) > 0.01f)
            {
                shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset = Vector3.Lerp(shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset, breathVector, 0.1f);
            }
            else
            {
                shotEffector.CurrentRecoilEffect.HandRotationRecoilEffect.Offset = breathVector;
            }

            float breathFactor = StanceController.BlockBreathEffect ? 0f : StanceController.MountingBreathReduction;
            processors[0].ProcessRaw(breathFrequency, (float)breathIntensityField.GetValue(__instance) * breathFactor * 0.7f); 
            processors[1].ProcessRaw(breathFrequency, (float)breathIntensityField.GetValue(__instance) * cameraSensetivity * breathFactor * 0.7f);
            return false;
        }
    }

    public class PlayerLateUpdatePatch : ModulePatch
    {
        private static FieldInfo surfaceField;

        private static float sprintCooldownTimer = 0f;
        private static bool doSwayReset = false;
        private static float sprintTimer = 0f;
        private static bool didSprintPenalties = false;
        private static bool resetSwayAfterFiring = false;

        private static void doSprintTimer(ProceduralWeaponAnimation pwa, Player.FirearmController fc, float mountingBonus)
        {
            sprintCooldownTimer += Time.deltaTime;

            if (!didSprintPenalties)
            {
                float sprintDurationModi = 1f + (sprintTimer / 7f);

                float breathIntensity = Mathf.Min(pwa.Breath.Intensity * sprintDurationModi, 3f);
                float inputIntensitry = Mathf.Min(pwa.HandsContainer.HandsRotation.InputIntensity * sprintDurationModi, 1.05f);
                pwa.Breath.Intensity = breathIntensity * mountingBonus;
                pwa.HandsContainer.HandsRotation.InputIntensity = inputIntensitry * mountingBonus;
                PlayerStats.SprintTotalBreathIntensity = breathIntensity;
                PlayerStats.SprintTotalHandsIntensity = inputIntensitry;
                PlayerStats.SprintHipfirePenalty = Mathf.Min(1f + (sprintTimer / 100f), 1.25f);
                PlayerStats.ADSSprintMulti = Mathf.Max(1f - (sprintTimer / 12f), 0.3f);


                didSprintPenalties = true;
                doSwayReset = false;
            }

            if (sprintCooldownTimer >= 0.35f)
            {
                PlayerStats.SprintBlockADS = false;
                if (PlayerStats.TriedToADSFromSprint)
                {
                    fc.ToggleAim();
                }
            }
            if (sprintCooldownTimer >= 4f)
            {
                PlayerStats.WasSprinting = false;
                doSwayReset = true;
                sprintCooldownTimer = 0f;
                sprintTimer = 0f;
            }
        }

        private static void resetSwayParams(ProceduralWeaponAnimation pwa, float mountingBonus)
        {
            float resetSwaySpeed = 0.035f;
            float resetSpeed = 0.4f;
            PlayerStats.SprintTotalBreathIntensity = Mathf.Lerp(PlayerStats.SprintTotalBreathIntensity, PlayerStats.TotalBreathIntensity, resetSwaySpeed);
            PlayerStats.SprintTotalHandsIntensity = Mathf.Lerp(PlayerStats.SprintTotalHandsIntensity, PlayerStats.TotalHandsIntensity, resetSwaySpeed);
            PlayerStats.ADSSprintMulti = Mathf.Lerp(PlayerStats.ADSSprintMulti, 1f, resetSpeed);
            PlayerStats.SprintHipfirePenalty = Mathf.Lerp(PlayerStats.SprintHipfirePenalty, 1f, resetSpeed);

            pwa.Breath.Intensity = PlayerStats.SprintTotalBreathIntensity * mountingBonus;
            pwa.HandsContainer.HandsRotation.InputIntensity = PlayerStats.SprintTotalHandsIntensity * mountingBonus;

            if (Utils.AreFloatsEqual(1f, PlayerStats.ADSSprintMulti) && Utils.AreFloatsEqual(pwa.Breath.Intensity, PlayerStats.TotalBreathIntensity) && Utils.AreFloatsEqual(pwa.HandsContainer.HandsRotation.InputIntensity, PlayerStats.TotalHandsIntensity))
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
                    PlayerStats.SprintBlockADS = true;
                    PlayerStats.WasSprinting = true;
                    didSprintPenalties = false;
                }
            }
            else
            {
                if (PlayerStats.WasSprinting)
                {
                    doSprintTimer(player.ProceduralWeaponAnimation, fc, mountingBonus);
                }
                if (doSwayReset)
                {
                    resetSwayParams(player.ProceduralWeaponAnimation, mountingBonus);
                }
            }

            if (!doSwayReset && !PlayerStats.WasSprinting)
            {
                PlayerStats.HasFullyResetSprintADSPenalties = true;
            }
            else
            {
                PlayerStats.HasFullyResetSprintADSPenalties = false;
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

        private static void PWAUpdate(Player player, Player.FirearmController fc) 
        {
            if (fc != null)
            {
                if (RecoilController.IsFiring)
                {
                    RecoilController.SetRecoilParams(player.ProceduralWeaponAnimation, fc.Item);
                    StanceController.IsPatrolStance = false;
                }

                ReloadController.ReloadStateCheck(player, fc, Logger);
                AimController.ADSCheck(player, fc);

                if (Plugin.EnableStanceStamChanges.Value)
                {
                    StanceController.SetStanceStamina(player, fc);
                }

                float remainStamPercent = player.Physical.HandsStamina.Current / player.Physical.HandsStamina.TotalCapacity;
                PlayerStats.RemainingArmStamPerc = 1f - ((1f - remainStamPercent) / 3f);
                PlayerStats.RemainingArmStamPercReload = 1f - ((1f - remainStamPercent) / 4f);
            }
            else if (Plugin.EnableStanceStamChanges.Value)
            {
                StanceController.ResetStanceStamina(player);
            }

            player.Physical.HandsStamina.Current = Mathf.Max(player.Physical.HandsStamina.Current, 1f);

            float stanceHipFactor = StanceController.IsActiveAiming ? 0.7f : StanceController.IsShortStock ? 1.35f : 1f;
            player.ProceduralWeaponAnimation.Breath.HipPenalty = Mathf.Clamp(WeaponStats.BaseHipfireInaccuracy * PlayerStats.SprintHipfirePenalty * stanceHipFactor, 0.2f, 1.6f);

            if (!RecoilController.IsFiring)
            {
                if (StanceController.CanResetDamping)
                {
                    player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = Mathf.Lerp(player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping, 0.45f, 0.01f);
                }
                else
                {
                    player.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.75f;
                }
                player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed = Mathf.Lerp(player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed, 10f * StanceController.WiggleReturnSpeed, 0.01f);
                player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[3].IntensityMultiplicator = 0;
                player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[4].IntensityMultiplicator = 0;
         /*       if (!RecoilController.IsFiringMovement) 
                {
                    player.ProceduralWeaponAnimation.CameraToWeaponAngleSpeedRange = Vector2.zero;
                    player.ProceduralWeaponAnimation.CameraToWeaponAngleStep = 0f;
                }*/
            }
            player.MovementContext.SetPatrol(StanceController.IsPatrolStance);
        }

        protected override MethodBase GetTargetMethod()
        {
            surfaceField = AccessTools.Field(typeof(Player), "_currentSet");
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix] 
        private static void PatchPostfix(Player __instance)
        {
            if (Plugin.EnableDeafen.Value && Plugin.ServerConfig.headset_changes)
            {
                SurfaceSet currentSet = (SurfaceSet)surfaceField.GetValue(__instance);
                currentSet.SprintSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
                currentSet.StopSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
                currentSet.JumpSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
                currentSet.LandingSoundBank.BaseVolume = Plugin.SharedMovementVolume.Value;
            }

            if (Utils.IsReady && __instance.IsYourPlayer)
            {
                Player.FirearmController fc = __instance.HandsController as Player.FirearmController;
                PlayerStats.IsSprinting = __instance.IsSprintEnabled;
                PlayerStats.EnviroType = __instance.Environment;
                StanceController.IsInInventory = __instance.IsInventoryOpened;
                PlayerStats.IsMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

                if (Plugin.EnableSprintPenalty.Value)
                {
                    DoSprintPenalty(__instance, fc, StanceController.BracingSwayBonus);
                }

                if (PlayerStats.HasFullyResetSprintADSPenalties) //!RecoilController.IsFiring && 
                {
                    __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerStats.TotalBreathIntensity * StanceController.BracingSwayBonus;
                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerStats.TotalHandsIntensity * StanceController.BracingSwayBonus;
                }

                PWAUpdate(__instance, fc);
            }
        }
    }
}

