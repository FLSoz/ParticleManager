using Harmony;
using System.Reflection;
using UnityEngine;

namespace ParticleManager
{
    internal class QPatch
    {
        public static void Main()
        {
            HarmonyInstance.Create("flsoz.ttmm.particlesystemmanager.mod").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
