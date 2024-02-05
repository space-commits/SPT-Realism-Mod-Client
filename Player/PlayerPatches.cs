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
/*using ProcessorClass = GClass2039;*/

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
        private static float newWeight = 0f;
        private static string[] stashIDs = { 
            "5fe49444ae6628187a2e78b8", 
            "5fe4977574f15b4ad31b66b6", 
            "5fe49cdfa19cac3fa9054115", 
            "5fe49a0e2694b0755a504876", 
            "5fe4a9285a72d07b663029e2", 
            "5fe4a9fcf5aec236ec3836f7",
            "5fe4a66e40ca750fd72b595a",
            "5fe4ab2240ca750fd72bbe1c"
        };

        private static void getTotalWeight(Inventory inventory, bool isInStash)
        {
            newWeight = calcWeightPenalties(inventory, isInStash);
            inventory.TotalWeight = new WeightClass(new Func<float>(() => newWeight));
        }

        private static float calcWeightPenalties(Inventory invClass, bool isInStash)
        {
            float modifiedWeight = 0f;
            float trueWeight = 0f;
            foreach (EquipmentSlot equipmentSlot in EquipmentClass.AllSlotNames)
            {
                IEnumerable<Item> items = invClass.Equipment.GetSlot(equipmentSlot).Items;
                foreach (Item item in items)
                {
                    float itemTotalWeight = item.GetSingleItemTotalWeight();
                    trueWeight += itemTotalWeight;
                    if (equipmentSlot == EquipmentSlot.Backpack || equipmentSlot == EquipmentSlot.TacticalVest)
                    {
                        float modifier = GearStats.ComfortModifier(item);
                        float containedItemsModifiedWeight = (itemTotalWeight - item.Weight) * modifier;
                        modifiedWeight += item.Weight + containedItemsModifiedWeight;
                    }
                    else
                    {
                        modifiedWeight += itemTotalWeight;
                    }
                }
            }

            if (!isInStash) 
            {
                Player player = Utils.GetPlayer();
                PlayerStats.TotalModifiedWeight = modifiedWeight;
                PlayerStats.TotalUnmodifiedWeight = trueWeight;
                PlayerStats.TotalMousePenalty = (-modifiedWeight / 10f);
                float weaponWeight = player?.HandsController != null && player?.HandsController?.Item != null ? player.HandsController.Item.GetSingleItemTotalWeight() : 1f;
                PlayerStats.TotalModifiedWeightMinusWeapon = PlayerStats.TotalModifiedWeight - weaponWeight;

                if (Plugin.EnableMouseSensPenalty.Value)
                {
                    player.RemoveMouseSensitivityModifier(Player.EMouseSensitivityModifier.Armor);
                    if (PlayerStats.TotalMousePenalty < 0f)
                    {
                        player.AddMouseSensitivityModifier(Player.EMouseSensitivityModifier.Armor, PlayerStats.TotalMousePenalty / 100f);
                    }
                }
            }

            if (Plugin.EnableLogging.Value)
            {
                Logger.LogWarning("Total Modified Weight " + modifiedWeight);
                Logger.LogWarning("Total Unmodified Weight " + trueWeight);
                Logger.LogWarning("Total Mouse Penalty" + PlayerStats.TotalMousePenalty);
                Logger.LogWarning("Total Modified Weight MinusWeapon " + PlayerStats.TotalModifiedWeightMinusWeapon);
            }

            return modifiedWeight;
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Inventory).GetMethod("UpdateTotalWeight", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(Inventory __instance, EventArgs args)
        {
            try 
            {

                /*   if ((geventArgs = (args as GEventArgs1)) != null)
                   {
                       Logger.LogWarning("args is not null");
                   }
                   else 
                   {
                       Logger.LogWarning("args is null");
                   }

   */
                /*       __instance.Stash.OriginalAddress.GetOwner().ID*/

                Logger.LogWarning("1");
                GEventArgs1 geventArgs;
                Logger.LogWarning("2");
                bool world = Singleton<GameWorld>.Instance != null;
                Logger.LogWarning("3");
                bool player = Singleton<GameWorld>.Instance?.MainPlayer != null;
                Logger.LogWarning("4");
                bool profile = Singleton<GameWorld>.Instance?.MainPlayer?.ProfileId != null;
                Logger.LogWarning("5");
                bool notNull = (geventArgs = (args as GEventArgs1)) != null;
                Logger.LogWarning("6");
                bool item = geventArgs?.Item != null;
                Logger.LogWarning("7");
                bool owner = geventArgs?.Item?.Owner != null;
                Logger.LogWarning("8");
                bool ownerID = geventArgs?.Item?.Owner?.ID != null;
                Logger.LogWarning("9");
                if (notNull && world && player && profile && item && owner && ownerID && geventArgs.Item.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
                {
                    Logger.LogWarning("match");
                    getTotalWeight(__instance, false);
                }
                else 
                {
                    //only way to differentiate player scav from pmc while in menu
                    //player scav has random stash id, possible pmc stash id's are known beforehand
                    if (stashIDs.Contains(__instance.Stash.Id)) 
                    {
                        Logger.LogWarning("stash");
                        getTotalWeight(__instance, true);
                    }          
                }
            }
            catch 
            {
                Logger.LogWarning("null ref");
                //somehow null check failed
            }
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
                Logger.LogWarning("init");
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
                StatCalc.SetGearParamaters(__instance);
            }
        }
    }

    /*    
    public class BreathProcessPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BreathEffector).GetMethod("Process", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BreathEffector __instance, float deltaTime, float ____breathIntensity, float ____shakeIntensity, float ____breathFrequency,
        float ____cameraSensetivity, Vector2 ____baseHipRandomAmplitudes, Spring ____recoilRotationSpring, Spring ____handsRotationSpring, AnimationCurve ____lackOfOxygenStrength, ProcessorClass[] ____processors)
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

            StaminaLevelClass staminaLevel = __instance.StaminaLevel;
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
            ____processors[0].ProcessRaw(____breathFrequency, PlayerStats.TotalBreathIntensity * 0.15f);
            ____processors[1].ProcessRaw(____breathFrequency, PlayerStats.TotalBreathIntensity * 0.15f * ____cameraSensetivity);
            return false;
        }
    }*/

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
            float resetSwaySpeed = 0.05f;
            float resetSpeed = 0.5f;
            PlayerStats.SprintTotalBreathIntensity = Mathf.Lerp(PlayerStats.SprintTotalBreathIntensity, PlayerStats.TotalBreathIntensity, resetSwaySpeed);
            PlayerStats.SprintTotalHandsIntensity = Mathf.Lerp(PlayerStats.SprintTotalHandsIntensity, PlayerStats.TotalHandsIntensity, resetSwaySpeed);
            PlayerStats.ADSSprintMulti = Mathf.Lerp(PlayerStats.ADSSprintMulti, 1f, resetSpeed);
            PlayerStats.SprintHipfirePenalty = Mathf.Lerp(PlayerStats.SprintHipfirePenalty, 1f, resetSpeed);

            if (!RecoilController.IsFiring)
            {
                pwa.Breath.Intensity = PlayerStats.SprintTotalBreathIntensity * mountingBonus;
                pwa.HandsContainer.HandsRotation.InputIntensity = PlayerStats.SprintTotalHandsIntensity * mountingBonus;
            }

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
                    doSprintTimer(player.ProceduralWeaponAnimation, fc);
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

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix] 


        private static void PatchPostfix(Player __instance)
        {
            if (Plugin.EnableDeafen.Value && Plugin.ServerConfig.headset_changes)
            {
                SurfaceSet currentSet = (SurfaceSet)AccessTools.Field(typeof(Player), "_currentSet").GetValue(__instance);
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
                float mountingSwayBonus = StanceController.IsMounting ? StanceController.MountingSwayBonus: StanceController.BracingSwayBonus;

                if (Plugin.EnableSprintPenalty.Value)
                {
                    DoSprintPenalty(__instance, fc, mountingSwayBonus);
                }

                if (!RecoilController.IsFiring && PlayerStats.HasFullyResetSprintADSPenalties)
                {
                    __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerStats.TotalBreathIntensity * mountingSwayBonus;
                    __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerStats.TotalHandsIntensity * mountingSwayBonus;
                }

                if (fc != null)
                {
                    if (RecoilController.IsFiring)
                    {
                        //did this due to weird weapon movement while firing, might not be necessary anymore
                  /*      if (Plugin.IsAiming)
                        {
                            __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerStats.TotalBreathIntensity * mountingSwayBonus * 0.01f;
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerStats.TotalHandsIntensity * mountingSwayBonus * 0.01f;
                        }
                        else
                        {
                            __instance.ProceduralWeaponAnimation.Breath.Intensity = PlayerStats.TotalBreathIntensity * mountingSwayBonus;
                            __instance.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = PlayerStats.TotalHandsIntensity * mountingSwayBonus;
                        }*/

                        RecoilController.SetRecoilParams(__instance.ProceduralWeaponAnimation, fc.Item);

                        StanceController.IsPatrolStance = false;
                    }

                    ReloadController.ReloadStateCheck(__instance, fc, Logger);
                    AimController.ADSCheck(__instance, fc, Logger);

                    if (Plugin.EnableStanceStamChanges.Value)
                    {
                        StanceController.SetStanceStamina(__instance, fc, Logger);
                    }

                    float remainStamPercent = __instance.Physical.HandsStamina.Current / __instance.Physical.HandsStamina.TotalCapacity;
                    PlayerStats.RemainingArmStamPerc = 1f - ((1f - remainStamPercent) / 3f);
                    PlayerStats.RemainingArmStamPercReload = 1f - ((1f - remainStamPercent) / 4f);
                }
                else if (Plugin.EnableStanceStamChanges.Value)
                {
                    StanceController.ResetStanceStamina(__instance, Logger);
                }

                __instance.Physical.HandsStamina.Current = Mathf.Max(__instance.Physical.HandsStamina.Current, 1f);

                float stanceHipFactor = StanceController.IsActiveAiming ? 0.7f : StanceController.IsShortStock ? 1.35f : 1f;
                __instance.ProceduralWeaponAnimation.Breath.HipPenalty = Mathf.Clamp(WeaponStats.BaseHipfireInaccuracy * PlayerStats.SprintHipfirePenalty * stanceHipFactor, 0.2f, 1.6f);

                if (!RecoilController.IsFiring)
                {
                    if (StanceController.CanResetDamping)
                    {
                        float resetSpeed = 0.01f;
                        __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = Mathf.Lerp(__instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping, 0.45f, resetSpeed);
                    }
                    else
                    {
                        __instance.ProceduralWeaponAnimation.HandsContainer.HandsPosition.Damping = 0.75f;
                        __instance.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[3].IntensityMultiplicator = 0;
                        __instance.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.RecoilProcessValues[4].IntensityMultiplicator = 0;
                    }
                    __instance.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed = Mathf.Lerp(__instance.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed, 10f * StanceController.WiggleReturnSpeed, 0.01f);
                }
                __instance.MovementContext.SetPatrol(StanceController.IsPatrolStance);
            }
        }
    }
}

