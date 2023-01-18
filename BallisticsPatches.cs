using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class CreateShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var result = typeof(EFT.Ballistics.BallisticsCalculator).GetMethod("CreateShot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return result;
        }

        [PatchPrefix]
        private static bool Prefix(EFT.Ballistics.BallisticsCalculator __instance, BulletClass ammo, Vector3 origin, Vector3 direction, int fireIndex, Player player, Item weapon, ref GClass2401 __result, float speedFactor, int fragmentIndex = 0)
        {
            CreateShotPatch p = new CreateShotPatch();
            __result = p.CreateShot(__instance, ammo, origin, direction, fireIndex, player, weapon, speedFactor, fragmentIndex = 0);
            return false;

        }


        public GClass2401 CreateShot(EFT.Ballistics.BallisticsCalculator __instance, BulletClass ammo, Vector3 origin, Vector3 direction, int fireIndex, Player player, Item weapon, float speedFactor, int fragmentIndex = 0)
        {
            /*            Logger.LogWarning("!!!!!!!!!!! Shot Created!! !!!!!!!!!!!!!!");
                        Logger.LogWarning("========================STARTING BULLET VALUES============================");
                        Logger.LogWarning("Round ID = " + ammo.TemplateId);
                        Logger.LogWarning("Round Damage = " + ammo.Damage);
                        Logger.LogWarning("Round Penetration Power = " + ammo.PenetrationPower);
                        Logger.LogWarning("Round Penetration Chance = " + ammo.PenetrationChance);
                        Logger.LogWarning("Round Frag Chance = " + ammo.FragmentationChance);
                        Logger.LogWarning("Round Intial Speed = " + ammo.InitialSpeed);
                        Logger.LogWarning("Round SPEED FACTOR = " + speedFactor);
                        Logger.LogWarning("Round BC = " + ammo.BallisticCoeficient);
                        Logger.LogWarning("==============================================================");*/

            int randomNum = UnityEngine.Random.Range(0, 512);
            float velocityFactored = ammo.InitialSpeed * speedFactor;
            float penChanceFactored = ammo.PenetrationChance * speedFactor;
            float damageFactored = ammo.Damage * speedFactor;
            float fragchanceFactored = Mathf.Max(ammo.FragmentationChance * speedFactor, 0);
            float penPowerFactored = EFT.Ballistics.BallisticsCalculator.GetAmmoPenetrationPower(ammo, randomNum, __instance.Randoms) * speedFactor;
            float bcFactored = Mathf.Max(ammo.BallisticCoeficient * speedFactor, 0.01f);


            /*            float penPowerUnfactored = EFT.Ballistics.BallisticsCalculator.GetAmmoPenetrationPower(ammo, randomNum, __instance.Randoms);
            */
            /* Logger.LogWarning("========================AFTER SPEED FACTOR============================");
             Logger.LogWarning("Round ID = " + ammo.TemplateId);
             Logger.LogWarning("Round Damage = " + damageFactored);
             Logger.LogWarning("Round Penetration Power UNFACTORED = " + penPowerUnfactored);
             Logger.LogWarning("Round Penetration Power = " + penPowerFactored);
             Logger.LogWarning("Round Penetration Chance = " + penChanceFactored);
             Logger.LogWarning("Round Frag Chance = " + fragchanceFactored);
             Logger.LogWarning("Round Factored Speed = " + velocityFactored);
             Logger.LogWarning("Round Factored BC = " + bcFactored);
             Logger.LogWarning("==============================================================");*/
            return GClass2401.Create(ammo, fragmentIndex, randomNum, origin, direction, velocityFactored, velocityFactored, ammo.BulletMassGram, ammo.BulletDiameterMilimeters, (float)damageFactored, penPowerFactored, penChanceFactored, ammo.RicochetChance, fragchanceFactored, 1f, ammo.MinFragmentsCount, ammo.MaxFragmentsCount, EFT.Ballistics.BallisticsCalculator.DefaultHitBody, __instance.Randoms, bcFactored, player, weapon, fireIndex, null);
        }
    }
}
