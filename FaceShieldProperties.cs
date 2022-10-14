using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static RealismMod.Helper;
using static EFT.Player;

namespace RealismMod
{

    public static class FaceShieldProperties
    {
        public static bool AllowsADS(Item fs)
        {

            if (Helper.nullCheck(fs.ConflictingItems))
            {
                return true;
            }

            return bool.Parse(fs.ConflictingItems[1]);
        }
    }
}
