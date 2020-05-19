# ParticleManager

Usage:

Add "ParticleManager.ModuleParticleManager" under root layer (deserializer)

sample usage: (in JSON block file)
```json
"Deserializer": {
  "ParticleManager.ModuleParticleManager": {
    "Input": [
      {
        "m_system": "/_spindle/m_BF_Radar_111_Sensor/FX_HE_PlasmaTeeth_Beam/ParticleSystem.",
        "value": 0.0,
        "CannonBarrel": null,
        "type": "Attach"
      },
      {
        "m_system": "/FX_HE_PlasmaTeeth_Beam_copy/ParticleSystem.",
        "value": 0.0,
        "CannonBarrel": null,
        "type": "WeaponFiring"
      }
    ],
    "Debug": true
  }
}
```

ModuleParticleManager has many fields which are initially empty. Pretty much all of these fields (which have been set public for Unity convenience reasons), should not be touched, because they, at best, provide no utility, and at worst, interfere with vital functions.

There are only two fields that you should be modifying in JSON files:
- Input
- Debug

Debug will enable debug printing in TT's output log when set to true (false by default)
Input is where configuration of the system is done.

Input consists of an array of ParticleSystemMetadata objects

```csharp
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
```

Each ParticleSystemMetadata object has 4 fields: m_system, value, CannonBarrel, type

The only 2 mandatory fields are m_system, and type

m_system should be passed a reference of the target ParticleSystem that you want controlled

type should be passed a MetadataType enum, either in int, or string form.

```csharp
[Serializable()]
public enum MetadataType
{
    Attach = 0,
    Anchor = 1,
    WeaponCharge = 2,
    BarrelCharge = 3,
    WeaponFiring = 4,
    None = 6
}
```

Currently, only Attach, Anchor, WeaponFiring, and WeaponCharge are operational.

- Attach plays the particle system when the block is attached to a tech, and doesn't otherwise.
```json
{
  "m_system": "/_gimbalBase/_gimbalElev/fx_Lightning/ParticleSystem.",
  "type": "Attach"
}
```

- Anchor plays the particle system when the block is on a tech that's anchored, and doesn't otherwise.
```json
{
  "m_system": "/_gimbalBase/_gimbalElev/fx_Lightning/ParticleSystem.",
  "type": "Anchor"
}
```

- WeaponFiring will play a particle system as long as that weapon is actively firing at something.
```json
{
  "m_system": "/_gimbalBase/_gimbalElev/fx_Lightning/ParticleSystem.",
  "type": "WeaponFiring"
}
```

- WeaponCharge will play a particle system from the moment a weapon tries to fire, till the time it fires its first shot. If the provided time for how long the particle system will play is longer than the time normally spent before the first shot is fire (i.e. spinner spinup), the ParticleManager will artificially inflate the time until the first shot is fired. (spinup times are unaffected)
Nota Bene: all particle systems will stop playing when the parent block is removed from the tech
```json
{
  "m_system": "/_gimbalBase/_gimbalElev/fx_Lightning/ParticleSystem.",
  "value": 5.0,
  "type": "WeaponCharge"
}
```

the fields CannonBarrel and value correspond to highly experimental functionality tied to weapon cooldowns. I make no promises about your functionality then.
