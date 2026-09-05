# Disco Ball

BepInEx/Jotunn mod that adds a craftable, spinning, color-cycling Disco Ball
piece. Standalone comfort item (+2, stacks with fire/chair/table/etc, same
group as Hot Tub and Maypole).

## Requirements

- [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) for Valheim
- [Jotunn](https://github.com/Valheim-Modding/Jotunn) installed as its own BepInEx plugin

## Install

Drop `DiscoBall.dll` into `BepInEx/plugins/DiscoBall/`. Needs to be on every
client (registers a new piece).

## Recipe

Crafted at a Workbench: Bronze x5, Fine wood x4.

## Build from source

```
dotnet build -c Release
```

Needs `libs/Jotunn.dll` (from the [Jotunn releases page](https://github.com/Valheim-Modding/Jotunn/releases))
and `-p:ValheimPath=/path/to/Valheim` if Valheim isn't in the default Steam location.
