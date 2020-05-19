using UnityEngine;
using System;
using System.Collections;
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
        public List<List<float>> defaultTimeNeeded;     // encodes time total for particle effect on input to OnPool. On output, encodes the default start Delays
        public float[] maxTimeNeeded;
        public float[] adjStartDelay;
        public float[] adjCycleDelay;
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

        private bool fireParticlesActive = false;

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
            this.DebugPrint("<MPM> Initialization");

            this.onBlockAttach = new List<ParticleSystem>();
            this.onAnchor = new List<ParticleSystem>();

            this.onWeaponCharge = new List<ParticleSystem>();
            this.onWeaponFiring = new List<ParticleSystem>();

            this.initBeforeBarrelFired = new List<ParticleSystem>();
            this.initTimeBeforeBarrelFired = new List<float>();
            this.initCannonBarrelList = new List<CannonBarrel>();

            this.DebugPrint("<MPM> Initialization initialization finished");
            this.DebugPrint("<MPM> Input Size: " + this.Input.Length.ToString());
            for (int i = 0; i < this.Input.Length; i++)
            {
                ParticleSystemMetadata metadata = this.Input[i];
                this.DebugPrint("<MPM> HANDLED INPUT");
                MetadataType type = metadata.type;
                ParticleSystem system = metadata.m_system;
                if (system == null)
                {
                    this.DebugPrint("<MPM> NULL INPUT SYSTEM?");
                }
                else
                {
                    // immediately stop the system
                    system.Stop(true);
                    var main = system.main;
                    main.stopAction = ParticleSystemStopAction.None;
                    main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

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
                        // var main = system.main;
                        // main.startDelay = r_value;
                        // main.startDelayMultiplier = 1.0f;

                        // this.beforeBarrelFired[0].Add(system);
                        // this.defaultTimeNeeded[0].Add(r_value);

                        this.initBeforeBarrelFired.Add(system);
                        this.initTimeBeforeBarrelFired.Add(r_value);
                        this.initCannonBarrelList.Add(metadata.CannonBarrel);
                    }
                    else
                    {
                        this.DebugPrint("<MPM> uh wut happened");
                    }
                }
            }
            this.DebugPrint("<MPM> Initialization Complete");
        }

        // m_NextBarrelToFire is which barrel will be fired immediately after, if this does not fail
        // return appropriate float of what should set m_ShotTimer to
        // return 0.0 if everything is kosher

        // should handle spinup times (looks like not possible rip)
        public float PrepareFiring(bool fireOrder, bool result, int m_NextBarrelToFire) {
            float retval = -1.0f;
            if (this.lastFireOrder != fireOrder) {
                this.DebugPrint("<MPM> PrepareFiring registered command state change");
                // toggled on, weapon now wants to fire
                if (fireOrder)
                {
                    this.fireParticlesActive = true;

                    // play generic weapon firing particles
                    this.playSelectedParticles(this.onWeaponFiring, "  ", "<MPM> Attempting to play overall weapon firing particles");

                    //Here, we only care about weapon charge stuff. Individual barrels are handled by ProcessFiring
                    if (this.onWeaponCharge != null) {
                        // startDelays have been set to account for FireSpinners in OnPool
                        // here, just need to play particles, set delay if > 0.0
                        this.playSelectedParticles(this.onWeaponCharge, "  ", "<MPM> Attempting to play overall weapon charge particles");

                        retval = this.maxWeaponChargeTime;
                    }
                    else
                    {
                        retval = 0.0f;
                    }
                }
                // toggled off, weapon now wants to stop firing, stop playing particles
                else
                {
                    this.fireParticlesActive = false;

                    // play generic weapon firing particles
                    this.stopSelectedParticles(this.onWeaponFiring, "  ", "<MPM> Attempting to stop overall weapon firing particles");

                    // stop weapon charge particles (if they haven't been stopped already)
                    this.stopSelectedParticles(this.onWeaponCharge, "  ", "<MPM> Attempting to stop overall weapon charge particles (just in case)");

                    // stop all Barrel charge particles
                    if (this.beforeBarrelFired != null)
                    {
                        for (int i = 0; i < this.beforeBarrelFired.Count; i++)
                        {
                            this.stopSelectedParticles(this.beforeBarrelFired[i],"  ", "<MPM> Attempting to stop barrel #" + i.ToString() + " particles");
                        }
                    }

                    // reset cooldown
                    retval = 0.0f;
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
                this.DebugPrint("<MPM> ProcessFiring");
                // first shot, forcibly set to proper cooldown
                // our being here ==> weapon charging/spinup cooldown has elapsed, can now stop weapon charging
                if (this.notFiredFirst)
                {
                    // disable weapon charging animations
                    this.stopSelectedParticles(this.onWeaponCharge, "  ", "<MPM> stopping on charge particles now");

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

                    // no longer modifying weapon startDelays means 
                    // this.ProcessBarrelFire(m_NextBarrelToFire);
                }
            }

            return retval;
        }

        // handle first shot stuffs
        private float ProcessFirstShot(int m_NextBarrelToFire)
        {
            this.DebugPrint("  <MPM> Detected first shot fired, setting appropriate delay for charging purposes");

            if (this.beforeBarrelFired != null)
            {
                // if AllAtOnce, need to play for all
                if (this.AllAtOnce)
                {
                    this.DebugPrint("  <MPM> AllAtOnce detected, starting all barrels' charge animations");
                    for (int i = 0; i < this.beforeBarrelFired.Count; i++)
                    {
                        List<ParticleSystem> systemList = this.beforeBarrelFired[i];

                        // no longer do this, since all startDelays are as if do right away, delay time is replaced with Coroutine execution delay
                        if (systemList != null)
                        {
                            for (int j = 0; j < systemList.Count; j++)
                            {
                                var main = systemList[j].main;
                                main.startDelay = this.defaultTimeNeeded[i][j] + this.adjStartDelay[i];
                            }
                        }
                        this.playSelectedParticles(systemList, "    ", "<MPM> Attempting to play Barrel #" + i.ToString() + " particles");
                    }
                    this.needToCorrectDelay = 1;
                }
                // else, need only play proper one(s)
                else
                {
                    this.DebugPrint("  <MPM> Attempt to play particle effect of first shot, need to set total of " + this.numStartModifications.ToString() + "shots");
                    int curr_barrel = m_NextBarrelToFire;
                    int m_NumCannonBarrels = this.maxTimeNeeded.Length;
                    int offset = 0;
                    for (int i = 0; i < this.numStartModifications; i++)
                    {
                        // no longer do this, since all startDelays are as if do right away, delay time is replaced with Coroutine execution delay
                        /* if (systemList != null)
                        List<ParticleSystem> systemList = this.beforeBarrelFired[curr_barrel];
                        if (systemList != null)
                        {
                            for (int j = 0; j < systemList.Count; j++)
                            {
                                if (this.adjStartDelay[curr_barrel] != 0.0f)
                                {
                                    var main = systemList[j].main;
                                    main.startDelay = this.defaultTimeNeeded[curr_barrel][j] - this.adjStartDelay[curr_barrel];

                                    var main2 = systemList[j].main;
                                    this.DebugPrint("    <MPM> Detected startDelay: " + main2.startDelay.constant.ToString());
                                }
                            }
                        } */

                        List<ParticleSystem> systemList = this.beforeBarrelFired[curr_barrel];
                        if (systemList != null)
                        {
                            int tarInd = i + offset;
                            int loop = tarInd / m_NumCannonBarrels;
                            tarInd = tarInd % m_NumCannonBarrels;
                            for (int j = 0; j < systemList.Count; j++)
                            {
                                var main = systemList[j].main;
                                main.startDelay = this.defaultTimeNeeded[curr_barrel][j] + this.adjStartDelay[tarInd];

                                var main2 = systemList[j].main;
                                this.DebugPrint("    <MPM> Detected startDelay: " + main2.startDelay.constant.ToString());
                            }
                        }
                        // StartCoroutine(this.playSelectedParticlesWithDelay(this.beforeBarrelFired[curr_barrel], "    ", "<MPM> Attempting to play Barrel #" + curr_barrel.ToString() + " particles", this.adjStartDelay[curr_barrel]));

                        this.playSelectedParticles(systemList, "    ", "<MPM> Attempting to play Barrel #" + curr_barrel.ToString() + " particles");
                        curr_barrel = curr_barrel == m_NumCannonBarrels - 1 ? 0 : curr_barrel + 1;
                    }

                    // either fired all barrels, which ignores this counter (used as bool), or fired only one barrel
                    // if one barrel was part of burst, we now must handle it
                    this.needToCorrectDelay = this.numStartModifications;
                }
                return this.maxTimeNeeded[m_NextBarrelToFire];
            }
            return 0.0f;
        }

        // handle resetting to factory condition. guarantee needToCorrectDelay has correct number of things
        private void ProcessWeaponReset(int m_NextBarrelToFire)
        {
            this.DebugPrint("  <MPM> Detected shot fired, non-first, reset needed");
            if (this.AllAtOnce)
            {
                this.DebugPrint("    <MPM> AllAtOnce found, need to fix all barrels");
                for (int i = 0; i < this.beforeBarrelFired.Count; i++)
                {
                    List<ParticleSystem> systemList = this.beforeBarrelFired[i];
                    if (systemList != null)
                    {
                        this.stopSelectedParticles(systemList, "    ", "<MPM> Attempting to stop Barrel #" + i.ToString() + " particles");
                        for (int j = 0; j < systemList.Count; j++)
                        {
                            var main = systemList[j].main;
                            main.startDelay = this.defaultTimeNeeded[i][j] + this.adjCycleDelay[i];
                        }
                        this.playSelectedParticles(systemList, "    ", "<MPM> Attempting to cycle Barrel #" + i.ToString() + " particles");
                    }
                }
            }
            // switching bool to int means we get better control of when we are finished counting, no need to case, second half works as intended
            // else if (this.m_BurstShotCount > 1) { }
            else
            {
                this.DebugPrint("    <MPM> No AllAtOnce, need to reset target barrels: " + m_NextBarrelToFire.ToString());
                List<ParticleSystem> systemList = this.beforeBarrelFired[m_NextBarrelToFire];
                if (systemList != null)
                {
                    /*if (this.maxTimeNeeded.Length == 1)
                    {
                        this.stopAndClearSelectedParticles(systemList, "    ", "<MPM> Attempting to stop and clear Barrel #" + m_NextBarrelToFire.ToString() + " particles");
                    }
                    else
                    { */
                        // Stop particles from playing on this PS first
                    this.stopSelectedParticles(systemList, "    ", "<MPM> Attempting to stop Barrel #" + m_NextBarrelToFire.ToString() + " particles");
                    // }

                    // fix it
                    for (int j = 0; j < systemList.Count; j++)
                    {
                        var main = systemList[j].main;
                        main.startDelay = this.defaultTimeNeeded[m_NextBarrelToFire][j] + this.adjCycleDelay[m_NextBarrelToFire];

                        var main2 = systemList[j].main;
                        this.DebugPrint("    <MPM> Detected startDelay: " + main2.startDelay.constant.ToString());
                    }

                    this.playSelectedParticles(systemList, "    ", "<MPM> Attempting to play Barrel #" + m_NextBarrelToFire.ToString() + " particles");
                }
                int m_NumCannonBarrels = this.maxTimeNeeded.Length;
                int nextBarrelToPlay = m_NextBarrelToFire == m_NumCannonBarrels - 1 ? 0 : m_NextBarrelToFire + 1;

                // var main3 = this.beforeBarrelFired[nextBarrelToPlay][0].main;
                // this.DebugPrint("    <MPM> Detected startDelay: " + main3.startDelay.constant.ToString());
                // this.playSelectedParticles(this.beforeBarrelFired[nextBarrelToPlay], "    ", "<MPM> Attempting to play Barrel #" + nextBarrelToPlay.ToString() + " particles");
            }
        }

        // handle last case
        private void ProcessBarrelFire(int m_NextBarrelToFire)
        {
            this.DebugPrint("  <MPM> Detected shot fired, non-first, no reset");
            if (this.AllAtOnce)
            {
                this.DebugPrint("  <MPM> AllAtOnce need to cycle all particles");
                for (int i = 0; i < this.beforeBarrelFired.Count; i++)
                {
                    this.DebugPrint("    <MPM> Attempting to cycle Barrel #" + i.ToString() + " particles");
                    List<ParticleSystem> systemList = this.beforeBarrelFired[i];
                    this.stopSelectedParticles(systemList, "      ", "<MPM> Attempting to stop Barrel #" + i.ToString() + " particles");

                    /* if (this.adjCycleDelay[0] > 0.0f)
                    {
                        StartCoroutine(this.playSelectedParticlesWithDelay(systemList, "      ", "<MPM> Attempting to play Barrel #" + i.ToString() + " particles", this.adjCycleDelay[0]));
                    }
                    else
                    {
                        this.playSelectedParticles(systemList, "      ", "<MPM> Attempting to play Barrel #" + i.ToString() + " particles");
                    } */
                    this.playSelectedParticles(systemList, "      ", "<MPM> Attempting to play Barrel #" + i.ToString() + " particles");
                }
            }
            // don't care about burst count after it's been corrected
            else
            {
                this.DebugPrint("  <MPM> Single Barrel" + m_NextBarrelToFire.ToString() + " need to cycle particles");

                this.DebugPrint("    <MPM> Attempting to cycle Barrel #" + m_NextBarrelToFire.ToString() + " particles");
                this.stopSelectedParticles(this.beforeBarrelFired[m_NextBarrelToFire], "      ", "<MPM> Attempting to stop Barrel #" + m_NextBarrelToFire.ToString() + " particles");

                // int m_NumCannonBarrels = this.maxTimeNeeded.Length;
                // int nextBarrelToPlay = m_NextBarrelToFire == m_NumCannonBarrels - 1 ? 0 : m_NextBarrelToFire + 1;

                /* if (this.adjCycleDelay[nextBarrelToPlay] > 0.0f) {
                    StartCoroutine(this.playSelectedParticlesWithDelay(this.beforeBarrelFired[nextBarrelToPlay], "      ", "<MPM> Attempting to play Barrel #" + nextBarrelToPlay.ToString() + " particles", this.adjCycleDelay[nextBarrelToPlay]));
                }
                else
                {
                    this.playSelectedParticles(this.beforeBarrelFired[nextBarrelToPlay], "      ", "<MPM> Attempting to play Barrel #" + nextBarrelToPlay.ToString() + " particles");
                } */
                this.playSelectedParticles(this.beforeBarrelFired[m_NextBarrelToFire], "      ", "<MPM> Attempting to play Barrel #" + m_NextBarrelToFire.ToString() + " particles");
            }
        }

        // called from ModuleWeaponGun OnPool
        // Input: proper order of things
        // Anything not explicitly assigned a CannonBarrel defaults to the first CannonBarrel slot
        public bool AlignCannonBarrels(CannonBarrel[] m_CannonBarrels)
        {
            this.DebugPrint("<MPM> AlignCannonBarrels");

            if (this.initBeforeBarrelFired != null && this.initBeforeBarrelFired.Count > 0) {
                this.beforeBarrelFired = new List<List<ParticleSystem>>();
                this.defaultTimeNeeded = new List<List<float>>();
                this.beforeBarrelFired.Add(new List<ParticleSystem>());
                this.defaultTimeNeeded.Add(new List<float>());

                // initialize the structures
                for (int i = 1; i < m_CannonBarrels.Length; i++)
                {
                    this.beforeBarrelFired.Add(new List<ParticleSystem>());
                    this.defaultTimeNeeded.Add(new List<float>());
                }

                // we know they've been shoved in order.
                List<ParticleSystem> init_system_dump = this.initBeforeBarrelFired;
                List<float> init_float_dump = this.initTimeBeforeBarrelFired;
                // this.beforeBarrelFired[0] = new List<ParticleSystem>();
                // this.defaultTimeNeeded[0] = new List<float>();
                this.maxTimeNeeded = new float[m_CannonBarrels.Length];
                this.adjStartDelay = new float[m_CannonBarrels.Length];
                this.adjCycleDelay = new float[m_CannonBarrels.Length];

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
                            this.defaultTimeNeeded[j].Add(init_float_dump[i]);
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
                        this.defaultTimeNeeded[0].Add(init_float_dump[i]);
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
            this.DebugPrint("<MPM> OnSpawn");
            this.StopAndClearAllParticles();
        }

        private void OnPool()
        {
            this.DebugPrint("<MPM> OnPool");
            this.GetComponentsInChildren<CannonBarrel>(true);

            // hook up the functions to the events (OnAttach, OnDetach, OnAnchorStatusChanged)
            this.block.AttachEvent.Subscribe(new System.Action(this.OnAttach));
            this.block.DetachEvent.Subscribe(new System.Action(this.OnDetach));
            this.DebugPrint("<MPM> Pool subscribe block attachments");

            this.m_ModuleWeaponGun = this.GetComponent<ModuleWeaponGun>();
            if (this.m_ModuleWeaponGun == null)
            {
                this.DebugPrint("<MPM> DID NOT FIND MODULEWEAPONGUN");
                // this.onSpacePress = null;
                // this.onSpaceRelease = null;
                this.beforeBarrelFired = null;
                this.defaultTimeNeeded = null;
            }

            this.DebugPrint("<MPM> OnPool Complete");
        }

        private void OnAttach()
        {
            this.DebugPrint("<MPM> OnAttach");
            this.playSelectedParticles(this.onBlockAttach, "  ", "<MPM> OnAttach Particles set to Play");

            if (this.block == null)
            {
                this.DebugPrint("<MPM> OnAttach this.block == NULL ?????");
            }
            // This part is probably already covered by game engine OnAnchorStatusChanged
            if (this.block.tank != null)
            {
                if (this.block.tank.IsBase)
                {
                    this.playSelectedParticles(this.onAnchor, "  ", "<MPM> OnAnchor Triggered (from OnAttach init)");
                }
                this.block.tank.AnchorEvent.Subscribe(new System.Action<ModuleAnchor, bool, bool>(this.OnAnchorStatusChanged));
            }
            return;
        }

        private void OnDetach()
        {
            this.DebugPrint("<MPM> OnDetach");
            if (this.block == null)
            {
                this.DebugPrint("<MPM> OnDetach this.block == NULL ?????");
            }
            if (this.block.tank != null)
            {
                this.block.tank.AnchorEvent.Unsubscribe(new System.Action<ModuleAnchor, bool, bool>(this.OnAnchorStatusChanged));
            }
            this.StopAllParticles();

            // Reset all weapon stuffs to factory condition
            this.fireParticlesActive = false;
            this.notFiredFirst = true;
            this.needToCorrectDelay = 0;
            this.lastFireOrder = false;
            // Reset weapon particle systems to default start delay
            return;
        }

        private void OnAnchorStatusChanged(ModuleAnchor anchor, bool anchored, bool fromAfterTechPopulate)
        {
            this.DebugPrint("<MPM> OnAnchorStatusChanged");
            if (anchored)
            {
                this.playSelectedParticles(this.onAnchor, "  ", "<MPM> Playing anchored particles");
            }
            else
            {
                this.stopSelectedParticles(this.onAnchor, "  ", "<MPM> Stopping anchored particles");
            }
            return;
        }

        private void StopAllParticles()
        {
            String prefix = "      ";
            this.DebugPrint("  <MPM> Request Stopping all Particles");
            this.DebugPrint("    <MPM> Stopping OnblockAttach");
            foreach (ParticleSystem system in this.onBlockAttach)
            {
                this.DebugPrint("      <MPM> Found a PS");
                if (system.isPlaying)
                {
                    system.Stop(true);
                    this.SanityCheckPS(prefix, system);
                }
            }
            this.DebugPrint("    <MPM> Stopping Onanchor");
            foreach (ParticleSystem system in this.onAnchor)
            {
                this.DebugPrint("      <MPM> Found a PS");
                if (system.isPlaying)
                {
                    system.Stop(true);
                    this.SanityCheckPS(prefix, system);
                }
            }
            this.DebugPrint("    <MPM> Stopping OnWeaponCharge");
            if (this.onWeaponCharge != null)
            {
                foreach (ParticleSystem system in this.onWeaponCharge)
                {
                    this.DebugPrint("      <MPM> Found a PS");
                    if (system.isPlaying)
                    {
                        system.Stop(true);
                        this.SanityCheckPS(prefix, system);
                    }
                }
            }
            this.DebugPrint("    <MPM> Stopping OnWeaponFiring");
            if (this.onWeaponFiring != null)
            {
                foreach (ParticleSystem system in this.onWeaponFiring)
                {
                    this.DebugPrint("      <MPM> Found a PS");
                    if (system.isPlaying)
                    {
                        system.Stop(true);
                        this.SanityCheckPS(prefix, system);
                    }
                }
            }
            this.DebugPrint("    <MPM> Stopping beforeBarrelFired");
            if (this.beforeBarrelFired != null)
            {
                foreach (List<ParticleSystem> system_list in this.beforeBarrelFired)
                {
                    foreach (ParticleSystem system in system_list)
                    {
                        this.DebugPrint("      <MPM> Found a PS");
                        if (system.isPlaying)
                        {
                            system.Stop(true);
                            this.SanityCheckPS(prefix, system);
                        }
                    }
                }
            }
            return;
        }

        private void StopAndClearAllParticles()
        {
            String prefix = "      ";
            this.DebugPrint("  <MPM> Request Stopping all Particles");
            this.DebugPrint("    <MPM> Stopping OnblockAttach");
            foreach (ParticleSystem system in this.onBlockAttach)
            {
                this.DebugPrint("      <MPM> Found a PS");
                if (system.isPlaying)
                {
                    system.Stop(true);
                    system.Clear(true);
                    this.SanityCheckPS(prefix, system);
                }
            }
            this.DebugPrint("    <MPM> Stopping Onanchor");
            foreach (ParticleSystem system in this.onAnchor)
            {
                this.DebugPrint("      <MPM> Found a PS");
                if (system.isPlaying)
                {
                    system.Stop(true);
                    system.Clear(true);
                    this.SanityCheckPS(prefix, system);
                }
            }
            this.DebugPrint("    <MPM> Stopping OnWeaponCharge");
            if (this.onWeaponCharge != null)
            {
                foreach (ParticleSystem system in this.onWeaponCharge)
                {
                    this.DebugPrint("      <MPM> Found a PS");
                    if (system.isPlaying)
                    {
                        system.Stop(true);
                        system.Clear(true);
                        this.SanityCheckPS(prefix, system);
                    }
                }
            }
            this.DebugPrint("    <MPM> Stopping OnWeaponFiring");
            if (this.onWeaponFiring != null)
            {
                foreach (ParticleSystem system in this.onWeaponFiring)
                {
                    this.DebugPrint("      <MPM> Found a PS");
                    if (system.isPlaying)
                    {
                        system.Stop(true);
                        system.Clear(true);
                        this.SanityCheckPS(prefix, system);
                    }
                }
            }
            this.DebugPrint("    <MPM> Stopping beforeBarrelFired");
            if (this.beforeBarrelFired != null)
            {
                foreach (List<ParticleSystem> system_list in this.beforeBarrelFired)
                {
                    foreach (ParticleSystem system in system_list)
                    {
                        this.DebugPrint("      <MPM> Found a PS");
                        if (system.isPlaying)
                        {
                            system.Stop(true);
                            system.Clear(true);
                            this.SanityCheckPS(prefix, system);
                        }
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
                    this.DebugPrint(prefix + "  <MPM> Found a PS");
                    if (system.isPlaying)
                    {
                        system.Stop(true);
                        system.Clear(true);
                    }
                    if (!system.isPlaying)
                    {
                        // ParticleSystem grabSystem = system.gameObject.GetComponent<ParticleSystem>();
                        // grabSystem.Play(true);
                        system.Play(true);
                    }
                    this.SanityCheckPS(prefix, system);
                }
            }
            else
            {
                this.DebugPrint(prefix + "<MPM> NULL PS List");
            }
            return;
        }

        // used only with regard to fireParticles
        private IEnumerator playSelectedParticlesWithDelay(List<ParticleSystem> systemList, string prefix, string toPrint, float delay)
        {
            yield return new WaitForSeconds(delay);
            this.DebugPrint(prefix + "<MPM> Delayed particle play execution for " + delay.ToString() + " seconds");
            if (this.fireParticlesActive)
            {
                this.playSelectedParticles(systemList, prefix, toPrint);
            }
            if (!this.fireParticlesActive)
            {
                this.stopSelectedParticles(systemList, prefix, "<MPM> coroutine executed edge case, reverting play command");
            }
        }

        private void stopSelectedParticles(List<ParticleSystem> systemList, string prefix, string toPrint)
        {
            if (systemList != null)
            {
                this.DebugPrint(prefix + toPrint);
                foreach (ParticleSystem system in systemList)
                {
                    this.DebugPrint(prefix + "  <MPM> Found a PS");
                    if (system.isPlaying)
                    {
                        system.Stop(true);
                        this.SanityCheckPS(prefix, system);
                    }
                }
            }
            else
            {
                this.DebugPrint(prefix + "<MPM> NULL PS List");
            }
            return;
        }

        private void stopAndClearSelectedParticles(List<ParticleSystem> systemList, string prefix, string toPrint)
        {
            if (systemList != null)
            {
                this.DebugPrint(prefix + toPrint);
                foreach (ParticleSystem system in systemList)
                {
                    this.DebugPrint(prefix + "  <MPM> Found a PS");
                    if (system.isPlaying)
                    {
                        system.Stop(true);
                        system.Clear(true);
                        this.SanityCheckPS(prefix, system);
                    }
                }
            }
            else
            {
                this.DebugPrint(prefix + "<MPM> NULL PS List");
            }
            return;
        }

        private void OnRecycle()
        {
            this.StopAllParticles();
            return;
        }

        private void SanityCheckPS(String prefix, ParticleSystem system)
        {
            if (this.Debug)
            {
                this.DebugPrint(prefix + "  <MPM> [SanityCheck] Playing: " + system.isPlaying.ToString() + ", Paused: " + system.isPaused.ToString() + ", Stopped: " + system.isStopped.ToString() + ", Emitting: " + system.isEmitting.ToString() + ", Particle Count: " + system.particleCount.ToString());
                GameObject gameObject = system.gameObject;
                this.DebugPrint(prefix + "  <MPM> [SanityCheck] GameObject Hierarchy Active: " + gameObject.activeInHierarchy.ToString() + ", GameObject Local Active: " + gameObject.activeSelf.ToString());
                ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();
                this.DebugPrint(prefix + "  <MPM> [SanityCheck] ParticleSystemRenderer isVisible: " + renderer.isVisible.ToString() + ", enabled: " + renderer.enabled.ToString());
                ParticleSystem.EmissionModule emission = system.emission;
                this.DebugPrint(prefix + "  <MPM> [SanityCheck] EmissionModule enabled: " + emission.enabled.ToString() + ", rateOverTime: " + emission.rateOverTime.constant.ToString() + ", rateOverTimeMultiplier: " + emission.rateOverTimeMultiplier.ToString() + ", rateOverTimeMode: " + emission.rateOverTime.mode.ToString());
                ParticleSystem.MainModule main = system.main;
                this.DebugPrint(prefix + "  <MPM> [SanityCheck] MainModule startDelay: " + main.startDelay.constant.ToString() + ", startDelayMultiplier: " + main.startDelayMultiplier.ToString() + ", startLifetime: " + main.startLifetime.constant.ToString() + ", startLifetimeMultiplier: " + main.startLifetimeMultiplier.ToString());
            }
        }

        public void DebugPrint(string text)
        {
            if (this.Debug)
            {
                Console.WriteLine("[" + Time.time.ToString() + "]" + text);
            }
        }
    }
}
