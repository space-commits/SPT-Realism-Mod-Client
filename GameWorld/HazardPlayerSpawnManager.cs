using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public static class HazardPlayerSpawnManager
    {
        private static List<Collider> HazardZones = new List<Collider>();

        public static void Register(Collider col)
        {
            if (!HazardZones.Contains(col)) HazardZones.Add(col);
        }

        public static void Unregister(Collider col)
        {
            HazardZones.Remove(col);
        }

        public static void RestOnRaidEnd() 
        {
            HazardZones.Clear();
        }

        public static BoxCollider GetZoneContaining(Vector3 position)
        {
            foreach (var zone in HazardZones)
            {
                if (zone == null || zone as BoxCollider == null) continue;
                if (zone.bounds.Contains(position)) return zone as BoxCollider;
            }
            return null;
        }
    }
}
