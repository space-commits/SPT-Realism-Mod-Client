using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class HazardZoneLocations
    {
        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> FactoryGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "FactoryTent", (0f, 25f, new Vector3(-17.5f, 0.35f, -41.4f), new Vector3(0f, 0f, 0f), new Vector3(6.5f, 4f, 38f)) },
            { "FactoryBasement", (0f, 50f, new Vector3(-6f, -3.6f, -20.5f), new Vector3(0f, 0f, 0f), new Vector3(25f, 3f, 36f)) },
            { "FactoryTanks", (0f, 40f, new Vector3(7f, -2.5f, 17f), new Vector3(0f, 0f, 0f), new Vector3(35f, 3f, 12f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> CustomsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "CustomsSwitchBasement", (0f, 60f, new Vector3(335f, -3.1f, -60.3f), new Vector3(0f, 0f, 0f), new Vector3(25f, 6.5f, 50f)) },
            { "CustomsZB013", (0f, 20f, new Vector3(199f, -2.8f, -145f), new Vector3(0f, 0f, 0f), new Vector3(12f, 5f, 16f)) },
            { "CustomsWarehouse5", (0f, 80f, new Vector3(474f, 2.6f, -68f), new Vector3(0f, 4f, 0f), new Vector3(23f, 4f, 46f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GZGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "GZUnderground", (-3f, 300f, new Vector3(80f, 15f, -20f), new Vector3(0f, 0f, 0f), new Vector3(50f, 3f, 110f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ShorelineGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ShorelinePool", (1f, 100f, new Vector3(-185f, -9.4f, -84f), new Vector3(0f, 0f, 0f), new Vector3(100f, 8f, 40f)) },
            { "ShorelineTruck1", (0f, 50f, new Vector3(-770f, -59.3f, 461f), new Vector3(0f, 0f, 0f), new Vector3(15f, 1.5f, 7f)) },
            { "ShorelineTunnel", (0f, 50f, new Vector3(385f, -59.9f, 310f), new Vector3(0f, 0f, 0f), new Vector3(35f, 8f, 15f)) },
            { "ShorelineSwamp1", (0f, 100f, new Vector3(273.7f, -55.39f, -125.5f), new Vector3(0f, 0f, 0f), new Vector3(20f, 8f, 20f)) }, //small next to graveyard
            { "ShorelineSwamp2", (0f, 100f, new Vector3(239.5f, -55.3f, -88.5f), new Vector3(0f, 0f, 0f), new Vector3(40f, 8f, 20f)) }, //next to church
            { "ShorelineSwamp3", (0f, 100f, new Vector3(238.5f, -54.3f, -174.5f), new Vector3(0f, 0f, 0f), new Vector3(35f, 8f, 65f)) },//big near church
            { "ShorelineSwamp4", (0f, 100f, new Vector3(305f, -55.3f, -150f), new Vector3(0f, -7f, 0f), new Vector3(25f, 8f, 75f)) },//middle
            { "ShorelineSwamp5", (0f, 100f, new Vector3(300f, -53f, -85f), new Vector3(0f, 25f, 0f), new Vector3(25f, 8f, 40f)) }, //middle
            { "ShorelineSwamp6", (0f, 100f, new Vector3(360f, -55f, -155f), new Vector3(0f, 0f, 0f), new Vector3(45f, 8f, 30f)) }, //near truck
            { "ShorelineSwamp7", (0f, 100f, new Vector3(374f, -54f, -107f), new Vector3(0f, 0f, 0f), new Vector3(35f, 8f, 30f)) }, //tree island
            { "ShorelineSwamp8", (0f, 100f, new Vector3(335f, -55f, -95f), new Vector3(0f, 15f, 0f), new Vector3(25f, 8f, 35f)) }, //actual center
            { "ShorelineSanitarOffice", (10f, 25f, new Vector3(-321f, -3.6f, -77f), new Vector3(0f, 0f, 0f), new Vector3(8f, 2f, 6f)) },
            { "ShorelineTrench1", (5f, 50f, new Vector3(-615f, -29.6f, -250f), new Vector3(0f, 10f, 0f), new Vector3(7f, 1f, 90f)) },
            { "ShorelineTrench2", (5f, 50f, new Vector3(-555f, -29.6f, -212f), new Vector3(0f, 48f, 0f), new Vector3(7f, 1f, 90f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LabsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "Labs", (10f, 380f, new Vector3(-193.5f, -4.1f, -342.9f), new Vector3(0f, 0f, 0f), new Vector3(200f, 25f, 200f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> InterchangeGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "InterchangeSaferoom", (10f, 5f, new Vector3(-48.4f, 22f, 43.6f), new Vector3(0f, 0f, 0f), new Vector3(10f, 3f, 4.5f)) },
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> LighthouseGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "LighthouseTunnel", (0f, 100f, new Vector3(-67f, 6f, 330f), new Vector3(0f, 8f, 0f), new Vector3(25f, 8f, 30f)) },
            { "LighthouseTrench", (0f, 100f, new Vector3(-98f, 1f, -584f), new Vector3(0f, 0f, 0f), new Vector3(80f, 3f, 6f)) }
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> WoodsGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "WoodsBloodSample", (0f, 25f, new Vector3(-96f, -15f, 220f), new Vector3(0f, 0f, 0f), new Vector3(3f, 3f, 4f)) },
            { "WoodsScavBunker", (0f, 50f, new Vector3(230f, 20f, -708f), new Vector3(0f, 5f, 0f), new Vector3(25f, 3f, 10f)) }
        };

        private static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> ReserveGasZones = new Dictionary<string, (float spawnChance, float strength, Vector3, Vector3, Vector3)>
        {
            { "ReserveStorage", (0f, 500f, new Vector3(50f, -13.5f, -110f), new Vector3(0f, 0f, 0f), new Vector3(90f, 6f, 190f)) },
            { "ReserveShaft", (0f, 50f, new Vector3(-58.9f, -15.9f, 179.9f), new Vector3(0f, 0f, 0f), new Vector3(14f, 55f, 16f)) },
            { "ReserveD2Extract", (0f, 40f, new Vector3(-109f, -18.4f, 161f), new Vector3(0f, 0f, 0f), new Vector3(50f, 5f, 25f)) },
            { "ReserveD2Rat", (0f, 50f, new Vector3(-67f, -18.6f, 141f), new Vector3(0f, 15f, 0f), new Vector3(45f, 4.5f, 15f)) },
            { "ReserveD2Tank", (0f, 50f, new Vector3(-78.5f, -19.8f, 113f), new Vector3(0f, 15f, 0f), new Vector3(55f, 4.5f, 15f)) },
            { "ReserveBunker", (0f, 250f, new Vector3(-105.5f, -14.5f, 42.5f), new Vector3(0f, 9f, 0f), new Vector3(65f, 4.5f, 45f)) }
        };

        public static Dictionary<string, (float spawnChance, float strength, Vector3 position, Vector3 rotation, Vector3 size)> GetMapZones(string map)
        {
            switch (map)
            {
                case "rezervbase":
                    return ReserveGasZones;
                case "bigmap":
                    return CustomsGasZones;
                case "factory4_night":
                case "factory4_day":
                    return FactoryGasZones;
                case "interchange":
                    return InterchangeGasZones;
                case "laboratory":
                    return LabsGasZones;
                case "shoreline":
                    return ShorelineGasZones;
                case "sandbox":
                    return GZGasZones;
                case "woods":
                    return WoodsGasZones;
                case "lighthouse":
                    return LighthouseGasZones;
                default:
                    return null;
            }
        }

    }
}
