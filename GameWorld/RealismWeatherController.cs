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
            DoPreExplosionWeather();
            if(wc == null) wc = WeatherController.Instance; //keep trying to get instance
            if (GameWorldController.GameStarted && wc != null)
            {
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

        private void DoExplosionWeather() 
        {
            _elapsedTime += Time.deltaTime;
            wc.WeatherDebug.Enabled = Enable;

            WindDirection = WeatherDebug.Direction.SE;
            TopWindDirection = Vector2.down;

            LighteningThunder = Mathf.Min(PluginConfig.test4.Value * Time.deltaTime, 100f);
            WindMagnitude = Mathf.Min(PluginConfig.test5.Value * Time.deltaTime, 10f);

            if (_elapsedTime >= PluginConfig.test1.Value)
            {
                Rain = Mathf.Min(PluginConfig.test2.Value * Time.deltaTime, 10f);
                Fog = Mathf.Min(PluginConfig.test3.Value * Time.deltaTime, 1f);
                CloudDensity = Mathf.Min(CloudDensity + (PluginConfig.test7.Value * Time.deltaTime), 1f);
            }
            else 
            {
                CloudDensity = Mathf.Max(CloudDensity - (PluginConfig.test6.Value * Time.deltaTime), -1f);
            }
        }

        private void DoPreExplosionWeather()
        {
            Enable = true;
            CloudDensity = 1;
            Fog = 0.15f;
            Rain = 0.1f;
            WindMagnitude = 0;
            LighteningThunder = 0;
            WindDirection = WeatherDebug.Direction.SE;
            TopWindDirection = Vector2.down;
        }
    }
}
