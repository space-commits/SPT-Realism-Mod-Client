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
        public static float FiringResetTimer = 0.0f;
        public static float FiringDuration = 0.0f;
        public static float ShotCountResetTimer = 0.0f;
        public static float DeafenResetTimer = 0.0f;
        public static float WiggleResetTimer = 0.0f;
        public static float MovementSpeedResetTimer = 0.0f;
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


        private static AnimationCurve _convergenceCurve = new AnimationCurve(
         new Keyframe(0, 1f),
         new Keyframe(0.25f, 0.95f),
         new Keyframe(0.5f, 0.9f),
         new Keyframe(0.75f, 0.85f),
         new Keyframe(1f, 0.8f),
         new Keyframe(1.25f, 0.75f),
         new Keyframe(1.5f, 0.7f),
         new Keyframe(1.75f, 0.65f),
         new Keyframe(2f, 0.5f)
        );

        private static AnimationCurve _autoPistolConvergenceCurve = new AnimationCurve(
        new Keyframe(0, 1f),
        new Keyframe(0.05f, 2f),
        new Keyframe(0.1f, 2.5f),
        new Keyframe(0.3f, 2f),
        new Keyframe(0.5f, 1.5f),
        new Keyframe(0.7f, 1.25f),
        new Keyframe(0.9f, 1.15f),
        new Keyframe(1.2f, 1f)
       );


        public static void ShootUpdate(Player player, FirearmController fc)
        {
            //bool fireButtonIsBeingHeld = Input.GetMouseButton(0);
            bool isAutoFiring =  player.ProceduralWeaponAnimation.method_18(); // || fc.IsTriggerPressed is literally holding mb

            if (IsFiring || isAutoFiring) 
            {
                DeafenController.IncreaseDeafeningShooting();
                RecoilController(player);
                FiringDuration += Time.deltaTime;
            }

            DeafenResetTimer += Time.deltaTime;
            WiggleResetTimer += Time.deltaTime;
            FiringResetTimer += Time.deltaTime;
            MovementSpeedResetTimer += Time.deltaTime;
            ShotCountResetTimer += Time.deltaTime;

            if (!isAutoFiring) 
            {
                FiringDuration = 0f;
            }

            if (FiringResetTimer >= PluginConfig.ShotResetDelay.Value && !isAutoFiring)
            {
                FiringResetTimer = 0f;
                _targetRotation = Vector2.zero;
                ShotCount = 0;
                IsFiring = false;
            }

        /*    if (ShotCountTimer >= -1f && !isAutoFiring)//make instant for testing
            {
                ShotCountTimer = 0f;
                ShotCount = 0;
            }
*/
            if (DeafenResetTimer >= PluginConfig.DeafenResetDelay.Value)
            {
                IsFiringDeafen = false;
                DeafenResetTimer = 0f;
            }

            if (WiggleResetTimer >= 0.12f)
            {
                DoFiringWiggle = false;
                WiggleResetTimer = 0f;
            }

            if (MovementSpeedResetTimer >= 0.5f)
            {
                IsFiringMovement = false;
                MovementSpeedResetTimer = 0f;
            }

            StanceController.StanceShotTimer();
            UpdateConvergence(fc, player);
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
            return isAutoPistol ? _autoPistolConvergenceCurve.Evaluate(FiringDuration) : _convergenceCurve.Evaluate(FiringDuration);
        }


        public static void UpdateConvergence(FirearmController fc, Player player)
        {
            bool isAutoPistol = WeaponStats.IsPistol && WeaponStats.FireMode == Weapon.EFireMode.fullauto;
            _convergenceMulti = fc.autoFireOn ? ShotConvergenceFactor() : 1f;
            float stanceFactor = player.IsInPronePose ? 0.75f : 1f;
            FactoredTotalConvergence = BaseTotalConvergence * stanceFactor * _convergenceMulti;
            player.ProceduralWeaponAnimation.Shootingg.CurrentRecoilEffect.HandRotationRecoilEffect.ReturnSpeed = FactoredTotalConvergence;
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

        //update and reset firing state
        public static void ResetFiringState(ProceduralWeaponAnimation pwa, Weapon weapon, Player player)
        {
            ShootController.SetRecoilParams(pwa, weapon, player);
            ShootController.ShotCount++;
            ShootController.FiringResetTimer = 0f;
            ShootController.ShotCountResetTimer = 0f;
            ShootController.DeafenResetTimer = 0f;
            ShootController.WiggleResetTimer = 0f;
            ShootController.MovementSpeedResetTimer = 0f;
            StanceController.StanceShotTime = 0f;
            ShootController.IsFiring = true;
            ShootController.IsFiringDeafen = true;
            ShootController.DoFiringWiggle = true;
            ShootController.IsFiringMovement = true;
            StanceController.IsFiringFromStance = true;
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
        }
    }
}
