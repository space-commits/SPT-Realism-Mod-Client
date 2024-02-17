using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;
using System;
using static EFT.Player;
using EFT.Interactive;
using System.Linq;
/*using IWeapon = GInterface273;*/
using PlayerInterface = GInterface127;
using EFT.Animations.Recoil;
using EFT.Animations;
using EFT.Animations.NewRecoil;
using System.Collections.Generic;
using System.IO;
/*using WeaponSkillsClass = EFT.SkillManager.GClass1638;*/

namespace RealismMod
{


    public class AutoFireModePatch : ModulePatch
    {
        private static FieldInfo autoFireField;
        private static FieldInfo fcField;

        protected override MethodBase GetTargetMethod()
        {
            autoFireField = AccessTools.Field(typeof(NewRecoilShotEffect), "_autoFireOn");
            fcField = AccessTools.Field(typeof(NewRecoilShotEffect), "_firearmController");
            return typeof(NewRecoilShotEffect).GetMethod("method_5");
        }

        [PatchPostfix]
        public static void PatchPostfix(NewRecoilShotEffect __instance)
        {
            if ((FirearmController)fcField.GetValue(__instance) != null)
            {
                autoFireField.SetValue(__instance, false);
                __instance.HandRotationRecoil.SetAutoFireMode(false);
            }
        }
    }

    public class IndexPatch : ModulePatch
    {
        private static FieldInfo index;

        protected override MethodBase GetTargetMethod()
        {
            index = AccessTools.Field(typeof(NewRecoilShotEffect), "_autoFireShotIndex");
            return typeof(NewRecoilShotEffect).GetMethod("FixedUpdate");
        }

        [PatchPostfix]
        public static void PatchPrefix(NewRecoilShotEffect __instance)
        {

            Logger.LogWarning("index " + (int)index.GetValue(__instance));
        }
    }

    public class StabilizePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(NewRotationRecoilProcess).GetMethod("SetStableMode");
        }

        [PatchPrefix]
        public static bool PatchPrefix(NewRotationRecoilProcess __instance)
        {
            __instance.CurrentAngleAdd = 0f;
            __instance.StableOn = false;
            return false;
        }
    }

    public class RotatePatch : ModulePatch
    {
        private static FieldInfo movementContextField;
        private static FieldInfo playerField;

        private static Vector2 initialRotation = Vector2.zero;
        private static Vector2 recordedRotation = Vector2.zero;
        private static Vector2 targetRotation = Vector2.zero;
        private static Vector2 currentRotation = Vector2.zero;
        private static Vector2 resetTarget = Vector2.zero;
        private static bool hasReset = false;
        private static float timer = 0.0f;
        private static float resetTime = 0.5f;

        private static Queue<float> distanceHistory = new Queue<float>();
        private static int historySize = 10;
        private static float maxIncreasePercentage = 1.25f;

        protected override MethodBase GetTargetMethod()
        {
            movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            playerField = AccessTools.Field(typeof(MovementContext), "_player");

            return typeof(MovementState).GetMethod("Rotate", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void resetTimer(Vector2 target, Vector2 current)
        {
            timer += Time.deltaTime;

            bool doHybridReset = (Plugin.EnableHybridRecoil.Value && !WeaponStats.HasShoulderContact) || (Plugin.EnableHybridRecoil.Value && Plugin.HybridForAll.Value);
            if ((doHybridReset && timer >= resetTime && target == current) || (!doHybridReset && (timer >= resetTime || target == current)))
            {
                hasReset = true;
            }
        }

        private static float calculateAverageDistance()
        {
            float sum = 0f;
            foreach (float dist in distanceHistory)
            {
                sum += dist;
            }
            return sum / distanceHistory.Count;
        }

        private static void adjustTargetVector(float averageDistance, float proposedDistance)
        {
            float desiredDistance = averageDistance;
            float adjustmentFactor = desiredDistance / proposedDistance;
            Vector2 direction = (targetRotation - currentRotation).normalized;
            targetRotation = currentRotation + direction * (proposedDistance * adjustmentFactor);
        }

        private static void updateDistanceHistory(float distance)
        {
            if (distanceHistory.Count >= historySize)
            {
                distanceHistory.Dequeue();
            }
            distanceHistory.Enqueue(distance);
        }

        [PatchPrefix]
        private static bool Prefix(MovementState __instance, Vector2 deltaRotation, bool ignoreClamp)
        {
            MovementContext movementContext = (MovementContext)movementContextField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(movementContext);

            if (player.IsYourPlayer && !ignoreClamp)
            {
                deltaRotation = movementContext.ClampRotation(deltaRotation);
                StanceController.MouseRotation = deltaRotation;

                if (!StanceController.IsMounting)
                {
                    initialRotation = movementContext.Rotation;
                }

                float fpsFactor = 144f / (1f / Time.unscaledDeltaTime);

                bool hybridBlocksReset = Plugin.EnableHybridRecoil.Value && !WeaponStats.HasShoulderContact && !Plugin.EnableHybridReset.Value;
                bool canResetVert = Plugin.ResetVertical.Value && !hybridBlocksReset;
                bool canResetHorz = Plugin.ResetHorizontal.Value && !hybridBlocksReset;

                if (RecoilController.ShotCount > RecoilController.PrevShotCount)
                {
                    float controlFactor = RecoilController.ShotCount <= 3f ? Plugin.PlayerControlMulti.Value * 3 : Plugin.PlayerControlMulti.Value;
                    RecoilController.PlayerControl += Mathf.Abs(deltaRotation.y) * controlFactor;

                    hasReset = false;
                    timer = 0f;

                    float shotCountFactor = (float)Math.Round(Mathf.Min(RecoilController.ShotCount * 0.4f, 1.2f), 2);
                    float baseAngle = RecoilController.BaseTotalRecoilAngle;
                    float totalRecAngle = StanceController.IsMounting ? Mathf.Min(baseAngle + 15, 90f) : StanceController.IsBracing ? Mathf.Min(baseAngle + 8f, 90f) : baseAngle;
                    totalRecAngle = WeaponStats._WeapClass != "pistol" ? totalRecAngle : totalRecAngle - 5;
                    float hipfireModifier = !StanceController.IsAiming ? 1.1f : 1f;
                    float dispersionSpeedFactor = WeaponStats._WeapClass != "pistol" ? 1f + (-WeaponStats.TotalDispersionDelta) : 1f;
                    float dispersionAngleFactor = WeaponStats._WeapClass != "pistol" ? 1f + (-WeaponStats.TotalDispersionDelta * 0.035f) : 1f;
                    float angle = (Plugin.RecoilDispersionFactor.Value == 0f ? 0f : ((90f - (totalRecAngle * dispersionAngleFactor)) / 50f));
                    float angleDispFactor = 90f / totalRecAngle;

                    float dispersion = Mathf.Max(RecoilController.FactoredTotalDispersion * Plugin.RecoilDispersionFactor.Value * shotCountFactor * fpsFactor * angleDispFactor * hipfireModifier, 0f);
                    float dispersionSpeed = Math.Max(Time.time * Plugin.RecoilDispersionSpeed.Value * dispersionSpeedFactor, 0.1f);
                    float recoilClimbMulti = WeaponStats._WeapClass == "pistol" ? Plugin.PistolRecClimbFactor.Value : Plugin.RecoilClimbFactor.Value;

                    float xRotation = 0f;
                    float yRotation = 0f;

                    if (!RecoilController.IsKrissVector)
                    {
                        xRotation = (float)Math.Round(Mathf.Lerp(-dispersion, dispersion, Mathf.PingPong(dispersionSpeed, 1f)) + angle, 3);
                        yRotation = (float)Math.Round(Mathf.Min(-RecoilController.FactoredTotalVRecoil * recoilClimbMulti * shotCountFactor * fpsFactor, 0f), 3);
                    }
                    else
                    {
                        float recoilAmount = RecoilController.FactoredTotalVRecoil * recoilClimbMulti * shotCountFactor * fpsFactor;
                        dispersion = Mathf.Max(RecoilController.FactoredTotalDispersion * Plugin.RecoilDispersionFactor.Value * shotCountFactor * fpsFactor, 0f);
                        xRotation = (float)Math.Round(Mathf.Lerp(-dispersion, dispersion, Mathf.PingPong(Time.time * 8f, 1f)), 3);
                        yRotation = (float)Math.Round(Mathf.Lerp(-recoilAmount, recoilAmount, Mathf.PingPong(Time.time * 4f, 1f)), 3);
                    }

                    targetRotation = movementContext.Rotation;
                    targetRotation.x += xRotation;
                    targetRotation.y += yRotation;

                    if ((canResetVert && (movementContext.Rotation.y > (recordedRotation.y + 2f) * Plugin.NewPOASensitivity.Value || deltaRotation.y <= -1f * Plugin.NewPOASensitivity.Value)) || (canResetHorz && Mathf.Abs(deltaRotation.x) >= 1f * Plugin.NewPOASensitivity.Value))
                    {
                        recordedRotation = movementContext.Rotation;
                    }
                }
                else if ((canResetHorz || canResetVert) && !hasReset && !RecoilController.IsFiring)
                {
                    bool isHybrid = Plugin.EnableHybridRecoil.Value && (Plugin.HybridForAll.Value || (!Plugin.HybridForAll.Value && !WeaponStats.HasShoulderContact));
                    float resetSpeed = RecoilController.BaseTotalConvergence * WeaponStats.ConvergenceDelta * Plugin.ResetSpeed.Value;
                    float resetSens = isHybrid ? (float)Math.Round(Plugin.ResetSensitivity.Value * 0.4f, 3) : Plugin.ResetSensitivity.Value;

                    bool xIsBelowThreshold = Mathf.Abs(deltaRotation.x) <= Mathf.Clamp((float)Math.Round(resetSens / 2.5f, 3), 0f, 0.1f);
                    bool yIsBelowThreshold = Mathf.Abs(deltaRotation.y) <= resetSens;

                    if (canResetVert && canResetHorz && xIsBelowThreshold && yIsBelowThreshold)
                    {
                        resetTarget.x = recordedRotation.x;
                        resetTarget.y = recordedRotation.y;
                        movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, resetTarget, resetSpeed);
                    }
                    else if (canResetHorz && xIsBelowThreshold && !canResetVert)
                    {
                        resetTarget.x = recordedRotation.x;
                        resetTarget.y = movementContext.Rotation.y;
                        movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, resetTarget, resetSpeed);
                    }
                    else if (canResetVert && yIsBelowThreshold && !canResetHorz)
                    {
                        resetTarget.x = movementContext.Rotation.x;
                        resetTarget.y = recordedRotation.y;
                        movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, resetTarget, resetSpeed);
                    }
                    else
                    {
                        resetTarget = movementContext.Rotation;
                        recordedRotation = movementContext.Rotation;
                    }

                    resetTimer(resetTarget, movementContext.Rotation);
                }
                else if (!RecoilController.IsFiring)
                {
                    if (Mathf.Abs(deltaRotation.y) > 0.1f)
                    {
                        RecoilController.PlayerControl += Mathf.Abs(deltaRotation.y) * Plugin.PlayerControlMulti.Value;
                    }
                    else
                    {
                        RecoilController.PlayerControl = 0f;
                    }

                    recordedRotation = movementContext.Rotation;
                }

                if (RecoilController.IsFiring)
                {

                    if (targetRotation.y <= recordedRotation.y - Plugin.RecoilClimbLimit.Value)
                    {
                        Logger.LogWarning("====HIT MAX====");
                        targetRotation.y = movementContext.Rotation.y;
                    }

                    float differenceX = Mathf.Abs(movementContext.Rotation.x - targetRotation.x);
                    targetRotation.x = differenceX <= 2f ? targetRotation.x : movementContext.Rotation.x;

                    /*
                                        float differenceY = Mathf.Abs(movementContext.Rotation.y - targetRotation.y);
                                        targetRotation.y = differenceY <= 2f ? targetRotation.y : movementContext.Rotation.y;*/

                    currentRotation = movementContext.Rotation;
                    float proposedDistance = Vector2.Distance(currentRotation, targetRotation);
                    updateDistanceHistory(proposedDistance);
                    float averageDistance = calculateAverageDistance();

                    if (proposedDistance > averageDistance * maxIncreasePercentage)
                    {
                        Logger.LogWarning("TARGET EXCEEDS PERSMISSABLE DIFFERENCE");
                        adjustTargetVector(averageDistance, proposedDistance);
                    }

                    Logger.LogWarning("Distance Before " + proposedDistance);
                    Logger.LogWarning("Distance After " + Vector2.Distance(currentRotation, targetRotation));

                    movementContext.Rotation = Vector2.Lerp(movementContext.Rotation, targetRotation, Plugin.RecoilSmoothness.Value);
                }
                else 
                {
                    distanceHistory.Clear();
                }

                if (RecoilController.ShotCount == RecoilController.PrevShotCount)
                {
                    RecoilController.PlayerControl = Mathf.Lerp(RecoilController.PlayerControl, 0f, 0.05f);
                }

                if (StanceController.IsMounting)
                {
                    FirearmController fc = player.HandsController as FirearmController;

                    Vector2 currentRotation = movementContext.Rotation;

                    deltaRotation *= (fc.AimingSensitivity * 0.9f);

                    float lowerClampXLimit = StanceController.BracingDirection == EBracingDirection.Top ? -17f : StanceController.BracingDirection == EBracingDirection.Right ? -4f : -15f;
                    float upperClampXLimit = StanceController.BracingDirection == EBracingDirection.Top ? 17f : StanceController.BracingDirection == EBracingDirection.Right ? 15f : 1f;

                    float lowerClampYLimit = StanceController.BracingDirection == EBracingDirection.Top ? -10f : -8f;
                    float upperClampYLimit = StanceController.BracingDirection == EBracingDirection.Top ? 10f : 15f;

                    float relativeLowerXLimit = initialRotation.x + lowerClampXLimit;
                    float relativeUpperXLimit = initialRotation.x + upperClampXLimit;
                    float relativeLowerYLimit = initialRotation.y + lowerClampYLimit;
                    float relativeUpperYLimit = initialRotation.y + upperClampYLimit;

                    float clampedX = Mathf.Clamp(currentRotation.x + deltaRotation.x, relativeLowerXLimit, relativeUpperXLimit);
                    float clampedY = Mathf.Clamp(currentRotation.y + deltaRotation.y, relativeLowerYLimit, relativeUpperYLimit);

                    deltaRotation.x = clampedX - currentRotation.x;
                    deltaRotation.y = clampedY - currentRotation.y;

                    deltaRotation = movementContext.ClampRotation(deltaRotation);
                    movementContext.Rotation += deltaRotation;

                    return false;
                }
            }
            return true;
        }
    }



    public class RecoilAnglesPatch : ModulePatch
    {
        private static FieldInfo fcField;
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            fcField = AccessTools.Field(typeof(NewRecoilShotEffect), "_firearmController");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(NewRecoilShotEffect).GetMethod("CalculateBaseRecoilParameters");
        }

        [PatchPrefix]
        public static bool Prefix(NewRecoilShotEffect __instance, float recoilSuspensionY, List<ShotsGroupSettings> shotsGroupSettingsList)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary) 
            {
                if (shotsGroupSettingsList != null)
                {
                    __instance.ShotsGroupsSettings = shotsGroupSettingsList;
                }

                float angle = Mathf.LerpAngle(!Plugin.EnableAngle.Value ? 90f : WeaponStats.TotalRecoilAngle * Plugin.RecoilAngleMulti.Value, (float)__instance.HandRotationRecoil.MaxRecoilRotationAngle, recoilSuspensionY);
                float dispersion = WeaponStats.TotalDispersion / (1f + recoilSuspensionY);
                __instance.BasicPlayerRecoilDegreeRange = new Vector2(angle - dispersion, angle + dispersion);
                __instance.BasicRecoilRadian = __instance.BasicPlayerRecoilDegreeRange * 0.017453292f;

                RecoilController.BaseTotalRecoilAngle = (float)Math.Round(angle, 2);
                RecoilController.BaseTotalDispersion = (float)Math.Round(dispersion, 2);

                return false;
            }
            return true;
        }
    }

    public class RecalcWeaponParametersPatch : ModulePatch
    {
        private static FieldInfo fcField;
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            fcField = AccessTools.Field(typeof(NewRecoilShotEffect), "_firearmController");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(NewRecoilShotEffect).GetMethod("RecalculateRecoilParamsOnChangeWeapon", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(NewRecoilShotEffect __instance, WeaponTemplate template, BackendConfigSettingsClass.AimingConfiguration AimingConfig, Player.FirearmController firearmController, float recoilSuppressionX, float recoilSuppressionY, float recoilSuppressionFactor, float modsFactorRecoil)
        {
            //make sure the firearmcontroller is instatiated before using it to determine IsYourPlayer, make sure it's set correctly
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {

                //force stats to be calculated 
                float calcStats = firearmController.Weapon.ErgonomicsDelta;

                fcField.SetValue(__instance, firearmController);
                __instance.RecoilStableShotIndex = (int)Plugin.test10.Value;
                __instance.HandRotationRecoil.RecoilReturnTrajectoryOffset = template.RecoilReturnPathOffsetHandRotation;
                __instance.HandRotationRecoil.StableAngleIncreaseStep = template.RecoilStableAngleIncreaseStep;
                __instance.HandRotationRecoil.AfterRecoilOffsetVerticalRange = template.PostRecoilVerticalRangeHandRotation;
                __instance.HandRotationRecoil.AfterRecoilOffsetHorizontalRange = template.PostRecoilHorizontalRangeHandRotation;
                __instance.HandRotationRecoil.ProgressRecoilAngleOnStable = template.ProgressRecoilAngleOnStable;
                __instance.HandRotationRecoil.ReturnTrajectoryDumping = template.RecoilReturnPathDampingHandRotation;
                __instance.HandRotationRecoil.CategoryIntensityMultiplier = template.RecoilCategoryMultiplierHandRotation * Plugin.RecoilIntensity.Value;

                float totalVRecoilDelta = Mathf.Max(0f, (1f + WeaponStats.VRecoilDelta) * (1f - recoilSuppressionX - recoilSuppressionY * recoilSuppressionFactor));
                float totalHRecoilDelta = Mathf.Max(0f, (1f + WeaponStats.HRecoilDelta) * (1f - recoilSuppressionX - recoilSuppressionY * recoilSuppressionFactor));

         
                //this may not be the right reference
                //need to figure out a sensible way to represent recoil of underbarrel
/*                if (firearmController.Weapon.IsUnderBarrelDeviceActive) 
                {
                }
*/
                __instance.BasicPlayerRecoilRotationStrength = __instance.BasicRecoilRotationStrengthRange * ((template.RecoilForceUp * totalVRecoilDelta + AimingConfig.RecoilVertBonus) * __instance.IncomingRotationStrengthMultiplier);
                __instance.BasicPlayerRecoilPositionStrength = __instance.BasicRecoilPositionStrengthRange * ((template.RecoilForceBack * totalHRecoilDelta + AimingConfig.RecoilBackBonus) * __instance.IncomingRotationStrengthMultiplier);
              
                float cameraRecoil = WeaponStats.TotalCamRecoil;
                __instance.ShotRecoilProcessValues[3].IntensityMultiplicator = cameraRecoil;
                __instance.ShotRecoilProcessValues[4].IntensityMultiplicator = -cameraRecoil;
                RecoilController.BaseTotalCamRecoil = cameraRecoil;

                RecoilController.BaseTotalVRecoil = (float)Math.Round(__instance.BasicPlayerRecoilRotationStrength.y, 3);
                RecoilController.BaseTotalHRecoil = (float)Math.Round(__instance.BasicPlayerRecoilPositionStrength.y, 3);
                RecoilController.BaseTotalConvergence = WeaponStats.TotalModdedConv * Plugin.ConvergenceMulti.Value;

                RecoilController.BaseTotalRecoilDamping = (float)Math.Round(WeaponStats.TotalRecoilDamping * Plugin.RecoilDampingMulti.Value, 3);
                RecoilController.BaseTotalHandDamping = (float)Math.Round(WeaponStats.TotalRecoilHandDamping * Plugin.HandsDampingMulti.Value, 3);
                RecoilController.IsKrissVector = firearmController.Weapon.TemplateId == "5fb64bc92b1b027b1f50bcf2" || firearmController.Weapon.TemplateId == "5fc3f2d5900b1d5091531e57" ? true : false;
                WeaponStats.TotalWeaponWeight = firearmController.Weapon.GetSingleItemTotalWeight();

                if (WeaponStats.WeapID != template._id)
                {
                    StanceController.DidWeaponSwap = true;
                }
                WeaponStats.WeapID = template._id;

                if (Plugin.EnableLogging.Value)
                {
                    Logger.LogWarning("============RecalcWeapParams========");
                    Logger.LogWarning("vert recoil " + __instance.BasicPlayerRecoilRotationStrength);
                    Logger.LogWarning("horz recoil " + __instance.BasicPlayerRecoilPositionStrength);
                    Logger.LogWarning("cam recoil " + cameraRecoil);
                    Logger.LogWarning("====================");
                }

                return false;
            }
            return true;
        }
    }

    public class AddRecoilForcePatch : ModulePatch
    {
        private static string[] allowedCalibers = { "Caliber556x45NATO", "Caliber545x39", "Caliber762x39", "Caliber9x39", "Caliber762x35" };

        private static FieldInfo shotIndexField;
        private static FieldInfo fcField;
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            fcField = AccessTools.Field(typeof(NewRecoilShotEffect), "_firearmController");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            shotIndexField = AccessTools.Field(typeof(NewRecoilShotEffect), "_autoFireShotIndex");

            return typeof(NewRecoilShotEffect).GetMethod("AddRecoilForce");
        }

        [PatchPrefix]
        public static bool Prefix(NewRecoilShotEffect __instance, float incomingForce)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                //Conditional recoil modifiers 
                float totalPlayerWeight = PlayerStats.TotalModifiedWeightMinusWeapon;
                float playerWeightFactorBuff = 1f - (totalPlayerWeight / 550f);
                float playerWeightFactorDebuff = 1f + (totalPlayerWeight / 100f);

                float activeAimingBonus = StanceController.IsActiveAiming ? 0.9f : 1f;
                float aimCamRecoilBonus = StanceController.IsActiveAiming || !StanceController.IsAiming ? 0.8f : 1f;
                float shortStockingDebuff = StanceController.IsShortStock ? 1.15f : 1f;
                float shortStockingCamBonus = StanceController.IsShortStock ? 0.6f : 1f;

                float mountingVertModi = StanceController.IsMounting ? StanceController.MountingRecoilBonus : StanceController.IsBracing ? StanceController.BracingRecoilBonus : 1f;
                float mountingDispModi = Mathf.Clamp(StanceController.IsMounting ? StanceController.MountingRecoilBonus * 1.25f : StanceController.IsBracing ? StanceController.BracingRecoilBonus * 1.2f : 1f, 0.85f, 1f);
                float baseRecoilAngle = RecoilController.BaseTotalRecoilAngle;
                float mountingAngleModi = StanceController.IsMounting ? Mathf.Min(baseRecoilAngle + 15f, 90f) : StanceController.IsBracing ? Mathf.Min(baseRecoilAngle + 8f, 90f) : baseRecoilAngle;
                
                float opticRecoilMulti = allowedCalibers.Contains(firearmController.Weapon.AmmoCaliber) && StanceController.IsAiming && WeaponStats.HasOptic ? 0.93f : 1f;
                float fovFactor = (Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView / 70f) * Plugin.HRecLimitMulti.Value;
                float opticLimit = StanceController.IsAiming && WeaponStats.HasOptic ? 15f * fovFactor : 25f * fovFactor;

                float shotFactor = firearmController.Weapon.WeapClass == "pistol" && RecoilController.ShotCount >= 1f && firearmController.Weapon.SelectedFireMode == Weapon.EFireMode.fullauto ? 0.5f : 1f;

                //BSG stuff related to recoil modifier based on shot index/count. Unused by Realism mod.
                int shotIndex = (int)shotIndexField.GetValue(__instance) + 1;
                shotIndexField.SetValue(__instance, shotIndex);
                if ((int)shotIndexField.GetValue(__instance) == __instance.RecoilStableShotIndex)
                {
                    __instance.HandRotationRecoil.SetStableMode(true);
                }
                __instance.HandRotationRecoil.MultiplayerByShotIndex = __instance.method_0();

                //Calc Vert and Horz recoil
                float rotationRecoilPower;
                float positionRecoilPower;
                __instance.method_2(incomingForce, out rotationRecoilPower, out positionRecoilPower);

                //Modify Vert and Horz recoil based on various factors
                float vertFactor = PlayerStats.RecoilInjuryMulti * activeAimingBonus * shortStockingDebuff * playerWeightFactorBuff * mountingVertModi * shotFactor * opticRecoilMulti * Plugin.VertMulti.Value;
                float horzFactor = PlayerStats.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorBuff * shotFactor * Plugin.HorzMulti.Value;
                RecoilController.FactoredTotalVRecoil = vertFactor * RecoilController.BaseTotalVRecoil;
                RecoilController.FactoredTotalHRecoil = horzFactor * RecoilController.BaseTotalHRecoil;
                horzFactor = Mathf.Min(horzFactor * fovFactor, opticLimit); //I put it here after setting FactoredTotalHRecoil so that visual recoil isn't affected
                rotationRecoilPower *= vertFactor;
                positionRecoilPower *= horzFactor;

                //Recalculate and modify dispersion
                float dispFactor = incomingForce * PlayerStats.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorDebuff * mountingDispModi * opticRecoilMulti * Plugin.DispMulti.Value;
                RecoilController.FactoredTotalDispersion = RecoilController.BaseTotalDispersion * dispFactor;
                __instance.BasicPlayerRecoilDegreeRange = new Vector2(RecoilController.BaseTotalRecoilAngle - RecoilController.FactoredTotalDispersion, RecoilController.BaseTotalRecoilAngle + RecoilController.FactoredTotalDispersion);
                __instance.BasicRecoilRadian = __instance.BasicPlayerRecoilDegreeRange * 0.017453292f;

                Vector2 finalRecoilRadian;
                __instance.method_3(out finalRecoilRadian);

                //Reset camera recoil values and modify by various factors
                /*float camShotFactor = Mathf.Min((RecoilController.ShotCount * 0.25f) + 1f, 1.4f);*/
                float totalCamRecoil = RecoilController.BaseTotalCamRecoil * incomingForce * PlayerStats.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff * opticRecoilMulti * Plugin.CamMulti.Value; // * camShotFactor
                RecoilController.FactoredTotalCamRecoil = totalCamRecoil;
                __instance.ShotRecoilProcessValues[3].IntensityMultiplicator = totalCamRecoil;
                __instance.ShotRecoilProcessValues[4].IntensityMultiplicator = -totalCamRecoil;

                //Do recoil
                __instance.method_1(finalRecoilRadian, rotationRecoilPower, positionRecoilPower);
                ShotEffector.RecoilShotVal[] shotRecoilProcessValues = __instance.ShotRecoilProcessValues;
                for (int i = 0; i < shotRecoilProcessValues.Length; i++)
                {
                    shotRecoilProcessValues[i].Process(__instance.RecoilDirection);
                }

                if (Plugin.EnableLogging.Value) 
                {
                    Logger.LogWarning("==========shoot==========");
                    Logger.LogWarning("camFactor " + (incomingForce * PlayerStats.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff * opticRecoilMulti));
                    Logger.LogWarning("vertFactor " + vertFactor);
                    Logger.LogWarning("horzFactor " + horzFactor);
                    Logger.LogWarning("dispFactor " + dispFactor);
                    Logger.LogWarning("mounting " + mountingVertModi);
                    Logger.LogWarning("====================");
                }

                //Calculate offest for zero shift
                if (WeaponStats.ScopeAccuracyFactor <= 1f)
                {
                    float gunFactor = firearmController.Weapon.TemplateId == "6183afd850224f204c1da514" || firearmController.Weapon.TemplateId == "6165ac306ef05c2ce828ef74" ? 4f : 1f;
                    float shiftRecoilFactor = (RecoilController.FactoredTotalVRecoil + RecoilController.FactoredTotalHRecoil) * (1f + totalCamRecoil) * gunFactor;
                    float scopeFactor = ((1f - WeaponStats.ScopeAccuracyFactor) + (shiftRecoilFactor * 0.002f));

                    int num = UnityEngine.Random.Range(1, 20);
                    if (scopeFactor * 10f > num)
                    {
                        Logger.LogWarning("shift");
                        float offsetFactor = scopeFactor * 0.2f;
                        float offsetX = Random.Range(-offsetFactor, offsetFactor);
                        float offsetY = Random.Range(-offsetFactor, offsetFactor);
                        WeaponStats.ZeroRecoilOffset = new Vector2(offsetX, offsetY);
                        if (WeaponStats.ScopeID != null && WeaponStats.ScopeID != "")
                        {
                            if (WeaponStats.ZeroOffsetDict.ContainsKey(WeaponStats.ScopeID))
                            {
                                WeaponStats.ZeroOffsetDict[WeaponStats.ScopeID] = WeaponStats.ZeroRecoilOffset;
                            }
                        }
                    }
                }

                //update or reset firing state
                RecoilController.ShotCount++;
                RecoilController.ShotTimer = 0f;
                RecoilController.DeafenShotTimer = 0f;
                RecoilController.WiggleShotTimer = 0f;
                RecoilController.MovementSpeedShotTimer = 0f;
                StanceController.StanceShotTime = 0f;
                RecoilController.IsFiring = true;
                RecoilController.IsFiringDeafen = true;
                RecoilController.IsFiringWiggle = true;
                RecoilController.IsFiringMovement = true;
                StanceController.IsFiringFromStance = true;

                return false;
            }
            return true;
        }
    }

    public class ShootPatch : ModulePatch
    {
        private static FieldInfo fcField;
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
            playerField = AccessTools.Field(typeof(FirearmController), "_player");
            return typeof(ProceduralWeaponAnimation).GetMethod("Shoot");
        }

        [PatchPostfix]
        public static void PatchPostfix(ProceduralWeaponAnimation __instance, float str)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
            {
                RecoilController.SetRecoilParams(__instance, firearmController.Weapon);
            }
        }
    }
}

