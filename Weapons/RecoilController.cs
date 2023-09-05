using EFT.Animations;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class RecoilController
    {
        public static void DoCantedRecoil(ref Vector3 targetRecoil, ref Vector3 currentRecoil, ref Quaternion weapRotation) 
        {
            if (Plugin.IsFiring)
            {
                float recoilAmount = Plugin.StartingHRecoilX / 15f;
                float recoilSpeed = Plugin.StartingConvergence;
                float totalRecoil = Mathf.Lerp(-recoilAmount, recoilAmount, Mathf.PingPong(Time.time * recoilSpeed, 1.0f));
                targetRecoil = new Vector3(0f, totalRecoil, 0f);
            }
            else
            {
                targetRecoil = Vector3.zero;
            }

            currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, 1f);
            Quaternion recoilQ = Quaternion.Euler(currentRecoil);
            weapRotation *= recoilQ;
        }

        public static void SetRecoilParams(ProceduralWeaponAnimation pwa, Weapon weapon) 
        {
            pwa.HandsContainer.Recoil.Damping = Plugin.CurrentDamping * (Plugin.EnableExperimentalRecoil.Value ? 0.9f : 1f);
            pwa.HandsContainer.HandsPosition.Damping = Plugin.CurrentHandDamping;

            if (weapon.WeapClass != "pistol")
            {
                if (Plugin.ShotCount <= 1)
                {
                    pwa.HandsContainer.Recoil.ReturnSpeed = Plugin.CurrentConvergence * Plugin.ConvSemiMulti.Value;
                }
                else
                {
                    pwa.HandsContainer.Recoil.ReturnSpeed = Plugin.CurrentConvergence * Plugin.ConvAutoMulti.Value;
                }
            }
            else
            {
                if (Plugin.ShotCount <= 1)
                {
                    pwa.HandsContainer.Recoil.ReturnSpeed = Plugin.CurrentConvergence * Plugin.ConvSemiMulti.Value;
                }
                else
                {
                    pwa.HandsContainer.Recoil.ReturnSpeed = Plugin.CurrentConvergence * Plugin.ConvSemiMulti.Value * 1.25f;
                }
            }
        }

        private static void VRecoilClimb(float climbFactor)
        {
            Plugin.CurrentVRecoilX = Mathf.Clamp((float)Math.Round(Plugin.CurrentVRecoilX * climbFactor * Plugin.vRecoilChangeMulti.Value, 3), Plugin.CurrentVRecoilX, Plugin.StartingVRecoilX * Plugin.vRecoilLimit.Value);
            Plugin.CurrentVRecoilY = Mathf.Clamp((float)Math.Round(Plugin.CurrentVRecoilY * climbFactor * Plugin.vRecoilChangeMulti.Value, 3), Plugin.CurrentVRecoilY, Plugin.StartingVRecoilY * Plugin.vRecoilLimit.Value);
        }

        private static void HRecoilClimb(float climbFactor)
        {
            Plugin.CurrentHRecoilX = Mathf.Clamp((float)Math.Round(Plugin.CurrentHRecoilX * climbFactor * Plugin.hRecoilChangeMulti.Value, 3), Plugin.CurrentHRecoilX, Plugin.StartingHRecoilX * Plugin.hRecoilLimit.Value);
            Plugin.CurrentHRecoilY = Mathf.Clamp((float)Math.Round(Plugin.CurrentHRecoilY * climbFactor * Plugin.hRecoilChangeMulti.Value, 3), Plugin.CurrentHRecoilY, Plugin.StartingHRecoilY * Plugin.hRecoilLimit.Value);
        }

        private static void ConvergenceClimb()
        {
            Plugin.CurrentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((Plugin.ConvergenceProporitonK / Plugin.CurrentVRecoilX), Plugin.CurrentConvergence), 2), Plugin.StartingConvergence * Plugin.convergenceLimit.Value, Plugin.CurrentConvergence);
        }

        private static void DampingClimb(float climbFactor)
        {
            Plugin.CurrentDamping = Mathf.Clamp((float)Math.Round(Plugin.CurrentDamping * climbFactor, 3), Plugin.StartingDamping * WeaponProperties.DampingLimit, Plugin.CurrentDamping);
            Plugin.CurrentHandDamping = Mathf.Clamp((float)Math.Round(Plugin.CurrentHandDamping * climbFactor, 3), Plugin.StartingHandDamping * WeaponProperties.DampingLimit, Plugin.CurrentHandDamping);
        }

        public static void DoRecoilClimb()
        {
            if (Plugin.ShotCount == 1)
            {
                VRecoilClimb(1.15f);
                HRecoilClimb(1.12f);
                ConvergenceClimb();

            }
            if (Plugin.ShotCount >= 2 && Plugin.ShotCount <= 3 && Plugin.CurrentlyShootingWeapon.SelectedFireMode == Weapon.EFireMode.fullauto)
            {
                VRecoilClimb(1.16f);
                HRecoilClimb(1.12f);
                ConvergenceClimb();
            }
            if (Plugin.ShotCount >= 4 && Plugin.ShotCount <= 5)
            {
                VRecoilClimb(1.155f);
                HRecoilClimb(1.11f);
                ConvergenceClimb();
                DampingClimb(0.98f);
            }
            if (Plugin.ShotCount > 5 && Plugin.ShotCount <= 7)
            {
                VRecoilClimb(1.13f);
                HRecoilClimb(1.09f);
                ConvergenceClimb();
                DampingClimb(0.98f);
            }
            if (Plugin.ShotCount > 8 && Plugin.ShotCount <= 10)
            {
                VRecoilClimb(1.1f);
                HRecoilClimb(1.07f);
                ConvergenceClimb();
                DampingClimb(0.98f);
            }
            if (Plugin.ShotCount > 10 && Plugin.ShotCount <= 15)
            {
                VRecoilClimb(1.07f);
                HRecoilClimb(1.045f);
                ConvergenceClimb();
                DampingClimb(0.98f);
            }

            if (Plugin.ShotCount > 15 && Plugin.ShotCount <= 20)
            {
                VRecoilClimb(1.04f);
                HRecoilClimb(1.027f);
                ConvergenceClimb();
                DampingClimb(0.97f);
            }

            if (Plugin.ShotCount > 20 && Plugin.ShotCount <= 25)
            {
                VRecoilClimb(1.03f);
                HRecoilClimb(1.02f);
                ConvergenceClimb();
                DampingClimb(0.96f);
            }

            if (Plugin.ShotCount > 25 && Plugin.ShotCount <= 30)
            {
                VRecoilClimb(1.03f);
                HRecoilClimb(1.015f);
                ConvergenceClimb();
                DampingClimb(0.95f);
            }

            if (Plugin.ShotCount > 30 && Plugin.ShotCount <= 35)
            {
                VRecoilClimb(1.03f);
                HRecoilClimb(1.01f);
                ConvergenceClimb();
                DampingClimb(0.95f);
            }

            if (Plugin.ShotCount > 35)
            {
                VRecoilClimb(1.03f);
                HRecoilClimb(1.01f);
                ConvergenceClimb();
                DampingClimb(0.95f);
            }

            if (Plugin.ReduceCamRecoil.Value == true)
            {
                Plugin.CurrentCamRecoilX = Mathf.Clamp((float)Math.Round(Plugin.CurrentCamRecoilX * WeaponProperties.CamRecoilChangeRate, 4), Plugin.StartingCamRecoilX * WeaponProperties.CamRecoilLimit, Plugin.CurrentCamRecoilX);
                Plugin.CurrentCamRecoilY = Mathf.Clamp((float)Math.Round(Plugin.CurrentCamRecoilY * WeaponProperties.CamRecoilChangeRate, 4), Plugin.StartingCamRecoilY * WeaponProperties.CamRecoilLimit, Plugin.CurrentCamRecoilY);
            }

            Plugin.CurrentAimSens = Mathf.Clamp((float)Math.Round(Plugin.CurrentAimSens * Plugin.SensChangeRate.Value, 4), Plugin.StartingAimSens * Plugin.SensLimit.Value, Plugin.CurrentAimSens);
            Plugin.CurrentHipSens = Mathf.Clamp((float)Math.Round(Plugin.CurrentHipSens * Plugin.SensChangeRate.Value, 4), Plugin.StartingHipSens * Plugin.SensLimit.Value, Plugin.CurrentHipSens);
        }

        public static void ResetRecoil()
        {
            if (Plugin.StartingAimSens <= Plugin.CurrentAimSens && Plugin.StartingHipSens <= Plugin.CurrentHipSens && Plugin.StartingConvergence <= Plugin.CurrentConvergence && Plugin.StartingVRecoilX >= Plugin.CurrentVRecoilX && Plugin.StartingHRecoilX >= Plugin.CurrentHRecoilX)
            {
                Plugin.CurrentAimSens = Plugin.StartingAimSens;
                Plugin.CurrentHipSens = Plugin.StartingHipSens;
                Plugin.StatsAreReset = true;
            }
            else
            {
                Plugin.CurrentAimSens = Mathf.Clamp(Plugin.CurrentAimSens * Plugin.SensResetRate.Value, Plugin.CurrentAimSens, Plugin.StartingAimSens);
                Plugin.CurrentHipSens = Mathf.Clamp(Plugin.CurrentHipSens * Plugin.SensResetRate.Value, Plugin.CurrentHipSens, Plugin.StartingHipSens);

                Plugin.CurrentConvergence = Mathf.Clamp(Plugin.CurrentConvergence * Plugin.ConvergenceResetRate.Value, Plugin.CurrentConvergence, Plugin.StartingConvergence);

                Plugin.CurrentDamping = Mathf.Clamp(Plugin.CurrentDamping * WeaponProperties.DampingResetRate, Plugin.CurrentDamping, Plugin.StartingDamping);
                Plugin.CurrentHandDamping = Mathf.Clamp(Plugin.CurrentHandDamping * WeaponProperties.DampingResetRate, Plugin.CurrentHandDamping, Plugin.StartingHandDamping);

                Plugin.CurrentVRecoilX = Mathf.Clamp(Plugin.CurrentVRecoilX * Plugin.vRecoilResetRate.Value, Plugin.StartingVRecoilX, Plugin.CurrentVRecoilX);
                Plugin.CurrentVRecoilY = Mathf.Clamp(Plugin.CurrentVRecoilY * Plugin.vRecoilResetRate.Value, Plugin.StartingVRecoilY, Plugin.CurrentVRecoilY);

                Plugin.CurrentHRecoilX = Mathf.Clamp(Plugin.CurrentHRecoilX * Plugin.hRecoilResetRate.Value, Plugin.StartingHRecoilX, Plugin.CurrentHRecoilX);
                Plugin.CurrentHRecoilY = Mathf.Clamp(Plugin.CurrentHRecoilY * Plugin.hRecoilResetRate.Value, Plugin.StartingHRecoilY, Plugin.CurrentHRecoilY);
                Plugin.StatsAreReset = false;
            }
        }
    }
}
