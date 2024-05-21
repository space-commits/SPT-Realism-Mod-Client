using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.Animations.NewRecoil;
using EFT.InventoryLogic;
using EFT.Visual;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static EFT.Player;
using Random = UnityEngine.Random;

namespace RealismMod
{
    public class UpdateHipInaccuracyPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo tacticalModesField;


        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            tacticalModesField = AccessTools.Field(typeof(TacticalComboVisualController), "list_0");
            return typeof(EFT.Player.FirearmController).GetMethod("UpdateHipInaccuracy", BindingFlags.Instance | BindingFlags.Public);
        }

        private static bool CheckVisibleLaser(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "light" under the mode, here's hoping BSG stay consistent
                foreach (Transform child in tacticalMode.GetChildren())
                {
                    if (child.name.StartsWith("VIS_"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CheckIRLight(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "VolumetricLight", hopefully only visible flashlights have these
                IkLight irLight = tacticalMode.GetComponentInChildren<IkLight>();
                if (irLight != null)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CheckIRLaser(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "light" under the mode, here's hoping BSG stay consistent
                foreach (Transform child in tacticalMode.GetChildren())
                {
                    if (child.name.StartsWith("IR_"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool CheckWhiteLight(List<Transform> tacticalModes)
        {
            foreach (Transform tacticalMode in tacticalModes)
            {
                // Skip disabled modes
                if (!tacticalMode.gameObject.activeInHierarchy) continue;

                // Try to find a "VolumetricLight", hopefully only visible flashlights have these
                VolumetricLight volumetricLight = tacticalMode.GetComponentInChildren<VolumetricLight>();
                if (volumetricLight != null)
                {
                    return true;
                }
            }
            return false;
        }


        private static void CheckDevice(FirearmController firearmController, FieldInfo tacticalModesField)
        {
            if (tacticalModesField == null)
            {
                Logger.LogError("Could find not find _tacticalModesField");
                return;
            }

            // Get the list of tacticalComboVisualControllers for the current weapon (One should exist for every flashlight, laser, or combo device)
            Transform weaponRoot = firearmController.WeaponRoot;
            List<TacticalComboVisualController> tacticalComboVisualControllers = weaponRoot.GetComponentsInChildrenActiveIgnoreFirstLevel<TacticalComboVisualController>();
            if (tacticalComboVisualControllers == null)
            {
                Logger.LogError("Could find not find tacticalComboVisualControllers");
                return;
            }

            PlayerState.WhiteLightActive = false;
            PlayerState.LaserActive = false;
            PlayerState.IRLightActive = false;
            PlayerState.IRLaserActive = false;

            // Loop through all of the tacticalComboVisualControllers, then its modes, then that modes children, and look for a light
            foreach (TacticalComboVisualController tacticalComboVisualController in tacticalComboVisualControllers)
            {
                List<Transform> tacticalModes = tacticalModesField.GetValue(tacticalComboVisualController) as List<Transform>;
                if (CheckWhiteLight(tacticalModes)) PlayerState.WhiteLightActive = true;
                if (CheckVisibleLaser(tacticalModes)) PlayerState.LaserActive = true;
                if (CheckIRLight(tacticalModes)) PlayerState.IRLightActive = true;
                if (CheckIRLaser(tacticalModes)) PlayerState.IRLaserActive = true;
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (__instance.AimingDevices.Length > 0 && __instance.AimingDevices.Any(x => x.Light.IsActive))
                {
                    CheckDevice(__instance, tacticalModesField);
                    PlayerState.HasActiveDevice = true;

                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                    if (nvgIsOn)
                    {
                        float bonus = 
                            PlayerState.IRLaserActive || PlayerState.LaserActive ? 0.5f : 
                            PlayerState.IRLightActive && (PlayerState.IRLaserActive || PlayerState.LaserActive) ? 0.4f :
                            PlayerState.IRLightActive ? 0.6f : 1f;

                        PlayerState.DeviceBonus = PlayerState.WhiteLightActive ? 1f : bonus;
                    }
                    else
                    {
                        PlayerState.DeviceBonus = 
                        PlayerState.LaserActive ? 0.5f : 
                        PlayerState.WhiteLightActive && PlayerState.LaserActive ? 0.4f :
                        PlayerState.WhiteLightActive ? 0.6f : 1f;
                    }

                }
                else
                {
                    PlayerState.HasActiveDevice = false;
                    PlayerState.DeviceBonus = 1f;
                    return;
                }
            }
        }
    }

    public class GetCameraRotationRecoilPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(NewRecoilShotEffect).GetMethod("GetCameraRotationRecoil", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(NewRecoilShotEffect __instance, ref Vector3 __result)
        {
            Vector3 currentCameraRotation = __instance.CameraRotationRecoil.GetRecoil(false);
            currentCameraRotation.y *= 0.75f;
            __result = currentCameraRotation;
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

            if (player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && !ignoreClamp)
            {
                /*deltaRotation = movementContext.ClampRotation(deltaRotation);*/

                if (!StanceController.IsMounting)
                {
                    initialRotation = movementContext.Rotation;
                }

                bool hybridBlocksReset = Plugin.EnableHybridRecoil.Value && !WeaponStats.HasShoulderContact && !Plugin.EnableHybridReset.Value;
                bool canResetVert = Plugin.ResetVertical.Value && !hybridBlocksReset;
                bool canResetHorz = Plugin.ResetHorizontal.Value && !hybridBlocksReset;

                float fpsFactor = 144 / Plugin.FPS;
                fpsFactor = 1 + (fpsFactor - 1) * 0.31f;

                if (RecoilController.ShotCount > RecoilController.PrevShotCount)
                {
                    float controlFactor = RecoilController.ShotCount <= 3f ? Plugin.PlayerControlMulti.Value * 3 : Plugin.PlayerControlMulti.Value;
                    RecoilController.PlayerControl += Mathf.Abs(deltaRotation.y) * controlFactor;

                    hasReset = false;
                    timer = 0f;

                    float shotCountFactor = (float)Math.Round(Mathf.Min(RecoilController.ShotCount * 0.4f, 1.2f), 2);
                    float baseAngle = RecoilController.BaseTotalRecoilAngle;
                    float totalRecAngle = StanceController.IsMounting ? Mathf.Min(baseAngle + 10, 90f) : StanceController.IsBracing ? Mathf.Min(baseAngle + 5f, 90f) : baseAngle;
                    totalRecAngle = WeaponStats._WeapClass != "pistol" ? totalRecAngle : totalRecAngle - 5;
                    float hipfireModifier = !StanceController.IsAiming ? 1.1f : 1f;
                    float dispersionSpeedFactor = WeaponStats._WeapClass != "pistol" ? 1f + (-WeaponStats.TotalDispersionDelta) : 1f;
                    float dispersionAngleFactor = WeaponStats._WeapClass != "pistol" ? 1f + (-WeaponStats.TotalDispersionDelta * 0.035f) : 1f;
                    float angle = (Plugin.RecoilDispersionFactor.Value == 0f ? 0f : ((90f - (totalRecAngle * dispersionAngleFactor)) / 50f));
                    float angleDispFactor = 90f / totalRecAngle;

                    float dispersion = Mathf.Max(RecoilController.FactoredTotalDispersion * Plugin.RecoilDispersionFactor.Value * shotCountFactor * angleDispFactor * hipfireModifier, 0f);
                    float dispersionSpeed = Math.Max(Time.time * Plugin.RecoilDispersionSpeed.Value * dispersionSpeedFactor, 0.1f);
                    float recoilClimbMulti = WeaponStats._WeapClass == "pistol" ? Plugin.PistolRecClimbFactor.Value : Plugin.RecoilClimbFactor.Value;

                    float xRotation = 0f;
                    float yRotation = 0f;

                    xRotation = (float)Math.Round(Mathf.Lerp(-dispersion, dispersion, Mathf.PingPong(dispersionSpeed, 1f)) + angle, 3) * fpsFactor;
                    yRotation = (float)Math.Round(Mathf.Min(-RecoilController.FactoredTotalVRecoil * recoilClimbMulti * shotCountFactor, 0f), 3) * fpsFactor;

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
                    float resetSpeed = RecoilController.BaseTotalConvergence * WeaponStats.ConvergenceDelta * Plugin.ResetSpeed.Value * fpsFactor;
                    float resetSens = isHybrid ? (float)Math.Round(Plugin.ResetSensitivity.Value * 0.4f, 3) : Plugin.ResetSensitivity.Value * fpsFactor;

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

                    if (targetRotation.y <= recordedRotation.y - (Plugin.RecoilClimbLimit.Value * fpsFactor))
                    {
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
                        adjustTargetVector(averageDistance, proposedDistance);
                    }

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
                    if (fc == null) return true;

                    Vector2 currentRotation = movementContext.Rotation;

                    deltaRotation *= (fc.AimingSensitivity * 0.9f);

                    float lowerClampXLimit = StanceController.BracingDirection == EBracingDirection.Top ? -19f : StanceController.BracingDirection == EBracingDirection.Right ? -4f : -15f;
                    float upperClampXLimit = StanceController.BracingDirection == EBracingDirection.Top ? 19f : StanceController.BracingDirection == EBracingDirection.Right ? 15f : 1f;

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

            if (player != null && player.IsYourPlayer) 
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

            if (player != null && player.IsYourPlayer)
            {

                //force stats to be calculated 
                float calcStats = firearmController.Weapon.ErgonomicsDelta;

                fcField.SetValue(__instance, firearmController);

                float stockedPistolFactor = WeaponStats.IsStockedPistol ? 0.75f : 1f;

                __instance.RecoilStableShotIndex = WeaponStats.IsStocklessPistol ? 2 : 1; 
                __instance.HandRotationRecoil.RecoilReturnTrajectoryOffset = template.RecoilReturnPathOffsetHandRotation * Plugin.AfterRecoilRandomness.Value;
                __instance.HandRotationRecoil.StableAngleIncreaseStep = template.RecoilStableAngleIncreaseStep;
                __instance.HandRotationRecoil.AfterRecoilOffsetVerticalRange = Vector2.zero; // template.PostRecoilVerticalRangeHandRotation * Plugin.AfterRecoilRandomness.Value;
                __instance.HandRotationRecoil.AfterRecoilOffsetHorizontalRange = Vector2.zero; // template.PostRecoilHorizontalRangeHandRotation * Plugin.AfterRecoilRandomness.Value;

                __instance.HandRotationRecoil.ProgressRecoilAngleOnStable = new Vector2(RecoilController.FactoredTotalDispersion * Plugin.RecoilRandomness.Value, RecoilController.FactoredTotalDispersion * Plugin.RecoilRandomness.Value);

                __instance.HandRotationRecoil.ReturnTrajectoryDumping = template.RecoilReturnPathDampingHandRotation ;
                __instance.HandRotationRecoilEffect.Damping = template.RecoilDampingHandRotation * Plugin.RecoilDampingMulti.Value; 
                __instance.HandRotationRecoil.CategoryIntensityMultiplier =  template.RecoilCategoryMultiplierHandRotation * Plugin.RecoilIntensity.Value * stockedPistolFactor; 

                float totalVRecoilDelta = Mathf.Max(0f, (1f + WeaponStats.VRecoilDelta) * (1f - recoilSuppressionX - recoilSuppressionY * recoilSuppressionFactor));
                float totalHRecoilDelta = Mathf.Max(0f, (1f + WeaponStats.HRecoilDelta) * (1f - recoilSuppressionX - recoilSuppressionY * recoilSuppressionFactor));


                //this may not be the right reference
                //need to figure out a sensible way to represent recoil of underbarrel
                /*                if (firearmController.Weapon.IsUnderBarrelDeviceActive) 
                                {
                                }
                */

                __instance.BasicRecoilRotationStrengthRange = new Vector2(0.95f, 1.05f); //should mess around with this, consider making it unique per weapon
                __instance.BasicRecoilPositionStrengthRange = new Vector2(0.95f, 1.05f); //should mess around with this

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

        private static float zeroShiftGunFactor(string weapType, string id) 
        {
            switch (weapType)
            {
                case "6165ac306ef05c2ce828ef74":
                case "6183afd850224f204c1da514":
                    return 1.5f;
            }

            switch (weapType) 
            {
                case "marksmanRifle":
                case "machinegun":
                case "grenadeLauncher":
                case "sniperRifle":
                    return 1.15f;
                case "pistol":
                case "smg":
                    return 0.5f;
                case "shotgun":
                    return 1f;
                default:
                    return 1;
            }
        }

        [PatchPrefix]
        public static bool Prefix(NewRecoilShotEffect __instance, float incomingForce)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer)
            {
                //Conditional recoil modifiers 
                float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
                float playerWeightFactorBuff = 1f - (totalPlayerWeight / 650f);
                float playerWeightFactorDebuff = 1f + (totalPlayerWeight / 200f);

                float activeAimingBonus = StanceController.CurrentStance == EStance.ActiveAiming ? 0.9f : 1f;
                float aimCamRecoilBonus = StanceController.CurrentStance == EStance.ActiveAiming || !StanceController.IsAiming ? 0.8f : 1f;
                float shortStockingDebuff = StanceController.CurrentStance == EStance.ShortStock ? 1.15f : 1f;
                float shortStockingCamBonus = StanceController.CurrentStance == EStance.ShortStock ? 0.6f : 1f;

                float beltFedPenalty = firearmController.Weapon.IsBeltMachineGun && !StanceController.IsBracing && !StanceController.IsMounting ? 1.1f : 1f;
                float mountingDispModi = Mathf.Clamp(StanceController.BracingRecoilBonus, 0.85f, 1f);
                float baseRecoilAngle = RecoilController.BaseTotalRecoilAngle;
                    
                float opticRecoilMulti = allowedCalibers.Contains(firearmController.Weapon.AmmoCaliber) && StanceController.IsAiming && WeaponStats.IsOptic && StanceController.IsAiming ? 0.95f : 1f;
                float fovFactor = (Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView / 70f);
      /*          float opticLimit = StanceController.IsAiming && WeaponStats.HasOptic ? 15f * fovFactor : Plugin.HRecLimitMulti.Value * fovFactor;*/

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
                float vertFactor = PlayerState.RecoilInjuryMulti * activeAimingBonus * shortStockingDebuff 
                    * playerWeightFactorBuff * StanceController.BracingRecoilBonus * shotFactor * opticRecoilMulti
                    * beltFedPenalty * Plugin.VertMulti.Value;
                float horzFactor = PlayerState.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorBuff * shotFactor * beltFedPenalty * Plugin.HorzMulti.Value;
                RecoilController.FactoredTotalVRecoil = vertFactor * RecoilController.BaseTotalVRecoil;
                RecoilController.FactoredTotalHRecoil = horzFactor * RecoilController.BaseTotalHRecoil;

                horzFactor *= fovFactor * opticRecoilMulti; //I put it here after setting FactoredTotalHRecoil so that visual recoil isn't affected
                rotationRecoilPower *= vertFactor;
                positionRecoilPower *= horzFactor;

                //Recalculate and modify dispersion
                float dispFactor = incomingForce * PlayerState.RecoilInjuryMulti * shortStockingDebuff 
                    * playerWeightFactorDebuff * mountingDispModi * opticRecoilMulti 
                    * beltFedPenalty * Plugin.DispMulti.Value;
                RecoilController.FactoredTotalDispersion = RecoilController.BaseTotalDispersion * dispFactor;

                __instance.HandRotationRecoil.ProgressRecoilAngleOnStable = new Vector2(RecoilController.FactoredTotalDispersion * Plugin.RecoilRandomness.Value, RecoilController.FactoredTotalDispersion * Plugin.RecoilRandomness.Value);

                __instance.BasicPlayerRecoilDegreeRange = new Vector2(RecoilController.BaseTotalRecoilAngle, RecoilController.BaseTotalRecoilAngle);
                __instance.BasicRecoilRadian = __instance.BasicPlayerRecoilDegreeRange * 0.017453292f;

                Vector2 finalRecoilRadian;
                __instance.method_3(out finalRecoilRadian);

                //Reset camera recoil values and modify by various factors
                float camShotFactor = RecoilController.ShotCount > 1 ? Mathf.Min((RecoilController.ShotCount * 0.1f) + 1f, 1.55f) : 1f;
                float totalCamRecoil = RecoilController.BaseTotalCamRecoil * incomingForce * PlayerState.RecoilInjuryMulti * shortStockingCamBonus 
                    * aimCamRecoilBonus * playerWeightFactorBuff * opticRecoilMulti * camShotFactor  * Plugin.CamMulti.Value;
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
                    Logger.LogWarning("camFactor " + (incomingForce * PlayerState.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff * opticRecoilMulti));
                    Logger.LogWarning("vertFactor " + vertFactor);
                    Logger.LogWarning("horzFactor " + horzFactor);
                    Logger.LogWarning("dispFactor " + dispFactor);
                    Logger.LogWarning("mounting " + StanceController.BracingRecoilBonus);
                    Logger.LogWarning("====================");
                }

                //Calculate offest for zero shift
                if (WeaponStats.ScopeAccuracyFactor < 1f)
                {
                    float gunFactor = zeroShiftGunFactor(WeaponStats._WeapClass, firearmController.Weapon.TemplateId);
                    float shiftRecoilFactor = (RecoilController.FactoredTotalVRecoil + RecoilController.FactoredTotalHRecoil) * (1f + totalCamRecoil) * gunFactor;
                    float scopeFactor = ((1f - WeaponStats.ScopeAccuracyFactor) + (shiftRecoilFactor * 0.1f));

                    int rnd = UnityEngine.Random.Range(1, 20);
                    if (scopeFactor > rnd)
                    {
                        float offsetFactor = scopeFactor * 0.015f;
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

            if (player != null && player.IsYourPlayer)
            {
                RecoilController.SetRecoilParams(__instance, firearmController.Weapon);
            }
        }
    }
}

