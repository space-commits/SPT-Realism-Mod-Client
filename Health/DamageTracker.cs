using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RealismMod
{
    public class DamageTracker
    {
        public Dictionary<EDamageType, Dictionary<EBodyPart, float>> DamageRecord = new Dictionary<EDamageType, Dictionary<EBodyPart, float>>();

        public float TotalHeavyBleedDamage = 0f;
        public float TotalLightBleedDamage = 0f;
        public float TotalDehydrationDamage = 0f;
        public float TotalExhaustionDamage = 0f;
        public float ToxicityRate = 0f;

        private float _totalToxicity;

        public float TotalToxicity 
        {
            get 
            {
                return _totalToxicity;
            }
            set 
            {
                _totalToxicity = Mathf.Clamp(value, 0f, 100f); 
            }
        }

        //need to differentiate between head and body blunt damage
        public void UpdateDamage(EDamageType damageType, EBodyPart bodyPart, float damage)
        {
            switch (damageType)
            {
                case EDamageType.HeavyBleeding:
                    TotalHeavyBleedDamage += damage;
                    return;
                case EDamageType.LightBleeding:
                    TotalLightBleedDamage += damage;
                    return;
            }

            if (!DamageRecord.ContainsKey(damageType))
            {
                DamageRecord[damageType] = new Dictionary<EBodyPart, float>();
            }

            Dictionary<EBodyPart, float> innerDict = DamageRecord[damageType];
            if (!innerDict.ContainsKey(bodyPart))
            {
                innerDict[bodyPart] = damage;
            }
            else
            {
                innerDict[bodyPart] += damage;
            }
        }

        public void ResetTracker()
        {
            if (DamageRecord.Any())
            {
                DamageRecord.Clear();
            }

            TotalHeavyBleedDamage = 0f;
            TotalLightBleedDamage = 0f;
            TotalDehydrationDamage = 0f;
            TotalExhaustionDamage = 0f;
        }
    }
}
