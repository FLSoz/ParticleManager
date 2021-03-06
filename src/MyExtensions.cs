﻿using Harmony;
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
                    
                    if (to_set >= 0.0)
                    {
                        Console.WriteLine("Attempting to set shotTimer to: " + to_set.ToString());
                        m_ShotTimer.SetValue(__instance, to_set);

                        if (to_set > 0.0) __result = false;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(ModuleWeaponGun))]
    [HarmonyPatch("ProcessFiring")]
    public static class PatchModuleWeaponGunProcess
    {
        public static bool Prefix(ref ModuleWeaponGun __instance, ref bool firing)
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
                        firing = false;
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
                            main.startDelay = startDelay.constant + incrStartDelay;
                        }
                    }
                }

                // align CannonBarrel arrays. If no effects are present and tied to it, end immediately, we don't care
                if (__m_FireControlMode == ModuleWeaponGun.FireControlMode.AllAtOnce)
                {
                    newModule.AllAtOnce = true;
                }
                if (!newModule.AlignCannonBarrels(__m_CannonBarrels)) return;

                // since we want good charge timing, no variance is allowed
                __m_CooldownVariancePct = 0.0f;

                // Get info on what to do based on reload times:

                // if AllAtOnce, then we simplify, just use shotCoolDown, forcibly floor cooldown to the minimum of reload
                if (__m_FireControlMode == ModuleWeaponGun.FireControlMode.AllAtOnce)
                {
                    newModule.AllAtOnce = true;

                    PatchModuleWeaponGunPool.OnPoolAllAtOnce(__m_ShotCooldown, __m_BurstCooldown, __m_BurstShotCount, __m_NumCannonBarrels, newModule);
                }
                // handle burst fire. burst > 1 means it's actual burst fire. burst == 1 means it's sequential fire, with burst cooldown overriding shot cooldown
                else if (__m_BurstShotCount > 1)
                {
                    // newModule.m_ResetBurstOnInterrupt = m_ResetBurstOnInterrupt;
                    // reset on interrupt isn't dynamic, but breaks assumptions used in start-barrel calculations, so we forcibly set it to true
                    m_ResetBurstOnInterrupt.SetValue(__instance, true);

                    // if this is true, can be safe in knowledge that existing cycle_timer stuff is good
                    // we don't care. we solve this by using ModuleParticleManager.HandleFirst(m_currentBarrelInd) or thereabouts
                    /* if (m_ResetBurstOnInterrupt)
                    {

                    } */

                    PatchModuleWeaponGunPool.OnPoolBurst(__m_ShotCooldown, __m_BurstCooldown, __m_BurstShotCount, __m_NumCannonBarrels, newModule);
                }
                // Plain-old sequential fire (burst count == 1 is also sequential)
                else
                {
                    PatchModuleWeaponGunPool.OnPoolSequential(__m_ShotCooldown, __m_BurstCooldown, __m_BurstShotCount, __m_NumCannonBarrels, newModule);
                }
            }
            return;
        }

        private static void OnPoolAllAtOnce(float m_ShotCooldown, float m_BurstCooldown, int m_BurstShotCount, int m_NumCannonBarrels, ModuleParticleManager newModule)
        {
            // select which reload we gon be using
            float cycle_time = m_ShotCooldown;
            if (m_BurstShotCount > 0)
            {
                if (m_NumCannonBarrels < m_BurstShotCount)
                {
                    // this is dynamic cooldown
                    // cycle_time = Mathf.Min(cycle_time, m_BurstCooldown);
                    // newModule.dynamicTimeCalc = true;
                    newModule.beforeBarrelFired = null;
                    newModule.defaultTimeNeeded = null;

                    newModule.DebugPrint("<NPM> Dynamic Time Calc Detected [AllAtOnce]");
                    return;
                }
                else
                {
                    cycle_time = m_BurstCooldown;
                }
            }
            newModule.DebugPrint("<NPM> [OnPoolAllAtOnce] cycle_time calculated at: " + cycle_time.ToString());

            for (int i = 0; i < m_NumCannonBarrels; i++)
            {
                List<float> curr_list = newModule.defaultTimeNeeded[i];
                List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];
                float maxTime = 0.0f;
                float[] timeRequested = new float[curr_list.Count];
                bool found_to_remove = false;
                for (int j = 0; j < curr_list.Count; j++)
                {
                    float maxTimeAvailable = Mathf.Min(curr_list[j], cycle_time);
                    newModule.DebugPrint("<MPM> [OnPoolAllAtOnce] Requested play time of " + maxTimeAvailable.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());

                    var main = curr_system_list[j].main;
                    timeRequested[j] = maxTimeAvailable;
                    if (timeRequested[j] <= 0.0f)
                    {
                        found_to_remove = true;
                    }
                    else
                    {
                        maxTime = Mathf.Max(maxTimeAvailable, maxTime);
                    }
                }
                // newModule.adjStartDelay[i] = cycle_time - maxTime;
                newModule.adjCycleDelay[i] = cycle_time - maxTime;  // how much time between cycle end, and start of particles in next cycle
                newModule.maxTimeNeeded[i] = maxTime;               // how much time is enough for every single system

                // remove particle systems that request 0 time
                if (found_to_remove)
                {
                    List<float> temp_list = new List<float>();
                    List<ParticleSystem> temp_system_list = new List<ParticleSystem>();

                    int offset = 0;

                    for (int j = 0; j < curr_list.Count; j++)
                    {
                        if (timeRequested[j] > 0.0f)
                        {
                            temp_list.Add(curr_list[j]);
                            temp_system_list.Add(curr_system_list[j]);
                            timeRequested[j - offset] = timeRequested[j];
                        }
                        else
                        {
                            offset += 1;
                        }
                    }

                    newModule.defaultTimeNeeded[i] = temp_list;
                    newModule.beforeBarrelFired[i] = temp_system_list;

                    curr_list = newModule.defaultTimeNeeded[i];
                    curr_system_list = newModule.beforeBarrelFired[i];
                }

                // set proper startDelays for remaining PS
                for (int j = 0; j < curr_list.Count; j++)
                {
                    float newDelay = maxTime - timeRequested[j];

                    ParticleSystem currSystem = curr_system_list[j];
                    var main = currSystem.main;
                    main.startDelay = newDelay;
                    // main.startDelayMultiplier = 1.0f;
                    newModule.DebugPrint("<MPM> [OnPool] Assign new startDelay of " + newDelay.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());
                    newModule.defaultTimeNeeded[i][j] = newDelay;
                }
            }
        }

        private static void OnPoolBurst(float m_ShotCooldown, float m_BurstCooldown, int m_BurstShotCount, int m_NumCannonBarrels, ModuleParticleManager newModule)
        {

            // If burst shot count is fraction/multiple of barrel num, then can take shortcut
            // burst shot count is multiple of number of barrels, just forcibly set stuff to the min
            if (m_BurstShotCount % m_NumCannonBarrels == 0)
            {
                // If it's bigger, then barrels will have both burst + shot_cooldown, and shot_cooldown, so it's technically dynamic. unsupported
                if (m_BurstShotCount > m_NumCannonBarrels)
                {
                    /* for (int i = 0; i < m_NumCannonBarrels; i++)
                    {
                        List<float> curr_list = newModule.defaultTimeNeeded[i];
                        List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];
                        for (int j = 0; j < curr_list.Count; j++)
                        {
                            curr_list[j] = Mathf.Min(curr_list[j], m_ShotCooldown);
                            var main = curr_system_list[j].main;
                            main.startDelay = m_ShotCooldown - curr_list[j];
                            // main.startDelayMultiplier = 1.0f;
                        }
                    } */
                    // newModule.dynamicTimeCalc = true;
                    newModule.beforeBarrelFired = null;
                    newModule.defaultTimeNeeded = null;

                    newModule.DebugPrint("<NPM> Dynamic Time Calc Detected [Burst] ShotCount > CannonBarrels");
                    return;
                }
                // must be same number - each burst fires off a volley
                else
                {
                    // grace time is time to go through, + burst cooldown
                    float cycle_time = (m_ShotCooldown * (m_BurstShotCount - 1)) + m_BurstCooldown;
                    newModule.DebugPrint("<NPM> [OnPoolBurst] cycle_time calculated at: " + cycle_time.ToString());

                    // set cycle_time as ceiling
                    for (int i = 0; i < m_NumCannonBarrels; i++)
                    {
                        List<float> curr_list = newModule.defaultTimeNeeded[i];
                        List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];
                        float maxTime = 0.0f;
                        float[] timeRequested = new float[curr_list.Count];
                        bool found_to_remove = false;
                        for (int j = 0; j < curr_list.Count; j++)
                        {
                            float maxTimeAvailable = Mathf.Min(curr_list[j], cycle_time);
                            newModule.DebugPrint("<MPM> [OnPoolBurst] Requested play time of " + maxTimeAvailable.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());

                            var main = curr_system_list[j].main;
                            timeRequested[j] = maxTimeAvailable;
                            if (timeRequested[j] <= 0.0f)
                            {
                                found_to_remove = true;
                            }
                            else
                            {
                                maxTime = Mathf.Max(maxTimeAvailable, maxTime);
                            }
                        }
                        // newModule.adjStartDelay[i] = cycle_time - maxTime;
                        newModule.adjCycleDelay[i] = cycle_time - maxTime;  // how much time between cycle end, and start of particles in next cycle
                        newModule.maxTimeNeeded[i] = maxTime;               // how much time is enough for every single system

                        // remove particle systems that request 0 time
                        if (found_to_remove)
                        {
                            List<float> temp_list = new List<float>();
                            List<ParticleSystem> temp_system_list = new List<ParticleSystem>();

                            int offset = 0;

                            for (int j = 0; j < curr_list.Count; j++)
                            {
                                if (timeRequested[j] > 0.0f)
                                {
                                    temp_list.Add(curr_list[j]);
                                    temp_system_list.Add(curr_system_list[j]);
                                    timeRequested[j - offset] = timeRequested[j];
                                }
                                else
                                {
                                    offset += 1;
                                }
                            }

                            newModule.defaultTimeNeeded[i] = temp_list;
                            newModule.beforeBarrelFired[i] = temp_system_list;

                            curr_list = newModule.defaultTimeNeeded[i];
                            curr_system_list = newModule.beforeBarrelFired[i];
                        }

                        // set proper startDelays for remaining PS
                        for (int j = 0; j < curr_list.Count; j++)
                        {
                            float newDelay = maxTime - timeRequested[j];

                            ParticleSystem currSystem = curr_system_list[j];
                            var main = currSystem.main;
                            main.startDelay = newDelay;
                            // main.startDelayMultiplier = 1.0f;
                            newModule.DebugPrint("<MPM> [OnPoolBurst] Assign new startDelay of " + newDelay.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());
                            newModule.defaultTimeNeeded[i][j] = newDelay;
                        }

                        float comparator = m_ShotCooldown * i;
                        if (maxTime > comparator)
                        {
                            newModule.numStartModifications += 1;
                            newModule.adjStartDelay[i] = comparator;
                        }
                    }
                }
            }
            // If we know each burst is only fraction of number of barrels, then can set times individually for each thing
            else if (m_NumCannonBarrels % m_BurstShotCount == 0)
            {
                // should be > 1, else we would have done it in prior case
                int num_sets = m_NumCannonBarrels / m_BurstShotCount;
                float cycle_time = (m_ShotCooldown * (m_BurstShotCount - 1)) + (num_sets * m_BurstCooldown);

                // set cycle_time as ceiling
                for (int i = 0; i < m_NumCannonBarrels; i++)
                {
                    List<float> curr_list = newModule.defaultTimeNeeded[i];
                    List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];
                    float maxTime = 0.0f;
                    float[] timeRequested = new float[curr_list.Count];
                    bool found_to_remove = false;
                    for (int j = 0; j < curr_list.Count; j++)
                    {
                        float maxTimeAvailable = Mathf.Min(curr_list[j], cycle_time);
                        newModule.DebugPrint("<MPM> [OnPool] Requested play time of " + maxTimeAvailable.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());

                        var main = curr_system_list[j].main;
                        timeRequested[j] = maxTimeAvailable;
                        if (timeRequested[j] <= 0.0f)
                        {
                            found_to_remove = true;
                        }
                        else
                        {
                            maxTime = Mathf.Max(maxTimeAvailable, maxTime);
                        }
                    }
                    // newModule.adjStartDelay[i] = cycle_time - maxTime;
                    newModule.adjCycleDelay[i] = cycle_time - maxTime;  // how much time between cycle end, and start of particles in next cycle
                    newModule.maxTimeNeeded[i] = maxTime;               // how much time is enough for every single system

                    // remove particle systems that request 0 time
                    if (found_to_remove)
                    {
                        List<float> temp_list = new List<float>();
                        List<ParticleSystem> temp_system_list = new List<ParticleSystem>();

                        int offset = 0;

                        for (int j = 0; j < curr_list.Count; j++)
                        {
                            if (timeRequested[j] > 0.0f)
                            {
                                temp_list.Add(curr_list[j]);
                                temp_system_list.Add(curr_system_list[j]);
                                timeRequested[j - offset] = timeRequested[j];
                            }
                            else
                            {
                                offset += 1;
                            }
                        }

                        newModule.defaultTimeNeeded[i] = temp_list;
                        newModule.beforeBarrelFired[i] = temp_system_list;

                        curr_list = newModule.defaultTimeNeeded[i];
                        curr_system_list = newModule.beforeBarrelFired[i];
                    }

                    // set proper startDelays for remaining PS
                    for (int j = 0; j < curr_list.Count; j++)
                    {
                        float newDelay = maxTime - timeRequested[j];

                        ParticleSystem currSystem = curr_system_list[j];
                        var main = currSystem.main;
                        main.startDelay = newDelay;
                        // main.startDelayMultiplier = 1.0f;
                        newModule.DebugPrint("<MPM> [OnPool] Assign new startDelay of " + newDelay.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());
                        newModule.defaultTimeNeeded[i][j] = newDelay;
                    }

                    int num_bursts = i / m_BurstShotCount;
                    float comparator = num_bursts * (m_BurstCooldown) + m_ShotCooldown * (i - num_bursts);
                    if (maxTime > comparator)
                    {
                        newModule.numStartModifications += 1;
                        newModule.adjStartDelay[i] = comparator;
                    }
                }
            }
            // else, must set shot timer every single time you've got a change to make
            // currently unsupported
            else
            {
                // newModule.dynamicTimeCalc = true;
                newModule.beforeBarrelFired = null;
                newModule.defaultTimeNeeded = null;

                newModule.DebugPrint("<NPM> Dynamic Time Calc Detected [Burst] ShotCount unrelated to Barrel Count");
                return;
            }
        }

        private static void OnPoolSequential(float m_ShotCooldown, float m_BurstCooldown, int m_BurstShotCount, int m_NumCannonBarrels, ModuleParticleManager newModule)
        {
            float cycle_time = m_ShotCooldown;
            if (m_BurstShotCount == 1)
            {
                cycle_time = m_BurstCooldown;
            }
            cycle_time *= m_NumCannonBarrels;

            newModule.DebugPrint("<NPM> [OnPoolSequential] cycle_time calculated at: " + cycle_time.ToString());

            // set cycle_time as ceiling
            for (int i = 0; i < m_NumCannonBarrels; i++)
            {
                List<float> curr_list = newModule.defaultTimeNeeded[i];
                List<ParticleSystem> curr_system_list = newModule.beforeBarrelFired[i];
                float maxTime = 0.0f;
                float[] timeRequested = new float[curr_list.Count];
                bool found_to_remove = false;
                for (int j = 0; j < curr_list.Count; j++)
                {
                    float maxTimeAvailable = Mathf.Min(curr_list[j], cycle_time);
                    newModule.DebugPrint("<MPM> [OnPoolSequential] Requested play time of " + maxTimeAvailable.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());

                    var main = curr_system_list[j].main;
                    timeRequested[j] = maxTimeAvailable;
                    if (timeRequested[j] <= 0.0f)
                    {
                        found_to_remove = true;
                    }
                    else
                    {
                        maxTime = Mathf.Max(maxTimeAvailable, maxTime);
                    }
                }
                // newModule.adjStartDelay[i] = cycle_time - maxTime;
                newModule.adjCycleDelay[i] = cycle_time - maxTime;  // how much time between cycle end, and start of particles in next cycle
                newModule.maxTimeNeeded[i] = maxTime;               // how much time is enough for every single system

                // remove particle systems that request 0 time
                if (found_to_remove)
                {
                    List<float> temp_list = new List<float>();
                    List<ParticleSystem> temp_system_list = new List<ParticleSystem>();

                    int offset = 0;

                    for (int j = 0; j < curr_list.Count; j++)
                    {
                        if (timeRequested[j] > 0.0f)
                        {
                            temp_list.Add(curr_list[j]);
                            temp_system_list.Add(curr_system_list[j]);
                            timeRequested[j - offset] = timeRequested[j];
                        }
                        else
                        {
                            offset += 1;
                        }
                    }

                    newModule.defaultTimeNeeded[i] = temp_list;
                    newModule.beforeBarrelFired[i] = temp_system_list;

                    curr_list = newModule.defaultTimeNeeded[i];
                    curr_system_list = newModule.beforeBarrelFired[i];
                }

                // set proper startDelays for remaining PS
                for (int j = 0; j < curr_list.Count; j++)
                {
                    float newDelay = maxTime - timeRequested[j];

                    ParticleSystem currSystem = curr_system_list[j];
                    var main = currSystem.main;
                    main.startDelay = newDelay;
                    newModule.DebugPrint("<MPM> [OnPoolSequential] Assign new startDelay of " + newDelay.ToString() + " s to PS #" + j.ToString() + " of Barrel #" + i.ToString());
                    newModule.defaultTimeNeeded[i][j] = newDelay;
                    // main.startDelayMultiplier = 1.0f;
                }

                float comparator = m_ShotCooldown * i;
                if (m_BurstShotCount == 1)
                {
                    comparator = m_BurstCooldown * i;
                }
                if (maxTime > comparator)
                {
                    newModule.numStartModifications += 1;
                    newModule.adjStartDelay[i] = comparator;
                }
            }
        }
    }

}
