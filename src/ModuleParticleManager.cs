﻿using UnityEngine;
using System;
using System.Collections.Generic;

namespace ParticleManager
{

    [Serializable()]
    public class ModuleParticleManager: Module
    {
        // use with TankBlock OnAttach(Tank t)
        // plays when attached, stops on:
        // OnDetach(Tank t, bool initRigidbody, bool resumeTileManagement)
        public List<ParticleSystem> onBlockAttach;

        // use with OnAnchorStatusChanged(ModuleAnchor anchor)
        public List<ParticleSystem> onAnchor;

        // only available for weapon blocks
        // for weapon firing
        public List<ParticleSystem> onWeaponFiring;

        // For standard one-off weapon charging
        public List<ParticleSystem> onWeaponCharge;
        public float maxWeaponChargeTime = 0.0f;

        // Pair of arrs determine how far ahead of weapon firing it will play effects.
        // sets m_ShotTimer to designated time when first pressing space
        // WARNING: will override particle system start delay to be the same time
        public List<List<ParticleSystem>> beforeBarrelFired;
        public List<List<float>> defaultStartDelay;
        public float[] maxTimeNeeded;
        public float[] adjStartDelay;
        public List<ParticleSystem> initBeforeBarrelFired;
        public List<float> initTimeBeforeBarrelFired;
        public List<CannonBarrel> initCannonBarrelList;

        public ModuleWeaponGun m_ModuleWeaponGun;
        public CannonBarrel[] m_CannonBarrels;
        public bool[] m_BarrelFired;

        // auto-set options to control fire flow
        public int m_BurstShotCount = 0;
        public int numStartModifications = 0;
        public int needToCorrectDelay = 0;
        public bool AllAtOnce = false;
        // public bool m_ResetBurstOnInterrupt;
        public bool lastFireOrder = false;
        public bool notFiredFirst = true;

        // boolean to control printing
        public bool Debug = false;

        // gotta initialize it here so that block injector can do its thing
        public ParticleSystemMetadata[] _Input = new ParticleSystemMetadata[5];
        public ParticleSystemMetadata[] Input {
            get
            {
                return _Input;
            }
            set
            {
                this.DebugPrint("WOLOLOLOLO");
                this.DebugPrint("Detected length " + value.Length.ToString() + " array");

                _Input = new ParticleSystemMetadata[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    this.DebugPrint("set a term in initParams");
                    _Input[i] = value[i];
                }


                this.DebugPrint("New _Input length: " + _Input.Length.ToString());
                this.DebugPrint("New Input length: " + this.Input.Length.ToString());

                this.initialize_values();
            }
        }

        // PrePool step shoved upstream to happen before duplication
        private void initialize_values()
        {
            this.DebugPrint("<ModuleParticleManager> Initialization");

            this.onBlockAttach = new List<ParticleSystem>();
            this.onAnchor = new List<ParticleSystem>();

            this.onWeaponCharge = new List<ParticleSystem>();
            this.onWeaponFiring = new List<ParticleSystem>();

            this.initBeforeBarrelFired = new List<ParticleSystem>();
            this.initTimeBeforeBarrelFired = new List<float>();
            this.initCannonBarrelList = new List<CannonBarrel>();

            this.DebugPrint("<ModuleParticleManager> Initialization initialization finished");
            this.DebugPrint("<ModuleParticleManager> Input Size: " + this.Input.Length.ToString());
            for (int i = 0; i < this.Input.Length; i++)
            {
                ParticleSystemMetadata metadata = this.Input[i];
                this.DebugPrint("<ModuleParticleManager> HANDLED INPUT");
                MetadataType type = metadata.type;
                ParticleSystem system = metadata.m_system;
                if (system == null)
                {
                    this.DebugPrint("<ModuleParticleManager> NULL INPUT SYSTEM?");
                }
                else
                {
                    // immediately stop the system
                    system.Stop();
                    float r_value = metadata.value;
                    if (type == MetadataType.Attach)
                    {
                        this.onBlockAttach.Add(system);
                    }
                    else if (type == MetadataType.Anchor)
                    {
                        this.onAnchor.Add(system);
                    }
                    else if (type == MetadataType.WeaponCharge)
                    {
                        this.onWeaponCharge.Add(system);
                        this.maxWeaponChargeTime = r_value;
                    }
                    else if (type == MetadataType.WeaponFiring)
                    {
                        this.onWeaponFiring.Add(system);
                    }
                    else if (type == MetadataType.BarrelCharge)
                    {
                        var main = system.main;
                        main.startDelay = new ParticleSystem.MinMaxCurve(r_value);
                        main.startDelayMultiplier = 1.0f;

                        // this.beforeBarrelFired[0].Add(system);
                        // this.defaultStartDelay[0].Add(r_value);

                        this.initBeforeBarrelFired.Add(system);
                        this.initTimeBeforeBarrelFired.Add(r_value);
                        this.initCannonBarrelList.Add(metadata.CannonBarrel);
                    }
                    else
                    {
                        this.DebugPrint("<ModuleParticleManager> uh wut happened");
                    }
                }
            }
            this.DebugPrint("<ModuleParticleManager> Initialization Complete");
        }

        // m_NextBarrelToFire is which barrel will be fired immediately after, if this does not fail
        // return appropriate float of what should set m_ShotTimer to
        // return 0.0 if everything is kosher

        // should handle spinup times (looks like not possible rip)
        public float PrepareFiring(bool fireOrder, bool result, int m_NextBarrelToFire) {
            float retval = 0.0f;
            if (this.lastFireOrder != fireOrder) {
                this.DebugPrint("<ModuleParticleManager> PrepareFiring registered command state change");
                // toggled on, weapon now wants to fire
                if (fireOrder)
                {
                    // play generic weapon firing particles
                    this.playSelectedParticles(this.onWeaponFiring, "  ", "<ModuleParticleManager> Attempting to play overall weapon firing particles");

                    //Here, we only care about weapon charge stuff. Individual barrels are handled by ProcessFiring
                    if (this.onWeaponCharge != null) {
                        // startDelays have been set to account for FireSpinners in OnPool
                        // here, just need to play particles, set delay if > 0.0
                        this.playSelectedParticles(this.onWeaponCharge, "  ", "<ModuleParticleManager> Attempting to play overall weapon charge particles");

                        retval = this.maxWeaponChargeTime;
                    }
                }
                // toggled off, weapon now wants to stop firing, stop playing particles
                else
                {
                    // play generic weapon firing particles
                    this.stopSelectedParticles(this.onWeaponFiring, "  ", "<ModuleParticleManager> Attempting to stop overall weapon firing particles");

                    // stop weapon charge particles (if they haven't been stopped already)
                    this.stopSelectedParticles(this.onWeaponCharge, "  ", "<ModuleParticleManager> Attempting to stop overall weapon charge particles (just in case)");
                }

                // regardless of change, to change means we reset these params
                this.notFiredFirst = true;
                this.needToCorrectDelay = 0;
            }
            this.lastFireOrder = fireOrder;
            return retval;
        }

        // is Prefixed, so m_NextBarrelToFire is the correct barrel (that will be fired)
        public float ProcessFiring(bool toFire, int m_NextBarrelToFire) {
            
            float retval = 0.0f;
            if (toFire)
            {
                this.DebugPrint("<ModuleParticleManager> ProcessFiring");
                // first shot, forcibly set to proper cooldown
                // our being here ==> weapon charging/spinup cooldown has elapsed, can now stop weapon charging
                if (this.notFiredFirst)
                {
                    // disable weapon charging animations
                    this.stopSelectedParticles(this.onWeaponCharge, "  ", "<ModuleParticleManager> stopping on charge particles now");

                    retval = this.ProcessFirstShot(m_NextBarrelToFire);

                    this.notFiredFirst = false;
                }
                // not first shot, don't want to touch cooldowns
                // just stop playing the charging animations of the to-be-fired barrels, start charging animations of the next one
                else if (this.beforeBarrelFired != null)
                {
                    // If need to correct stuff, do everything here
                    if (this.needToCorrectDelay > 0)
                    {
                        this.ProcessWeaponReset(m_NextBarrelToFire);
                        this.needToCorrectDelay -= 1;
                    }
                    // no need to correct stuff, just stop, and play as appropriate
                    else
                    {
                        this.ProcessBarrelFire(m_NextBarrelToFire);
                    }
                }
            }

            return retval;
        }

        // handle first shot stuffs
        private float ProcessFirstShot(int m_NextBarrelToFire)
        {
            this.DebugPrint("  <ModuleParticleManager> Detected first shot fired, setting appropriate delay for charging purposes");

            if (this.beforeBarrelFired != null)
            {
                // if AllAtOnce, need to play for all
                if (this.AllAtOnce)
                {
                    this.DebugPrint("  <ModuleParticleManager> AllAtOnce detected, starting all barrels' charge animations");
                    for (int i = 0; i < this.beforeBarrelFired.Count; i++)
                    {
                        List<ParticleSystem> systemList = this.beforeBarrelFired[i];
                        if (systemList != null)
                        {
                            for (int j = 0; j < systemList.Count; j++)
                            {
                                if (this.adjStartDelay[i] != 0.0f)
                                {
                                    var main = systemList[j].main;
                                    main.startDelay = new ParticleSystem.MinMaxCurve(this.defaultStartDelay[i][j] - this.adjStartDelay[i]);
                                }
                            }
                        }
                        this.playSelectedParticles(systemList, "    ", "<ModuleParticleManager> Attempting to play Barrel #" + i.ToString() + " particles");
                    }
                    this.needToCorrectDelay = 1;
                }
                // else, need only play proper one
                else
                {
                    this.DebugPrint("  <ModuleParticleManager> Attempt to play particle effect of first shot, need to set total of " + this.numStartModifications.ToString() + "shots");
                    int curr_barrel = m_NextBarrelToFire;
                    int m_NumCannonBarrels = this.maxTimeNeeded.Length;
                    for (int i = 0; i < this.numStartModifications; i++)
                    {
                        List<ParticleSystem> systemList = this.beforeBarrelFired[curr_barrel];
                        if (systemList != null)
                        {
                            for (int j = 0; j < systemList.Count; j++)
                            {
                                if (this.adjStartDelay[curr_barrel] != 0.0f)
                                {
                                    var main = systemList[j].main;
                                    main.startDelay = new ParticleSystem.MinMaxCurve(this.defaultStartDelay[curr_barrel][j] - this.adjStartDelay[curr_barrel]);
                                }
                            }
                        }
                        this.playSelectedParticles(this.beforeBarrelFired[curr_barrel], "    ", "<ModuleParticleManager> Attempting to play Barrel #" + curr_barrel.ToString() + " particles");
                        curr_barrel = curr_barrel == m_NumCannonBarrels - 1 ? 0 : m_NextBarrelToFire + 1;
                    }

                    // either fired all barrels, which ignores this counter (used as bool), or fired only one barrel
                    // if one barrel was part of burst, we now must handle it
                    this.needToCorrectDelay = this.numStartModifications;
                }
            }
            return this.maxTimeNeeded[m_NextBarrelToFire];
        }

        // handle resetting to factory condition. guarantee needToCorrectDelay has correct number of things
        private void ProcessWeaponReset(int m_NextBarrelToFire)
        {
            this.DebugPrint("  <ModuleParticleManager> Detected shot fired, non-first, reset needed");
            if (this.AllAtOnce)
            {
                this.DebugPrint("    <ModuleParticleManager> AllAtOnce found, need to fix all barrels");
                for (int i = 0; i < this.beforeBarrelFired.Count; i++)
                {
                    List<ParticleSystem> systemList = this.beforeBarrelFired[i];
                    if (systemList != null)
                    {
                        this.stopSelectedParticles(systemList, "    ", "<ModuleParticleManager> Attempting to stop Barrel #" + i.ToString() + " particles");
                        for (int j = 0; j < systemList.Count; j++)
                        {
                            if (this.adjStartDelay[i] != 0.0f)
                            {
                                var main = systemList[j].main;
                                main.startDelay = new ParticleSystem.MinMaxCurve(this.defaultStartDelay[i][j]);
                            }
                        }
                    }
                }
            }
            // switching bool to int means we get better control of when we are finished counting, no need to case, second half works as intended
            // else if (this.m_BurstShotCount > 1) { }
            else
            {
                this.DebugPrint("    <ModuleParticleManager> No AllAtOnce, need to reset target barrels: " + m_NextBarrelToFire.ToString());
                List<ParticleSystem> systemList = this.beforeBarrelFired[m_NextBarrelToFire];
                if (systemList != null)
                {
                    // Stop particles from playing on this PS first
                    this.stopSelectedParticles(systemList, "    ", "<ModuleParticleManager> Attempting to stop Barrel #" + m_NextBarrelToFire.ToString() + " particles");

                    // fix it
                    for (int j = 0; j < systemList.Count; j++)
                    {
                        if (this.adjStartDelay[m_NextBarrelToFire] != 0.0f)
                        {
                            var main = systemList[j].main;
                            main.startDelay = new ParticleSystem.MinMaxCurve(this.defaultStartDelay[m_NextBarrelToFire][j]);
                        }
                    }
                }
            }
        }

        // handle last case
        private void ProcessBarrelFire(int m_NextBarrelToFire)
        {
            this.DebugPrint("  <ModuleParticleManager> Detected shot fired, non-first, no reset");
            if (this.AllAtOnce)
            {
                this.DebugPrint("  <ModuleParticleManager> AllAtOnce need to cycle all particles");
                for (int i = 0; i < this.beforeBarrelFired.Count; i++)
                {
                    this.DebugPrint("    <ModuleParticleManager> Attempting to cycle Barrel #" + i.ToString() + " particles");
                    List<ParticleSystem> systemList = this.beforeBarrelFired[i];
                    this.stopSelectedParticles(systemList, "      ", "<ModuleParticleManager> Attempting to stop Barrel #" + i.ToString() + " particles");
                    this.playSelectedParticles(systemList, "      ", "<ModuleParticleManager> Attempting to play Barrel #" + i.ToString() + " particles");
                }
            }
            // don't care about burst count after it's been corrected
            else
            {
                this.DebugPrint("  <ModuleParticleManager> Single Barrel" + m_NextBarrelToFire.ToString() + " need to cycle particles");

                this.DebugPrint("    <ModuleParticleManager> Attempting to cycle Barrel #" + m_NextBarrelToFire.ToString() + " particles");
                this.stopSelectedParticles(this.beforeBarrelFired[m_NextBarrelToFire], "      ", "<ModuleParticleManager> Attempting to stop Barrel #" + m_NextBarrelToFire.ToString() + " particles");

                int m_NumCannonBarrels = this.maxTimeNeeded.Length;
                int nextBarrelToPlay = m_NextBarrelToFire == m_NumCannonBarrels - 1 ? 0 : m_NextBarrelToFire + 1;
                this.playSelectedParticles(this.beforeBarrelFired[nextBarrelToPlay], "      ", "<ModuleParticleManager> Attempting to play Barrel #" + nextBarrelToPlay.ToString() + " particles");
            }
        }

        // called from ModuleWeaponGun OnPool
        // Input: proper order of things
        // Anything not explicitly assigned a CannonBarrel defaults to the first CannonBarrel slot
        public bool AlignCannonBarrels(CannonBarrel[] m_CannonBarrels)
        {
            this.DebugPrint("<ModuleParticleManager> AlignCannonBarrels");

            if (this.initBeforeBarrelFired != null && this.initBeforeBarrelFired.Count > 0) {
                this.beforeBarrelFired = new List<List<ParticleSystem>>();
                this.defaultStartDelay = new List<List<float>>();
                this.beforeBarrelFired.Add(new List<ParticleSystem>());
                this.defaultStartDelay.Add(new List<float>());

                // initialize the structures
                for (int i = 1; i < m_CannonBarrels.Length; i++)
                {
                    this.beforeBarrelFired.Add(new List<ParticleSystem>());
                    this.defaultStartDelay.Add(new List<float>());
                }

                // we know they've been shoved in order.
                List<ParticleSystem> init_system_dump = this.initBeforeBarrelFired;
                List<float> init_float_dump = this.initTimeBeforeBarrelFired;
                // this.beforeBarrelFired[0] = new List<ParticleSystem>();
                // this.defaultStartDelay[0] = new List<float>();
                this.maxTimeNeeded = new float[m_CannonBarrels.Length];
                this.adjStartDelay = new float[m_CannonBarrels.Length];

                this.DebugPrint("Stuff was initialized (CannonBarrelAlign)");

                // Identify proper barrel index, shove it in there. Shove into barrel 0 if no CannonBarrel provided
                for (int i = 0; i < init_system_dump.Count; i++)
                {
                    this.DebugPrint("Begin init entry " + i.ToString());
                    CannonBarrel target_barrel = this.initCannonBarrelList[i];
                    bool match = false;
                    for (int j = 0; j < m_CannonBarrels.Length; j++)
                    {
                        if (target_barrel == m_CannonBarrels[j])
                        {
                            this.DebugPrint("  entry [" + i.ToString() + "] has matched to barrel <" + j.ToString() + ">");
                            this.beforeBarrelFired[j].Add(init_system_dump[i]);
                            this.DebugPrint("  dump1");
                            this.defaultStartDelay[j].Add(init_float_dump[i]);
                            this.DebugPrint("  dump2");
                            this.maxTimeNeeded[j] = Mathf.Max(this.maxTimeNeeded[j], init_float_dump[i]);
                            this.DebugPrint("  dump3");
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                    {
                        this.beforeBarrelFired[0].Add(init_system_dump[i]);
                        this.DebugPrint("  dump1");
                        this.defaultStartDelay[0].Add(init_float_dump[i]);
                        this.DebugPrint("  dump2");
                        this.maxTimeNeeded[0] = Mathf.Max(this.maxTimeNeeded[0], init_float_dump[i]);
                        this.DebugPrint("  dump3");
                    }
                }
                return true;
            }

            // no longer need, free up for GC
            this.initCannonBarrelList = null;
            return false;
        }

        private void OnSpawn() {
            this.DebugPrint("<ModuleParticleManager> OnSpawn");
            this.StopAllParticles();
        }

        private void OnPool()
        {
            this.DebugPrint("<ModuleParticleManager> OnPool");
            this.GetComponentsInChildren<CannonBarrel>(true);

            // hook up the functions to the events (OnAttach, OnDetach, OnAnchorStatusChanged)
            this.block.AttachEvent.Subscribe(new System.Action(this.OnAttach));
            this.block.DetachEvent.Subscribe(new System.Action(this.OnDetach));
            this.DebugPrint("<ModuleParticleManager> Pool subscribe block attachments");

            this.m_ModuleWeaponGun = this.GetComponent<ModuleWeaponGun>();
            if (this.m_ModuleWeaponGun == null)
            {
                this.DebugPrint("<ModuleParticleManager> DID NOT FIND MODULEWEAPONGUN");
                // this.onSpacePress = null;
                // this.onSpaceRelease = null;
                this.beforeBarrelFired = null;
                this.defaultStartDelay = null;
            }

            this.DebugPrint("<ModuleParticleManager> OnPool Complete");
        }

        private void OnAttach()
        {
            this.DebugPrint("<ModuleParticleManager> OnAttach");
            this.playSelectedParticles(this.onBlockAttach, "  ", "<ModuleParticleManager> OnAttach Particles set to Play");

            if (this.block == null)
            {
                this.DebugPrint("<ModuleParticleManager> OnAttach this.block == NULL ?????");
            }
            // This part is probably already covered by game engine OnAnchorStatusChanged
            if (this.block.tank != null)
            {
                if (this.block.tank.IsBase)
                {
                    this.playSelectedParticles(this.onAnchor, "  ", "<ModuleParticleManager> OnAnchor Triggered (from OnAttach init)");
                }
                this.block.tank.AnchorEvent.Subscribe(new System.Action<ModuleAnchor, bool, bool>(this.OnAnchorStatusChanged));
            }
            return;
        }

        private void OnDetach()
        {
            this.DebugPrint("<ModuleParticleManager> OnDetach");
            if (this.block == null)
            {
                this.DebugPrint("<ModuleParticleManager> OnDetach this.block == NULL ?????");
            }
            if (this.block.tank != null)
            {
                this.block.tank.AnchorEvent.Unsubscribe(new System.Action<ModuleAnchor, bool, bool>(this.OnAnchorStatusChanged));
            }
            this.StopAllParticles();

            // Reset all weapon stuffs to factory condition
            this.notFiredFirst = true;
            this.needToCorrectDelay = 0;
            this.lastFireOrder = false;
            // Reset weapon particle systems to default start delay
            return;
        }

        private void OnAnchorStatusChanged(ModuleAnchor anchor, bool anchored, bool fromAfterTechPopulate)
        {
            this.DebugPrint("<ModuleParticleManager> OnAnchorStatusChanged");
            if (anchored)
            {
                this.playSelectedParticles(this.onAnchor, "  ", "<ModuleParticleManager> Playing anchored particles");
            }
            else
            {
                this.stopSelectedParticles(this.onAnchor, "  ", "<ModuleParticleManager> Stopping anchored particles");
            }
            return;
        }

        private void StopAllParticles()
        {
            this.DebugPrint("  <ModuleParticleManager> Request Stopping all Particles");
            this.DebugPrint("    <ModuleParticleManager> Stopping OnblockAttach");
            foreach (ParticleSystem system in this.onBlockAttach)
            {
                this.DebugPrint("      <ModuleParticleManager> Found a PS");
                system.Stop();
            }
            this.DebugPrint("    <ModuleParticleManager> Stopping Onanchor");
            foreach (ParticleSystem system in this.onAnchor)
            {
                this.DebugPrint("      <ModuleParticleManager> Found a PS");
                system.Stop();
            }
            this.DebugPrint("    <ModuleParticleManager> Stopping OnWeaponCharge");
            if (this.onWeaponCharge != null)
            {
                foreach (ParticleSystem system in this.onWeaponCharge)
                {
                    this.DebugPrint("      <ModuleParticleManager> Found a PS");
                    system.Stop();
                }
            }
            this.DebugPrint("    <ModuleParticleManager> Stopping OnWeaponFiring");
            if (this.onWeaponFiring != null)
            {
                foreach (ParticleSystem system in this.onWeaponFiring)
                {
                    this.DebugPrint("      <ModuleParticleManager> Found a PS");
                    system.Stop();
                }
            }
            this.DebugPrint("    <ModuleParticleManager> Stopping beforeBarrelFired");
            if (this.beforeBarrelFired != null)
            {
                foreach (List<ParticleSystem> system_list in this.beforeBarrelFired)
                {
                    foreach (ParticleSystem system in system_list)
                    {
                        this.DebugPrint("      <ModuleParticleManager> Found a PS");
                        system.Stop();
                    }
                }
            }
            return;
        }

        private void playSelectedParticles(List<ParticleSystem> systemList, string prefix, string toPrint)
        {
            if (systemList != null)
            {
                this.DebugPrint(prefix + toPrint);
                foreach (ParticleSystem system in systemList)
                {
                    this.DebugPrint(prefix + "  <ModuleParticleManager> Found a PS");
                    system.Clear();
                    system.Play();
                }
            }
            else
            {
                this.DebugPrint(prefix + "<ModuleParticleManager> NULL PS List");
            }
            return;
        }

        private void stopSelectedParticles(List<ParticleSystem> systemList, string prefix, string toPrint)
        {
            if (systemList != null)
            {
                this.DebugPrint(prefix + toPrint);
                foreach (ParticleSystem system in systemList)
                {
                    this.DebugPrint(prefix + "  <ModuleParticleManager> Found a PS");
                    system.Stop();
                }
            }
            else
            {
                this.DebugPrint(prefix + "<ModuleParticleManager> NULL PS List");
            }
            return;
        }

        private void OnRecycle()
        {
            this.StopAllParticles();
            return;
        }

        private void DebugPrint(string text)
        {
            if (this.Debug)
            {
                Console.WriteLine(text);
            }
        }
    }
}
