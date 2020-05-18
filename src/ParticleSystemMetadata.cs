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
        Attach = 0,
        Anchor = 1,
        SpacePress = 2,
        SpaceRelease = 3,
        WeaponCharge = 4,
        None = 5
    }
}
