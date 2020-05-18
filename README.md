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
        "type": "SpaceRelease"
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
    SpacePress = 2,
    SpaceRelease = 3,
    WeaponCharge = 4,
    None = 5
}
```

Currently, only Attach, Anchor, SpacePress, and SpaceRelease are operational.
- Attach plays the particle system when the block is attached to a tech, and doesn't otherwise.
- Anchor plays the particle system when the block is on a tech that's anchored, and doesn't otherwise.
- SpacePress will play the particle system when space is pressed (correspond with weapon firing). It's meant to be used on weapon blocks, but works fine on non-weapon blocks. When space is released, the particle system will stop playing
- SpaceRelease will play the particle system when space is released (corresponds with stopped firing, no idea what you would want it for). When space is then pressed, the particle system will stop playing
Nota Bene: all particle systems will stop playing when the parent block is removed from the tech

the fields CannonBarrel and value correspond to highly experimental functionality tied to weapon cooldowns. I make no promises about your functionality then.
