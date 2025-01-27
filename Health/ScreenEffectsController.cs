using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod.Health
{
    public static class ScreenEffectsController
    {
        public static PrismEffects PrismEffects { get; set; }
        private static float _radiationEffectStrength = 0;

        public static void EffectsUpdate() 
        {
            if (PrismEffects != null) 
            {
                if (PluginConfig.ShowRadEffects.Value) DoRadiationEffects();
            }

            /*          PrismEffects.useSharpen = true;
                        PrismEffects.sharpenAmount = PluginConfig.test5.Value;*/

            /*        PrismEffects.useLensDirt = true;
                      PrismEffects.dirtIntensity = PluginConfig.test6.Value;*/ //not sure what to use it for


            /*            PrismEffects.useChromaticBlur = true;
                        PrismEffects.useChromaticAberration = true;
                        PrismEffects.chromaticBlurWidth = PluginConfig.test3.Value; //how blurry, by itself it's not chromatic just blur which could be good for things like adrenaline and such?, 0 - 20, could also be used to simulate gas in eyes
                        PrismEffects.chromaticIntensity = PluginConfig.test4.Value; //how much of an effect, 0-1*/
        }

        public static void DoRadiationEffects() 
        {
            float effectStrength = (HazardTracker.TotalRadiationRate * 80f) + (HazardTracker.TotalRadiation * 0.25f);
            _radiationEffectStrength = Mathf.Lerp(_radiationEffectStrength, effectStrength, 0.01f);
            if (_radiationEffectStrength > 0.1f)
            {
                PrismEffects.noiseIntensity = _radiationEffectStrength; 
                PrismEffects.useNoise = true;
            }
            else
            {
                PrismEffects.noiseIntensity = 0f;
                PrismEffects.useNoise = false;
            }
        }
    }
}
