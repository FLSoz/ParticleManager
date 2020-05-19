using Harmony;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace ParticleManager
{
    [HarmonyPatch(typeof(ModuleWeaponGun))]
    [HarmonyPatch("PrepareFiring")]
    public static class PatchModuleWeaponGunPrepare
    {
        public static void Postfix(ref ModuleWeaponGun __instance, ref bool __result, bool prepareFiring)
        {
            ModuleParticleManager newModule = __instance.GetComponent<ModuleParticleManager>();
            if (newModule != null)
            {
                if (newModule.beforeBarrelFired != null || newModule.onWeaponCharge != null)
                {
                    FieldInfo m_NextBarrelToFire = typeof(ModuleWeaponGun).GetField("m_NextBarrelToFire", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo m_ShotTimer = typeof(ModuleWeaponGun).GetField("m_ShotTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    int __m_NextBarrelToFire = (int)m_NextBarrelToFire.GetValue((object)__instance);

                    float to_set = newModule.PrepareFiring(prepareFiring, __result, __m_NextBarrelToFire);
                    if (to_set > 0.0)
                    {
                        Console.WriteLine("Attempting to set shotTimer to: " + to_set.ToString());
                        m_ShotTimer.SetValue(__instance, to_set);

                        __result = false;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ModuleWeaponGun))]
    [HarmonyPatch("ProcessFiring")]
    public static class PatchModuleWeaponGunProcess
    {
        public static bool Prefix(ref ModuleWeaponGun __instance, bool firing)
        {
            ModuleParticleManager newModule = __instance.GetComponent<ModuleParticleManager>();
            if (newModule != null)
            {
                if (newModule.beforeBarrelFired != null || newModule.onWeaponCharge != null)
                {
                    FieldInfo m_NextBarrelToFire = typeof(ModuleWeaponGun).GetField("m_NextBarrelToFire", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo m_ShotTimer = typeof(ModuleWeaponGun).GetField("m_ShotTimer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    int __m_NextBarrelToFire = (int)m_NextBarrelToFire.GetValue((object)__instance);
                    //float __m_ShotTimer = (float)m_ShotTimer.GetValue((object)__instance);

                    float to_set = newModule.ProcessFiring(firing, __m_NextBarrelToFire);
                    if (to_set > 0.0f)
                    {
                        Console.WriteLine("Attempting to set shotTimer to: " + to_set.ToString());
                        m_ShotTimer.SetValue(__instance, to_set);
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ModuleWeaponGun))]
    [HarmonyPatch("OnPool")]
    public static class PatchModuleWeaponGunPool
    {
        public static void Postfix(ref ModuleWeaponGun __instance)
        {
            ModuleParticleManager newModule = __instance.GetComponent<ModuleParticleManager>();
            if (newModule != null)
            {

                FieldInfo m_CannonBarrels = typeof(ModuleWeaponGun).GetField("m_CannonBarrels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo m_NumCannonBarrels = typeof(ModuleWeaponGun).GetField("m_NumCannonBarrels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo m_BurstShotCount = typeof(ModuleWeaponGun).GetField("m_BurstShotCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo m_BurstCooldown = typeof(ModuleWeaponGun).GetField("m_BurstCooldown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo m_ShotCooldown = typeof(ModuleWeaponGun).GetField("m_ShotCooldown", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo m_FireControlMode = typeof(ModuleWeaponGun).GetField("m_FireControlMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo m_ResetBurstOnInterrupt = typeof(ModuleWeaponGun).GetField("m_ResetBurstOnInterrupt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo m_CooldownVariancePct = typeof(ModuleWeaponGun).GetField("m_CooldownVariancePct", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                CannonBarrel[] __m_CannonBarrels = (CannonBarrel[]) m_CannonBarrels.GetValue((object)__instance);
                int __m_NumCannonBarrels = (int) m_NumCannonBarrels.GetValue((object)__instance);
                int __m_BurstShotCount = (int) m_BurstShotCount.GetValue((object)__instance);
                float __m_BurstCooldown = (float) m_BurstCooldown.GetValue((object)__instance);
                float __m_ShotCooldown = (float) m_ShotCooldown.GetValue((object)__instance);
                ModuleWeaponGun.FireControlMode __m_FireControlMode = (ModuleWeaponGun.FireControlMode) m_FireControlMode.GetValue((object)__instance);
                // bool __m_ResetBurstOnInterrupt = (bool) m_ResetBurstOnInterrupt.GetValue((object)__instance);
                float __m_CooldownVariancePct = (float) m_CooldownVariancePct.GetValue((object)__instance);

                // do Module-Specific NewModule hookups
                newModule.m_CannonBarrels = __m_CannonBarrels;
                newModule.m_BarrelFired = new bool[__m_NumCannonBarrels];
                newModule.m_BurstShotCount = __m_BurstShotCount;
                for (int i = 0; i < __m_NumCannonBarrels; i++)
                {
                    newModule.m_BarrelFired[i] = false;
                }

                // handle WeaponCharge first
                if (newModule.onWeaponCharge != null) {
                    float spinUpTime = 0.0f;
                    // if __result != preparefiring, then either spinner is charging.
                    // if __result == preparefiring, then either no spinner, or spinner is charged
                    FieldInfo m_FireSpinner = typeof(CannonBarrel).GetField("m_FireSpinner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    FieldInfo m_SpinUpTime = typeof(Spinner).GetField("m_SpinUpTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (CannonBarrel barrel in __m_CannonBarrels)
                    {
                        Spinner spinner = (Spinner)m_FireSpinner.GetValue(barrel);
                        if (spinner != null)
                        {
                            float instanceSpinUpTime = (float)m_SpinUpTime.GetValue(spinner);
                            spinUpTime = Mathf.Max(spinUpTime, instanceSpinUpTime);
                        }
                    }
                    float incrStartDelay = spinUpTime - newModule.maxWeaponChargeTime;
                    if (incrStartDelay <= 0.0f)
                    {
                        newModule.maxWeaponChargeTime = -incrStartDelay;
                    }
                    else
                    {
                        foreach (ParticleSystem system in newModule.onWeaponCharge)
                        {
                            var main = system.main;
                            ParticleSystem.MinMaxCurve startDelay = main.startDelay;
                            main.startDelay = new ParticleSystem.MinMaxCurve(startDelay.constant + incrStartDelay);
                        }
                    }
                }

                // align CannonBarrel arrays. If no effects are present and tied to it, end immediately, we don't care
                if (!newModule.AlignCannonBarrels(__m_CannonBarrels)) return;

                // since we want good charge timing, no variance is allowed
                __m_CooldownVariancePct = 0.0f;

                // Get info on what to do based on reload times:

                // if AllAtOnce, then we simplify, just use shotCoolDown, forcibly floor cooldown to the minimum of reload
                if (__m_FireControlMode == ModuleWeaponGun.FireControlMode.AllAtOnce)
                {
                    newModule.AllAtOnce = true;

                    // select which reload we gon be using
                    float min_reload = __m_ShotCooldown;
                    if (__m_BurstShotCount > 0)
                    {
                        if (__m_NumCannonBarrels != __m_BurstShotCount)
                        {
                            // this is dynamic cooldown
                            // min_reload = Mathf.Min(min_reload, __m_BurstCooldown);
                            // newModule.dynamicTimeCalc = true;
                            newModule.beforeBarrelFired = null;
                            newModule.defaultStartDelay = null;
                        }
                        else
                        {
                            min_reload = __m_BurstCooldown;
                        }
                    }

                    for (int i = 0; i < __m_NumCannonBarrels; i++)
                    {
                        List<float> curr_list = newModule.defaultStartDelay[i];
                        List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];
                        float maxTime = 0.0f;
                        for (int j = 0; j < curr_list.Count; j++)
                        {
                            float maxTimeAvailable = Mathf.Min(curr_list[j], min_reload);

                            curr_list[j] = min_reload - maxTimeAvailable;
                            var main = curr_system_list[j].main;
                            main.startDelay = new ParticleSystem.MinMaxCurve(curr_list[j]);
                            main.startDelayMultiplier = 1.0f;

                            maxTime = Mathf.Max(curr_list[j], maxTime);
                        }
                        newModule.adjStartDelay[i] = min_reload - maxTime;
                        newModule.maxTimeNeeded[i] = maxTime;
                    }
                }
                // handle burst fire. burst > 1 means it's actual burst fire. burst == 1 means it's sequential fire, with burst cooldown overriding shot cooldown
                else if (__m_BurstShotCount > 1)
                {
                    // newModule.m_ResetBurstOnInterrupt = __m_ResetBurstOnInterrupt;
                    // reset on interrupt isn't dynamic, but breaks assumptions used in start-barrel calculations, so we forcibly set it to true
                    m_ResetBurstOnInterrupt.SetValue(__instance, true);

                    // if this is true, can be safe in knowledge that existing grace_timer stuff is good
                    // we don't care. we solve this by using ModuleParticleManager.HandleFirst(m_currentBarrelInd) or thereabouts
                    /* if (__m_ResetBurstOnInterrupt)
                    {

                    } */


                    // If burst shot count is fraction/multiple of barrel num, then can take shortcut
                    // burst shot count is multiple of number of barrels, just forcibly set stuff to the min
                    if (__m_BurstShotCount % __m_NumCannonBarrels == 0)
                    {
                        // If it's bigger, then barrels will have both burst + shot_cooldown, and shot_cooldown, so it's technically dynamic. unsupported
                        if (__m_BurstCooldown > __m_NumCannonBarrels)
                        {
                            /* for (int i = 0; i < __m_NumCannonBarrels; i++)
                            {
                                List<float> curr_list = newModule.defaultStartDelay[i];
                                List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];
                                for (int j = 0; j < curr_list.Count; j++)
                                {
                                    curr_list[j] = Mathf.Min(curr_list[j], __m_ShotCooldown);
                                    var main = curr_system_list[j].main;
                                    main.startDelay = new ParticleSystem.MinMaxCurve(__m_ShotCooldown - curr_list[j]);
                                    main.startDelayMultiplier = 1.0f;
                                }
                            } */
                            // newModule.dynamicTimeCalc = true;
                            newModule.beforeBarrelFired = null;
                            newModule.defaultStartDelay = null;
                        }
                        // must be same number - each burst fires off a volley
                        else
                        {
                            // grace time is time to go through, + burst cooldown
                            float grace_time = (__m_ShotCooldown * (__m_BurstShotCount - 1)) + __m_BurstCooldown;

                            // set grace_time as ceiling
                            for (int i = 0; i < __m_NumCannonBarrels; i++)
                            {
                                List<float> curr_list = newModule.defaultStartDelay[i];
                                List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];

                                float maxTime = 0.0f;
                                for (int j = 0; j < curr_list.Count; j++)
                                {
                                    float maxTimeAvailable = Mathf.Min(curr_list[j], grace_time);
                                    curr_list[j] = grace_time - maxTimeAvailable;

                                    var main = curr_system_list[j].main;
                                    main.startDelay = new ParticleSystem.MinMaxCurve(curr_list[j]);
                                    main.startDelayMultiplier = 1.0f;

                                    maxTime = Mathf.Max(curr_list[j], maxTime);
                                }
                                newModule.adjStartDelay[i] = grace_time - maxTime;
                                newModule.maxTimeNeeded[i] = maxTime;

                                float comparator = __m_ShotCooldown * i;
                                if (maxTime > comparator)
                                {
                                    newModule.numStartModifications += 1;
                                    newModule.adjStartDelay[i] -= comparator;
                                }
                            }
                        }
                    }
                    // If we know each burst is only fraction of number of barrels, then can set times individually for each thing
                    else if (__m_NumCannonBarrels % __m_BurstShotCount == 0)
                    {
                        // should be > 1, else we would have done it in prior case
                        int num_sets = __m_NumCannonBarrels / __m_BurstShotCount;
                        float grace_time = (__m_ShotCooldown * (__m_BurstShotCount - 1)) + (num_sets * __m_BurstCooldown);

                        // set grace_time as ceiling
                        for (int i = 0; i < __m_NumCannonBarrels; i++)
                        {
                            List<float> curr_list = newModule.defaultStartDelay[i];
                            List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];

                            float maxTime = 0.0f;
                            for (int j = 0; j < curr_list.Count; j++)
                            {
                                float maxTimeAvailable = Mathf.Min(curr_list[j], grace_time);
                                curr_list[j] = grace_time - maxTimeAvailable;

                                var main = curr_system_list[j].main;
                                main.startDelay = new ParticleSystem.MinMaxCurve(curr_list[j]);
                                main.startDelayMultiplier = 1.0f;

                                maxTime = Mathf.Max(curr_list[j], maxTime);
                            }
                            newModule.adjStartDelay[i] = grace_time - maxTime;
                            newModule.maxTimeNeeded[i] = maxTime;

                            int num_bursts = i / __m_BurstShotCount;
                            float comparator = num_bursts * (__m_BurstCooldown) + __m_ShotCooldown * (i - num_bursts);
                            if (maxTime > comparator)
                            {
                                newModule.numStartModifications += 1;
                                newModule.adjStartDelay[i] -= comparator;
                            }
                        }
                    }
                    // else, must set shot timer every single time you've got a change to make
                    // currently unsupported
                    else
                    {
                        // newModule.dynamicTimeCalc = true;
                        newModule.beforeBarrelFired = null;
                        newModule.defaultStartDelay = null;
                    }
                }
                // Plain-old sequential fire (burst count == 1 is also sequential)
                else
                {
                    float grace_time = __m_ShotCooldown;
                    if (__m_BurstShotCount == 1)
                    {
                        grace_time = __m_BurstCooldown;
                    }
                    grace_time *= __m_NumCannonBarrels;

                    // set grace_time as ceiling
                    for (int i = 0; i < __m_NumCannonBarrels; i++)
                    {
                        List<float> curr_list = newModule.defaultStartDelay[i];
                        List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];

                        float maxTime = 0.0f;
                        for (int j = 0; j < curr_list.Count; j++)
                        {
                            float maxTimeAvailable = Mathf.Min(curr_list[j], grace_time);
                            curr_list[j] = grace_time - maxTimeAvailable;

                            var main = curr_system_list[j].main;
                            main.startDelay = new ParticleSystem.MinMaxCurve(curr_list[j]);
                            main.startDelayMultiplier = 1.0f;

                            maxTime = Mathf.Max(curr_list[j], maxTime);
                        }
                        newModule.adjStartDelay[i] = grace_time - maxTime;
                        newModule.maxTimeNeeded[i] = maxTime;

                        float comparator = __m_ShotCooldown * i;
                        if (maxTime > comparator)
                        {
                            newModule.numStartModifications += 1;
                            newModule.adjStartDelay[i] -= comparator;
                        }
                    }
                }
            }
            return;
        }
    }

}
