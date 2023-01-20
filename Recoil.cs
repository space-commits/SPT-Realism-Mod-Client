using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class Recoil
    {
        public static void vRecoilClimb(float climbFactor)
        {
            Plugin.currentVRecoilX = Mathf.Clamp((float)Math.Round(Plugin.currentVRecoilX * climbFactor * Plugin.vRecoilChangeMulti.Value, 3), Plugin.currentVRecoilX, Plugin.StartingVRecoilX * Plugin.vRecoilLimit.Value);
            Plugin.currentVRecoilY = Mathf.Clamp((float)Math.Round(Plugin.currentVRecoilY * climbFactor * Plugin.vRecoilChangeMulti.Value, 3), Plugin.currentVRecoilY, Plugin.startingVRecoilY * Plugin.vRecoilLimit.Value);
        }

        public static void hRecoilClimb(float climbFactor)
        {
            Plugin.CurrentHRecoilX = Mathf.Clamp((float)Math.Round(Plugin.CurrentHRecoilX * climbFactor * Plugin.hRecoilChangeMulti.Value, 3), Plugin.CurrentHRecoilX, Plugin.StartingHRecoilX * Plugin.hRecoilLimit.Value);
            Plugin.CurrentHRecoilY = Mathf.Clamp((float)Math.Round(Plugin.CurrentHRecoilY * climbFactor * Plugin.hRecoilChangeMulti.Value, 3), Plugin.CurrentHRecoilY, Plugin.StartingHRecoilY * Plugin.hRecoilLimit.Value);
        }

        public static void convergenceClimb()
        {
            Plugin.currentConvergence = Mathf.Clamp((float)Math.Round(Mathf.Min((Plugin.convergenceProporitonK / Plugin.currentVRecoilX), Plugin.currentConvergence), 2), Plugin.startingConvergence * Plugin.convergenceLimit.Value, Plugin.currentConvergence);
        }

        public static void dampingClimb(float climbFactor)
        {
            Plugin.currentDamping = Mathf.Clamp((float)Math.Round(Plugin.currentDamping * climbFactor, 3), Plugin.startingDamping * WeaponProperties.DampingLimit, Plugin.currentDamping);
            Plugin.currentHandDamping = Mathf.Clamp((float)Math.Round(Plugin.currentHandDamping * climbFactor, 3), Plugin.startingHandDamping * WeaponProperties.DampingLimit, Plugin.currentHandDamping);
        }

        public static void DoRecoilClimb()
        {
            if (Plugin.IsAiming == true)
            {
                if (Plugin.ShotCount > Plugin.PrevShotCount)
                {
                    if (Plugin.ShotCount >= 1 && Plugin.ShotCount <= 3)
                    {
                        vRecoilClimb(1.13f);
                        hRecoilClimb(1.13f);
                        convergenceClimb();

                    }
                    if (Plugin.ShotCount >= 4 && Plugin.ShotCount <= 5)
                    {
                        vRecoilClimb(1.125f);
                        hRecoilClimb(1.125f);
                        convergenceClimb();
                    }
                    if (Plugin.ShotCount > 5 && Plugin.ShotCount <= 7)
                    {
                        vRecoilClimb(1.1f);
                        hRecoilClimb(1.1f);
                        convergenceClimb();
                    }
                    if (Plugin.ShotCount > 8 && Plugin.ShotCount <= 10)
                    {
                        vRecoilClimb(1.08f);
                        hRecoilClimb(1.08f);
                        convergenceClimb();
                    }
                    if (Plugin.ShotCount > 10 && Plugin.ShotCount <= 15)
                    {
                        vRecoilClimb(1.05f);
                        hRecoilClimb(1.04f);
                        convergenceClimb();
                        dampingClimb(0.98f);

                    }

                    if (Plugin.ShotCount > 15 && Plugin.ShotCount <= 20)
                    {
                        vRecoilClimb(1.03f);
                        hRecoilClimb(1.02f);
                        convergenceClimb();
                        dampingClimb(0.98f);
                    }

                    if (Plugin.ShotCount > 20 && Plugin.ShotCount <= 25)
                    {
                        vRecoilClimb(1.03f);
                        hRecoilClimb(1.01f);
                        convergenceClimb();
                        dampingClimb(0.98f);
                    }

                    if (Plugin.ShotCount > 25 && Plugin.ShotCount <= 30)
                    {
                        vRecoilClimb(1.03f);
                        hRecoilClimb(1.01f);
                        convergenceClimb();
                        dampingClimb(0.98f);
                    }

                    if (Plugin.ShotCount > 30 && Plugin.ShotCount <= 35)
                    {
                        vRecoilClimb(1.03f);
                        hRecoilClimb(1.01f);
                        convergenceClimb();
                        dampingClimb(0.98f);
                    }

                    if (Plugin.ShotCount > 35)
                    {
                        vRecoilClimb(1.03f);
                        hRecoilClimb(1.01f);
                        convergenceClimb();
                        dampingClimb(0.98f);
                    }

                    if (Plugin.reduceCamRecoil.Value == true)
                    {
                        Plugin.currentCamRecoilX = Mathf.Clamp((float)Math.Round(Plugin.currentCamRecoilX * WeaponProperties.CamRecoilChangeRate, 4), Plugin.startingCamRecoilX * WeaponProperties.CamRecoilLimit, Plugin.currentCamRecoilX);
                        Plugin.currentCamRecoilY = Mathf.Clamp((float)Math.Round(Plugin.currentCamRecoilY * WeaponProperties.CamRecoilChangeRate, 4), Plugin.startingCamRecoilY * WeaponProperties.CamRecoilLimit, Plugin.currentCamRecoilY);
                    }

                    Plugin.currentSens = Mathf.Clamp((float)Math.Round(Plugin.currentSens * Plugin.sensChangeRate.Value, 4), Plugin.startingSens * Plugin.sensLimit.Value, Plugin.currentSens);

                    Plugin.PrevShotCount = Plugin.ShotCount;
                    Plugin.IsFiring = true;
                }
            }
            else
            {
                if (Plugin.ShotCount > Plugin.PrevShotCount)
                {
                    Plugin.PrevShotCount = Plugin.ShotCount;
                    Plugin.IsFiring = true;
                }
            }
        }

        public static void ResetRecoil()
        {
            if (Plugin.startingSens <= Plugin.currentSens && Plugin.startingConvergence <= Plugin.currentConvergence && Plugin.StartingVRecoilX >= Plugin.currentVRecoilX)
            {
                Plugin.StatsAreReset = true;
            }
            else
            {
                Plugin.StatsAreReset = false;
            }

            if (Plugin.StatsAreReset == false)
            {

                Plugin.currentSens = Mathf.Clamp(Plugin.currentSens * Plugin.sensResetRate.Value, Plugin.currentSens, Plugin.startingSens);

                Plugin.currentConvergence = Mathf.Clamp(Plugin.currentConvergence * Plugin.convergenceResetRate.Value, Plugin.currentConvergence, Plugin.startingConvergence);

                Plugin.currentDamping = Mathf.Clamp(Plugin.currentDamping * WeaponProperties.DampingResetRate, Plugin.currentDamping, Plugin.startingDamping);
                Plugin.currentHandDamping = Mathf.Clamp(Plugin.currentHandDamping * WeaponProperties.DampingResetRate, Plugin.currentHandDamping, Plugin.startingHandDamping);

                Plugin.currentVRecoilX = Mathf.Clamp(Plugin.currentVRecoilX * Plugin.vRecoilResetRate.Value, Plugin.StartingVRecoilX, Plugin.currentVRecoilX);
                Plugin.currentVRecoilY = Mathf.Clamp(Plugin.currentVRecoilY * Plugin.vRecoilResetRate.Value, Plugin.startingVRecoilY, Plugin.currentVRecoilY);

                Plugin.CurrentHRecoilX = Mathf.Clamp(Plugin.CurrentHRecoilX * Plugin.hRecoilResetRate.Value, Plugin.StartingHRecoilX, Plugin.CurrentHRecoilX);
                Plugin.CurrentHRecoilY = Mathf.Clamp(Plugin.CurrentHRecoilY * Plugin.hRecoilResetRate.Value, Plugin.StartingHRecoilY, Plugin.CurrentHRecoilY);

            }
            else
            {
                Plugin.currentSens = Plugin.startingSens;
            }

        }
    }
}
