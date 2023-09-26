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

namespace RealismMod
{
    public class RecoilRotatePatch : ModulePatch
    {
        private static FieldInfo movementContextField;
        private static FieldInfo playerField;

        private static Vector2 recordedRotation = Vector3.zero;
        private static Vector2 targetRotation = Vector3.zero;
        private static bool hasReset = false;
        private static float timer = 0.0f;
        private static float resetTime = 0.5f;

        protected override MethodBase GetTargetMethod()
        {
            movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
            playerField = AccessTools.Field(typeof(GClass1667), "player_0");

            return typeof(MovementState).GetMethod("Rotate", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void resetTimer(Vector2 target, Vector2 current)
        {
            timer += Time.deltaTime;

            bool doHybridReset = (Plugin.EnableHybridRecoil.Value && !WeaponProperties.HasShoulderContact) || (Plugin.EnableHybridRecoil.Value && Plugin.HybridForAll.Value);
            if ((doHybridReset && timer >= resetTime && target == current) || (!doHybridReset && (timer >= resetTime || target == current)))
            {
                hasReset = true;
            }
        }

        [PatchPrefix]
        private static void Prefix(MovementState __instance, Vector2 deltaRotation, bool ignoreClamp)
        {
            GClass1667 MovementContext = (GClass1667)movementContextField.GetValue(__instance);
            Player player = (Player)playerField.GetValue(MovementContext);

            if (player.IsYourPlayer)
            {
                float fpsFactor = 144f / (1f / Time.unscaledDeltaTime);

                //restet is enabled && if hybrid for all is NOT enabled || if hybrid is eanbled + for all is false + is pistol or folded stock/stockless
                bool hybridBlocksReset = Plugin.EnableHybridRecoil.Value && !WeaponProperties.HasShoulderContact && !Plugin.EnableHybridReset.Value;
                bool canResetVert = Plugin.ResetVertical.Value && !hybridBlocksReset;
                bool canResetHorz = Plugin.ResetHorizontal.Value && !hybridBlocksReset;

                if (RecoilController.ShotCount > RecoilController.PrevShotCount)
                {
                    float controlFactor = RecoilController.ShotCount <= 2f ? Plugin.PlayerControlMulti.Value * 3 : Plugin.PlayerControlMulti.Value;
                    RecoilController.PlayerControl += Mathf.Abs(deltaRotation.y) * controlFactor;

                    hasReset = false;
                    timer = 0f;

                    FirearmController fc = player.HandsController as FirearmController;
                    float baseAngle = WeaponProperties._WeapClass != "pistol" ? RecoilController.BaseTotalRecoilAngle : RecoilController.BaseTotalRecoilAngle - 5;
                    float hipfireModifier = !Plugin.IsAiming ? 1.1f : 1f;
                    float shotCountFactor = (float)Math.Round(Mathf.Min(RecoilController.ShotCount * 0.38f, 1.65f), 2);
                    float dispersionSpeedFactor = WeaponProperties._WeapClass != "pistol" ? 1f + (-WeaponProperties.TotalDispersionDelta) : 1f;
                    float dispersionAngleFactor = WeaponProperties._WeapClass != "pistol" ? 1f + (-WeaponProperties.TotalDispersionDelta * 0.035f) : 1f;
                    float angle = (Plugin.RecoilDispersionFactor.Value == 0f ? 0f : ((90f - (baseAngle * dispersionAngleFactor)) / 50f));
                    float angleDispFactor = 90f / baseAngle;

                    float dispersion = Mathf.Max(RecoilController.FactoredTotalDispersion * Plugin.RecoilDispersionFactor.Value * shotCountFactor * fpsFactor * angleDispFactor * hipfireModifier, 0f);
                    float dispersionSpeed = Math.Max(Time.time * Plugin.RecoilDispersionSpeed.Value * dispersionSpeedFactor, 0.1f);
                    float recoilClimbMulti = WeaponProperties._WeapClass == "pistol" ? Plugin.PistolRecClimbFactor.Value : Plugin.RecoilClimbFactor.Value;

                    float xRotation = 0f;
                    float yRotation = 0f;

                    if (!RecoilController.IsVector)
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
                        Logger.LogWarning(xRotation);
                    }

                    //Spiral/circular, could modify x axis with ping pong or something to make it more random or simply use random.range
                    /*              spiralTime += Time.deltaTime * 20f;
                                  float xRotaion = Mathf.Sin(spiralTime * 10f) * 1f;
                                  float yRotation = Mathf.Cos(spiralTime * 10f) * 1f;*/

                    targetRotation = MovementContext.Rotation + new Vector2(xRotation, yRotation);

                    if ((canResetVert && (MovementContext.Rotation.y > recordedRotation.y + 2f || deltaRotation.y <= -1f)) || (canResetHorz && Mathf.Abs(deltaRotation.x) >= 1f))
                    {
                        recordedRotation = MovementContext.Rotation;
                    }

                }
                else if (!hasReset && !RecoilController.IsFiring)
                {
                    float resetSpeed = RecoilController.BaseTotalConvergence * WeaponProperties.ConvergenceDelta * Plugin.ResetSpeed.Value;

                    bool xIsBelowThreshold = Mathf.Abs(deltaRotation.x) <= Mathf.Clamp(Plugin.ResetSensitivity.Value / 2.5f, 0f, 0.1f);
                    bool yIsBelowThreshold = Mathf.Abs(deltaRotation.y) <= Plugin.ResetSensitivity.Value;

                    Vector2 resetTarget = MovementContext.Rotation;

                    if (canResetVert && canResetHorz && xIsBelowThreshold && yIsBelowThreshold)
                    {
                        resetTarget = new Vector2(recordedRotation.x, recordedRotation.y);
                        MovementContext.Rotation = Vector2.Lerp(MovementContext.Rotation, new Vector2(recordedRotation.x, recordedRotation.y), resetSpeed);
                    }
                    else if (canResetHorz && xIsBelowThreshold)
                    {
                        resetTarget = new Vector2(recordedRotation.x, MovementContext.Rotation.y);
                        MovementContext.Rotation = Vector2.Lerp(MovementContext.Rotation, new Vector2(recordedRotation.x, MovementContext.Rotation.y), resetSpeed);
                    }
                    else if (canResetVert && yIsBelowThreshold)
                    {
                        resetTarget = new Vector2(MovementContext.Rotation.x, recordedRotation.y);
                        MovementContext.Rotation = Vector2.Lerp(MovementContext.Rotation, new Vector2(MovementContext.Rotation.x, recordedRotation.y), resetSpeed);
                    }
                    else
                    {
                        resetTarget = MovementContext.Rotation;
                        recordedRotation = MovementContext.Rotation;
                    }

                    resetTimer(resetTarget, MovementContext.Rotation);
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

                    recordedRotation = MovementContext.Rotation;
                }
                if (RecoilController.IsFiring)
                {
                    if (targetRotation.y <= recordedRotation.y - Plugin.RecoilClimbLimit.Value)
                    {
                        targetRotation.y = MovementContext.Rotation.y;
                    }

                    MovementContext.Rotation = Vector2.Lerp(MovementContext.Rotation, targetRotation, Plugin.RecoilSmoothness.Value);
                }

                if (RecoilController.ShotCount == RecoilController.PrevShotCount)
                {
                    RecoilController.PlayerControl = Mathf.Lerp(RecoilController.PlayerControl, 0f, 0.05f);
                }
            }
        }
    }

    public class OnWeaponParametersChangedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ShotEffector).GetMethod("OnWeaponParametersChanged", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref ShotEffector __instance)
        {
            IWeapon _weapon = (IWeapon)AccessTools.Field(typeof(ShotEffector), "_weapon").GetValue(__instance);

            if (_weapon.Item.Owner.ID.StartsWith("pmc") || _weapon.Item.Owner.ID.StartsWith("scav"))
            {
         
                SkillsClass.GClass1743 buffInfo = (SkillsClass.GClass1743)AccessTools.Field(typeof(ShotEffector), "_buffs").GetValue(__instance);
                WeaponTemplate template = _weapon.WeaponTemplate;

                float vRecoilDelta;
                float hRecoilDelta;

                if (_weapon.IsUnderbarrelWeapon)
                {
                    Weapon _mainWeaponInHands = (Weapon)AccessTools.Field(typeof(ShotEffector), "_mainWeaponInHands").GetValue(__instance);

                    vRecoilDelta = _mainWeaponInHands.StockRecoilDelta;
                    hRecoilDelta = _mainWeaponInHands.StockRecoilDelta;
                }
                else
                {
                    vRecoilDelta = WeaponProperties.VRecoilDelta;
                    hRecoilDelta = WeaponProperties.HRecoilDelta;
                }

                float totalVRecoilDelta = Mathf.Max(0f, (1f + vRecoilDelta) * (1f - buffInfo.RecoilSupression.x));
                float totalHRecoilDelta = Mathf.Max(0f, (1f + hRecoilDelta) * (1f - buffInfo.RecoilSupression.x));

                float lowerMulti = 2f - Plugin.RandomnessMulti.Value;
                __instance.RecoilStrengthXy = new Vector2(Mathf.Clamp(0.9f * lowerMulti, 0.45f, 1f), Mathf.Clamp(1.15f * Plugin.RandomnessMulti.Value, 1f, 1.7f)) * __instance.ConvertFromTaxanomy(template.RecoilForceUp * totalVRecoilDelta);
                __instance.RecoilStrengthZ = new Vector2(Mathf.Clamp(0.65f * lowerMulti, 0.32f, 1f), Mathf.Clamp(1.05f * Plugin.RandomnessMulti.Value, 1f, 1.6f)) * __instance.ConvertFromTaxanomy(template.RecoilForceBack * totalHRecoilDelta);

                float buffFactoredDispersion = WeaponProperties.TotalDispersion * (1f - buffInfo.RecoilSupression.y);
                float angle = Mathf.LerpAngle(!Plugin.EnableAngle.Value ? 90f : WeaponProperties.TotalRecoilAngle * Plugin.RecoilAngleMulti.Value, 90f, buffInfo.RecoilSupression.y);
                __instance.RecoilDegree = new Vector2(angle - buffFactoredDispersion, angle + buffFactoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                float cameraRecoil = WeaponProperties.TotalCamRecoil;
                __instance.ShotVals[3].Intensity = cameraRecoil;
                __instance.ShotVals[4].Intensity = -cameraRecoil;

                RecoilController.BaseTotalCamRecoil = cameraRecoil;
                RecoilController.BaseTotalVRecoil = (float)Math.Round(__instance.RecoilStrengthXy.y, 3);
                RecoilController.BaseTotalHRecoil = (float)Math.Round(__instance.RecoilStrengthZ.y, 3);
                RecoilController.BaseTotalConvergence = WeaponProperties.TotalModdedConv * Plugin.ConvergenceMulti.Value;
                RecoilController.BaseTotalRecoilAngle = (float)Math.Round(angle, 2);
                RecoilController.BaseTotalDispersion = (float)Math.Round(buffFactoredDispersion, 2);
                RecoilController.BaseTotalRecoilDamping = (float)Math.Round(WeaponProperties.TotalRecoilDamping * Plugin.RecoilDampingMulti.Value, 3);
                RecoilController.BaseTotalHandDamping = (float)Math.Round(WeaponProperties.TotalRecoilHandDamping * Plugin.HandsDampingMulti.Value, 3);
                RecoilController.IsVector = _weapon.Item.TemplateId == "5fb64bc92b1b027b1f50bcf2" || _weapon.Item.TemplateId == "5fc3f2d5900b1d5091531e57" ? true : false;
                WeaponProperties.TotalWeaponWeight = _weapon.Item.GetSingleItemTotalWeight();

                if (WeaponProperties.WeapID != template._id) 
                {
                    Plugin.DidWeaponSwap = true;
                }
                WeaponProperties.WeapID = template._id;

                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class ProcessPatch : ModulePatch
    {
        private static FieldInfo iWeaponField;
        private static FieldInfo weaponClassField;
        private static FieldInfo intensityFactorsField;
        private static FieldInfo buffInfoField;

        protected override MethodBase GetTargetMethod()
        {
            iWeaponField = AccessTools.Field(typeof(ShotEffector), "_weapon");
            weaponClassField = AccessTools.Field(typeof(ShotEffector), "_mainWeaponInHands");
            intensityFactorsField = AccessTools.Field(typeof(ShotEffector), "_separateIntensityFactors");
            buffInfoField = AccessTools.Field(typeof(ShotEffector), "_buffs");

            return typeof(ShotEffector).GetMethod("Process");
        }

        [PatchPrefix]
        public static bool Prefix(ref ShotEffector __instance, float str = 1f)
        {
            IWeapon iWeapon = (IWeapon)iWeaponField.GetValue(__instance);

            if (iWeapon.Item.Owner.ID.StartsWith("pmc") || iWeapon.Item.Owner.ID.StartsWith("scav"))
            {
                Weapon weaponClass = (Weapon)weaponClassField.GetValue(__instance);
                Vector3 separateIntensityFactors = (Vector3)intensityFactorsField.GetValue(__instance);
                SkillsClass.GClass1743 buffInfo = (SkillsClass.GClass1743)buffInfoField.GetValue(__instance);

                float totalPlayerWeight = PlayerProperties.TotalUnmodifiedWeight - WeaponProperties.TotalWeaponWeight;
                float playerWeightFactorBuff = 1f - (totalPlayerWeight / 550f);
                float playerWeightFactorDebuff = 1f + (totalPlayerWeight / 100f);

                float activeAimingBonus = StanceController.IsActiveAiming ? 0.9f : 1f;
                float aimCamRecoilBonus = StanceController.IsActiveAiming || !Plugin.IsAiming ? 0.8f : 1f;
                float shortStockingDebuff = StanceController.IsShortStock ? 1.15f : 1f;
                float shortStockingCamBonus = StanceController.IsShortStock ? 0.75f : 1f;

                float mountingVertModi = StanceController.IsMounting ? StanceController.MountingRecoilBonus : StanceController.IsBracing ? StanceController.BracingRecoilBonus : 1f;
                float mountingDispModi = Mathf.Clamp(StanceController.IsMounting ? StanceController.MountingRecoilBonus * 1.25f : StanceController.IsBracing ? StanceController.BracingRecoilBonus * 1.2f : 1f, 0.85f, 1f);
                float baseRecoilAngle = RecoilController.BaseTotalRecoilAngle;
                float mountingAngleModi = StanceController.IsMounting ? Mathf.Min(baseRecoilAngle + 17f, 90f) : StanceController.IsBracing ? Mathf.Min(baseRecoilAngle + 10f, 90f) : baseRecoilAngle;

                Vector3 poseIntensityFactors = (Vector3)AccessTools.Field(typeof(ShotEffector), "_separateIntensityFactors").GetValue(__instance);

                float factoredDispersion = RecoilController.BaseTotalDispersion * str * PlayerProperties.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorDebuff * mountingDispModi * Plugin.DispMulti.Value;
                RecoilController.FactoredTotalDispersion = factoredDispersion;

                float angle = Mathf.LerpAngle(mountingAngleModi, 90f, buffInfo.RecoilSupression.y);
                __instance.RecoilDegree = new Vector2(angle - factoredDispersion, angle + factoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                float totalCamRecoil = RecoilController.BaseTotalCamRecoil * str * PlayerProperties.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff * Plugin.CamMulti.Value;
                RecoilController.FactoredTotalCamRecoil = totalCamRecoil;

                //try inverting for fun
                __instance.ShotVals[3].Intensity = totalCamRecoil;
                __instance.ShotVals[4].Intensity = -totalCamRecoil;

                float fovFactor = (Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView / 70f) * Plugin.HRecLimitMulti.Value;
                float opticLimit = Plugin.IsAiming && Plugin.IsOptic ? 15f * fovFactor: 30f * fovFactor;
                float shotFactor = 1f;
                if (weaponClass.WeapClass == "pistol" && RecoilController.ShotCount > 1f && weaponClass.SelectedFireMode == Weapon.EFireMode.fullauto) 
                {
                    shotFactor = 0.5f;
                }

                float totalDispersion = Random.Range(__instance.RecoilRadian.x, __instance.RecoilRadian.y);
                float totalVerticalRecoil = Random.Range(__instance.RecoilStrengthXy.x, __instance.RecoilStrengthXy.y)  * str * PlayerProperties.RecoilInjuryMulti * activeAimingBonus * shortStockingDebuff * playerWeightFactorBuff * mountingVertModi * shotFactor * Plugin.VertMulti.Value;
                float totalHorizontalRecoil = Random.Range(__instance.RecoilStrengthZ.x, __instance.RecoilStrengthZ.y) * str * PlayerProperties.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorBuff * shotFactor * Plugin.HorzMulti.Value;
                RecoilController.FactoredTotalVRecoil = totalVerticalRecoil;
                RecoilController.FactoredTotalHRecoil = totalHorizontalRecoil;

                totalHorizontalRecoil = Mathf.Min(totalHorizontalRecoil * fovFactor, opticLimit); //put it after setting factored so that visual recoil isn't affected
                __instance.RecoilDirection = new Vector3(-Mathf.Sin(totalDispersion) * totalVerticalRecoil * poseIntensityFactors.x, Mathf.Cos(totalDispersion) * totalVerticalRecoil * poseIntensityFactors.y, totalHorizontalRecoil * poseIntensityFactors.z) * __instance.Intensity;
                Vector2 heatDirection = (iWeapon != null) ? iWeapon.MalfState.OverheatBarrelMoveDir : Vector2.zero;
                float heatFactor = (iWeapon != null) ? iWeapon.MalfState.OverheatBarrelMoveMult : 0f;
                float totalRecoilFactor = (__instance.RecoilRadian.x + __instance.RecoilRadian.y) / 2f * ((__instance.RecoilStrengthXy.x + __instance.RecoilStrengthXy.y) / 2f) * heatFactor;
                __instance.RecoilDirection.x = __instance.RecoilDirection.x + heatDirection.x * totalRecoilFactor;
                __instance.RecoilDirection.y = __instance.RecoilDirection.y + heatDirection.y * totalRecoilFactor;

                ShotEffector.ShotVal[] shotVals = __instance.ShotVals;
                for (int i = 0; i < shotVals.Length; i++)
                {
                    shotVals[i].Process(__instance.RecoilDirection);
                }

                RecoilController.ShotCount++;
                RecoilController.ShotTimer = 0f;
                RecoilController.WiggleShotTimer = 0f;
                RecoilController.MovementSpeedShotTimer = 0f;
                StanceController.StanceShotTime = 0f;
                RecoilController.IsFiring = true;
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
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("Shoot");
        }

        [PatchPostfix]
        public static void PatchPostfix(EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            GInterface114 ginterface114 = (GInterface114)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "ginterface114_0").GetValue(__instance);

            if (ginterface114 != null && ginterface114.Weapon != null)
            {
                Weapon weapon = ginterface114.Weapon;
                Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(weapon.Owner.ID);
                if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
                {
                    RecoilController.SetRecoilParams(__instance, weapon);
                }
            }
        }
    }

    public class SetCurveParametersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.RecoilSpring).GetMethod("SetCurveParameters");
        }
        [PatchPostfix]
        public static void PatchPostfix(EFT.Animations.RecoilSpring __instance)
        {
            float[] _originalKeyValues = (float[])AccessTools.Field(typeof(EFT.Animations.RecoilSpring), "_originalKeyValues").GetValue(__instance);

            float value = __instance.ReturnSpeedCurve[0].value;
            for (int i = 1; i < _originalKeyValues.Length; i++)
            {
                Keyframe key = __instance.ReturnSpeedCurve[i];
                key.value = value + _originalKeyValues[i] * Plugin.ConvergenceSpeedCurve.Value;
                __instance.ReturnSpeedCurve.RemoveKey(i);
                __instance.ReturnSpeedCurve.AddKey(key);
            }
        }
    }

}

