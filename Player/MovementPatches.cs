using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SkillMovementStruct = EFT.SkillManager.GStruct242;
using ValueHandler = GClass807;

namespace RealismMod
{
    /*    public class SprintPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return typeof(PlayerAnimator).GetMethod("EnableSprint", BindingFlags.Instance | BindingFlags.Public);
            }

            [PatchPrefix]
            private static bool PatchPrefix(PlayerAnimator __instance)
            {
                Player player = Utils.GetYourPlayer();
                if (player == null) return true;
                if (player.MovementContext.PlayerAnimator == __instance)
                {
                    return false;
                }
                return true;
            }
        }
    */
    public class StaminaRegenRatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PlayerPhysicalClass).GetMethod("method_21", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(PlayerPhysicalClass __instance, float baseValue, ref float __result)
        {
            float[] float_7 = (float[])AccessTools.Field(typeof(PlayerPhysicalClass), "float_7").GetValue(__instance);
            PlayerPhysicalClass.EPose epose_0 = (PlayerPhysicalClass.EPose)AccessTools.Field(typeof(PlayerPhysicalClass), "epose_0").GetValue(__instance);
            Player player_0 = (Player)AccessTools.Field(typeof(PlayerPhysicalClass), "player_0").GetValue(__instance);
            float Single_0 = (float)AccessTools.Property(typeof(PlayerPhysicalClass), "Single_0").GetValue(__instance);

            float gearFactor = GearController.HasRespirator ? 0.75f :  GearController.HasGasMask ? 0.5f : GearController.FSIsActive ? 0.75f : 1f;
            float stanceFactor =
                StanceController.IsAiming ? 0.9f : StanceController.CurrentStance == EStance.ActiveAiming ? 0.95f :
                StanceController.CurrentStance == EStance.HighReady ? 1.05f :
                StanceController.CurrentStance == EStance.ShortStock ? 1.1f :
                StanceController.CurrentStance == EStance.LowReady ? 1.15f :
                StanceController.CurrentStance == EStance.PatrolStance ? 1.25f :
                StanceController.CurrentStance == EStance.PistolCompressed ? 1.2f : 1f;
            float playerWeightFactor = 1f - ((PlayerValues.TotalModifiedWeightMinusWeapon / 100f) * (1f - PlayerValues.StrengthWeightBuff)); 
            __result = baseValue * float_7[(int)epose_0] * Singleton<BackendConfigSettingsClass>.Instance.StaminaRestoration.GetAt(player_0.HealthController.Energy.Normalized) * (player_0.Skills.EnduranceBuffRestoration + 1f) * PlayerValues.HealthStamRegenFactor * gearFactor * stanceFactor * playerWeightFactor / Single_0;
            return false;
        }
    }

    public class ClampSpeedPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementContext).GetMethod("ClampSpeed", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(MovementContext __instance, float speed, ref float __result)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                float slopeFactor = 1f;

                if (Utils.PlayerIsReady && PluginConfig.EnableSlopeSpeed.Value)
                {
                    slopeFactor = MovementSpeedController.GetSlope(player);
                }

                float weaponFactor = WeaponStats._WeapClass == "pistol" ? 1f : Mathf.Pow(1f - ((WeaponStats.ErgoFactor / 100f) * (1f - PlayerValues.StrengthWeightBuff)), 0.15f);
                float playerWeightFactor = Mathf.Pow(1f - ((PlayerValues.TotalModifiedWeightMinusWeapon / 100f) * (1f - PlayerValues.StrengthWeightBuff)), 0.3f); //doubling up because BSG's calcs are shit
                float surfaceMulti = PluginConfig.EnableMaterialSpeed.Value ? MovementSpeedController.GetSurfaceSpeed() : 1f;
                float firingMulti = MovementSpeedController.GetFiringMovementSpeedFactor(player);
                float stanceFactor = StanceController.CurrentStance == EStance.PatrolStance ? 1.33f : StanceController.CurrentStance == EStance.LowReady ? 1.15f : StanceController.CurrentStance == EStance.HighReady ? 1.05f : StanceController.CurrentStance == EStance.ShortStock ? 0.95f : 1f;
                float totalModifier = PlayerValues.HealthWalkSpeedFactor * surfaceMulti * slopeFactor * firingMulti * stanceFactor * weaponFactor * playerWeightFactor * Plugin.RealHealthController.AdrenalineMovementBonus;
                __result = Mathf.Clamp(speed, 0f, __instance.StateSpeedLimit * totalModifier);
                return false;
            }
            return true;
        }
    }

    public class CalculateSurfacePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("method_73", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, ref ValueTuple<bool, BaseBallistic.ESurfaceSound> __result)
        {

            if (__instance.IsYourPlayer)
            {
                MovementSpeedController.CurrentSurface = __result.Item2;
            }
        }
    }

    public class SprintAccelerationPatch : ModulePatch
    {
        private static FieldInfo playerField;
        private static FieldInfo rotationFrameSpanField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            rotationFrameSpanField = AccessTools.Field(typeof(MovementContext), "_averageRotationX");
            return typeof(MovementContext).GetMethod("SprintAcceleration", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, float deltaTime)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer)
            {
                ValueHandler rotationFrameSpan = (ValueHandler)rotationFrameSpanField.GetValue(__instance);
                float enduranceFactor = 1f + player.Skills.EnduranceBuffRestoration.Value;
                float armorSkillFactor = 1f + player.Skills.HeavyVestMoveSpeedPenaltyReduction.Value;

                float gearPenalty = 
                    GearController.HasRespirator ? 0.25f * enduranceFactor : 
                    GearController.HasGasMask ? 0.3f * enduranceFactor : 
                    GearController.FSIsActive && GearController.GearBlocksMouth ? 0.5f * armorSkillFactor :
                    GearController.NVGIsActive ? 0.6f : 1f;
                float weaponFactor = WeaponStats._WeapClass == "pistol" ? 1f : Mathf.Pow(1f - ((WeaponStats.ErgoFactor / 100f) * (1f - PlayerValues.StrengthWeightBuff)), 0.15f);
                float playerWeightFactor = PlayerValues.TotalModifiedWeightMinusWeapon >= 50f ? 1f - ((PlayerValues.TotalModifiedWeightMinusWeapon / 100f) * (1f - PlayerValues.StrengthWeightBuff)) : 1f; //doubling up because BSG's calcs are shit
                float slopeFactor = PluginConfig.EnableSlopeSpeed.Value ? MovementSpeedController.GetSlope(player) : 1f;
                float surfaceMulti = PluginConfig.EnableMaterialSpeed.Value ? MovementSpeedController.GetSurfaceSpeed() : 1f;
                float stanceSpeedBonus = StanceController.IsDoingTacSprint ? 1.15f * (1f + player.Skills.EnduranceBuffRestoration.Value) : 1f;
                float stanceAccelBonus = StanceController.CurrentStance == EStance.PatrolStance ? 1.45f : StanceController.CurrentStance == EStance.ShortStock ? 0.9f : StanceController.CurrentStance == EStance.LowReady ? 1.25f : StanceController.IsDoingTacSprint ? 1.37f : StanceController.CurrentStance == EStance.HighReady ? 1.2f : 1f;

                if (surfaceMulti < 1.0f)
                {
                    surfaceMulti = Mathf.Max(surfaceMulti * 0.85f, 0.2f);
                }
      
                float sprintAccel = player.Physical.SprintAcceleration * stanceAccelBonus * PlayerValues.HealthSprintAccelFactor * surfaceMulti * slopeFactor * PlayerValues.GearSpeedPenalty * weaponFactor * Plugin.RealHealthController.AdrenalineMovementBonus * gearPenalty * deltaTime * playerWeightFactor;
                float speed = (player.Physical.SprintSpeed * __instance.SprintingSpeed + 1f) * __instance.StateSprintSpeedLimit * stanceSpeedBonus * PlayerValues.HealthSprintSpeedFactor * surfaceMulti * slopeFactor * PlayerValues.GearSpeedPenalty * weaponFactor * gearPenalty * Plugin.RealHealthController.AdrenalineMovementBonus * playerWeightFactor;
                float sprintInertia = Mathf.Max(EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(Mathf.Abs((float)rotationFrameSpan.Average)), EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(2.1474836E+09f) * (2f - player.Physical.Inertia));
                speed = Mathf.Clamp(speed * sprintInertia, 0.1f, speed);
                __instance.SprintSpeed = Mathf.Clamp(__instance.SprintSpeed + sprintAccel * Mathf.Sign(speed - __instance.SprintSpeed), 0.01f, speed) * stanceSpeedBonus;
                return false;
            }
            return true;
        }
    }

    public class EnduranceSprintActionPatch : ModulePatch
    {

        private static Type _targetType;
        private static MethodInfo _method_0;

        public EnduranceSprintActionPatch()
        {
            _targetType = PatchConstants.EftTypes.Single(EndurancePatchHelper.IsEnduraStrngthType);
            _method_0 = _targetType.GetMethod("method_0", BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
        }

        protected override MethodBase GetTargetMethod()
        {
            return _method_0;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result, SkillMovementStruct movement, SkillManager __instance)
        {
            float xp = __instance.Settings.Endurance.SprintAction * (1f + __instance.Settings.Endurance.GainPerFatigueStack * movement.Fatigue);
            if (movement.Overweight <= 0f)
            {
                __result = xp;
            }
            else
            {
                __result = xp * 0.5f;
            }

            return false;
        }
    }

    public class EnduranceMovementActionPatch : ModulePatch
    {

        private static Type _targetType;
        private static MethodInfo _method_1;

        public EnduranceMovementActionPatch()
        {
            _targetType = PatchConstants.EftTypes.Single(EndurancePatchHelper.IsEnduraStrngthType);
            _method_1 = _targetType.GetMethod("method_1", BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
        }


        protected override MethodBase GetTargetMethod()
        {
            return _method_1;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result, SkillMovementStruct movement, SkillManager __instance)
        {
            float xp = __instance.Settings.Endurance.MovementAction * (1f + __instance.Settings.Endurance.GainPerFatigueStack * movement.Fatigue);
            if (movement.Overweight <= 0f)
            {
                __result = xp;
            }
            else
            {
                __result = xp * 0.5f;
            }

            return false;
        }
    }

    public static class EndurancePatchHelper
    {
        public static bool IsEnduraStrngthType(Type type)
        {
            return type.GetField("skillsRelatedToHealth") != null && type.GetField("skillManager_0") != null;
        }
    }
}
