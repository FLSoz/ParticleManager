using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ParticleManager
{
    internal class QPatch
    {
        public static void Main()
        {
            new Harmony("flsoz.ttmm.particlesystemmanager.mod").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
