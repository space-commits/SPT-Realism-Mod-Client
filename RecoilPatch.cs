using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static RealismMod.Helper;
using static EFT.Player;
using Random = UnityEngine.Random;

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
            Weapon wep = (Weapon)AccessTools.Field(typeof(ShotEffector), "_weapon").GetValue(__instance);
            if (wep.Owner.ID.StartsWith("pmc"))
            {

                OnWeaponParametersChangedPatch p = new OnWeaponParametersChangedPatch();
                SkillsClass.GClass1546 buffInfo = (SkillsClass.GClass1546)AccessTools.Field(typeof(ShotEffector), "_buffs").GetValue(__instance);
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

                Plugin.startingCamRecoilX = cameraRecoil;
                Plugin.startingCamRecoilY = -cameraRecoil;
                Plugin.currentCamRecoilX = cameraRecoil;
                Plugin.currentCamRecoilY = -cameraRecoil;

                Plugin.startingVRecoilX = __instance.RecoilStrengthXy.x;
                Plugin.startingVRecoilY = __instance.RecoilStrengthXy.y;
                Plugin.currentVRecoilX = Plugin.startingVRecoilX;
                Plugin.currentVRecoilY = Plugin.startingVRecoilY;

                Plugin.startingConvergence = wep.Template.Convergence * p.GlobalsAiming.RecoilConvergenceMult;
                Plugin.currentConvergence = Plugin.startingConvergence;
                Plugin.convergenceProporitonK = Plugin.startingConvergence * Plugin.startingVRecoilX;

                Plugin.startingRecoilAngle = angle;

                Plugin.startingDispersion = buffFactoredDispersion;
                Plugin.currentDispersion = Plugin.startingDispersion;
                Plugin.dispersionProportionK = Plugin.startingDispersion * Plugin.startingVRecoilX;

                Plugin.startingDamping = WeaponProperties.TotalRecoilDamping;
                Plugin.currentDamping = Plugin.startingDamping;
                Plugin.dampingProporitonK = Plugin.startingDamping * Plugin.startingVRecoilX;

                return false;
            }
            else
            {
                return true;
            }
        }

        public GClass1162.GClass1204 GlobalsAiming
        {
            get
            {
                return Singleton<GClass1162>.Instance.Aiming;
            }
        }
    }

    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, ref float ___float_7, ref Player.ValueBlender ___valueBlender_0)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                if (firearmController.Item.Owner.ID.StartsWith("pmc"))
                {
                    Player.ValueBlender valueBlended = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "valueBlender_0").GetValue(__instance);
                    float _aimsSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").GetValue(__instance);
                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.startingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;
                    float aimSpeed = _aimsSpeed * (1f + WeaponProperties.AimSpeedModifier);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").SetValue(__instance, aimSpeed);
                }
            }
        }
    }

    public class SyncWithCharacterSkillsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("SyncWithCharacterSkills", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Player.FirearmController __instance)
        {
            if (__instance.Item.Owner.ID.StartsWith("pmc"))
            {
                SkillsClass.GClass1546 skillsClass = (SkillsClass.GClass1546)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1546_0").GetValue(__instance);
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
                SkillsClass.GClass1546 weaponInfo = player.Skills.GetWeaponInfo(__instance.Item);

                skillsClass.ReloadSpeed = weaponInfo.ReloadSpeed * (1 + WeaponProperties.ReloadSpeedModifier);
                skillsClass.FixSpeed = weaponInfo.FixSpeed * (1 + WeaponProperties.FixSpeedModifier);
                skillsClass.AimMovementSpeed = weaponInfo.AimMovementSpeed + WeaponProperties.AimMoveSpeedModifier;

            }

        }
    }


    public class UpdateSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("UpdateSensitivity");
        }

        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref bool ____isAiming, ref float ____aimingSens)
        {
            if (__instance.Item.Owner.ID.StartsWith("pmc") && ____isAiming)
            {
                Plugin.startingSens = ____aimingSens;
                Plugin.currentSens = ____aimingSens;
            }
        }
    }

    public class AimingSensitivityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("get_AimingSensitivity");
        }
        [PatchPostfix]
        public static void PatchPostfix(ref Player.FirearmController __instance, ref bool ____isAiming, ref float ____aimingSens)
        {
            if (__instance.Item.Owner.ID.StartsWith("pmc") && ____isAiming)
            {
                ____aimingSens = Plugin.currentSens;
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

            if (wep.Owner.ID.StartsWith("pmc"))
            {
                Plugin.timer = 0f;
                Plugin.isFiring = true;
                Plugin.shotCount++;

                Vector3 _separateIntensityFactors = (Vector3)AccessTools.Field(typeof(ShotEffector), "_separateIntensityFactors").GetValue(__instance);

                float buffFactoredDispersion = Plugin.currentDispersion;
                float angle = Plugin.startingRecoilAngle;
                __instance.RecoilDegree = new Vector2(angle - buffFactoredDispersion, angle + buffFactoredDispersion);
                __instance.RecoilRadian = __instance.RecoilDegree * 0.017453292f;
                __instance.RecoilStrengthXy.x = Plugin.currentVRecoilX;
                __instance.RecoilStrengthXy.y = Plugin.currentVRecoilY;
                __instance.ShotVals[3].Intensity = Plugin.currentCamRecoilX;
                __instance.ShotVals[4].Intensity = Plugin.currentCamRecoilY;

                float num = Random.Range(__instance.RecoilRadian.x, __instance.RecoilRadian.y);
                float num2 = Random.Range(__instance.RecoilStrengthXy.x, __instance.RecoilStrengthXy.y) * str;
                float num3 = Random.Range(__instance.RecoilStrengthZ.x, __instance.RecoilStrengthZ.y) * str;
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
                if (firearmController.Item.Owner.ID.StartsWith("pmc"))
                {
                    __instance.HandsContainer.Recoil.Damping = Plugin.currentDamping;
                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.currentConvergence;
                }
            }
        }
    }
}

