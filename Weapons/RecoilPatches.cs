using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.Animations.NewRecoil;
using EFT.InventoryLogic;
using EFT.Visual;
using HarmonyLib;
using SPT.Reflection.Patching;
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
        private static FieldInfo _playerField;
        private static FieldInfo _tacticalModesField;


        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            _tacticalModesField = AccessTools.Field(typeof(TacticalComboVisualController), "list_0");
            return typeof(EFT.Player.FirearmController).GetMethod("UpdateHipInaccuracy", BindingFlags.Instance | BindingFlags.Public);
        }

        //thanks to Solarint for letting me use these checks
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

            PlayerValues.WhiteLightActive = false;
            PlayerValues.LaserActive = false;
            PlayerValues.IRLightActive = false;
            PlayerValues.IRLaserActive = false;

            // Loop through all of the tacticalComboVisualControllers, then its modes, then that modes children, and look for a light
            foreach (TacticalComboVisualController tacticalComboVisualController in tacticalComboVisualControllers)
            {
                List<Transform> tacticalModes = tacticalModesField.GetValue(tacticalComboVisualController) as List<Transform>;
                if (CheckWhiteLight(tacticalModes)) PlayerValues.WhiteLightActive = true;
                if (CheckVisibleLaser(tacticalModes)) PlayerValues.LaserActive = true;
                if (CheckIRLight(tacticalModes)) PlayerValues.IRLightActive = true;
                if (CheckIRLaser(tacticalModes)) PlayerValues.IRLaserActive = true;
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                if (__instance.AimingDevices.Length > 0 && __instance.AimingDevices.Any(x => x.Light.IsActive))
                {
                    CheckDevice(__instance, _tacticalModesField);
                    PlayerValues.HasActiveDevice = true;

                    NightVisionComponent nvgComponent = player.NightVisionObserver.Component;
                    bool nvgIsOn = nvgComponent != null && (nvgComponent.Togglable == null || nvgComponent.Togglable.On);

                    if (nvgIsOn)
                    {
                        float bonus = 
                            PlayerValues.IRLaserActive || PlayerValues.LaserActive ? 0.5f : 
                            PlayerValues.IRLightActive && (PlayerValues.IRLaserActive || PlayerValues.LaserActive) ? 0.4f :
                            PlayerValues.IRLightActive ? 0.6f : 1f;

                        PlayerValues.DeviceBonus = PlayerValues.WhiteLightActive ? 1f : bonus;
                    }
                    else
                    {
                        PlayerValues.DeviceBonus = 
                        PlayerValues.LaserActive ? 0.5f : 
                        PlayerValues.WhiteLightActive && PlayerValues.LaserActive ? 0.4f :
                        PlayerValues.WhiteLightActive ? 0.6f : 1f;
                    }
                }
                else
                {
                    PlayerValues.HasActiveDevice = false;
                    PlayerValues.DeviceBonus = 1f;
                    return;
                }
            }
        }
    }

    //unused for now
/*    public class GetCameraRotationRecoilPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(NewRecoilShotEffect).GetMethod("GetCameraRotationRecoil", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(NewRecoilShotEffect __instance, ref Vector3 __result)
        {
            Vector3 currentCameraRotation = __instance.CameraRotationRecoil.GetRecoil(false);
            currentCameraRotation.x *= PluginConfig.test1.Value; //0.75
            currentCameraRotation.y *= PluginConfig.test2.Value; //1
            __result = currentCameraRotation;
            return false;

        }
    }*/

    public class RotatePatch : ModulePatch
    {
        private static FieldInfo _movementContextField;
        private static FieldInfo _playerField;

        private const float FpsFactor = 144f;
        private const float FpsSmoothingFactor = 0.42f;
        private const float ShotCountThreshold = 3f;

        private static Vector2 _initialRotation = Vector2.zero;

        protected override MethodBase GetTargetMethod()
        {
            _movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            _playerField = AccessTools.Field(typeof(MovementContext), "_player");

            return typeof(MovementState).GetMethod("Rotate", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void DoMounting(MovementContext movementContext, FirearmController fc, ref Vector2 deltaRotation) 
        {
            Vector2 currentRotation = movementContext.Rotation;

            deltaRotation *= (fc.AimingSensitivity * 0.9f);

            float xLimit = WeaponStats.BipodIsDeployed ? 14f : 19f;
            float lowerClampXLimit = StanceController.BracingDirection == EBracingDirection.Top ? -xLimit : StanceController.BracingDirection == EBracingDirection.Right ? -4f : -15f;
            float upperClampXLimit = StanceController.BracingDirection == EBracingDirection.Top ? xLimit : StanceController.BracingDirection == EBracingDirection.Right ? 15f : 1f;

            float yLimit = WeaponStats.BipodIsDeployed ? 12f : 10f;
            float lowerClampYLimit = StanceController.BracingDirection == EBracingDirection.Top ? -yLimit : -8f;
            float upperClampYLimit = StanceController.BracingDirection == EBracingDirection.Top ? yLimit : 15f;

            float relativeLowerXLimit = _initialRotation.x + lowerClampXLimit;
            float relativeUpperXLimit = _initialRotation.x + upperClampXLimit;
            float relativeLowerYLimit = _initialRotation.y + lowerClampYLimit;
            float relativeUpperYLimit = _initialRotation.y + upperClampYLimit;

            float clampedX = Mathf.Clamp(currentRotation.x + deltaRotation.x, relativeLowerXLimit, relativeUpperXLimit);
            float clampedY = Mathf.Clamp(currentRotation.y + deltaRotation.y, relativeLowerYLimit, relativeUpperYLimit);

            deltaRotation.x = clampedX - currentRotation.x;
            deltaRotation.y = clampedY - currentRotation.y;

            deltaRotation = movementContext.ClampRotation(deltaRotation);
            movementContext.Rotation += deltaRotation;
        }

        [PatchPrefix]
        private static bool Prefix(MovementState __instance, Vector2 deltaRotation, bool ignoreClamp)
        {
            MovementContext movementContext = (MovementContext)_movementContextField.GetValue(__instance);
            Player player = (Player)_playerField.GetValue(movementContext);

            if (player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary && !ignoreClamp)
            {
                deltaRotation = movementContext.ClampRotation(deltaRotation);

                if (!StanceController.IsMounting)
                {
                    _initialRotation = movementContext.Rotation;
                }

                if (StanceController.IsMounting)
                {
                    FirearmController fc = player.HandsController as FirearmController;
                    if (fc == null) return true;
                    DoMounting(movementContext, fc, ref deltaRotation);
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

                float angle = Mathf.LerpAngle(WeaponStats.TotalRecoilAngle * PluginConfig.RecoilAngleMulti.Value, (float)__instance.HandRotationRecoil.MaxRecoilRotationAngle, recoilSuspensionY);
                float dispersion = WeaponStats.TotalDispersion / (1f + recoilSuspensionY);
                __instance.BasicPlayerRecoilDegreeRange = new Vector2(angle - dispersion, angle + dispersion);
                __instance.BasicRecoilRadian = __instance.BasicPlayerRecoilDegreeRange * 0.017453292f;

                ShootController.BaseTotalRecoilAngle = (float)Math.Round(angle, 2);
                ShootController.BaseTotalDispersion = (float)Math.Round(dispersion, 2);

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
                __instance.HandRotationRecoil.RecoilReturnTrajectoryOffset = template.RecoilReturnPathOffsetHandRotation * PluginConfig.AfterRecoilRandomness.Value;
                __instance.HandRotationRecoil.StableAngleIncreaseStep = template.RecoilStableAngleIncreaseStep;
                __instance.HandRotationRecoil.AfterRecoilOffsetVerticalRange = Vector2.zero; // template.PostRecoilVerticalRangeHandRotation * Plugin.AfterRecoilRandomness.Value;
                __instance.HandRotationRecoil.AfterRecoilOffsetHorizontalRange = Vector2.zero; // template.PostRecoilHorizontalRangeHandRotation * Plugin.AfterRecoilRandomness.Value;

                __instance.HandRotationRecoil.ProgressRecoilAngleOnStable = new Vector2(ShootController.FactoredTotalDispersion * PluginConfig.RecoilRandomness.Value, ShootController.FactoredTotalDispersion * PluginConfig.RecoilRandomness.Value);

                __instance.HandRotationRecoil.ReturnTrajectoryDumping = template.RecoilReturnPathDampingHandRotation ;
                __instance.HandRotationRecoilEffect.Damping = template.RecoilDampingHandRotation * PluginConfig.RecoilDampingMulti.Value; 
                __instance.HandRotationRecoil.CategoryIntensityMultiplier =  template.RecoilCategoryMultiplierHandRotation * PluginConfig.RecoilIntensity.Value * stockedPistolFactor; 

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
                ShootController.BaseTotalCamRecoil = cameraRecoil;

                ShootController.BaseTotalVRecoil = (float)Math.Round(__instance.BasicPlayerRecoilRotationStrength.y, 3);
                ShootController.BaseTotalHRecoil = (float)Math.Round(__instance.BasicPlayerRecoilPositionStrength.y, 3);
                ShootController.BaseTotalConvergence = WeaponStats.TotalModdedConv * (WeaponStats.IsPistol ? PluginConfig.PistolConvergenceMulti.Value : PluginConfig.ConvergenceMulti.Value);

                ShootController.BaseTotalRecoilDamping = (float)Math.Round(WeaponStats.TotalRecoilDamping * PluginConfig.RecoilDampingMulti.Value, 3);
                ShootController.BaseTotalHandDamping = (float)Math.Round(WeaponStats.TotalRecoilHandDamping * PluginConfig.HandsDampingMulti.Value, 3);
                WeaponStats.TotalWeaponWeight = firearmController.Weapon.TotalWeight;
                WeaponStats.TotalWeaponLength = firearmController.Item.CalculateCellSize().X;
                if (WeaponStats.WeapID != template._id)
                {
                    StanceController.DidWeaponSwap = true;
                }
                WeaponStats.WeapID = template._id;

                if (PluginConfig.EnableRecoilLogging.Value)
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

        private static float ZeroShiftGunFactor(string weapType, string id) 
        {
            switch (id)
            {
                case "6165ac306ef05c2ce828ef74":
                case "6183afd850224f204c1da514":
                    return 1.35f;
            }

            switch (weapType) 
            {
                case "marksmanRifle":
                case "machinegun":
                case "grenadeLauncher":
                case "sniperRifle":
                    return 1.15f;
                case "pistol":
                    return 0.15f;
                case "smg":
                    return 0.5f;
                case "shotgun":
                    return 1f;
                default:
                    return 1;
            }
        }


        private static float PistolShotFactor(int shot)
        {
            switch (shot)
            {
                case 0:
                    return 0.9f;
                case 1:
                    return 0.15f;
                case 2:
                    return 0.2f;
                case 3:
                    return 0.3f;
                case 4:
                    return 0.4f;
                case 5:
                    return 0.45f;
                case 6:
                    return 0.5f;
                case 7:
                    return 0.6f;
                case >= 8:
                    return 0.7f;
                default:
                    return 1;
            }
        }

        public static float RifleShotModifier(int shotCount)
        {
            return 0.95f + (shotCount * 0.05f);
        }

        [PatchPrefix]
        public static bool Prefix(NewRecoilShotEffect __instance, float incomingForce)
        {
            FirearmController firearmController = (FirearmController)fcField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(firearmController);

            if (player != null && player.IsYourPlayer)
            {
                //Conditional recoil modifiers 
                float totalPlayerWeight = WeaponStats.IsStocklessPistol || (!WeaponStats.HasShoulderContact && !WeaponStats.IsPistol) ? 0f : PlayerValues.TotalModifiedWeightMinusWeapon;
                float playerWeightFactorBuff = 1f - (totalPlayerWeight / 650f);
                float playerWeightFactorDebuff = 1f + (totalPlayerWeight / 200f);
                float leftShoulderFactor = StanceController.IsLeftShoulder ? 1.14f : 1f;

                float activeAimingBonus = StanceController.CurrentStance == EStance.ActiveAiming ? 0.9f : 1f;
                float aimCamRecoilBonus = StanceController.CurrentStance == EStance.ActiveAiming || !StanceController.IsAiming ? 0.8f : 1f;
                float shortStockingDebuff = StanceController.CurrentStance == EStance.ShortStock ? 1.15f : 1f;
                float shortStockingCamBonus = StanceController.CurrentStance == EStance.ShortStock ? 0.6f : 1f;

                float mountingDispMulti = Mathf.Clamp(Mathf.Pow(StanceController.BracingRecoilBonus, 0.75f), 0.8f, 1f);

                float baseRecoilAngle = ShootController.BaseTotalRecoilAngle;
                    
                float opticRecoilMulti = allowedCalibers.Contains(firearmController.Weapon.AmmoCaliber) && StanceController.IsAiming && WeaponStats.IsOptic && StanceController.IsAiming ? 0.95f : 1f;
                float fovFactor = (Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView / 70f);
                /*float opticLimit = StanceController.IsAiming && WeaponStats.HasOptic ? 15f * fovFactor : Plugin.HRecLimitMulti.Value * fovFactor;*/

                float pistolShotFactor = firearmController.Weapon.WeapClass == "pistol" && firearmController.Weapon.SelectedFireMode == Weapon.EFireMode.fullauto ? PistolShotFactor(ShootController.ShotCount) : 1f;
                float rifleShotFactor = firearmController.Weapon.WeapClass != "pistol" ? Mathf.Clamp(RifleShotModifier(ShootController.ShotCount), 0.95f, 1.15f): 1f;

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
                float vertFactor = PlayerValues.RecoilInjuryMulti * activeAimingBonus * shortStockingDebuff * playerWeightFactorBuff * 
                    StanceController.BracingRecoilBonus * opticRecoilMulti * pistolShotFactor *
                    leftShoulderFactor;
                vertFactor = Mathf.Clamp(vertFactor, 0.25f, 1.25f) * PluginConfig.VertMulti.Value;
                float horzFactor = PlayerValues.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorBuff * pistolShotFactor;
                horzFactor = Mathf.Clamp(horzFactor, 0.25f, 1.25f) * PluginConfig.HorzMulti.Value;
                ShootController.FactoredTotalVRecoil = vertFactor * ShootController.BaseTotalVRecoil;
                ShootController.FactoredTotalHRecoil = horzFactor * ShootController.BaseTotalHRecoil;

                horzFactor *= fovFactor * opticRecoilMulti; //I put it here after setting FactoredTotalHRecoil so that visual recoil isn't affected
                rotationRecoilPower *= vertFactor * rifleShotFactor; //don't want this factor to affect recoil climb
                positionRecoilPower *= horzFactor;

                //Recalculate and modify dispersion
                float dispFactor = incomingForce * PlayerValues.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorDebuff * 
                    mountingDispMulti * opticRecoilMulti * leftShoulderFactor * rifleShotFactor * PluginConfig.DispMulti.Value;
                ShootController.FactoredTotalDispersion = ShootController.BaseTotalDispersion * dispFactor;

                __instance.HandRotationRecoil.ProgressRecoilAngleOnStable = new Vector2(ShootController.FactoredTotalDispersion * PluginConfig.RecoilRandomness.Value, ShootController.FactoredTotalDispersion * PluginConfig.RecoilRandomness.Value);

                __instance.BasicPlayerRecoilDegreeRange = new Vector2(ShootController.BaseTotalRecoilAngle, ShootController.BaseTotalRecoilAngle);
                __instance.BasicRecoilRadian = __instance.BasicPlayerRecoilDegreeRange * 0.017453292f;

                Vector2 finalRecoilRadian;
                __instance.method_3(out finalRecoilRadian);

                //Reset camera recoil values and modify by various factors
                float camShotFactor = ShootController.ShotCount > 1 ? Mathf.Min((ShootController.ShotCount * 0.1f) + 1f, 1.55f) : 1f;
                float totalCamRecoil = ShootController.BaseTotalCamRecoil * incomingForce * PlayerValues.RecoilInjuryMulti * shortStockingCamBonus 
                    * aimCamRecoilBonus * playerWeightFactorBuff * opticRecoilMulti * camShotFactor * PluginConfig.CamMulti.Value;
                ShootController.FactoredTotalCamRecoil = totalCamRecoil;
                __instance.ShotRecoilProcessValues[3].IntensityMultiplicator = totalCamRecoil;
                __instance.ShotRecoilProcessValues[4].IntensityMultiplicator = -totalCamRecoil;

                //Do recoil
                __instance.method_1(finalRecoilRadian, rotationRecoilPower, positionRecoilPower);
                ShotEffector.RecoilShotVal[] shotRecoilProcessValues = __instance.ShotRecoilProcessValues;
                for (int i = 0; i < shotRecoilProcessValues.Length; i++)
                {
                    shotRecoilProcessValues[i].Process(__instance.RecoilDirection);
                }

                if (PluginConfig.EnableRecoilLogging.Value) 
                {
                    Logger.LogWarning("==========shoot==========");
                    Logger.LogWarning("camFactor " + (incomingForce * PlayerValues.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff * opticRecoilMulti));
                    Logger.LogWarning("vertFactor " + vertFactor);
                    Logger.LogWarning("horzFactor " + horzFactor);
                    Logger.LogWarning("dispFactor " + dispFactor);
                    Logger.LogWarning("mounting " + StanceController.BracingRecoilBonus);
                    Logger.LogWarning("====================");
                }

                //Calculate offest for zero shift
                if (WeaponStats.ScopeAccuracyFactor < 0f)
                {
                    float gunFactor = ZeroShiftGunFactor(WeaponStats._WeapClass, firearmController.Weapon.TemplateId);
                    float shiftRecoilFactor = (ShootController.FactoredTotalVRecoil + ShootController.FactoredTotalHRecoil) * (1f + totalCamRecoil) * gunFactor;
                    float scopeFactor = ((1f - WeaponStats.ScopeAccuracyFactor) * 2f) + (shiftRecoilFactor * 0.1f) * 0.15f;

                    int rnd = UnityEngine.Random.Range(1, 21);
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
                ShootController.ShotCount++;
                ShootController.FiringTimer = 0f;
                ShootController.ShotCountTimer = 0f;
                ShootController.DeafenShotTimer = 0f;
                ShootController.WiggleShotTimer = 0f;
                ShootController.MovementSpeedShotTimer = 0f;
                StanceController.StanceShotTime = 0f;
                ShootController.IsFiring = true;
                ShootController.IsFiringDeafen = true;
                ShootController.IsFiringWiggle = true;
                ShootController.IsFiringMovement = true;
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
                ShootController.SetRecoilParams(__instance, firearmController.Weapon, player);
            }
        }
    }
}

