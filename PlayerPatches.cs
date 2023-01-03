using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using System;
using System.Linq;
using System.Reflection;


namespace RealismMod
{

    public class EnduranceSprintActionPatch : ModulePatch
    {

        private static Type _targetType;
        private static MethodInfo _method0;

        public EnduranceSprintActionPatch()
        {
            _targetType = PatchConstants.EftTypes.Single(PlayerHelper.IsEnduraStrngthType);
            _method0 = _targetType.GetMethod("method_0", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        protected override MethodBase GetTargetMethod()
        {
            return _method0;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result, SkillsClass.GStruct182 movement, SkillsClass __instance)
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
        private static MethodInfo _method1;

        public EnduranceMovementActionPatch()
        {
            _targetType = PatchConstants.EftTypes.Single(PlayerHelper.IsEnduraStrngthType);
            _method1 = _targetType.GetMethod("method_1", BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);
        }


        protected override MethodBase GetTargetMethod()
        {
            return _method1;
        }

        [PatchPrefix]
        private static bool Prefix(ref float __result, SkillsClass.GStruct182 movement, SkillsClass __instance)
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

    public static class PlayerHelper
    {
        public static bool IsEnduraStrngthType(Type type)
        {
            return type.GetField("skillsRelatedToHealth") != null && type.GetField("gclass1559_0") != null;
        }
    }
}

