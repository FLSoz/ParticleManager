using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ParticleManager
{
    internal class QPatch : ModBase
    {
        const string HarmonyID = "flsoz.ttmm.particlesystemmanager.mod";
        internal static Harmony harmony = new Harmony(HarmonyID);

        public static void Main()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void DeInit()
        {
            harmony.UnpatchAll(HarmonyID);
        }

        public override void Init()
        {
            Main();
        }
    }
}
