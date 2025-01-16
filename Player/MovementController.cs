using BepInEx.Logging;
using EFT;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealismMod
{
    public static class MovementSpeedController
    {
        private static Dictionary<BaseBallistic.ESurfaceSound, float> SurfaceSpeedModifiers = new Dictionary<BaseBallistic.ESurfaceSound, float>()
        {
            {BaseBallistic.ESurfaceSound.Metal, 0.95f },
            {BaseBallistic.ESurfaceSound.MetalThin, 0.95f },
            {BaseBallistic.ESurfaceSound.GarbageMetal, 0.82f },
            {BaseBallistic.ESurfaceSound.Garbage, 0.8f },
            {BaseBallistic.ESurfaceSound.Concrete, 1f },
            {BaseBallistic.ESurfaceSound.Asphalt, 1f },
            {BaseBallistic.ESurfaceSound.Gravel, 0.84f },
            {BaseBallistic.ESurfaceSound.Slate, 0.85f },
            {BaseBallistic.ESurfaceSound.Tile, 0.81f },
            {BaseBallistic.ESurfaceSound.Plastic, 0.95f },
            {BaseBallistic.ESurfaceSound.Glass, 0.86f },
            {BaseBallistic.ESurfaceSound.WholeGlass, 0.91f },
            {BaseBallistic.ESurfaceSound.Wood, 0.96f},
            {BaseBallistic.ESurfaceSound.WoodThick, 0.95f },
            {BaseBallistic.ESurfaceSound.WoodThin, 0.94f },
            {BaseBallistic.ESurfaceSound.Soil, 0.95f},
            {BaseBallistic.ESurfaceSound.Grass, 0.94f },
            {BaseBallistic.ESurfaceSound.Swamp, 1.0f },
            {BaseBallistic.ESurfaceSound.Puddle, 0.8f },
            {BaseBallistic.ESurfaceSound.Snow, 0.88f },
        };

        public static BaseBallistic.ESurfaceSound CurrentSurface;

        private static float currentModifier = 1f;
        private static float targetModifier = 1f;
        private static float smoothness = 0.5f;

        public static float GetSurfaceSpeed()
        {
            targetModifier = SurfaceSpeedModifiers.TryGetValue(CurrentSurface, out float value) ? value : 1f;
            currentModifier = Mathf.Lerp(currentModifier, targetModifier, smoothness);
            return (float)Math.Round(currentModifier, 3);
        }

        private static float maxSlopeAngle = 1f;
        private static float maxSlowdownFactor = 0.1f;

        public static float GetSlope(Player player)
        {
            Vector3 movementDirecion = player.MovementContext.MovementDirection.normalized;
            Vector3 position = player.Transform.position;
            RaycastHit hit;
            float slowdownFactor = 1f;

            if (Physics.Raycast(position, -Vector3.up, out hit))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > maxSlopeAngle)
                {
                    slowdownFactor = Mathf.Lerp(1f, maxSlowdownFactor, (slopeAngle - maxSlopeAngle) / (90f - maxSlopeAngle));
                }
            }
            return slowdownFactor;
        }

        public static float GetFiringMovementSpeedFactor(Player player)
        {
            if (!RecoilController.IsFiringMovement)
            {
                return 1f;
            }

            Player.FirearmController fc = player.HandsController as Player.FirearmController;
            if (fc == null)
            {
                return 1f;
            }

            float convergenceFactor = 1f - (RecoilController.BaseTotalConvergence / 100f);
            float dispersionFactor = 1f + (RecoilController.BaseTotalDispersion / 100f);
            float recoilFactor = RecoilController.FactoredTotalVRecoil + RecoilController.FactoredTotalHRecoil;
            float ergoFactor = Mathf.Clamp(1f - ((80f - WeaponStats.ErgoFactor) / 100f), 0.1f, 1f);
            recoilFactor = recoilFactor * dispersionFactor * convergenceFactor * ergoFactor;
            recoilFactor = fc.Item.WeapClass == "pistol" ? recoilFactor * 0.1f : recoilFactor;
            float recoilLimit = 1f - (recoilFactor / 100f);
            float totalRecoilFactor = 1f - ((recoilFactor / 400f) * RecoilController.ShotCount);
            totalRecoilFactor = Mathf.Clamp(totalRecoilFactor, 0.6f * recoilLimit, 1f);
            return totalRecoilFactor;
        }
    }
}
