using Aki.Reflection.Patching;
using EFT;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class PlayerFixedUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (Helper.CheckIsReady())
            {

                Helper.IsReady = true;

                Recoil.DoRecoilClimb();

                if (Plugin.ShotCount == Plugin.PrevShotCount)
                {
                    Plugin.Timer += Time.deltaTime;
                    if (Plugin.Timer >= Plugin.resetTime.Value)
                    {
                        Plugin.IsFiring = false;
                        Plugin.ShotCount = 0;
                        Plugin.PrevShotCount = 0;
                        Plugin.Timer = 0f;
                    }
                }

                if (Plugin.IsBotFiring == true)
                {
                    Plugin.BotTimer += Time.deltaTime;
                    if (Plugin.BotTimer >= 1f)
                    {
                        Plugin.IsBotFiring = false;
                        Plugin.BotTimer = 0f;
                    }
                }

                if (Plugin.GrenadeExploded == true)
                {
                    Plugin.GrenadeTimer += Time.deltaTime;
                    if (Plugin.GrenadeTimer >= 2f)
                    {
                        Plugin.GrenadeExploded = false;
                        Plugin.GrenadeTimer = 0f;
                    }
                }

                Deafening.DoDeafening(Logger);

                if (Plugin.IsFiring == false)
                {
                    Recoil.ResetRecoil();
                }

            }
            else
            {
                Helper.IsReady = false;
            }
        }
    }
}
