using BepInEx.Logging;
using EFT.Animations;
using EFT.Animations.NewRecoil;
using EFT.InventoryLogic;
using EFT;
using UnityEngine;
using static EFT.Player;

namespace RealismMod
{
    public class ShootController
    {

        public static Vector2 RecoilRotation { get { return _currentRotation; } }
        public static bool IsFiring = false;
        public static bool IsFiringDeafen = false;
        public static bool IsFiringMovement = false;
        public static bool DoFiringWiggle = false;
        public static int ShotCount = 0;
        public static int PrevShotCount = ShotCount;
        public static float FiringTimer = 0.0f;
        public static float ShotCountTimer = 0.0f;
        public static float DeafenShotTimer = 0.0f;
        public static float WiggleShotTimer = 0.0f;
        public static float MovementSpeedShotTimer = 0.0f;
        private const float FpsFactor = 144f;
        private const float FpsSmoothingFactor = 0.42f;

        public static float BaseTotalHRecoil;
        public static float BaseTotalVRecoil;
        public static float BaseTotalDispersion;
        public static float BaseTotalCamRecoil;

        public static float BaseTotalRecoilDamping;
        public static float BaseTotalHandDamping;

        public static float BaseTotalConvergence;
        public static float FactoredTotalConvergence;
        public static float BaseTotalRecoilAngle;

        public static float FactoredTotalHRecoil;
        public static float FactoredTotalVRecoil;
        public static float FactoredTotalDispersion;
        public static float FactoredTotalCamRecoil;

        private static Vector2 _targetRotation = Vector2.zero;
        private static Vector2 _currentRotation = Vector2.zero;
        private static float _recoilAngle;
        private static float _dispersionSpeed;
        private static float _convergenceMulti = 1f;

        public static void ShootUpdate(Player player)
        {           
            //bool fireButtonIsBeingHeld = Input.GetMouseButton(0);
            if (ShotCount > PrevShotCount)
            {
                IsFiring = true;
                IsFiringDeafen = true;
                DoFiringWiggle = true;
                IsFiringMovement = true;
                StanceController.IsFiringFromStance = true;
                DeafenController.IncreaseDeafeningShooting();
                RecoilController(player);
                PrevShotCount = ShotCount;
            }

            DeafenShotTimer += Time.deltaTime;
            WiggleShotTimer += Time.deltaTime;
            FiringTimer += Time.deltaTime;
            MovementSpeedShotTimer += Time.deltaTime;
            ShotCountTimer += Time.deltaTime;

            if (FiringTimer >= PluginConfig.ShotResetDelay.Value)
            {
                FiringTimer = 0f;
                _targetRotation = Vector2.zero;
                IsFiring = false;
            }

            if (ShotCountTimer >= -1f && !player.ProceduralWeaponAnimation.method_18())//make instant for testing
            {
                ShotCountTimer = 0f;
                ShotCount = 0;
                PrevShotCount = 0;
            }

            if (DeafenShotTimer >= PluginConfig.DeafenResetDelay.Value)
            {
                IsFiringDeafen = false;
                DeafenShotTimer = 0f;
            }

            if (WiggleShotTimer >= 0.12f)
            {
                DoFiringWiggle = false;
                WiggleShotTimer = 0f;
            }

            if (MovementSpeedShotTimer >= 0.5f)
            {
                IsFiringMovement = false;
                MovementSpeedShotTimer = 0f;
            }

            StanceController.StanceShotTimer();
            LerpConvergenceMulti();
            LerpRecoilRotation(player);
        }

        private static float ShotModifier()
        {
            float baseValue = 0.3f;
            float shotModifier = baseValue + (ShootController.ShotCount * 0.3f);
            return Mathf.Clamp(shotModifier, baseValue, 1.2f);
        }

        public static void RecoilController(Player player) 
        {
            bool isPistol = WeaponStats.IsPistol;
            float shotCountFactor = ShotModifier();
            float mouseSensFactor = player.GetRotationMultiplier(); //BSG will factor the input vector by mouse sens, need to invert it so that it remains consistent
            float hipfireModifier = !StanceController.IsAiming ? 1.1f : 1f;

            //this should be a single pre-calculated value ideally
            float baseAngle = ShootController.BaseTotalRecoilAngle;
            float angleBonus = StanceController.IsMounting && WeaponStats.BipodIsDeployed ? 7.5f : StanceController.IsMounting ? 5f : StanceController.IsBracing ? 2.5f : 1f;
            float dispersionAngleFactor = !isPistol ? 1f + (-WeaponStats.TotalDispersionDelta * 0.035f) : 1f;
            float totalRecAngle = (baseAngle + angleBonus) * dispersionAngleFactor;
            totalRecAngle = !isPistol ? totalRecAngle : totalRecAngle - 5;
            totalRecAngle = Mathf.Clamp(totalRecAngle, 60f, 110f);
            float dispersionSpeedFactor = !isPistol ? 1f + (-WeaponStats.TotalDispersionDelta) : 1f;

            _recoilAngle = (Utils.AreFloatsEqual(PluginConfig.RecoilDispersionFactor.Value, 0f) ? 0f : (90f - totalRecAngle) / 250f);

            float angleDispFactor = 90f / totalRecAngle;
            ////////////////

            _dispersionSpeed = PluginConfig.RecoilDispersionSpeed.Value * dispersionSpeedFactor;
            float dispersion = Mathf.Max(ShootController.FactoredTotalDispersion * PluginConfig.RecoilDispersionFactor.Value * shotCountFactor * angleDispFactor * hipfireModifier, 0f); //0.001
            float recoilClimbMulti = isPistol ? PluginConfig.PistolRecClimbFactor.Value : PluginConfig.RecoilClimbFactor.Value; //0.0055

            float xRotation = dispersion / mouseSensFactor;
            float yRotation = (-ShootController.FactoredTotalVRecoil * recoilClimbMulti * shotCountFactor) / mouseSensFactor; // * fpsFactor
            xRotation = Mathf.Clamp(xRotation, -1f, 1f);
            yRotation = Mathf.Clamp(yRotation, -1f, 0f);

            _targetRotation.x = xRotation;
            _targetRotation.y = yRotation;
        }

        public static void LerpRecoilRotation(Player player)
        {
            float stanceFactor = player.IsInPronePose ? 0.75f : 1f;
            FactoredTotalConvergence = BaseTotalConvergence * stanceFactor * _convergenceMulti;

            float fpsFactor = 1f;
            if (PluginConfig.UseFpsRecoilFactor.Value)
            {
                fpsFactor = Plugin.FPS >= 1f ? FpsFactor / Plugin.FPS : 1f;
                fpsFactor = Mathf.Clamp(fpsFactor, 0.05f, 5f);
            }

            float xRoation = !IsFiring ? 0f : Mathf.Lerp(-_targetRotation.x, _targetRotation.x, Mathf.PingPong(Time.time * _dispersionSpeed, 1f)) + _recoilAngle; //need angle and dispersionSpeed
            Vector2 newRotation = new Vector2(xRoation, _targetRotation.y);
            float speed = Mathf.InverseLerp(0f, 9f, FactoredTotalConvergence);
            _currentRotation = Vector2.Lerp(_currentRotation, newRotation * fpsFactor, speed);
            player.Rotate(_currentRotation, false);
        }

        private static float ShotConvergenceFactor()
        {
            bool isAutoPistol = WeaponStats.IsPistol && WeaponStats.FireMode == Weapon.EFireMode.fullauto;
            switch (ShotCount)
            {
                case 0:
                    return isAutoPistol ? 1f : 1f;
                case 1:
                    return isAutoPistol ? 1.1f : 0.95f;
                case 2:
                    return isAutoPistol ? 2.5f : 0.9f;
                case 3:
                    return isAutoPistol ? 2.3f : 0.85f;
                case 4:
                    return isAutoPistol ? 2.1f : 0.8f;
                case 5:
                    return isAutoPistol ? 1.9f : 0.7f;
                case 6:
                    return isAutoPistol ? 1.0f : 0.6f;
                case 7:
                    return isAutoPistol ? 1.7f : 0.6f;
                case 8:
                    return isAutoPistol ? 1.6f : 0.6f;
                case >= 9:
                    return isAutoPistol ? 1.5f : 0.6f;
                default:
                    return 1;
            }
        }

        public static void LerpConvergenceMulti()
        {
            float target = IsFiring ? ShotConvergenceFactor() : 1f;
            _convergenceMulti = Mathf.Lerp(_convergenceMulti, target, 0.9f);
        }

        public static void DoVisualRecoil(ref Vector3 targetRecoil, ref Vector3 currentRecoil, ref Quaternion weapRotation)
        {
            float cantedRecoilSpeed = Mathf.Clamp(BaseTotalConvergence * 0.95f, 9f, 16f);

            if (DoFiringWiggle)
            {
                float cantedRecoilAmount = FactoredTotalHRecoil / 31f;
                float totalCantedRecoil = Mathf.Lerp(-cantedRecoilAmount, cantedRecoilAmount, Mathf.PingPong(Time.time * cantedRecoilSpeed * 1.05f, 1.0f));
                float additionalRecoilAmount = FactoredTotalDispersion / 16f;
                float totalSideRecoil = Mathf.Lerp(-additionalRecoilAmount, additionalRecoilAmount, Mathf.PingPong(Time.time * cantedRecoilSpeed, 1.0f)) * 0.05f;
                float totalVertical = Mathf.Lerp(-additionalRecoilAmount, additionalRecoilAmount, Mathf.PingPong(Time.time * cantedRecoilSpeed * 1.5f, 1.0f)) * 0.1f;
                targetRecoil = new Vector3(totalVertical * 0.95f, totalCantedRecoil, totalSideRecoil * 0.89f) * PluginConfig.VisRecoilMulti.Value * WeaponStats.CurrentVisualRecoilMulti;
            }
            else
            {
                targetRecoil = Vector3.Lerp(targetRecoil, Vector3.zero, 0.1f);
            }

            currentRecoil = Vector3.Lerp(currentRecoil, targetRecoil, 1f);
            Quaternion recoilQ = Quaternion.Euler(currentRecoil);
            weapRotation *= recoilQ;
        }

        public static void SetRecoilParams(ProceduralWeaponAnimation pwa, Weapon weapon, Player player)
        {
            NewRecoilShotEffect newRecoil = pwa.Shootingg.CurrentRecoilEffect as NewRecoilShotEffect;
            bool hasOptic = WeaponStats.IsOptic && StanceController.IsAiming;
            float shoulderContactFactor = weapon.WeapClass != "pistol" && !WeaponStats.HasShoulderContact ? 1.25f : WeaponStats.IsStockedPistol ? 0.85f : 1f;
            float opticFactorRear = StanceController.IsAiming && hasOptic ? 0.9f : 1f;
            float opticFactorVert = StanceController.IsAiming && hasOptic ? 0.95f : 1f;

            newRecoil.HandRotationRecoil.CategoryIntensityMultiplier = weapon.Template.RecoilCategoryMultiplierHandRotation * PluginConfig.RecoilIntensity.Value * shoulderContactFactor;
       
            newRecoil.HandRotationRecoil.ReturnTrajectoryDumping = weapon.Template.RecoilReturnPathDampingHandRotation * PluginConfig.HandsDampingMulti.Value * opticFactorRear;
            pwa.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.Damping = weapon.Template.RecoilDampingHandRotation * PluginConfig.RecoilDampingMulti.Value * opticFactorVert;

            pwa.Shootingg.CurrentRecoilEffect.CameraRotationRecoilEffect.Damping = PluginConfig.CamWiggle.Value;
            pwa.Shootingg.CurrentRecoilEffect.CameraRotationRecoilEffect.ReturnSpeed = PluginConfig.CamReturn.Value; 
            pwa.Shootingg.CurrentRecoilEffect.CameraRotationRecoilEffect.Intensity = 1; 

            pwa.Shootingg.CurrentRecoilEffect.HandPositionRecoilEffect.Damping = 0.68f; // 0.77
            pwa.Shootingg.CurrentRecoilEffect.HandPositionRecoilEffect.ReturnSpeed = 0.14f; //0.15

            newRecoil.HandRotationRecoil.NextStablePointDistanceRange.x = 1; //1  (defaults are 0.1, 6)
            newRecoil.HandRotationRecoil.NextStablePointDistanceRange.y = 4; //4

            pwa.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed = FactoredTotalConvergence;
        }
    }
}
