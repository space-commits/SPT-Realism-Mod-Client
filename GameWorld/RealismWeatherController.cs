using EFT.Weather;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class RealismWeatherController : MonoBehaviour
    {
        private static FieldInfo FogField = AccessTools.Field(typeof(WeatherDebug), "Fog");
        private static FieldInfo LighteningThunderField = AccessTools.Field(typeof(WeatherDebug), "LightningThunderProbability");
        private static FieldInfo RainField = AccessTools.Field(typeof(WeatherDebug), "Rain");
        private static FieldInfo TemperatureField = AccessTools.Field(typeof(WeatherDebug), "Temperature");

        private WeatherController wc;
        public bool DoExplosionEffect { get; set; }
        public bool Enable { get; set; }
        public float CloudDensity { get; set; }
        public float WindMagnitude { get; set; }
        public float Fog { get; set; }
        public float LighteningThunder { get; set; }
        public float Rain { get; set; }
        public Vector2 TopWindDirection { get; set; }
        public WeatherDebug.Direction WindDirection { get; set; }

        private float _elapsedTime = 0f;

        void Awake()
        {
        }

        void Update() 
        {
            if (wc == null) wc = WeatherController.Instance; //keep trying to get instance
            if (GameWorldController.GameStarted && wc != null)
            {
                HazardTracker.IsPreExplosion = true;
                if (HazardTracker.IsPreExplosion && !HazardTracker.HasExploded) DoPreExplosionWeather();
                if (HazardTracker.HasExploded) DoExplosionWeather();
                wc.WeatherDebug.Enabled = Enable;
                if (Enable) 
                {
                    wc.WeatherDebug.CloudDensity = CloudDensity;
                    wc.WeatherDebug.WindMagnitude = WindMagnitude;
                    wc.WeatherDebug.TopWindDirection = TopWindDirection;
                    wc.WeatherDebug.WindDirection = WindDirection;
                    FogField.SetValue(wc.WeatherDebug, Fog);
                    LighteningThunderField.SetValue(wc.WeatherDebug, LighteningThunder);
                    RainField.SetValue(wc.WeatherDebug, Rain);
                }
            }       
        }

        //change all this to a lerp
        private void DoExplosionWeather()
        {
            float delay = 200f;
            _elapsedTime += Time.deltaTime;
            wc.WeatherDebug.Enabled = Enable;

            WindDirection = WeatherDebug.Direction.South;
            TopWindDirection = Vector2.up;

            if (_elapsedTime >= delay)
            {
                Rain = Mathf.Lerp(Rain, 2f, 0.025f * Time.deltaTime);
                Fog = Mathf.Lerp(Fog, 0.075f, 0.025f * Time.deltaTime);
                CloudDensity = Mathf.Lerp(CloudDensity, 1f, 0.025f * Time.deltaTime);
                LighteningThunder = Mathf.Lerp(LighteningThunder, 1f, 0.1f * Time.deltaTime);
                WindMagnitude = Mathf.Lerp(WindMagnitude, 0.1f, 0.05f * Time.deltaTime);
            }
            else if (_elapsedTime >= 10f && _elapsedTime < delay)
            {
                Fog = Mathf.Lerp(Fog, 0f, 0.05f * Time.deltaTime);
                CloudDensity = Mathf.Lerp(CloudDensity, -0.75f, 0.25f * Time.deltaTime);
                WindMagnitude = Mathf.Lerp(WindMagnitude, 1.2f, 0.25f * Time.deltaTime);
            }
       
        }

        private void DoPreExplosionWeather()
        {
            Enable = true;
            CloudDensity = 1;
            Fog = 0.05f;
            Rain = 0.1f;
            WindMagnitude = 0;
            LighteningThunder = 0;
            WindDirection = WeatherDebug.Direction.East;
            TopWindDirection = Vector2.down;
        }
    }
}
