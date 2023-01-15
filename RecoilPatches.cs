using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;
using System;

namespace RealismMod
{

    public class OnWeaponParametersChangedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ShotEffector).GetMethod("OnWeaponParametersChanged", BindingFlags.Instance | BindingFlags.Public);
        }

        public BackendConfigSettingsClass.GClass1221 GlobalsAiming
        {
            get
            {
                return Singleton<BackendConfigSettingsClass>.Instance.Aiming;
            }
        }

        [PatchPrefix]
        private static bool Prefix(ref ShotEffector __instance)
        {
            Weapon wep = (Weapon)AccessTools.Field(typeof(ShotEffector), "_weapon").GetValue(__instance);

            if (wep.Owner.ID.StartsWith("pmc") || wep.Owner.ID.StartsWith("scav"))
            {
                OnWeaponParametersChangedPatch p = new OnWeaponParametersChangedPatch();
                SkillsClass.GClass1560 buffInfo = (SkillsClass.GClass1560)AccessTools.Field(typeof(ShotEffector), "_buffs").GetValue(__instance);
                WeaponTemplate template = wep.Template;

                float vRecoilDelta = WeaponProperties.VRecoilDelta;
                float hRecoilDelta = WeaponProperties.HRecoilDelta;

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

                Plugin.startingCamRecoilX = (float)Math.Round(cameraRecoil, 4);
                Plugin.startingCamRecoilY = (float)Math.Round(-cameraRecoil, 4);
                Plugin.currentCamRecoilX = Plugin.startingCamRecoilX;
                Plugin.currentCamRecoilY = Plugin.startingCamRecoilY;

                Plugin.startingVRecoilX = (float)Math.Round(__instance.RecoilStrengthXy.x, 3);
                Plugin.startingVRecoilY = (float)Math.Round(__instance.RecoilStrengthXy.y, 3);
                Plugin.currentVRecoilX = Plugin.startingVRecoilX;
                Plugin.currentVRecoilY = Plugin.startingVRecoilY;

                Plugin.startingHRecoilX = (float)Math.Round(__instance.RecoilStrengthZ.x, 3);
                Plugin.startingHRecoilY = (float)Math.Round(__instance.RecoilStrengthZ.y, 3);
                Plugin.currentHRecoilX = Plugin.startingHRecoilX;
                Plugin.currentHRecoilY = Plugin.startingHRecoilY;

                Plugin.startingConvergence = (float)Math.Round(wep.Template.Convergence * p.GlobalsAiming.RecoilConvergenceMult, 2);
                Plugin.currentConvergence = Plugin.startingConvergence;
                Plugin.convergenceProporitonK = (float)Math.Round(Plugin.startingConvergence * Plugin.startingVRecoilX, 2);

                Plugin.startingRecoilAngle = (float)Math.Round(angle, 2);

                Plugin.startingDispersion = (float)Math.Round(buffFactoredDispersion, 2);
                Plugin.currentDispersion = Plugin.startingDispersion;
/*                Plugin.dispersionProportionK = (float)Math.Round(Plugin.startingDispersion * Plugin.startingVRecoilX, 2);
*/
                Plugin.startingDamping = (float)Math.Round(WeaponProperties.TotalRecoilDamping, 3);
                Plugin.currentDamping = Plugin.startingDamping;

                Plugin.startingHandDamping = (float)Math.Round(WeaponProperties.TotalRecoilHandDamping, 3);
                Plugin.currentHandDamping = Plugin.startingHandDamping;


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

            Weapon wep = (Weapon)AccessTools.Field(typeof(ShotEffector), "_weapon").GetValue(__instance);

            if (wep.Owner.ID.StartsWith("pmc") || wep.Owner.ID.StartsWith("scav"))
            {

                Plugin.timer = 0f;
                Plugin.isFiring = true;
                Plugin.shotCount++;

                Vector3 _separateIntensityFactors = (Vector3)AccessTools.Field(typeof(ShotEffector), "_separateIntensityFactors").GetValue(__instance);

                float buffFactoredDispersion = Plugin.currentDispersion * str * PlayerProperties.RecoilInjuryMulti;
                float angle = Plugin.startingRecoilAngle;
                __instance.RecoilDegree = new Vector2(angle - buffFactoredDispersion, angle + buffFactoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;

                //instead of shot count, can check weapon firemode in here. Can also get weapon class/type.
                //would be more efficient to have a static bool "getsSemiRecoilIncrease" and check the weap class in stat detla instead.
                //1.5f recoil on pistols unironically felt good, even a lot of the rifles. Some got a bit too fucked by it some some rebalancing might be needed.
                //wep.FireMode.FireMode == Weapon.EFireMode.single, problem with restrcting it to semi only is that then firing one shot in full auto is more controlalble than semi
                if (Plugin.shotCount == 1 && WeaponProperties.ShouldGetSemiIncrease == true)
                {
                    __instance.RecoilStrengthXy.x = Plugin.currentVRecoilX * 1.35f;
                    __instance.RecoilStrengthXy.y = Plugin.currentVRecoilY * 1.35f;
                    __instance.RecoilStrengthZ.x = Plugin.currentHRecoilX * 1.35f;
                    __instance.RecoilStrengthZ.y = Plugin.currentHRecoilY * 1.35f;
                }
                else if (Plugin.shotCount > 1)
                {
                    __instance.RecoilStrengthXy.x = Plugin.currentVRecoilX * 0.63f;
                    __instance.RecoilStrengthXy.y = Plugin.currentVRecoilY * 0.63f;
                    __instance.RecoilStrengthZ.x = Plugin.currentHRecoilX * 0.6f;
                    __instance.RecoilStrengthZ.y = Plugin.currentHRecoilY * 0.6f;
                }
                else
                {
                    __instance.RecoilStrengthXy.x = Plugin.currentVRecoilX;
                    __instance.RecoilStrengthXy.y = Plugin.currentVRecoilY;
                    __instance.RecoilStrengthZ.x = Plugin.currentHRecoilX;
                    __instance.RecoilStrengthZ.y = Plugin.currentHRecoilY;
                }

                __instance.ShotVals[3].Intensity = Plugin.currentCamRecoilX * str * PlayerProperties.RecoilInjuryMulti;
                __instance.ShotVals[4].Intensity = Plugin.currentCamRecoilY * str * PlayerProperties.RecoilInjuryMulti;

                float num = Random.Range(__instance.RecoilRadian.x, __instance.RecoilRadian.y);
                float num2 = Random.Range(__instance.RecoilStrengthXy.x, __instance.RecoilStrengthXy.y) * str * PlayerProperties.RecoilInjuryMulti;
                float num3 = Random.Range(__instance.RecoilStrengthZ.x, __instance.RecoilStrengthZ.y) * str * PlayerProperties.RecoilInjuryMulti;
                __instance.RecoilDirection = new Vector3(-Mathf.Sin(num) * num2 * _separateIntensityFactors.x, Mathf.Cos(num) * num2 * _separateIntensityFactors.y, num3 * _separateIntensityFactors.z) * __instance.Intensity;
                Weapon weapon = wep;
                Vector2 vector = (weapon != null) ? weapon.MalfState.OverheatBarrelMoveDir : Vector2.zero;
                Weapon weapon2 = wep;
                float num4 = (weapon2 != null) ? weapon2.MalfState.OverheatBarrelMoveMult : 0f;
                float num5 = (__instance.RecoilRadian.x + __instance.RecoilRadian.y) / 2f * ((__instance.RecoilStrengthXy.x + __instance.RecoilStrengthXy.y) / 2f) * num4;
                __instance.RecoilDirection.x = __instance.RecoilDirection.x + vector.x * num5;
                __instance.RecoilDirection.y = __instance.RecoilDirection.y + vector.y * num5;
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
                if (!player.IsAI)
                {
                    __instance.HandsContainer.Recoil.Damping = Plugin.currentDamping;
                    __instance.HandsContainer.HandsPosition.Damping = Plugin.currentHandDamping;

                    if (Plugin.shotCount > 1)
                    {
                        __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.currentConvergence * 0.6f;
                    }
                    else
                    {
                        __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.currentConvergence;
                    }
                }
            }
        }
    }
}

