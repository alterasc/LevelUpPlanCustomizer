# How to make a auto level up plan (or feature list as it is named in game)

## Version 1 (the only one at the moment)

### Why and how

V1 structure is almost completely identical to in-game used feature list blueprint list.    
This was done to prevent maximum compatibility and allow maximum flexibility. Also that allows user to just copy-paste things from blueprint without additional editing.   

You could start your modification by copying entire block of blueprint and it will work.


### Structure of feature list blueprint in Owlcat provided format
You can find those blueprints in blueprints.zip in game directory.    
Seelah's normal feature list is in Units\Companions\Seelah\Seelah_FeatureList.jbp   
.jbp files are just json in structure, you can open then with any text editor


Excerpt from that file with some of the lines removed

```json
{
  "AssetId": "777ae11136378a64883059457966a325",
  "Data": {
    "$type": "cb208b98ceacca84baee15dba53b1979, BlueprintFeature",
    "PrototypeLink": "",
    "m_Overrides": [
      "$AddClassLevels$364174a3-89cc-489f-ada0-e3d47e9ecda9"
    ],
    "Components": [
      {
        "$type": "81d1333a815e48c4baf215e1b7adf8d6, AddClassLevels",
        "m_Flags": 0,
        "name": "$AddClassLevels$364174a3-89cc-489f-ada0-e3d47e9ecda9",
        "PrototypeLink": {
          "guid": "",
          "name": ""
        },
        "m_Overrides": [],
        "m_CharacterClass": "!bp_bfa11238e7ae3544bbeb4d0b92e897ec",
        "m_Archetypes": [],
        "Levels": 20,
        "RaceStat": "Strength",
        "LevelsStat": "Charisma",
        "Skills": [
          "SkillPersuasion",
          "SkillKnowledgeWorld",
          "SkillMobility"
        ],
        "m_SelectSpells": [],
        "m_MemorizeSpells": [],
        "Selections": [
          {
            "IsParametrizedFeature": false,
            "m_Selection": "!bp_a7c8b73528d34c2479b4bd638503da1d",
            "m_Features": [
              "!bp_88d5da04361b16746bf5b65795e0c38c"
            ],
            "m_ParametrizedFeature": null,
            "m_ParamObject": null,
            "ParamSpellSchool": "None",
            "ParamWeaponCategory": "UnarmedStrike",
            "Stat": "Unknown"
          },
          {
            "IsParametrizedFeature": false,
            "m_Selection": "!bp_02b187038a8dce545bb34bbfb346428d",
            "m_Features": [
              "!bp_7ee2ef06226a4884f80b7647a2aa2dee",
              "!bp_3990a92ce97efa3439e55c160412ce14",
              "!bp_d306e052d2f85b24cb5082951459045e",
              "!bp_19250e5a12db1fc448970b81e0f38922",
              "!bp_445c696bead5140459909ffbed14750e",
              "!bp_62582b4636218d74db34606f872c9c05"
            ],
            "m_ParametrizedFeature": null,
            "m_ParamObject": null,
            "ParamSpellSchool": "None",
            "ParamWeaponCategory": "UnarmedStrike",
            "Stat": "Unknown"
          }
        ],
        "DoNotApplyAutomatically": false
      }
    ]
  }
}
```

Critical information to get from it:  
- AssetId - this is the ID of blueprint. The feature list to update will be identified by it
- Components property - this is core part
    - m_CharacterClass - this is class
    - m_Archetypes - array of archetypes. Yes, it can be multiple, game has support for that, but you need mods to enable more than one
    - Levels - number of levels this block describes. In base game most companions have one block of 20 levels for their class. Skills taken and level up stat are defined once per class block. Blocks are applied from first to last. So if you want for example Sorcerer 3 -> Monk Scaled Fist 1 -> Sorcerer 6 -> Dragon Disciple 10, that would be no less that 4 blocks. (One of 3 levels, then of 1 level, then 6, then 10).  
    - RaceStat - if race allows choice of race stat, the value defined in the first block will be used. Values in next blocks will be ignored. If race has no choice, value is ignored.
    - LevelsStat - stat to raise on stat level ups. 
    - Skills - applies those skills. You can add as many as you want, skill points are spent from up to down. I recommend adding at least one skill more, in case intelligence is raised. Having more skills than used have no effect, but not having enough skills to spend skill points on will stop the auto level.
    - m_SelectSpells - empty, since Seelah is divine caster that receives all spells. Fill this if needed. As always, first entries are taken first, and so on.
    - m_MemorizeSpells - i think only matters on your first encounter, so could be left blank.
    - Selections - class feature selections and feats
        - m_Selection - ID of selection. 
        - m_Features - features to be taken. It selection is encountered multiple times over the course of covered class levels, features are taken one by one. In provided example first selection is PaladinDeitySelection, where Seelah takes Iomedae. Second selection is SelectionMercy, where Seelah will take Sickened, Diseased, Confused, Staggered, Cursed, Paralyzed. Note that some selection have selections as choices and those selections should have block where they are selections, and features are what choices. Example of such feature is Armor Focus, where first goes feat selection of Armor Focus, and then selection of Heavy Armor in that Armor Focus selection.
        - IsParametrizedFeature - if this is false, the rest of the fields don't matter and are ignored. Some selections are parametrized. Weapon Focus, Spell Focus, and the likes. Then you will need to specify parameter in one of the fields, be that weapon, or spell school.
        - m_ParametrizedFeature - if IsParametrizedFeature is true, this should be equal to feature, that is being parametrized.


Mythic classes level ups have no difference, and are described after normal classes.

You can see working example of custom build in Samples folder.

### So you got (vague) idea of the structure, what's next?

Next is you will need to go through blueprints to look at ids. No way around it.
That said, this is not something particularly complex. 

My recommendation - get [BubblePrints](https://github.com/factubsio/BubblePrints/releases) to quickly find what you need.  
Internal names are most of the time similar enough to display names.
For class feature search \<ClasName\>Class or \<ClasName\>Progression.  


### GUID formats that mod can recognize

`!bp_62582b4636218d74db34606f872c9c05` - Owlcat provided blueprint GUID
`Blueprint:3990a92ce97efa3439e55c160412ce14:MercyDiseased` - format used in blueprint dumps provided by Vek17. You can find them in WotR discord in channel #mod-dev-technical. Text after guid is ignored, feel free to write whatever 
`link: 11f971b6453f74d4594c538e3c88d499 (VitalStrikeAbilityGreater :BlueprintAbility)` - if you press "Open in Editor" button in Bubbleprints, other blueprint will be referenced like that. Text after guid is ignored.
`3990a92ce97efa3439e55c160412ce14` - just guid.
any other format that C# Guid.Parse can recognize by default.


## Resources

### Stats

- Strength
- Dexterity
- Constitution
- Intelligence
- Wisdom
- Charisma

### Skills 

- SkillMobility
- SkillAthletics
- SkillPersuasion
- SkillThievery
- SkillLoreNature
- SkillPerception
- SkillStealth
- SkillUseMagicDevice
- SkillLoreReligion
- SkillKnowledgeWorld
- SkillKnowledgeArcana

### Spell schools

- None
- Abjuration
- Conjuration
- Divination
- Enchantment
- Evocation
- Illusion
- Necromancy
- Transmutation
- Universalist

### Weapon types

- UnarmedStrike
- Dagger
- LightMace
- PunchingDagger
- Sickle
- Club
- HeavyMace
- Shortspear
- Greatclub
- Longspear
- Quarterstaff
- Spear
- Trident
- Dart
- LightCrossbow
- HeavyCrossbow
- Javelin
- Sling
- Handaxe
- Kukri
- LightHammer
- LightPick
- Shortsword
- Starknife
- WeaponLightShield
- SpikedLightShield
- Battleaxe
- Flail
- HeavyPick
- Longsword
- Rapier
- Scimitar
- Warhammer
- WeaponHeavyShield
- SpikedHeavyShield
- EarthBreaker
- Falchion
- Glaive
- Greataxe
- Greatsword
- HeavyFlail
- Scythe
- Shortbow
- Longbow
- Kama
- Nunchaku
- Sai
- Siangham
- BastardSword
- DuelingSword
- DwarvenWaraxe
- Estoc
- Falcata
- Tongi
- ElvenCurvedBlade
- Fauchard
- HandCrossbow
- LightRepeatingCrossbow
- HeavyRepeatingCrossbow
- Shuriken
- SlingStaff
- Touch
- Ray
- Bomb
- Bite
- Claw
- Gore
- OtherNaturalWeapons
- Bardiche
- DoubleSword
- DoubleAxe
- Urgrosh
- HookedHammer
- KineticBlast
- ThrowingAxe
- Tail


## Q&A

Q: Why didn't you just made it use human-readable names?  
A: Because while I could do that for feats, there's a lot of selections where that would be unfeasible. Also because it's early stage.

Q: Does it support modded setups?
A: Yes. You would need to get feature ids after from mod folder, or with [Data Viewer](https://www.nexusmods.com/pathfinderwrathoftherighteous/mods/9)