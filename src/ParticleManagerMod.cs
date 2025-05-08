using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ParticleManager
{
    public class ParticleManagerMod : ModBase
    {
        const string HarmonyID = "flsoz.ttmm.particlesystemmanager.mod";
        internal static Harmony harmony = new Harmony(HarmonyID);

        public override void DeInit()
        {
            harmony.UnpatchAll(HarmonyID);
        }

        public override void Init()
        {
            ParticleManagerMod.harmony.PatchAll();
        }
    }
}
