using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RealismMod
{

    public class MuzzleSmokePatch : ModulePatch
    {
        private static Vector3 target = Vector3.zero;
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MuzzleSmoke).GetMethod("LateUpdateValues", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(MuzzleSmoke __instance)
        {
            if (WeaponStats._WeapClass == "pistol" && (!WeaponStats.HasShoulderContact || (Plugin.WeapOffsetX.Value != 0f && WeaponStats.HasShoulderContact)))
            {
                target = new Vector3(-0.2f, -0.2f, -0.2f);
            }
            else
            {
                target = new Vector3(0f, 0f, -0.2f);
            }

            Transform transform = (Transform)AccessTools.Field(typeof(MuzzleSmoke), "transform_0").GetValue(__instance);
            Vector3 pos = (Vector3)AccessTools.Field(typeof(MuzzleSmoke), "vector3_0").GetValue(__instance);
            pos = Vector3.Slerp(pos, transform.position + target, 0.125f); // left/right, up/down, in/out
            AccessTools.Field(typeof(MuzzleSmoke), "vector3_0").SetValue(__instance, pos);
        }
    }

    public class MuzzleEffectsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MuzzleManager).GetMethod("Shot", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static void Prefix(MuzzleManager __instance)
        {
            //todo: make sure is player, factor in ammo, modify muzzle flame jet, modify fume and smoke

            float velocitySparkFactor = Mathf.Pow(1f - WeaponStats.VelocityDelta, 3.5f) * StatCalc.CaliberSparks(WeaponStats.Caliber);
            float muzzleSparkFactor = 1f + (WeaponStats.TotalMuzzleFlash / 100f); // each eligble muzzle device should have a flash stat
            int totalSparkFactor = (int)(velocitySparkFactor * muzzleSparkFactor);

            float flashCaliberFactor = StatCalc.CaliberMuzzleFlash(WeaponStats.Caliber);
            float velocityFlashFactor = WeaponStats.VelocityDelta < 0 ? Mathf.Pow(1f - WeaponStats.VelocityDelta, 3f) * flashCaliberFactor : flashCaliberFactor;
            float muzzleFlashFactor = 1f + (WeaponStats.TotalMuzzleFlash / 100f); //each eligble muzzle device should have a flash stat
            float totalFlashFactor = velocityFlashFactor * muzzleFlashFactor;
            Vector2 totalFlash = new Vector2(totalFlashFactor / 2f, totalFlashFactor);

            float velocitySmokeFactor = WeaponStats.VelocityDelta < 0 ? -WeaponStats.VelocityDelta * StatCalc.CaliberMuzzleFlash(WeaponStats.Caliber) * 10f : -WeaponStats.VelocityDelta * 10f;
            float loudnessSmokeFactor = (WeaponStats.MuzzleLoudness - 15f) * 0.1f;
            int totalSmokeFactor = (int)velocitySmokeFactor + (int)loudnessSmokeFactor;

            //factor in if pistol, factor in if has muzzle device, is suppressor, ideally have a flash suppressor stat instead of loudness
            //check if DI gun + has suppressor for smoke

            Logger.LogWarning("velocitySparkFactor " + velocitySparkFactor);
            Logger.LogWarning("muzzleSparkFactor " + muzzleSparkFactor);
            Logger.LogWarning("totalSparkFactor " + totalSparkFactor);
            Logger.LogWarning("velocityFlashFactor " + velocityFlashFactor);
            Logger.LogWarning("muzzleFlashFactor " + muzzleFlashFactor);
            Logger.LogWarning("totalFlashFactor " + totalFlashFactor);

            MuzzleSparks[] sparks = (MuzzleSparks[])AccessTools.Field(typeof(MuzzleManager), "muzzleSparks_0").GetValue(__instance);
            if (sparks != null)
            {
                for (int i = 0; i < sparks.Length; i++)
                {
                    sparks[i].CountMin = -5 + totalSparkFactor;
                    sparks[i].CountRange = 4 + totalSparkFactor;
                }
            }

/*            MuzzleFume[] fume = (MuzzleFume[])AccessTools.Field(typeof(MuzzleManager), "muzzleFume_0").GetValue(__instance);
            if (fume != null)
            {
                for (int i = 0; i < fume.Length; i++)
                {
                    Logger.LogWarning("Size " + fume[i].Size);
                    Logger.LogWarning("CountMin " + fume[i].CountMin);
                    Logger.LogWarning("CountRange " + fume[i].CountRange);
                }
                fume[0].Size = Plugin.test1.Value;
                fume[0].CountMin = (int)Plugin.test3.Value;
                fume[0].CountRange = (int)Plugin.test4.Value;
            }*/

            var float_1 = (float)AccessTools.Field(typeof(MuzzleManager), "float_1").GetValue(__instance);
            if (float_1 > 0f)
            {
                for (int i = 0; i < __instance.Light.Lights.Length; i++)
                {
                    __instance.Light.Range = totalFlash; //6, 12 is default
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
}
