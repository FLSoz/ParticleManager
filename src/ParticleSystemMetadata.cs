using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace ParticleManager
{
    [Serializable()]
    public class ParticleSystemMetadata
    {
        public ParticleSystem m_system;
        public float value;
        public CannonBarrel CannonBarrel;
        public MetadataType type;

        public ParticleSystemMetadata()
        {
            this.m_system = null;
            this.value = 0.0f;
            this.CannonBarrel = null;
            this.type = MetadataType.None;
        }
    }

    [Serializable()]
    public enum MetadataType
    {
        None = 0,
        Attach = 1,
        Anchor = 2,
        WeaponCharge = 3,
        BarrelCharge = 4,
        WeaponFiring = 5,
        BarrelFired = 6,
        FanFire = 7,
        FanIdle = 8,
        JetIdle = 9,
        OnEnergyPresent = 10
    }
}
