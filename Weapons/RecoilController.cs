/*using BepInEx.Logging;
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
        public static bool IsFiring = false;
        public static bool IsFiringMovement = false;
        public static bool IsFiringWiggle = false;
        public static float ShotCount = 0f;
        public static float PrevShotCount = ShotCount;
        public static float ShotTimer = 0.0f;
        public static float WiggleShotTimer = 0.0f;
        public static float MovementSpeedShotTimer = 0.0f;

        public static bool IsVector = false;

        public static float BaseTotalHRecoil;
        public static float BaseTotalVRecoil;
        public static float BaseTotalDispersion;
        public static float BaseTotalCamRecoil;

        public static float BaseTotalRecoilDamping;
        public static float BaseTotalHandDamping;

        public static float BaseTotalConvergence;
        public static float BaseTotalRecoilAngle;

        public static float PlayerControl;

        public static float FactoredTotalHRecoil;
        public static float FactoredTotalVRecoil;
        public static float FactoredTotalDispersion;
        public static float FactoredTotalCamRecoil;

        public static void DoVisualRecoil(ref Vector3 targetRecoil, ref Vector3 currentRecoil, ref Quaternion weapRotation, ManualLogSource logger) 
        {
            if (RecoilController.IsFiringWiggle)
            {
                float cantedRecoilAmount = RecoilController.FactoredTotalHRecoil / 20f;
                float cantedRecoilSpeed = Mathf.Max(RecoilController.BaseTotalConvergence * 0.85f, 14f);
                float totalCantedRecoil = Mathf.Lerp(-cantedRecoilAmount, cantedRecoilAmount, Mathf.PingPong(Time.time * cantedRecoilSpeed, 1.0f));

                if (Plugin.EnableAdditionalRec.Value)
                {
                    float additionalRecoilAmount = RecoilController.FactoredTotalDispersion / 18f;
                    float totalSideRecoil = Mathf.Lerp(-additionalRecoilAmount, additionalRecoilAmount, Mathf.PingPong(Time.time * cantedRecoilSpeed, 1.0f)) * 0.05f;
                    float totalVertical = Mathf.Lerp(-additionalRecoilAmount, additionalRecoilAmount, Mathf.PingPong(Time.time * cantedRecoilSpeed * 1.5f, 1.0f)) * 0.1f;
                    targetRecoil = new Vector3(totalVertical, totalCantedRecoil, totalSideRecoil) * Plugin.VisRecoilMulti.Value;
                }
                else 
                {
                    targetRecoil = new Vector3(0f, totalCantedRecoil, 0f) * Plugin.VisRecoilMulti.Value;

                }


            }
            else
            { 
                targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, 0.1f);
            }

          

            currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, 1f);
            Quaternion recoilQ = Quaternion.Euler(currentRecoil);
            weapRotation *= recoilQ;
        }

        public static void SetRecoilParams(ProceduralWeaponAnimation pwa, Weapon weapon) 
        {
            float dampingUpperLimit = Plugin.IsAiming && Plugin.HasOptic ? 0.74f : 0.9f;
            float dampingLowerLimit = Plugin.IsAiming && Plugin.HasOptic ? 0.5f : 0.5f;
            float opticFactor = Plugin.IsAiming && Plugin.HasOptic ? 0.92f : 1f;
            pwa.HandsContainer.Recoil.Damping = Mathf.Clamp(RecoilController.BaseTotalRecoilDamping * opticFactor, dampingLowerLimit, dampingUpperLimit);
            pwa.HandsContainer.HandsPosition.Damping = (float)Math.Round(RecoilController.BaseTotalHandDamping * (PlayerProperties.IsMoving ? 0.5f : 1f) * opticFactor, 3);

            if (Plugin.EnableHybridRecoil.Value && (Plugin.HybridForAll.Value || (!Plugin.HybridForAll.Value && !WeaponProperties.HasShoulderContact)))
            {
                pwa.HandsContainer.Recoil.ReturnSpeed = Mathf.Clamp((RecoilController.BaseTotalConvergence - Mathf.Clamp(25f + RecoilController.ShotCount, 0, 100f)) + Mathf.Clamp(15f + RecoilController.PlayerControl, 0f, 100f), 2f, RecoilController.BaseTotalConvergence);
            }
            else
            {
                pwa.HandsContainer.Recoil.ReturnSpeed = RecoilController.BaseTotalConvergence;
            }

        }     
    }
}
*/