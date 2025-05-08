using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace ParticleManager
{
    [Serializable()]
    public struct ParticleSystemMetadata
    {
        public ParticleSystem m_system;
        public Spinner spinner;
        public float value;
        public CannonBarrel CannonBarrel;
        public MetadataType type;
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
