using UnityEngine;
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

        // starts playing as soon as you press space
        public List<ParticleSystem> onSpacePress;
        // starts playing as soon as you let go of space
        public List<ParticleSystem> onSpaceRelease;

        // Pair of arrs determine how far ahead of weapon firing it will play effects.
        // sets m_ShotTimer to designated time when first pressing space
        // WARNING: will override particle system start delay to be the same time
        public List<List<ParticleSystem>> beforeWeaponFired;
        public List<List<float>> timeBeforeWeaponFired;
        public float[] maxTimeNeeded;
        public List<CannonBarrel> initCannonBarrelList;

        public ModuleWeaponGun m_ModuleWeaponGun;
        public CannonBarrel[] m_CannonBarrels;
        public bool[] m_BarrelFired;

        // boolean options to control fire flow
        public bool AllAtOnce = false;
        public bool LastFireOrder = false;
        public bool m_ResetBurstOnInterrupt;
        public bool dynamicTimeCalc = false;
        public bool firedFirst = false;

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
                this.DebugPrint("<ModuleParticleManager> PrePool");

                this.onBlockAttach = new List<ParticleSystem>();
                this.onAnchor = new List<ParticleSystem>();
                this.onSpacePress = new List<ParticleSystem>();
                this.onSpaceRelease = new List<ParticleSystem>();

                this.beforeWeaponFired = new List<List<ParticleSystem>>();
                this.timeBeforeWeaponFired = new List<List<float>>();
                this.beforeWeaponFired.Add(new List<ParticleSystem>());
                this.timeBeforeWeaponFired.Add(new List<float>());

                this.DebugPrint("<ModuleParticleManager> PrePool initialization finished");
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
                        else if (type == MetadataType.SpacePress)
                        {
                            this.onSpacePress.Add(system);
                        }
                        else if (type == MetadataType.SpaceRelease)
                        {
                            this.onSpaceRelease.Add(system);
                        }
                        else if (type == MetadataType.WeaponCharge)
                        {
                            var main = system.main;
                            main.startDelay = new ParticleSystem.MinMaxCurve(r_value);
                            main.startDelayMultiplier = 1.0f;

                            this.beforeWeaponFired[0].Add(system);
                            this.timeBeforeWeaponFired[0].Add(r_value);
                            this.initCannonBarrelList.Add(metadata.CannonBarrel);
                        }
                        else
                        {
                            this.DebugPrint("<ModuleParticleManager> uh wut happened");
                        }
                    }
                }
                this.DebugPrint("<ModuleParticleManager> PrePool Complete");
            }
        }

        // Register this, handles OnSpacePress, etc
        private void ControlInputManual(int aim, bool fire)
        {
            // this.DebugPrint("<ModuleParticleManager> ControlInputManual");
            if (this.LastFireOrder ^ fire)
            {
                // change to start firing
                if (fire)
                {
                    if (this.onSpacePress != null)
                    {
                        foreach (ParticleSystem system in this.onSpacePress)
                        {
                            system.Play();
                        }
                    }
                    if (this.onSpaceRelease != null)
                    {
                        foreach (ParticleSystem system in this.onSpaceRelease)
                        {
                            system.Stop();
                        }
                    }
                }
                // change to stop firing
                else
                {
                    if (this.onSpacePress != null) {
                        foreach (ParticleSystem system in this.onSpacePress)
                        {
                            system.Stop();
                        }
                    }
                    if (this.onSpaceRelease != null)
                    {
                        foreach (ParticleSystem system in this.onSpaceRelease)
                        {
                            system.Play();
                        }
                    }
                    this.firedFirst = false;
                }
            }

            this.LastFireOrder = fire;
            return;
        }

        // m_NextBarrelToFire is which barrel will be fired immediately after, if this does not fail
        // return appropriate float of what should set m_ShotTimer to
        // return 0.0 if everything is kosher

        // should handle spinup times (looks like not possible rip)
        public float PrepareFiring(int m_NextBarrelToFire) {
            this.DebugPrint("<ModuleParticleManager> PrepareFiring");
            if (!this.dynamicTimeCalc)
            {
                if (!this.firedFirst && this.beforeWeaponFired.Count > 0)
                {
                    foreach (ParticleSystem system in this.beforeWeaponFired[0])
                    {
                        system.Play();
                    }
                    this.firedFirst = true;
                    return this.maxTimeNeeded[m_NextBarrelToFire];
                }
            }
            return 0.0f;
        }

        // m_NextBarrelToFire has already been set, means should be appropriate barrel to play particle effects on
        public void ProcessFiring(int m_NextBarrelToFire, int numBarrelsFired) {
            this.DebugPrint("<ModuleParticleManager> ProcessFiring");
            if (!this.dynamicTimeCalc)
            {
                if (this.AllAtOnce)
                {
                    foreach (List<ParticleSystem> system_list in this.beforeWeaponFired)
                    {
                        foreach (ParticleSystem system in system_list)
                        {
                            system.Play();
                        }
                    }
                }
            }
        }

        // handle animations proper
        /* private void PrePool()
        {
            
            return;
        } */

        // called from ModuleWeaponGun OnPool
        // Input: proper order of things
        // Anything not explicitly assigned a CannonBarrel defaults to the first CannonBarrel slot
        public bool AlignCannonBarrels(CannonBarrel[] m_CannonBarrels)
        {
            this.DebugPrint("<ModuleParticleManager> AlignCannonBarrels");
            // only bother to align if we're aligning more than one something, of course
            if (!(this.timeBeforeWeaponFired == null) && (this.timeBeforeWeaponFired.Count > 0) && this.timeBeforeWeaponFired[0].Count > 0)
            {
                // initialize the structures
                for (int i = 1; i < m_CannonBarrels.Length; i++)
                {
                    this.beforeWeaponFired.Add(new List<ParticleSystem>());
                    this.timeBeforeWeaponFired.Add(new List<float>());
                }

                // we know they've been shoved in order.
                List<ParticleSystem> init_system_dump = this.beforeWeaponFired[0];
                List<float> init_float_dump = this.timeBeforeWeaponFired[0];
                this.beforeWeaponFired[0] = new List<ParticleSystem>();
                this.timeBeforeWeaponFired[0] = new List<float>();
                this.maxTimeNeeded = new float[m_CannonBarrels.Length];

                // Identify proper barrel index, shove it in there. Shove into barrel 0 if no CannonBarrel provided
                for (int i = 0; i < init_system_dump.Count; i++)
                {
                    CannonBarrel target_barrel = this.initCannonBarrelList[i];
                    bool match = false;
                    for (int j = 0; j < m_CannonBarrels.Length; j++)
                    {
                        if (target_barrel == m_CannonBarrels[j])
                        {
                            this.beforeWeaponFired[j].Add(init_system_dump[i]);
                            this.timeBeforeWeaponFired[j].Add(init_float_dump[i]);
                            this.maxTimeNeeded[j] = Mathf.Max(this.maxTimeNeeded[j], init_float_dump[i]);
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                    {
                        this.beforeWeaponFired[0].Add(init_system_dump[i]);
                        this.timeBeforeWeaponFired[0].Add(init_float_dump[i]);
                        this.maxTimeNeeded[0] = Mathf.Max(this.maxTimeNeeded[0], init_float_dump[i]);
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
                // this.onSpacePress = null;
                // this.onSpaceRelease = null;
                this.beforeWeaponFired = null;
                this.timeBeforeWeaponFired = null;
            }

            this.DebugPrint("<ModuleParticleManager> OnPool Complete");
        }

        public void OnRequestFire() { }

        private void OnAttach()
        {
            this.DebugPrint("<ModuleParticleManager> OnAttach");
            this.playAttachParticles();

            if (this.block == null)
            {
                this.DebugPrint("<ModuleParticleManager> OnAttach this.block == NULL ?????");
            }
            // This part is probably already covered by game engine OnAnchorStatusChanged
            if (this.block.tank != null)
            {
                if (this.block.tank.IsBase)
                {
                    foreach (ParticleSystem system in this.onAnchor)
                    {
                        system.Play();
                    }
                }
                this.block.tank.control.manualAimFireEvent.Subscribe(new System.Action<int, bool>(this.ControlInputManual));
            
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
            this.firedFirst = false;
            return;
        }

        private void OnAnchorStatusChanged(ModuleAnchor anchor, bool anchored, bool fromAfterTechPopulate)
        {
            this.DebugPrint("<ModuleParticleManager> OnAnchorStatusChanged");
            if (anchored)
            {
                foreach (ParticleSystem system in this.onAnchor)
                {
                    system.Play();
                }
            }
            else
            {
                foreach (ParticleSystem system in this.onAnchor)
                {
                    system.Stop();
                }
            }
            return;
        }

        private void StopAllParticles()
        {
            this.DebugPrint("  <ModuleParticleManager> Request Stopping all Particles");
            foreach (ParticleSystem system in this.onBlockAttach)
            {
                this.DebugPrint("      <ModuleParticleManager> Found a PS");
                system.Stop();
            }
            this.DebugPrint("    <ModuleParticleManager> OnblockAttach Stopped");
            foreach (ParticleSystem system in this.onAnchor)
            {
                this.DebugPrint("      <ModuleParticleManager> Found a PS");
                system.Stop();
            }
            this.DebugPrint("    <ModuleParticleManager> Onanchor Stopped");
            if (this.onSpacePress != null)
            {
                foreach (ParticleSystem system in this.onSpacePress)
                {
                    this.DebugPrint("      <ModuleParticleManager> Found a PS");
                    system.Stop();
                }
            }
            this.DebugPrint("    <ModuleParticleManager> OnSpacePress Stopped");
            if (this.onSpaceRelease != null)
            {
                foreach (ParticleSystem system in this.onSpaceRelease)
                {
                    this.DebugPrint("      <ModuleParticleManager> Found a PS");
                    system.Stop();
                }
            }
            this.DebugPrint("    <ModuleParticleManager> OnSpaceRelease Stopped");
            if (this.beforeWeaponFired != null)
            {
                foreach (List<ParticleSystem> system_list in this.beforeWeaponFired)
                {
                    foreach (ParticleSystem system in system_list)
                    {
                        this.DebugPrint("      <ModuleParticleManager> Found a PS");
                        system.Stop();
                    }
                }
            }
            this.DebugPrint("    <ModuleParticleManager> beforeWeaponFired stopped");
            return;
        }

        private void playAttachParticles()
        {
            this.DebugPrint("  <ModuleParticleManager> Play Attach Particles");
            foreach (ParticleSystem system in this.onBlockAttach)
            {
                this.DebugPrint("    <ModuleParticleManager> Found a PS");
                // ParticleSystem.MainModule main = system.main;
                // main.prewarm = true;
                system.Play();
            }
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
