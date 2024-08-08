using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RealismMod
{
   
    public class MuzzleEffectsPatch : ModulePatch
    {
        private static FieldInfo _muzzleManagerField;
        private static FieldInfo _muzzleSparksField;
        private static FieldInfo _muzzleJetsField;
        private static FieldInfo _floatField;
        private static FieldInfo _muzzleFumeField;
        private static FieldInfo _muzzleSmokeField;

        protected override MethodBase GetTargetMethod()
        {
            _muzzleManagerField = AccessTools.Field(typeof(FirearmsEffects), "_muzzleManager");
            _muzzleSparksField = AccessTools.Field(typeof(MuzzleManager), "muzzleSparks_0");
            _muzzleFumeField = AccessTools.Field(typeof(MuzzleManager), "muzzleFume_0");
            _muzzleSmokeField = AccessTools.Field(typeof(MuzzleManager), "muzzleSmoke_0");
            _muzzleJetsField = AccessTools.Field(typeof(MuzzleManager), "muzzleJet_0");
            _floatField = AccessTools.Field(typeof(MuzzleManager), "float_1");
            return typeof(WeaponManagerClass).GetMethod("PlayShotEffects", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(WeaponManagerClass __instance)
        {
            if ((__instance.Player != null && !__instance.Player.IsYourPlayer) || !PluginConfig.EnableMuzzleEffects.Value) return;
            var muzzleManager = (MuzzleManager)_muzzleManagerField.GetValue(__instance.FirearmsEffects);
            if (muzzleManager == null) return;
            Player.FirearmController fc = __instance.Player.HandsController as Player.FirearmController;

            // factor in ammo, modify muzzle flame jet, modify fume and smoke
            //aks should also get debuff for suppressor + smoke but less severe
            //gas piston guns should have reduced impact from velocity

            float duraFactor = Mathf.Pow(2f - (fc.Weapon.Repairable.Durability / 100f), 0.5f);
            float heatFactor = 1f + (fc.Weapon.MalfState.LastShotOverheat / 100f);

            float velocitySparkFactor = Mathf.Pow(1f - WeaponStats.VelocityDelta, 3.5f) * StatCalc.CaliberSparks(WeaponStats.Caliber);
            float muzzleSparkFactor = 1f + (WeaponStats.TotalMuzzleFlash / 100f); 
            int totalSparkFactor = (int)(velocitySparkFactor * muzzleSparkFactor * heatFactor * duraFactor);

            float flashCaliberFactor = StatCalc.CaliberMuzzleFlash(WeaponStats.Caliber) * 0.8f; //lazy
            float velocityFlashFactor = WeaponStats.VelocityDelta < 0 ? Mathf.Pow(1f - WeaponStats.VelocityDelta, 2.5f) * flashCaliberFactor : (1f - WeaponStats.VelocityDelta) * flashCaliberFactor;
            float muzzleFlashSuppression = 1f + (WeaponStats.TotalMuzzleFlash / 100f);
            float enviroFactorFlash = PlayerState.EnviroType == EnvironmentType.Indoor ? 1.25f : 1f;
            float totalFlashFactor = velocityFlashFactor * muzzleFlashSuppression * enviroFactorFlash;
            Vector2 totalFlash = new Vector2(totalFlashFactor / 2f, totalFlashFactor);

            float velocityFlameFactor = Mathf.Pow(1f - WeaponStats.VelocityDelta, 3f) * StatCalc.CaliberFlame(WeaponStats.Caliber);
            float muzzleFlameSuppression = Mathf.Clamp(1f + (WeaponStats.TotalMuzzleFlash / 50f), 0.1f, 3f);
            float enviroFactorFlame = PlayerState.EnviroType == EnvironmentType.Indoor ? 1.15f : 1f;
            float totalFlameFactor = velocityFlameFactor * muzzleFlameSuppression * heatFactor * duraFactor * enviroFactorFlame;

            float velocitySmokeFactor = Mathf.Pow(1f - WeaponStats.VelocityDelta, 3f) * StatCalc.CaliberSmoke(WeaponStats.Caliber) * 0.65f;
            float muzzleSmokeSuppression = WeaponStats.TotalMuzzleFlash > 0f ? 1f + WeaponStats.TotalMuzzleFlash / 75f : 1f + (WeaponStats.TotalMuzzleFlash / 200f);
            float modSmokeSuppression = 1f + (WeaponStats.TotalGas / 100f);
            float weaponSystemFactor = WeaponStats.IsDirectImpingement && WeaponStats.HasSuppressor ? 1.35f : 1f;
            float enviroFactorSmoke = PlayerState.EnviroType == EnvironmentType.Indoor ? 1.3f : 0.85f;
            float totalSmokeFactor = velocitySmokeFactor * muzzleSmokeSuppression * modSmokeSuppression * weaponSystemFactor * enviroFactorSmoke * duraFactor * heatFactor;
            float smoketrailFactor = 1f + totalSmokeFactor;
            float smoketrailFactorInverse =  Mathf.Max(1f - totalSmokeFactor, 0.1f);

            MuzzleSparks[] sparks = (MuzzleSparks[])_muzzleSparksField.GetValue(muzzleManager);
            if (sparks != null)
            {
                int sparkCount = sparks.Length;
                for (int i = 0; i < sparkCount; i++)
                {
                    MuzzleSparks spark = sparks[i];
                    spark.CountMin = Mathf.Max(-5 + totalSparkFactor, -4);
                    spark.CountRange = Mathf.Max(4 + totalSparkFactor, 0);
                }
            }

            MuzzleJet[] jets = (MuzzleJet[])_muzzleJetsField.GetValue(muzzleManager);
            if (jets != null)
            {
                int jetsCount = jets.Length;    
                for (int i = 0; i < jetsCount; i++)
                {
                    MuzzleJet jet = jets[i];
                    jet.Chance = totalFlameFactor / jetsCount;
                    int particleCount = jet.Particles.Length;
                    for (int j = 0; j < particleCount; j++)
                    {
                        jet.Particles[j].Size = totalFlameFactor;
                    }
                }
            }

            var float_1 = (float)_floatField.GetValue(muzzleManager);
            if (muzzleManager.Light != null && float_1 > 0f)
            {
                muzzleManager.Light.Range = totalFlash; //6, 12 is default
            }

            __instance.FirearmsEffects.UpdateMuzzle();

            //can give each effect a known name so that I only modify them once, allowing to retain the original value, but then
            //can't dynamically change the values based on weapon condition etc.
            MuzzleFume[] fumes = (MuzzleFume[])_muzzleFumeField.GetValue(muzzleManager);
            if (fumes != null)
            {
                int fumeCount = fumes.Length;    
                for (int i = 0; i < fumeCount; i++)
                {
                    MuzzleFume fume = fumes[i];
                    fume.Size = Mathf.Max(totalSmokeFactor, 0.3f); //0.5 feels about right for standard AR-15 with flashider, 1 about right for brake
                    fume.CountMin = 1; // set to 1, gently raise
                    fume.CountRange = 2; // set to 2, gently raise
                }
            }

            //try to make it based on current heat of gun?
            MuzzleSmoke[] smokes = (MuzzleSmoke[])_muzzleSmokeField.GetValue(muzzleManager);
            if (smokes != null)
            {
                MuzzleSmoke smoke = smokes[0];
                smoke.SmokeLength = WeaponStats.IsPistol ? 0f : 20f * smoketrailFactor; //how long it is, 20
                smoke.MuzzleSpeedMultiplier = 0.4f; //how much it twists and changes direction, lower = more straight, 1.2 
                smoke.SmokeVelocity = WeaponStats.IsPistol ? 100f : 0.05f * smoketrailFactorInverse; //how fast the smoke moves, lower to make it linger, 0.4
                smoke.SmokeIncreasingByShot = WeaponStats.IsPistol ? 0f : 0.25f * smoketrailFactor; //how many shots/how quickly smoke effect starts happening and increasing in intensity, 0.4
            }

        }
    }
}
