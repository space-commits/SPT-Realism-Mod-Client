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
        private static float spiralTime = 0.0f;

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
        private static void Prefix(MovementState __instance, ref Vector2 deltaRotation, bool ignoreClamp)
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
                    float shotCountFactor = Mathf.Min(RecoilController.ShotCount * 0.4f, 1.75f);
                    float angle = ((90f - RecoilController.BaseTotalRecoilAngle) / 50f);
                    float dispersion = Mathf.Max(RecoilController.FactoredTotalDispersion * 2.5f * Plugin.RecoilDispersionFactor.Value * shotCountFactor * fpsFactor, 0f);
                    float dispersionSpeed = Math.Max(Time.time * Plugin.RecoilDispersionSpeed.Value, 0.1f);

                    float xRotation = 0f;
                    float yRotation = 0f;

                    //S pattern
                    if (!RecoilController.IsVector)
                    {
                        xRotation = Mathf.Lerp(-dispersion * 1.1f, dispersion * 1.1f, Mathf.PingPong(dispersionSpeed, 1f)) + angle;
                        yRotation = Mathf.Min(-RecoilController.FactoredTotalVRecoil * Plugin.RecoilClimbFactor.Value * shotCountFactor * fpsFactor, 0f);
                    }
                    else
                    {
                        //spiral + pingpong, would work well as vector recoil
                        spiralTime += Time.deltaTime * 20f;
                        float recoilAmount = RecoilController.FactoredTotalVRecoil * Plugin.RecoilClimbFactor.Value * shotCountFactor * fpsFactor;
                        xRotation = Mathf.Sin(spiralTime * 10f) * 1f;
                        yRotation = Mathf.Lerp(-recoilAmount, recoilAmount, Mathf.PingPong(Time.time * 4f, 1f));
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
                    float resetSpeed = RecoilController.BaseTotalConvergence * Plugin.ResetSpeed.Value;

                    bool xIsBelowThreshold = Mathf.Abs(deltaRotation.x) <= Plugin.ResetSensitivity.Value;
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

                __instance.RecoilStrengthXy = new Vector2(0.9f, 1.15f) * __instance.ConvertFromTaxanomy(template.RecoilForceUp * totalVRecoilDelta);
                __instance.RecoilStrengthZ = new Vector2(0.65f, 1.05f) * __instance.ConvertFromTaxanomy(template.RecoilForceBack * totalHRecoilDelta);

                float buffFactoredDispersion = WeaponProperties.TotalDispersion * (1f - buffInfo.RecoilSupression.y);
                float angle = Mathf.LerpAngle(WeaponProperties.TotalRecoilAngle, 90f, buffInfo.RecoilSupression.y);
                __instance.RecoilDegree = new Vector2(angle - buffFactoredDispersion, angle + buffFactoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                float cameraRecoil = WeaponProperties.TotalCamRecoil;
                __instance.ShotVals[3].Intensity = cameraRecoil;
                __instance.ShotVals[4].Intensity = -cameraRecoil;

                RecoilController.BaseTotalCamRecoil = cameraRecoil;
                RecoilController.BaseTotalVRecoil = (float)Math.Round(__instance.RecoilStrengthXy.y, 3);
                RecoilController.BaseTotalHRecoil = (float)Math.Round(__instance.RecoilStrengthZ.y, 3);
                RecoilController.BaseTotalConvergence = WeaponProperties.ModdedConv * Plugin.ConvergenceMulti.Value;
                RecoilController.BaseTotalRecoilAngle = (float)Math.Round(angle, 2);
                RecoilController.BaseTotalDispersion = (float)Math.Round(buffFactoredDispersion, 2);
                RecoilController.BaseTotalRecoilDamping = (float)Math.Round(WeaponProperties.TotalRecoilDamping, 3);
                RecoilController.BaseTotalHandDamping = (float)Math.Round(WeaponProperties.TotalRecoilHandDamping, 3);

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

                Plugin.CurrentlyShootingWeapon = weaponClass;

                RecoilController.ShotTimer = 0f;
                StanceController.StanceShotTime = 0f;
                RecoilController.IsFiring = true;
                StanceController.IsFiringFromStance = true;
                RecoilController.ShotCount++;

                float totalPlayerWeight = PlayerProperties.TotalUnmodifiedWeight - weaponClass.GetSingleItemTotalWeight();
                float playerWeightFactorBuff = 1f - (totalPlayerWeight / 550f);
                float playerWeightFactorDebuff = 1f + (totalPlayerWeight / 100f);

                float activeAimingBonus = StanceController.IsActiveAiming ? 0.9f : 1f;
                float aimCamRecoilBonus = StanceController.IsActiveAiming || !Plugin.IsAiming ? 0.8f : 1f;
                float shortStockingDebuff = StanceController.IsShortStock ? 1.15f : 1f;
                float shortStockingCamBonus = StanceController.IsShortStock ? 0.75f : 1f;

                float mountingVertModi = StanceController.IsMounting ? StanceController.MountingRecoilBonus : StanceController.IsBracing ? StanceController.BracingRecoilBonus : 1f;
                float mountingDispModi = Mathf.Clamp(StanceController.IsMounting ? StanceController.MountingRecoilBonus * 1.25f : StanceController.IsBracing ? StanceController.BracingRecoilBonus * 1.2f : 1f, 0.85f, 1f);
                float mountingAngleModi = StanceController.IsMounting ? Mathf.Min(RecoilController.BaseTotalRecoilAngle + 17f, 90f) : StanceController.IsBracing ? Mathf.Min(RecoilController.BaseTotalRecoilAngle + 10f, 90f) : RecoilController.BaseTotalRecoilAngle;

                Vector3 _separateIntensityFactors = (Vector3)AccessTools.Field(typeof(ShotEffector), "_separateIntensityFactors").GetValue(__instance);

                float factoredDispersion = RecoilController.BaseTotalDispersion * str * PlayerProperties.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorDebuff * mountingDispModi;

                if (RecoilController.ShotCount > 1 && weaponClass.WeapClass == "pistol" && weaponClass.SelectedFireMode == Weapon.EFireMode.fullauto)
                {
                    factoredDispersion *= 0.5f;
                    __instance.RecoilStrengthZ.x *= 0.5f;
                    __instance.RecoilStrengthZ.y *= 0.5f;
                    __instance.RecoilStrengthXy.x = Mathf.Min(__instance.RecoilStrengthXy.x * 0.3f, 100f);
                    __instance.RecoilStrengthXy.y = Mathf.Min(__instance.RecoilStrengthXy.y * 0.3f, 100f);
                }

                float angle = Mathf.LerpAngle(mountingAngleModi, 90f, buffInfo.RecoilSupression.y);
                __instance.RecoilDegree = new Vector2(angle - factoredDispersion, angle + factoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                float totalCamRecoil = RecoilController.BaseTotalCamRecoil * str * PlayerProperties.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff * Plugin.CamMulti.Value;
                RecoilController.FactoredTotalCamRecoil = totalCamRecoil;

                //try inverting for fun
                __instance.ShotVals[3].Intensity = totalCamRecoil;
                __instance.ShotVals[4].Intensity = -totalCamRecoil;

                float totalDispersion = Random.Range(__instance.RecoilRadian.x, __instance.RecoilRadian.y) * Plugin.DispMulti.Value;
                float totalVerticalRecoil = Random.Range(__instance.RecoilStrengthXy.x, __instance.RecoilStrengthXy.y)  * str * PlayerProperties.RecoilInjuryMulti * activeAimingBonus * shortStockingDebuff * playerWeightFactorBuff * mountingVertModi * Plugin.VertMulti.Value;
                float totalHorizontalRecoil = Random.Range(__instance.RecoilStrengthZ.x, __instance.RecoilStrengthZ.y) * str * PlayerProperties.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorBuff * Plugin.HorzMulti.Value;

                RecoilController.FactoredTotalDispersion = totalDispersion;
                RecoilController.FactoredTotalVRecoil = totalVerticalRecoil;
                RecoilController.FactoredTotalHRecoil = totalHorizontalRecoil;

                __instance.RecoilDirection = new Vector3(-Mathf.Sin(totalDispersion) * totalVerticalRecoil * _separateIntensityFactors.x, Mathf.Cos(totalDispersion) * totalVerticalRecoil * _separateIntensityFactors.y, totalHorizontalRecoil * _separateIntensityFactors.z) * __instance.Intensity;
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
                return false;
            }
            else
            {
                return true;
            }
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

