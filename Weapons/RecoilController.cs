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
        public static float ShotCount = 0f;
        public static float PrevShotCount = ShotCount;
        public static float ShotTimer = 0.0f;
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


        public static void DoCantedRecoil(ref Vector3 targetRecoil, ref Vector3 currentRecoil, ref Quaternion weapRotation) 
        {
            if (RecoilController.IsFiring)
            {
                float recoilAmount = RecoilController.FactoredTotalDispersion / 35f;
                float recoilSpeed = RecoilController.BaseTotalConvergence * 0.75f;
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
            pwa.HandsContainer.Recoil.Damping = (float)Math.Round(RecoilController.BaseTotalRecoilDamping * Plugin.RecoilDampingMulti.Value, 3);

            if (Plugin.EnableHybridRecoil.Value && (Plugin.HybridForAll.Value || (!Plugin.HybridForAll.Value && !WeaponProperties.HasShoulderContact)))
            {
                pwa.HandsContainer.Recoil.ReturnSpeed = Mathf.Clamp((RecoilController.BaseTotalConvergence - Mathf.Clamp(25f + RecoilController.ShotCount, 0, 100f)) + Mathf.Clamp(15f + RecoilController.PlayerControl, 0f, 100f), 2f, RecoilController.BaseTotalConvergence);
            }
            else
            {
                pwa.HandsContainer.Recoil.ReturnSpeed = RecoilController.BaseTotalConvergence * Plugin.ConvergenceMulti.Value;
            }
            pwa.HandsContainer.HandsPosition.Damping = (float)Math.Round(RecoilController.BaseTotalHandDamping * Plugin.HandsDampingMulti.Value * (PlayerProperties.IsMoving ? 0.5f : 1f), 3);
        }     
    }
}
