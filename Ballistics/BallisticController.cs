using Comfort.Common;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DamageInfo = DamageInfoStruct;

namespace RealismMod
{
    public enum EBodyHitZone
    {
        AZone,
        CZone,
        DZone,
        Heart,
        Spine,
        Unknown
    }

    public enum EHitOrientation
    {
        FrontHit,
        BackHit,
        LeftSideHit,
        RightSideHit,
        TopHit,
        BottomHit,
        UnknownOrientation
    }

    public static class BallisticsController
    {
        public static EBodyPartColliderType[] HeadCollidors = { EBodyPartColliderType.Eyes, EBodyPartColliderType.Ears, EBodyPartColliderType.Jaw, EBodyPartColliderType.BackHead, EBodyPartColliderType.NeckFront, EBodyPartColliderType.NeckBack, EBodyPartColliderType.HeadCommon, EBodyPartColliderType.ParietalHead };
        public static EBodyPartColliderType[] ArmCollidors = { EBodyPartColliderType.LeftUpperArm, EBodyPartColliderType.RightUpperArm, EBodyPartColliderType.LeftForearm, EBodyPartColliderType.RightForearm, };
        public static EBodyPartColliderType[] FaceSpallProtectionCollidors = { EBodyPartColliderType.NeckBack, EBodyPartColliderType.NeckFront, EBodyPartColliderType.Jaw, EBodyPartColliderType.Eyes, EBodyPartColliderType.HeadCommon };
        public static EBodyPartColliderType[] LegSpallProtectionCollidors = { EBodyPartColliderType.PelvisBack, EBodyPartColliderType.Pelvis};
        private static List<EBodyPart> _spallingBodyParts = new List<EBodyPart> { EBodyPart.RightArm, EBodyPart.LeftArm, EBodyPart.LeftLeg, EBodyPart.RightLeg, EBodyPart.Head, EBodyPart.Common, EBodyPart.Common };
        private static List<ArmorComponent> _preAllocatedArmorComponents = new List<ArmorComponent>(20);

        public static void CalcAfterPenStats(float actualDurability, float armorClass, float templateDurability, ref float damage, ref float penetration, float factor = 1) 
        {
            float armorFactor = 1f - ((armorClass / 80f) * (actualDurability / templateDurability));
            float damageReductionFactor = Mathf.Clamp(armorFactor, 0.85f, 1f);
            float penReductionFactor = Mathf.Clamp(armorFactor, 0.85f, 1f) * factor;
            damage *= damageReductionFactor;
            penetration *= penReductionFactor;
        }

        public static void ModifyDamageByHitZone(float maxHP, EBodyPartColliderType hitPart, EBodyHitZone hitZone, ref DamageInfo di)
        {
            switch (hitZone) 
            {
                case EBodyHitZone.Unknown:
                    break;
                case EBodyHitZone.AZone:
                    di.Damage *= 2.1f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 1.5f;
                    di.LightBleedingDelta *= 2f;
                    return;
                case EBodyHitZone.CZone:
                    di.Damage *= PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 1f;
                    di.LightBleedingDelta *= 1f;
                    return;
                case EBodyHitZone.DZone:
                    di.Damage *= 0.77f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 0.8f;
                    di.LightBleedingDelta *= 0.5f;
                    return;
                case EBodyHitZone.Heart:
                    di.Damage = maxHP * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 10f;
                    di.LightBleedingDelta *= 10f;
                    return;
                case EBodyHitZone.Spine:
                    di.Damage = (maxHP * 0.9f) * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 1.15f;
                    di.LightBleedingDelta *= 1.5f;
                    return;
            }

            switch (hitPart)
            {
                case EBodyPartColliderType.RightCalf:
                case EBodyPartColliderType.LeftCalf:
                    di.Damage *= 0.8f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 0.6f;
                    di.LightBleedingDelta *= 0.7f;
                    break;
                case EBodyPartColliderType.RightThigh:
                case EBodyPartColliderType.LeftThigh:
                    di.Damage *= 1.1f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 1.3f;
                    di.LightBleedingDelta *= 1.3f;
                    break;
                case EBodyPartColliderType.RightForearm:
                case EBodyPartColliderType.LeftForearm:
                    di.Damage *= 0.7f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 0.4f;
                    di.LightBleedingDelta *= 0.5f;
                    break;
                case EBodyPartColliderType.LeftUpperArm:
                case EBodyPartColliderType.RightUpperArm:
                    di.Damage *= 0.9f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 0.65f;
                    di.LightBleedingDelta *= 0.65f;
                    break;
                case EBodyPartColliderType.PelvisBack:
                case EBodyPartColliderType.Pelvis:
                    di.Damage *= 1.1f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 1.1f;
                    di.LightBleedingDelta *= 1.1f;
                    break;
                case EBodyPartColliderType.RibcageUp:
                case EBodyPartColliderType.SpineTop:
                    di.Damage *= 1f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 1.15f;
                    di.LightBleedingDelta *= 1.15f;
                    break;
                case EBodyPartColliderType.RibcageLow:
                case EBodyPartColliderType.SpineDown:
                    di.Damage *= 0.9f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 0.95f;
                    di.LightBleedingDelta *= 0.95f;
                    break;
                case EBodyPartColliderType.LeftSideChestDown:
                case EBodyPartColliderType.RightSideChestDown:
                    di.Damage *= 0.9f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 0.9f;
                    di.LightBleedingDelta *= 0.9f;
                    break;
                case EBodyPartColliderType.LeftSideChestUp:
                case EBodyPartColliderType.RightSideChestUp:
                    di.Damage *= 1.15f * PluginConfig.GlobalDamageModifier.Value;
                    di.HeavyBleedingDelta *= 1.25f;
                    di.LightBleedingDelta *= 1.25f;
                    break;
                case EBodyPartColliderType.NeckBack:
                case EBodyPartColliderType.NeckFront:
                    di.Damage *= 0.85f;
                    di.HeavyBleedingDelta *= 1.15f;
                    di.LightBleedingDelta *= 1.15f;
                    break;
                case EBodyPartColliderType.Jaw:
                    di.Damage *= 0.85f;
                    di.HeavyBleedingDelta *= 0.95f;
                    di.LightBleedingDelta *= 0.95f;
                    break;
                case EBodyPartColliderType.ParietalHead:
                    di.Damage *= 0.9f;
                    di.HeavyBleedingDelta *= 0.9f;
                    di.LightBleedingDelta *= 0.9f;
                    break;
                case EBodyPartColliderType.BackHead:
                    di.Damage *= 1.1f;
                    di.HeavyBleedingDelta *= 1.15f;
                    di.LightBleedingDelta *= 1.15f;
                    break;
                default:
                    break;
            }
        }


        private static float GetBleedFactor(EBodyPart part)
        {
            switch (part)
            {
                case EBodyPart.Head:
                    return 0.4f;
                case EBodyPart.LeftLeg:
                case EBodyPart.RightLeg:
                    return 0.5f;
                case EBodyPart.LeftArm:
                case EBodyPart.RightArm:
                    return 0.25f;
                default:
                    return 1;
            }
        }

        public static void PlayBodyHitSound(EBodyPart part, Vector3 pos, int rndNum)
        {
            float dist = CameraClass.Instance.Distance(pos);
            float volClose = 0.4f * PluginConfig.FleshHitSoundMulti.Value;
            float volDist = 2f * PluginConfig.FleshHitSoundMulti.Value;
            float distThreshold = 30f;

            if (part == EBodyPart.Head)
            {
                Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.RealismAudioController.HitAudioClips["headshot.wav"], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, volClose * 0.6f, EOcclusionTest.Regular);
                return;
            }

            string audioClip = "flesh_dist_1.wav";
            if (dist >= distThreshold)
            {
                audioClip = rndNum == 0 ? "flesh_dist_1.wav" : rndNum == 1 ? "flesh_dist_2.wav" : "flesh_dist_2.wav";
            }
            else
            {
                audioClip = rndNum == 0 ? "flesh_1.wav" : rndNum == 1 ? "flesh_2.wav" : "flesh_3.wav";
            }

            Singleton<BetterAudio>.Instance.PlayAtPoint(pos, Plugin.RealismAudioController.HitAudioClips[audioClip], dist, BetterAudio.AudioSourceGroupType.Impacts, 100, dist >= distThreshold ? volDist : volClose, EOcclusionTest.Regular);
        }

        public static EBodyHitZone GetBodyHitZone(Player player, EBodyPartColliderType partHit, DamageInfo damageInfo)
        {
            EBodyHitZone hitZone = EBodyHitZone.Unknown;
            if (partHit == EBodyPartColliderType.RibcageUp || partHit == EBodyPartColliderType.RibcageLow || partHit == EBodyPartColliderType.SpineDown || partHit == EBodyPartColliderType.SpineTop)
            {
                Collider col = damageInfo.HitCollider;
                if (damageInfo.HitCollider == null) //fika can't send objects as part of packets, need to find matching collider by checking collider type
                {
                    List<Collider> collidors = player.GetComponent<PlayerPoolObject>().Colliders;
                    if (collidors == null || collidors.Count <= 0) return hitZone;
                    int count = collidors.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Collider collider = collidors[i];
                        BodyPartCollider bp = collider.GetComponent<BodyPartCollider>();

                        if (bp != null && bp.BodyPartColliderType == partHit)
                        {
                            Utils.Logger.LogWarning(bp.BodyPartColliderType);
                            col = collider;
                            break;
                        }
                    }
                    if (col == null) return hitZone;
                }

                Vector3 localPoint = col.transform.InverseTransformPoint(damageInfo.HitPoint);
                Vector3 hitNormal = damageInfo.HitNormal;
                EHitOrientation hitOrientation = HitZones.GetHitOrientation(hitNormal, col.transform);
                hitZone = HitZones.GetHitBodyZone(localPoint, hitOrientation, partHit);
                if (PluginConfig.EnableBallisticsLogging.Value)
                {
                    Utils.Logger.LogWarning("=========Hitzone Damage Info==========");
                    Utils.Logger.LogWarning("hit collider = " + partHit);
                    Utils.Logger.LogWarning("hit orientation = " + hitOrientation);
                    Utils.Logger.LogWarning("hit zone = " + hitZone);
                    Utils.Logger.LogWarning("damage before = " + damageInfo.Damage);
                    Utils.Logger.LogWarning("x = " + localPoint.x);
                    Utils.Logger.LogWarning("y = " + localPoint.y);
                    Utils.Logger.LogWarning("z = " + localPoint.z);
                    Utils.Logger.LogWarning("===================");
                }
            }
            return hitZone;
        }

        public static void ModifyDamageByZone(Player player, EBodyPartColliderType partHit, float maxHp, bool isBluntDamage, ref DamageInfo damageInfo)
        {
            if (!isBluntDamage)
            {
                EBodyHitZone hitZone = GetBodyHitZone(player, partHit, damageInfo);
                BallisticsController.ModifyDamageByHitZone(maxHp, partHit, hitZone, ref damageInfo);
            }
        }


        public static bool ShouldDoSpalling(bool isBuckshot, AmmoTemplate ammoTemp, DamageInfo damageInfo, EBodyPart bodyPartType, bool isBluntDamage)
        {
            if (isBuckshot || ammoTemp == null || damageInfo.DamageType == EDamageType.Melee || !isBluntDamage || (bodyPartType != EBodyPart.Chest && bodyPartType != EBodyPart.Stomach)) return false;
             //peformance is terrible as of SPT 3.10, so disabling for buckshot for now
/*            if (isBuckshot)
            {
                int rndNum = UnityEngine.Random.Range(1, 30);
                if (rndNum > 3)
                {
                    return false;
                }
            }*/
            return true;
        }

        public static void GetKineticEnergy(DamageInfo damageInfo, AmmoTemplate ammoTemp, ref float KE)
        {
            if (damageInfo.DamageType == EDamageType.Melee)
            {
                Weapon weap = damageInfo.Weapon as Weapon;
                bool isBayonet = !damageInfo.Player.IsAI && WeaponStats.HasBayonet && weap.WeapClass != "Knife" ? true : false;
                float meleeDamage = isBayonet ? damageInfo.Damage : damageInfo.Damage * 2f;
                KE = meleeDamage * 50f;
            }
            else
            {
                if (damageInfo.ArmorDamage <= 1)
                {
                    KE = (0.5f * ammoTemp.BulletMassGram * ammoTemp.InitialSpeed * ammoTemp.InitialSpeed) / 1000f;
                }
                else
                {
                    KE = (0.5f * ammoTemp.BulletMassGram * damageInfo.ArmorDamage * damageInfo.ArmorDamage) / 1000f;
                }
            }
        }

        public static bool ArmorHasSpecificColliders(ArmorComponent armorComp, EBodyPartColliderType[] colliders) 
        {
            bool isMatch = false;
            var armorColliders = armorComp.Template.ArmorColliders;
            int count = armorColliders.Count();

            for (int i = 0; i < count; i++)
            {
                if (colliders.Contains(armorColliders[i]))
                {
                    isMatch = true;
                    break;
                }
            }
            return isMatch;
        }

        public static int ArmorColliderCount(ArmorComponent armorComp, EBodyPartColliderType[] colliders)
        {
            int armorCount = 0;
            var armorColliders = armorComp.Template.ArmorColliders;
            int count = armorColliders.Count();

            for (int i = 0; i < count; i++)
            {
                if (colliders.Contains(armorColliders[i]))
                {
                    armorCount += 1;
                }
            }
            return armorCount;
        }

        public static void GetArmorComponents(Player player, DamageInfo damageInfo, EBodyPart bodyPartType, ref ArmorComponent armor, ref int faceProtectionCount, ref bool doSpalling, ref bool hasArmArmor, ref bool hasLegProtection)
        {
            _preAllocatedArmorComponents.Clear();
            player.Inventory.GetPutOnArmorsNonAlloc(_preAllocatedArmorComponents);

            foreach (ArmorComponent armorComponent in _preAllocatedArmorComponents)
            {
                if ((armorComponent.Item.Id == damageInfo.BlockedBy || armorComponent.Item.Id == damageInfo.DeflectedBy))
                {
                    armor = armorComponent;
                    if (!doSpalling && bodyPartType != EBodyPart.LeftArm && bodyPartType != EBodyPart.RightArm) break;
                }
                if (doSpalling || bodyPartType == EBodyPart.LeftArm || bodyPartType == EBodyPart.RightArm)
                {
                    if (ArmorHasSpecificColliders(armorComponent, ArmCollidors)) hasArmArmor = true;
                    if (!doSpalling) break;
                }
                if (doSpalling)
                {
                    if (ArmorHasSpecificColliders(armorComponent, LegSpallProtectionCollidors)) hasLegProtection = true;
                    faceProtectionCount += ArmorColliderCount(armorComponent, FaceSpallProtectionCollidors);
                }
            }

            _preAllocatedArmorComponents.Clear();
        }

        public static void CalculatSpalling(Player player, ref DamageInfo damageInfo, float KE, ArmorComponent armor, AmmoTemplate ammoTemp, int faceProtectionCount, bool hasArmArmor, bool hasLegProtection)
        {
            var gearStats = TemplateStats.GetDataObj<Gear>(TemplateStats.GearStats, armor.Item.TemplateId);

            bool isMetalArmor = armor.Template.ArmorMaterial == EArmorMaterial.ArmoredSteel || armor.Template.ArmorMaterial == EArmorMaterial.Titan ? true : false;
            float bluntDamage = damageInfo.Damage;
            float speedFactor = damageInfo.ArmorDamage / ammoTemp.InitialSpeed;
            float fragChance = ammoTemp.FragmentationChance * speedFactor;
            float lightBleedChance = 0.8f; //if it's blunt damage, I set bleed delta to -1, so need new value
            float heavyBleedChance = 0.25f;
            float ricochetChance = ammoTemp.RicochetChance * speedFactor;
            float spallReduction = gearStats.SpallReduction;
            float armorDamageActual = ammoTemp.ArmorDamage * speedFactor;
            float penPower = damageInfo.PenetrationPower;

            //need to redo this: for non-steel, higher pen should mean lower spall damage. I'm also sort of taking durability into account twice
            //ideally should use momentum instead too?

            float duraPercent = armor.Repairable.Durability / armor.Repairable.TemplateDurability;
            float spallDuraFactor = Mathf.Min(armor.Repairable.TemplateDurability / Mathf.Max(armor.Repairable.Durability, 1), 2);
            float armorFactor = armor.ArmorClass * 10f * duraPercent;
            float penDuraFactoredClass = 10f + Mathf.Max(1f, armorFactor - (penPower / 1.8f));
            float maxPotentialSpallDamage = KE / penDuraFactoredClass;

            float factoredSpallingDamage = maxPotentialSpallDamage * (fragChance + 1) * (ricochetChance + 1) * spallReduction * (isMetalArmor ? (1f - duraPercent) + 1f : 1f);
            float maxSpallingDamage = Mathf.Clamp(factoredSpallingDamage - bluntDamage, 7f, 35f * spallDuraFactor);
            float splitSpallingDmg = maxSpallingDamage / _spallingBodyParts.Count;

            damageInfo.BleedBlock = false;

            if (PluginConfig.EnableBallisticsLogging.Value)
            {
                Utils.Logger.LogWarning("===========SPALLING=============== ");
                Utils.Logger.LogWarning("Spall Reduction " + spallReduction);
                Utils.Logger.LogWarning("Dura Percent " + duraPercent);
                Utils.Logger.LogWarning("Armor factorPercent " + duraPercent);
                Utils.Logger.LogWarning("Max Dura Factored Damage " + maxPotentialSpallDamage);
                Utils.Logger.LogWarning("Factored Spalling Damage " + factoredSpallingDamage);
                Utils.Logger.LogWarning("Max Spalling Damage " + maxSpallingDamage);
                Utils.Logger.LogWarning("Split Spalling Dmg " + splitSpallingDmg);
            }

            int rndNum = Mathf.Max(1, UnityEngine.Random.Range(1, _spallingBodyParts.Count + 1));
            foreach (EBodyPart part in _spallingBodyParts.OrderBy(x => UnityEngine.Random.value).Take(rndNum))
            {
                if (part == EBodyPart.Common)
                {
                    return;
                }

                float damage = splitSpallingDmg;
                float bleedFactor = GetBleedFactor(part);

                if (part == EBodyPart.Head)
                {
                    float damageMulti = Mathf.Clamp(1f - (faceProtectionCount / 10f), 0.1f, 1f);
                    damage = Mathf.Min(10, splitSpallingDmg * damageMulti);
                    bleedFactor = bleedFactor * damageMulti;
                }

                if ((part == EBodyPart.LeftArm || part == EBodyPart.RightArm) && hasArmArmor)
                {
                    damage *= 0.5f;
                    bleedFactor *= 0.25f;
                }

                if ((part == EBodyPart.LeftLeg || part == EBodyPart.RightLeg) && hasLegProtection)
                {
                    damage *= 0.5f;
                    bleedFactor *= 0.25f;
                }

                if (PluginConfig.EnableBallisticsLogging.Value)
                {
                    Utils.Logger.LogWarning("==== ");
                    Utils.Logger.LogWarning("Part Hit " + part);
                    Utils.Logger.LogWarning("Damage " + damage);
                    Utils.Logger.LogWarning("=== ");
                }
                damageInfo.HeavyBleedingDelta = heavyBleedChance * bleedFactor;
                damageInfo.LightBleedingDelta = lightBleedChance * bleedFactor;
                player.ActiveHealthController.ApplyDamage(part, damage, damageInfo);
            }
        }

        private static void ModifyPlateHelper(Collider collider, BoxCollider boxCollider, string colliderName, string target, float x, float y, float z) 
        {
            if (colliderName.Contains(target))
            {
                float height = boxCollider.size.x * x;
                float depth = boxCollider.size.y * y;
                float width = boxCollider.size.z * z;
                boxCollider.size = new Vector3(height, depth, width);

                if (PluginConfig.EnableBallisticsLogging.Value) DebugGizmos.SingleObjects.VisualizeBoxCollider(boxCollider, collider.name);
            }
        }

        public static void ModifyPlateColliders(Player player) 
        {
            List<Collider> collidors = player.GetComponent<PlayerPoolObject>().Colliders;
            if (collidors == null || collidors.Count <= 0) return;
            int count = collidors.Count;
            for (int i = 0; i < count; i++)
            {
                Collider collider = collidors[i];
                BoxCollider boxCollider = collider as BoxCollider;
                if (boxCollider != null)
                {
                    string colliderName = collider.name.ToLower();
                    if (colliderName == "left" || colliderName == "right" || colliderName == "top") boxCollider.size *= 0f;
                    ModifyPlateHelper(collider, boxCollider, colliderName, "_chest", 0.985f, 0.475f, 0.87f); //chest plate, height, depth, width
                    ModifyPlateHelper(collider, boxCollider, colliderName, "_back", 0.78f, 0.58f, 0.84f); //height, depth, width
                    ModifyPlateHelper(collider, boxCollider, colliderName, "_side_", 0.82f, 1f, 0.7f); //height, width, depth
                    ModifyPlateHelper(collider, boxCollider, colliderName, "chesttop", 1.55f, 0.9f, 1f);//armpits, height, width, depth
                }
            }
        }
    }

    public static class HitZones
    {
        public static EHitOrientation GetHitOrientation(Vector3 hitNormal, Transform colliderTransform)
        {
            Vector3 localHitNormal = colliderTransform.InverseTransformDirection(hitNormal);

            if (Mathf.Abs(localHitNormal.y) > Mathf.Abs(localHitNormal.x) && Mathf.Abs(localHitNormal.y) > Mathf.Abs(localHitNormal.z))
            {
                if (localHitNormal.y > 0)
                {
                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("hit front side of the box collider");
                    }
                    return EHitOrientation.FrontHit; //FRONT
                }
                else
                {
                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("hit back side of the box collider");
                    }
                    return EHitOrientation.BackHit; //BACK
                }
            }
            else if (Mathf.Abs(localHitNormal.x) > Mathf.Abs(localHitNormal.y) && Mathf.Abs(localHitNormal.x) > Mathf.Abs(localHitNormal.z))
            {
                if (localHitNormal.x > 0)
                {
                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("hit bottom side of the box collider");
                    }
                    return EHitOrientation.BottomHit; //BOTTOM
                }
                else
                {
                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("hit top side of the box collider");
                    }
                    return EHitOrientation.TopHit; //TOP
                }
            }
            else
            {
                if (localHitNormal.z > 0)
                {
                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("hit left side of the box collider");
                    }
                    return EHitOrientation.LeftSideHit; // LEFT
                }
                else
                {
                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("hit right side of the box collider");
                    }
                    return EHitOrientation.RightSideHit; // RIGHT
                }
            }
        }

        private static bool hitSpine(Vector3 localPoint, bool isSideHit, float spineZ)
        {
            if (localPoint.z >= -spineZ && localPoint.z <= spineZ && !isSideHit)
            {
                if (PluginConfig.EnableBallisticsLogging.Value == true)
                {
                    Utils.Logger.LogWarning("SPINE HIT");
                }
                return true;
            }
            return false;
        }

        public static EBodyHitZone GetHitBodyZone(Vector3 localPoint, EHitOrientation hitOrientation, EBodyPartColliderType hitPart)
        {
            bool isSideHit = hitOrientation == EHitOrientation.LeftSideHit || hitOrientation == EHitOrientation.RightSideHit;

            if (hitPart == EBodyPartColliderType.RibcageUp || hitPart == EBodyPartColliderType.RibcageLow || hitPart == EBodyPartColliderType.SpineDown || hitPart == EBodyPartColliderType.SpineTop)
            {
                float spineZ = 0.0125f;
                float heartL = 0.0374f;
                float heartR = -0.02f;
                float heartTop = hitOrientation == EHitOrientation.BackHit ? -0.0435f : -0.063f;
                float heartBottom = hitOrientation == EHitOrientation.BackHit ? -0.028f : -0.05f;

                float dZoneZUpper = hitOrientation == EHitOrientation.BackHit ? 0.11f : 0.115f;
                float dZoneZMid = hitOrientation == EHitOrientation.BackHit ? 0.11f : 0.115f;
    
                float aZoneZUpper = 0.0465f;
                float aZoneZMid = 0.0465f;
                float aZoneXMid = -0.07f; //-0.17f

                float topNeckZ = 0.04f;
                /*float topNeckX = -0.25f;*/
                float rearNeckZ = 0.05f;
                float rearNeckX = -0.2f;

                if (hitOrientation == EHitOrientation.BottomHit) 
                {
                    return EBodyHitZone.CZone;
                }

                if (hitOrientation != EHitOrientation.TopHit && hitOrientation != EHitOrientation.BottomHit)
                {
                    if (isSideHit)
                    { 
                        if (PluginConfig.EnableBallisticsLogging.Value == true)
                        {
                            Utils.Logger.LogWarning("SIDE HIT");
                        }
                        return EBodyHitZone.DZone;
                    }

                    if (hitPart == EBodyPartColliderType.RibcageUp || hitPart == EBodyPartColliderType.SpineTop)
                    {
                        if (hitOrientation == EHitOrientation.BackHit && localPoint.z > -rearNeckZ && localPoint.z < rearNeckZ && localPoint.x < rearNeckX)
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("NECK: BACK HIT (Counting As A-Zone)");
                            }
                            return EBodyHitZone.AZone;
                        }

                        if (localPoint.z <= heartL && localPoint.z >= heartR && localPoint.x >= heartTop && localPoint.x <= heartBottom && !isSideHit)
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("HEART HIT");
                            }
                            return EBodyHitZone.Heart;
                        }

                        if (hitSpine(localPoint, isSideHit, spineZ))
                        {
                            return EBodyHitZone.Spine;
                        }

                        if (localPoint.z < -dZoneZUpper || localPoint.z > dZoneZUpper)
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("D-ZONE HIT: UPPER TORSO");
                            }
                            return EBodyHitZone.DZone;

                        }
                        else if (localPoint.z > -aZoneZUpper && localPoint.z < aZoneZUpper)
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("A-ZONE HIT: UPPER TORSO");
                            }
                            return EBodyHitZone.AZone;
                        }
                        else
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("C-ZONE HIT: UPPER TORSO");
                            }
                            return EBodyHitZone.CZone;
                        }
                    }

                    if (hitSpine(localPoint, isSideHit, spineZ))
                    {
                        return EBodyHitZone.Spine;
                    }

                    if (hitPart == EBodyPartColliderType.RibcageLow || hitPart == EBodyPartColliderType.SpineDown)
                    {
                        if (localPoint.z < -dZoneZMid || localPoint.z > dZoneZMid)
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("D-ZONE HIT: MID TORSO");
                            }
                            return EBodyHitZone.DZone;
                        }
                        else if (localPoint.z > -aZoneZMid && localPoint.z < aZoneZMid && localPoint.x < aZoneXMid)
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("A-ZONE HIT: MID TORSO");
                            }
                            return EBodyHitZone.AZone;
                        }
                        else
                        {
                            if (PluginConfig.EnableBallisticsLogging.Value == true)
                            {
                                Utils.Logger.LogWarning("C-ZONE HIT: MID TORSO");
                            }
                            return EBodyHitZone.CZone;
                        }
                    }

                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("COULDN'T FIND HIT ZONE");
                    }
                    return EBodyHitZone.Unknown;
                }

                if (hitOrientation == EHitOrientation.TopHit && (hitPart == EBodyPartColliderType.RibcageUp || hitPart == EBodyPartColliderType.SpineTop))
                {
                    if (localPoint.z > -spineZ && localPoint.z < spineZ)
                    {
                        if (PluginConfig.EnableBallisticsLogging.Value == true)
                        {
                            Utils.Logger.LogWarning("SPINE: TOP HIT");
                        }
                        return EBodyHitZone.Spine;
                    }
                    if (localPoint.z > -topNeckZ && localPoint.z < topNeckZ) // && localPoint.x < topNeckX
                    {
                        if (PluginConfig.EnableBallisticsLogging.Value == true)
                        {
                            Utils.Logger.LogWarning("NECK: TOP HIT (Counting As A-Zone)");
                        }
                        return EBodyHitZone.AZone;
                    }
                    if (PluginConfig.EnableBallisticsLogging.Value == true)
                    {
                        Utils.Logger.LogWarning("D-ZONE: TOP SHOULDERS HIT");
                    }
                    return EBodyHitZone.DZone;
                }
            }
            if (PluginConfig.EnableBallisticsLogging.Value == true)
            {
                Utils.Logger.LogWarning("COULDN'T FIND HIT ZONE");
            }
            return EBodyHitZone.Unknown;
        }
    }
}
