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
         
                SkillsClass.GClass1680 buffInfo = (SkillsClass.GClass1680)AccessTools.Field(typeof(ShotEffector), "_buffs").GetValue(__instance);
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

                float buffFactoredDispersion = WeaponProperties.Dispersion * (1f - buffInfo.RecoilSupression.y);
                float angle = Mathf.LerpAngle(WeaponProperties.RecoilAngle, 90f, buffInfo.RecoilSupression.y);
                __instance.RecoilDegree = new Vector2(angle - buffFactoredDispersion, angle + buffFactoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                float cameraRecoil = WeaponProperties.CamRecoil;
                __instance.ShotVals[3].Intensity = cameraRecoil;
                __instance.ShotVals[4].Intensity = -cameraRecoil;

                Plugin.StartingCamRecoilX = (float)Math.Round(cameraRecoil, 4);
                Plugin.StartingCamRecoilY = (float)Math.Round(-cameraRecoil, 4);
                Plugin.CurrentCamRecoilX = Plugin.StartingCamRecoilX;
                Plugin.CurrentCamRecoilY = Plugin.StartingCamRecoilY;

                Plugin.StartingVRecoilX = (float)Math.Round(__instance.RecoilStrengthXy.x, 3);
                Plugin.StartingVRecoilY = (float)Math.Round(__instance.RecoilStrengthXy.y, 3);
                Plugin.CurrentVRecoilX = Plugin.StartingVRecoilX;
                Plugin.CurrentVRecoilY = Plugin.StartingVRecoilY;

                Plugin.StartingHRecoilX = (float)Math.Round(__instance.RecoilStrengthZ.x, 3);
                Plugin.StartingHRecoilY = (float)Math.Round(__instance.RecoilStrengthZ.y, 3);
                Plugin.CurrentHRecoilX = Plugin.StartingHRecoilX;
                Plugin.CurrentHRecoilY = Plugin.StartingHRecoilY;

                Plugin.StartingConvergence = (float)Math.Round(WeaponProperties.ModdedConv * Singleton<BackendConfigSettingsClass>.Instance.Aiming.RecoilConvergenceMult, 2);
                Plugin.CurrentConvergence = Plugin.StartingConvergence;
                Plugin.ConvergenceProporitonK = (float)Math.Round(Plugin.StartingConvergence * Plugin.StartingVRecoilX, 2);

                Plugin.StartingRecoilAngle = (float)Math.Round(angle, 2);

                Plugin.StartingDispersion = (float)Math.Round(buffFactoredDispersion, 2);
                Plugin.CurrentDispersion = Plugin.StartingDispersion;
/*                Plugin.dispersionProportionK = (float)Math.Round(Plugin.startingDispersion * Plugin.startingVRecoilX, 2);
*/
                Plugin.StartingDamping = (float)Math.Round(WeaponProperties.TotalRecoilDamping, 3);
                Plugin.CurrentDamping = Plugin.StartingDamping;

                Plugin.StartingHandDamping = (float)Math.Round(WeaponProperties.TotalRecoilHandDamping, 3);
                Plugin.CurrentHandDamping = Plugin.StartingHandDamping;

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
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ShotEffector).GetMethod("Process");
        }
        [PatchPrefix]
        public static bool Prefix(ref ShotEffector __instance, float str = 1f)
        {

            IWeapon iWeapon = (IWeapon)AccessTools.Field(typeof(ShotEffector), "_weapon").GetValue(__instance);
            Weapon weaponClass = (Weapon)AccessTools.Field(typeof(ShotEffector), "_mainWeaponInHands").GetValue(__instance);

            if (iWeapon.Item.Owner.ID.StartsWith("pmc") || iWeapon.Item.Owner.ID.StartsWith("scav"))
            {
                Plugin.CurrentlyShootingWeapon = weaponClass;

                Plugin.ShotTimer = 0f;
                StanceController.StanceShotTime = 0f;
                Plugin.IsFiring = true;
                StanceController.IsFiringFromStance = true;
                Plugin.ShotCount++;

                float totalPlayerWeight = PlayerProperties.TotalModifiedWeight - weaponClass.GetSingleItemTotalWeight();
                float playerWeightFactorBuff = 1f - (totalPlayerWeight / 550f);
                float playerWeightFactorDebuff = 1f + (totalPlayerWeight / 100f);

                float activeAimingBonus = StanceController.IsActiveAiming ? 0.9f : 1f;
                float aimCamRecoilBonus = StanceController.IsActiveAiming || !Plugin.IsAiming ? 0.8f : 1f;
                float shortStockingDebuff = StanceController.IsShortStock ? 1.15f : 1f;
                float shortStockingCamBonus = StanceController.IsShortStock ? 0.75f : 1f;
                float mountingBonus = StanceController.WeaponIsMounting ? StanceController.MountingRecoilBonus : StanceController.BracingRecoilBonus;

                Vector3 _separateIntensityFactors = (Vector3)AccessTools.Field(typeof(ShotEffector), "_separateIntensityFactors").GetValue(__instance);

                //instead of shot count, can check weapon firemode in here. Can also get weapon class/type.
                //would be more efficient to have a static bool "getsSemiRecoilIncrease" and check the weap class in stat detla instead.
                //1.5f recoil on pistols unironically felt good, even a lot of the rifles. Some got a bit too fucked by it some some rebalancing might be needed.
                //wep.FireMode.FireMode == Weapon.EFireMode.single, problem with restrcting it to semi only is that then firing one shot in full auto is more controlalble than semi
                if (Plugin.ShotCount == 1 && WeaponProperties.ShouldGetSemiIncrease)
                {
                    __instance.RecoilStrengthXy.x = Plugin.CurrentVRecoilX * Plugin.VertRecSemiMulti.Value;
                    __instance.RecoilStrengthXy.y = Plugin.CurrentVRecoilY * Plugin.VertRecSemiMulti.Value;
                    __instance.RecoilStrengthZ.x = Plugin.CurrentHRecoilX * Plugin.HorzRecSemiMulti.Value;
                    __instance.RecoilStrengthZ.y = Plugin.CurrentHRecoilY * Plugin.HorzRecSemiMulti.Value;
                }
                else if (Plugin.ShotCount > 1 && weaponClass.SelectedFireMode == Weapon.EFireMode.fullauto)
                {
                    __instance.RecoilStrengthXy.x = Plugin.CurrentVRecoilX * Plugin.VertRecAutoMulti.Value;
                    __instance.RecoilStrengthXy.y = Plugin.CurrentVRecoilY * Plugin.VertRecAutoMulti.Value;
                    __instance.RecoilStrengthZ.x = Plugin.CurrentHRecoilX * Plugin.HorzRecAutoMulti.Value;
                    __instance.RecoilStrengthZ.y = Plugin.CurrentHRecoilY * Plugin.HorzRecAutoMulti.Value;
                }   
                else
                {
                    __instance.RecoilStrengthZ.x = Plugin.CurrentHRecoilX;
                    __instance.RecoilStrengthZ.y = Plugin.CurrentHRecoilY;
                    __instance.RecoilStrengthXy.x = Plugin.CurrentVRecoilX;
                    __instance.RecoilStrengthXy.y = Plugin.CurrentVRecoilY;
                }

                float factoredDispersion = Plugin.CurrentDispersion * str * PlayerProperties.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorDebuff;

                if (Plugin.ShotCount > 1 && weaponClass.WeapClass == "pistol" && weaponClass.SelectedFireMode == Weapon.EFireMode.fullauto)
                {
                    factoredDispersion *= 0.80f;
                    __instance.RecoilStrengthZ.x *= 0.5f;
                    __instance.RecoilStrengthZ.y *= 0.5f;
                    __instance.RecoilStrengthXy.x *= 0.3f;
                    __instance.RecoilStrengthXy.y *= 0.3f;
                }

                float angle = Plugin.StartingRecoilAngle;
                __instance.RecoilDegree = new Vector2(angle - factoredDispersion, angle + factoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                __instance.ShotVals[3].Intensity = Plugin.CurrentCamRecoilX * str * PlayerProperties.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff;
                __instance.ShotVals[4].Intensity = Plugin.CurrentCamRecoilY * str * PlayerProperties.RecoilInjuryMulti * shortStockingCamBonus * aimCamRecoilBonus * playerWeightFactorBuff;

                float totalDispersion = Random.Range(__instance.RecoilRadian.x, __instance.RecoilRadian.y);
                float totalVerticalRecoil = __instance.RecoilStrengthXy.y * str * PlayerProperties.RecoilInjuryMulti * activeAimingBonus * shortStockingDebuff * playerWeightFactorBuff * mountingBonus;
                float totalHorizontalRecoil = Mathf.Min(__instance.RecoilStrengthZ.y * str * PlayerProperties.RecoilInjuryMulti * shortStockingDebuff * playerWeightFactorBuff, Plugin.HorzRecLimit.Value);

                __instance.RecoilDirection = new Vector3(-Mathf.Sin(totalDispersion) * totalVerticalRecoil * _separateIntensityFactors.x, Mathf.Cos(totalDispersion) * totalVerticalRecoil * _separateIntensityFactors.y, totalHorizontalRecoil * _separateIntensityFactors.z) * __instance.Intensity;
                IWeapon weapon = iWeapon;
                Vector2 vector = (weapon != null) ? weapon.MalfState.OverheatBarrelMoveDir : Vector2.zero;
                IWeapon weapon2 = iWeapon;
                float malfFactor = (weapon2 != null) ? weapon2.MalfState.OverheatBarrelMoveMult : 0f;
                float totalRecoil = (__instance.RecoilRadian.x + __instance.RecoilRadian.y) / 2f * ((__instance.RecoilStrengthXy.x + __instance.RecoilStrengthXy.y) / 2f) * malfFactor;
                __instance.RecoilDirection.x = __instance.RecoilDirection.x + vector.x * totalRecoil;
                __instance.RecoilDirection.y = __instance.RecoilDirection.y + vector.y * totalRecoil;
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
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(firearmController);
                if (player.IsYourPlayer == true)
                {
                    __instance.HandsContainer.Recoil.Damping = Plugin.CurrentDamping;
                    __instance.HandsContainer.HandsPosition.Damping = Plugin.CurrentHandDamping;

                    float mountingBonus = StanceController.WeaponIsMounting ?  (2f - StanceController.MountingRecoilBonus) : (2f - StanceController.BracingRecoilBonus);

                    if (Plugin.ShotCount == 1 && firearmController.Item.WeapClass != "pistol")
                    {
                        __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.CurrentConvergence * Plugin.ConvSemiMulti.Value * mountingBonus;
                    }
                    if (Plugin.ShotCount > 1)
                    {
                        __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.CurrentConvergence * Plugin.ConvAutoMulti.Value * mountingBonus;
                    }
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

