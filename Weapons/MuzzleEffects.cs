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

        protected override MethodBase GetTargetMethod()
        {
            _muzzleManagerField = AccessTools.Field(typeof(FirearmsEffects), "_muzzleManager");
            _muzzleSparksField = AccessTools.Field(typeof(MuzzleManager), "muzzleSparks_0");
            _muzzleFumeField = AccessTools.Field(typeof(MuzzleManager), "muzzleFume_0");
            _muzzleJetsField = AccessTools.Field(typeof(MuzzleManager), "muzzleJet_0");
            _floatField = AccessTools.Field(typeof(MuzzleManager), "float_1");
            return typeof(WeaponManagerClass).GetMethod("PlayShotEffects", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(WeaponManagerClass __instance)
        {
            if (__instance.Player != null && !__instance.Player.IsYourPlayer) return;
            var muzzleManager = (MuzzleManager)_muzzleManagerField.GetValue(__instance.FirearmsEffects);
            if (muzzleManager == null) return;

            // factor in ammo, modify muzzle flame jet, modify fume and smoke
            //factor in if pistol, factor in if has muzzle device, is suppressor, ideally have a flash suppressor stat instead of loudness
            //check if DI gun + has suppressor for smoke
            //make gas buster charging handles do something

            float velocitySparkFactor = Mathf.Pow(1f - WeaponStats.VelocityDelta, 3.5f) * StatCalc.CaliberSparks(WeaponStats.Caliber);
            float muzzleSparkFactor = 1f + (WeaponStats.TotalMuzzleFlash / 100f); 
            int totalSparkFactor = (int)(velocitySparkFactor * muzzleSparkFactor);

            float flashCaliberFactor = StatCalc.CaliberMuzzleFlash(WeaponStats.Caliber) * 0.8f; //lazy
            float velocityFlashFactor = WeaponStats.VelocityDelta < 0 ? Mathf.Pow(1f - WeaponStats.VelocityDelta, 3f) * flashCaliberFactor : (1f - WeaponStats.VelocityDelta) * flashCaliberFactor;
            float muzzleFlashSuppression = 1f + (WeaponStats.TotalMuzzleFlash / 100f);
            float totalFlashFactor = velocityFlashFactor * muzzleFlashSuppression;
            Vector2 totalFlash = new Vector2(totalFlashFactor / 2f, totalFlashFactor);

            float velocityFlameFactor = Mathf.Pow(1f - WeaponStats.VelocityDelta, 3.5f) * StatCalc.CaliberFlame(WeaponStats.Caliber);
            float muzzleFlameSuppression = Mathf.Clamp(1f + (WeaponStats.TotalMuzzleFlash / 50f), 0.1f, 3f);
            float totalFlameFactor = velocityFlameFactor * muzzleFlameSuppression;

            float velocitySmokeFactor = Mathf.Pow(1f - WeaponStats.VelocityDelta, 3.5f) * StatCalc.CaliberSmoke(WeaponStats.Caliber);
            float muzzleSmokeSuppression = WeaponStats.TotalMuzzleFlash > 0f ? 1f + WeaponStats.TotalMuzzleFlash / 75f : 1f + (WeaponStats.TotalMuzzleFlash / 200f);
            float weaponSystemFactor = WeaponStats.IsDirectImpingement && WeaponStats.HasSuppressor ? 1.75f : 1f;
            weaponSystemFactor = WeaponStats.HasGasBuster && WeaponStats.HasSuppressor ? weaponSystemFactor * 0.7f : weaponSystemFactor;
            float totalSmokeFactor = velocitySmokeFactor * muzzleSmokeSuppression * weaponSystemFactor;
  
            MuzzleSparks[] sparks = (MuzzleSparks[])_muzzleSparksField.GetValue(muzzleManager);
            if (sparks != null)
            {
                int sparkCount = sparks.Length;
                for (int i = 0; i < sparkCount; i++)
                {
                    MuzzleSparks spark = sparks[i];
                    spark.CountMin = Mathf.Max(-5 + totalSparkFactor, -6);
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
                    int particleCount = jet.Particles.Length;
                    jet.Chance = totalFlameFactor / jetsCount;
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

            MuzzleFume[] fume = (MuzzleFume[])_muzzleFumeField.GetValue(muzzleManager);
            if (fume != null)
            {
                for (int i = 0; i < fume.Length; i++)
                {
                    fume[i].Size = totalSmokeFactor; //0.5 feels about right for standard AR-15 with flashider, 1 about right for brake
                    fume[i].CountMin = 1; // set to 1, gently raise
                    fume[i].CountRange = 2; // set to 2, gently raise
                }
            }


            //try to make it based on current heat of gun?
            /*       MuzzleSmoke[] smoke = (MuzzleSmoke[])AccessTools.Field(typeof(MuzzleManager), "muzzleSmoke_0").GetValue(__instance);
                   if (smoke != null && (isVisible || (!isVisible && sqrCameraDistance < 4f)))
                   {
                       for (int i = 0; i < smoke.Length; i++)
                       {
                           Logger.LogWarning("SmokeLength " + smoke[i].SmokeLength);
                           Logger.LogWarning("MuzzleSpeedMultiplier " + smoke[i].MuzzleSpeedMultiplier);
                           Logger.LogWarning("SmokeVelocity " + smoke[i].SmokeVelocity);
                           Logger.LogWarning("SmokeIncreasingByShot " + smoke[i].SmokeIncreasingByShot);
                           Logger.LogWarning("SmokeEnd " + smoke[i].SmokeEnd);

                           smoke[i].SmokeLength = Plugin.test1.Value; //how much smoke basically
                           smoke[i].MuzzleSpeedMultiplier = Plugin.test2.Value; //sort of the same as below
                           smoke[i].SmokeVelocity = Plugin.test3.Value; //how fast the smoke moves, lower to make it linger
                           smoke[i].SmokeIncreasingByShot = Plugin.test4.Value; //how many shots/how quickly smoke effect starts happening and increasing in intensity
                           smoke[i].SmokeEnd = Plugin.test5.Value; //not sure
                       }
                   }*/

        }
    }

    public class MuzzleSmokePatch : ModulePatch
    {
        private static Vector3 _target = Vector3.zero;
        private static FieldInfo _transformField;
        private static FieldInfo _vector3Field;
        protected override MethodBase GetTargetMethod()
        {
            _transformField = AccessTools.Field(typeof(MuzzleSmoke), "transform_0");
            _vector3Field = AccessTools.Field(typeof(MuzzleSmoke), "vector3_0");
            return typeof(MuzzleSmoke).GetMethod("LateUpdateValues", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(MuzzleSmoke __instance)
        {
            if (WeaponStats._WeapClass == "pistol" && (!WeaponStats.HasShoulderContact || (Plugin.WeapOffsetX.Value != 0f && WeaponStats.HasShoulderContact)))
            {
                _target = new Vector3(-0.2f, -0.2f, -0.2f);
            }
            else
            {
                _target = new Vector3(0f, 0f, -0.2f);
            }

            Transform transform = (Transform)_transformField.GetValue(__instance);
            Vector3 pos = (Vector3)_vector3Field.GetValue(__instance);
            pos = Vector3.Slerp(pos, transform.position + _target, 0.125f); // left/right, up/down, in/out
            _vector3Field.SetValue(__instance, pos);
        }
    }

}
