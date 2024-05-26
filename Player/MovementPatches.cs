using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SkillMovementStruct = EFT.SkillManager.GStruct228;
using ValueHandler = GClass733;

namespace RealismMod
{

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

                if (Utils.IsReady && Plugin.EnableSlopeSpeed.Value)
                {
                    slopeFactor = MovementSpeedController.GetSlope(player);
                }

                float surfaceMulti = Plugin.EnableMaterialSpeed.Value ? MovementSpeedController.GetSurfaceSpeed() : 1f;
                float firingMulti = MovementSpeedController.GetFiringMovementSpeedFactor(player);
                float stanceFactor = StanceController.CurrentStance == EStance.PatrolStance ? 1.4f : StanceController.CurrentStance == EStance.LowReady ? 1.25f : StanceController.CurrentStance == EStance.HighReady || StanceController.CurrentStance == EStance.ShortStock ? 0.95f : 1f;
                float totalModifier = PlayerState.HealthWalkSpeedFactor * surfaceMulti * slopeFactor * firingMulti * stanceFactor * Plugin.RealHealthController.AdrenalineMovementBonus;
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
            return typeof(Player).GetMethod("method_53", BindingFlags.Instance | BindingFlags.Public);
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

                bool canDoHighReadyBonus = StanceController.IsDoingTacSprint && !Plugin.RealHealthController.ArmsAreIncapacitated && !Plugin.RealHealthController.HasOverdosed;
                float slopeFactor = Plugin.EnableSlopeSpeed.Value ? MovementSpeedController.GetSlope(player) : 1f;
                float surfaceMulti = Plugin.EnableMaterialSpeed.Value ? MovementSpeedController.GetSurfaceSpeed() : 1f;
                float stanceSpeedBonus = canDoHighReadyBonus ? 1.15f : 1f;
                float stanceAccelBonus = StanceController.CurrentStance == EStance.PatrolStance ? 1.45f : StanceController.CurrentStance == EStance.ShortStock ? 0.9f : StanceController.CurrentStance == EStance.LowReady ? 1.25f : canDoHighReadyBonus ? 1.37f : StanceController.CurrentStance == EStance.HighReady ? 1.2f : 1f;

                if (surfaceMulti < 1.0f)
                {
                    surfaceMulti = Mathf.Max(surfaceMulti * 0.85f, 0.2f);
                }
                if (slopeFactor < 1.0f)
                {
                    surfaceMulti = Mathf.Max(surfaceMulti * 0.85f, 0.2f);
                }

                float sprintAccel = player.Physical.SprintAcceleration * stanceAccelBonus * PlayerState.HealthSprintAccelFactor * surfaceMulti * slopeFactor * PlayerState.GearSpeedPenalty * Plugin.RealHealthController.AdrenalineMovementBonus * deltaTime;
                float speed = (player.Physical.SprintSpeed * __instance.SprintingSpeed + 1f) * __instance.StateSprintSpeedLimit * stanceSpeedBonus * PlayerState.HealthSprintSpeedFactor * surfaceMulti * slopeFactor * PlayerState.GearSpeedPenalty * Plugin.RealHealthController.AdrenalineMovementBonus;
                float sprintInertia = Mathf.Max(EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(Mathf.Abs((float)rotationFrameSpan.Average)), EFTHardSettings.Instance.sprintSpeedInertiaCurve.Evaluate(2.1474836E+09f) * (2f - player.Physical.Inertia));
                speed = Mathf.Clamp(speed * sprintInertia, 0.1f, speed);
                __instance.SprintSpeed = Mathf.Clamp(__instance.SprintSpeed + sprintAccel * Mathf.Sign(speed - __instance.SprintSpeed), 0.01f, speed);
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
