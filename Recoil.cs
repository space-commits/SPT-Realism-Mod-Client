using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class Recoil
    {
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

            if (Plugin.ShotCount > Plugin.PrevShotCount)
            {
                if (Plugin.ShotCount >= 1 && Plugin.ShotCount <= 3)
                {
                    VRecoilClimb(1.13f);
                    HRecoilClimb(1.12f);
                    ConvergenceClimb();

                }
                if (Plugin.ShotCount >= 4 && Plugin.ShotCount <= 5)
                {
                    VRecoilClimb(1.125f);
                    HRecoilClimb(1.11f);
                    ConvergenceClimb();
                }
                if (Plugin.ShotCount > 5 && Plugin.ShotCount <= 7)
                {
                    VRecoilClimb(1.1f);
                    HRecoilClimb(1.09f);
                    ConvergenceClimb();
                }
                if (Plugin.ShotCount > 8 && Plugin.ShotCount <= 10)
                {
                    VRecoilClimb(1.08f);
                    HRecoilClimb(1.07f);
                    ConvergenceClimb();
                }
                if (Plugin.ShotCount > 10 && Plugin.ShotCount <= 15)
                {
                    VRecoilClimb(1.05f);
                    HRecoilClimb(1.045f);
                    ConvergenceClimb();
                    DampingClimb(0.98f);

                }

                if (Plugin.ShotCount > 15 && Plugin.ShotCount <= 20)
                {
                    VRecoilClimb(1.03f);
                    HRecoilClimb(1.027f);
                    ConvergenceClimb();
                    DampingClimb(0.98f);
                }

                if (Plugin.ShotCount > 20 && Plugin.ShotCount <= 25)
                {
                    VRecoilClimb(1.03f);
                    HRecoilClimb(1.02f);
                    ConvergenceClimb();
                    DampingClimb(0.98f);
                }

                if (Plugin.ShotCount > 25 && Plugin.ShotCount <= 30)
                {
                    VRecoilClimb(1.03f);
                    HRecoilClimb(1.015f);
                    ConvergenceClimb();
                    DampingClimb(0.98f);
                }

                if (Plugin.ShotCount > 30 && Plugin.ShotCount <= 35)
                {
                    VRecoilClimb(1.03f);
                    HRecoilClimb(1.01f);
                    ConvergenceClimb();
                    DampingClimb(0.98f);
                }

                if (Plugin.ShotCount > 35)
                {
                    VRecoilClimb(1.03f);
                    HRecoilClimb(1.01f);
                    ConvergenceClimb();
                    DampingClimb(0.98f);
                }

                if (Plugin.reduceCamRecoil.Value == true)
                {
                    Plugin.CurrentCamRecoilX = Mathf.Clamp((float)Math.Round(Plugin.CurrentCamRecoilX * WeaponProperties.CamRecoilChangeRate, 4), Plugin.StartingCamRecoilX * WeaponProperties.CamRecoilLimit, Plugin.CurrentCamRecoilX);
                    Plugin.CurrentCamRecoilY = Mathf.Clamp((float)Math.Round(Plugin.CurrentCamRecoilY * WeaponProperties.CamRecoilChangeRate, 4), Plugin.StartingCamRecoilY * WeaponProperties.CamRecoilLimit, Plugin.CurrentCamRecoilY);
                }

                Plugin.CurrentAimSens = Mathf.Clamp((float)Math.Round(Plugin.CurrentAimSens * Plugin.SensChangeRate.Value, 4), Plugin.StartingAimSens * Plugin.SensLimit.Value, Plugin.CurrentAimSens);
                Plugin.CurrentHipSens = Mathf.Clamp((float)Math.Round(Plugin.CurrentHipSens * Plugin.SensChangeRate.Value, 4), Plugin.StartingHipSens * Plugin.SensLimit.Value, Plugin.CurrentHipSens);


                Plugin.PrevShotCount = Plugin.ShotCount;
            }
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

                Plugin.CurrentConvergence = Mathf.Clamp(Plugin.CurrentConvergence * Plugin.convergenceResetRate.Value, Plugin.CurrentConvergence, Plugin.StartingConvergence);

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
