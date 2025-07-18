# RW-Repower
Rebalance Power, when not in use it should really Idle

> **Important:** This mod was originally Created by Texel.

**This is a continuation of the mod with a long history of Maintainers. I would like to attribute them:**  
- Texel (Original Author - 0.16)
- [EbonJaeger](https://github.com/EbonJaeger/RePowerReborn) (Previous Maintainer 1.0 - 1.3)
- [Mlie](https://github.com/emipa606/RePowerPatchPack) (Previous Maintainer - Complete Rewrite 1.1 - 1.5)

This version has been majorly rewritten to be more automatic, supporting mods by default when using inheritable Definitions, and providing more ways to extend through XML Patches or definitions. Because of that, I cannot call it a fork.

### Original Description:
Rebalance power consumption based on when an object is in use.

Generally speaking, power consumers will use substantially reduced power when not in use, and substantively more power when in use. Power consumption will accordingly vary wildly based on usage, rather than the vanilla behaviour of simply how many worktables or doors you have.

Buildings should run at 10% power when not in use, unless they have an idlePower setting defined.

You can find this on Steam Workshop: [RW-Repower](https://steamcommunity.com/sharedfiles/filedetails/?id=3528871130)    


## How to add your own Defs or Patches

#### To add a specific Def, you can Target it like below.
```
<defs>xml
  <RW_Repower.RePowerDef>
  <defName>AutodoorsIdle<defName>
  <targetDef>Autodoor<targetDef>
</defs>
```

#### To add a Class and hit all Defs belonging to it.
```xml
<defs>
	<RW_Repower.RePowerDef>
    <defName>Building_WorkTable</defName> -- Add a Unique Name for the Def.
    <className>Building_WorkTable</className> -- Add the ClassName you want the mod to handle for you.
  </RW_Repower.RePowerDef>
</defs>
```


#### To add your own Patch for ingame idle power (If you don't like the 10%). You can add a Patch like this.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Patch>
<Operation Class="XmlExtensions.PatchOperationSafeAddorReplace">
    <xpath>Defs/ThingDef[defName="ElectricStove"]</xpath>
    <compare>Name</compare>
    <checkAttributes>false</checkAttributes>
    <safetyDepth>-1</safetyDepth>
    <value>
      <comps>
        <li Class="CompProperties_Power">
          <idlePowerDraw>10</idlePowerDraw>
          <basePowerConsumption>350</basePowerConsumption>
        </li>
      </comps>
    </value>
  </Operation>
</Patch>
```