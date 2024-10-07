using EFT.Animals;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RealismMod
{
    public class Birb : MonoBehaviour
    {
        private bool _wasDestroyed = false;

        private Dictionary<string, int> _birbLoot = new Dictionary<string, int>
        {
             {"5751487e245977207e26a315", 100 }, //rye croutons
             {"57347d3d245977448f7b7f61", 100 }, //other rye
             {"5448ff904bdc2d6f028b456e", 100 }, //army crackers
             {"5c06779c86f77426e00dd782", 100 }, //wires
             {"573474f924597738002c6174", 100 }, //chain
             {"5c1267ee86f77416ec610f72", 20 }, //pro-kill
             {"62a09cfe4f842e1bd12da3e4", 30 }, //gold ring,
             {"5734758f24597738025ee253", 50 }, //gold chain,
             {"59faff1d86f7746c51718c9c", 10 }, //btc,
             {"59faf7ca86f7740dbe19f6c2", 40 }, //roler,
             {"5780cf7f2459777de4559322", 30 }, //customs marked,
             {"5d80c60f86f77440373c4ece", 30 }, //rb-bk marked,
             {"5ede7a8229445733cb4c18e2", 30 }, //rb-pkpm,
             {"5d80c62a86f7744036212b3f", 30 }, //rb-vo,
             {"62987dfc402c7f69bf010923", 30 }, //lighthouse marked
             {"63a3a93f8a56922e82001f5d", 30 }, //abandoned marked
             {"64ccc25f95763a1ae376e447", 30 }, //mysterious marked
             {"64d4b23dc1b37504b41ac2b6", 30 }, //rusted marked
             {"5c94bbff86f7747ee735c08f", 15 }, //labs keys
             {"5c1d0d6d86f7744bb2683e1f", 5 }, //labs yellow
             {"5c1e495a86f7743109743dfb", 5 }, //labs violet
             {"5c1d0c5f86f7744bb2683cf0", 5 }, //labs blue
        };

        void Update()
        {
            if (!_wasDestroyed && (GameWorldController.DoMapGasEvent || Plugin.ModInfo.IsPreExplosion))
            {
                if (this.gameObject == null) return;
                Destroy(this.gameObject, 20f);
                _wasDestroyed = true;
            }
        }

        private IEnumerator HandleHitAsync()
        {
            if (Utils.SystemRandom.Next(10) <= 5) yield return Utils.LoadLoot(this.transform.position, this.transform.rotation, Utils.GetRandomWeightedKey(_birbLoot)).AsCoroutine(); //make sure to wait for loot to drop before destroying birb
            if (Utils.SystemRandom.Next(10) <= 3) yield return Utils.LoadLoot(this.transform.position, this.transform.rotation, Utils.GetRandomWeightedKey(_birbLoot)).AsCoroutine();
            if (Utils.SystemRandom.Next(10) <= 2) yield return Utils.LoadLoot(this.transform.position, this.transform.rotation, Utils.GetRandomWeightedKey(_birbLoot)).AsCoroutine();
            Destroy(this.gameObject);
        }

        public void OnHit(DamageInfo di)
        {
            StartCoroutine(HandleHitAsync());
        }
    }
}
