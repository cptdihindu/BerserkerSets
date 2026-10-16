<div align="center">

# ᛒ Berserker Sets ᛋ

### ⚔ Five weapon sets. One warrior. Instant switching. ⚔

**A lightweight, quick-loadout mod for Valheim.**

<img width="600" alt="Berserker Sets Viking Emblem-v2" src="https://github.com/user-attachments/assets/6ce963c3-8ecf-43f6-a546-6241408484b3" />

</div>

---

## Features

- Save five main-hand/off-hand equipment sets per character.
- Cycle forward with `F6`.
- Hold `LB` and press `D-pad Right` or `D-pad Left` to cycle in either direction.
- Edit sets from a compact panel beside the player inventory.
- Navigate the panel with a controller and assign from the normal inventory using `A`.
- Drag the title bar to reposition the panel in game; right-click it to reset the position.
- Preserve exact item instances—items are never created, duplicated, moved, or replaced.
- Keep each slot in its own durable save record so normal cycling cannot erase assignments.
- Show missing, dropped, chest-stored, or destroyed items as **Missing** without clearing the slot.
- Configure input, panel position, scale, and notifications through BepInEx configuration.

## Requirements

- Valheim
- [BepInEx 5](https://github.com/BepInEx/BepInEx)
- [Jötunn](https://github.com/Valheim-Modding/Jotunn)

The mod is client-side. Servers do not need Berserker Sets installed for normal equipment switching.

## Installation

1. Install BepInEx 5 and Jötunn.
2. Download `BerserkerSets.dll` from the latest GitHub release.
3. Create `BepInEx/plugins/BerserkerSets/` if it does not exist.
4. Place only `BerserkerSets.dll` inside that folder.
5. Launch Valheim.

Do not bundle or copy Valheim, Unity, BepInEx, Harmony, Jötunn, or Newtonsoft DLLs with this mod.

## Controls

| Action | Default input |
|---|---|
| Cycle forward | `F6` |
| Controller forward | Hold `LB` + press `D-pad Right` |
| Controller backward | Hold `LB` + press `D-pad Left` |
| Begin assigning a slot | Left-click the Berserker Sets slot |
| Clear a slot | Right-click the Berserker Sets slot |
| Select the Berserker Sets panel | Cycle inventory sections with `LB` / `RB` |
| Controller panel navigation | `D-pad` after selecting its section |
| Controller assign | `A`, then select an inventory item and press `A` |
| Controller clear/cancel | `B` |

Forward cycling wraps Set 5 to Set 1. Backward cycling wraps Set 1 to Set 5.

## Assigning equipment

1. Open the inventory.
2. Left-click a main-hand or off-hand slot in the Berserker Sets panel.
3. Click the desired item in the player inventory.

Main-hand slots accept weapons, bows, tools, and torches. Off-hand slots accept shields and torches. Older saved sets containing an incompatible two-handed/off-hand pairing remain safe: switching suppresses the off-hand item without deleting its assignment.

New assignments enforce valid hand combinations: a two-handed main-hand item cannot be saved while the set has an off-hand assignment, and an off-hand item cannot be added beside a saved two-handed item.

The assigned physical item must be in the player's inventory when switching. If it is stored elsewhere or no longer exists, the panel displays **Missing**. Returning the same tagged item to the inventory makes the assignment available again.

## Configuration and saves

General configuration:

```text
BepInEx/config/dev.berserkersets.quickloadouts.cfg
```

The default panel position is `(0, 0)`. Under `[UI]`:

- `PanelPositionX`: positive moves right; negative moves left.
- `PanelPositionY`: positive moves up; negative moves down.
- `PanelScale`: changes the preferred panel scale.

Changes made through an in-game BepInEx configuration manager apply immediately. Restart Valheim after manually editing bindings in the configuration file.

Per-character loadouts are stored under:

```text
BepInEx/config/BerserkerSets/characters/
```

Each configured slot also has an independent durable record. Temporarily removing the mod does not delete these files. Only explicitly clearing a slot removes its slot record.

## Compatibility

- Uses Valheim's normal equip/unequip APIs so equipment remains visible through normal multiplayer synchronization.
- Does not resize or replace the player inventory, keeping Quickslots-style inventory extensions independent.
- Accepts Better Archery bows through Valheim's bow item category and does not alter arrow handling.
- Uses a namespaced item metadata key and preserves metadata belonging to other mods.
- UI and inventory overhaul combinations may require panel position or scale adjustments.

## Building from source

Close Valheim, then run:

```powershell
dotnet build .\BerserkerSets.csproj
```

The project targets .NET Framework 4.8 and references the locally installed Valheim, BepInEx, and Jötunn assemblies. Override a non-default game location with:

```powershell
dotnet build .\BerserkerSets.csproj -p:ValheimDir="D:\Path\To\Valheim"
```

A successful build creates `bin/Debug/BerserkerSets.dll` and deploys it to `BepInEx/plugins/BerserkerSets/`.

## Reporting problems

Include the Berserker Sets entries from `BepInEx/LogOutput.log`, the mod version, and the steps that caused the problem. See [MANUAL_TESTING.md](MANUAL_TESTING.md) for the current acceptance checklist.
