# Manual test checklist

Do these checks after you compile and install the DLL. Nothing in this repository claims a successful compile or in-game validation yet.

## First checks

1. Start Valheim and inspect `BepInEx\LogOutput.log` for `Berserker Sets 0.1.5 initialized.`
2. Load a disposable character and open inventory. Confirm one five-row panel appears and repeated open/close cycles do not duplicate it.
3. Assign a one-handed weapon and shield to Set 1. Confirm icons appear, close inventory, press F6, and verify both equip.
4. Configure a bow or other two-handed weapon with a saved off-hand item. Confirm the off-hand assignment remains visible but is not equipped.
5. Cycle to a genuinely empty set and confirm both hand items clear.

## Persistence and identity

- Restart the game and confirm assignments and the last active set persist.
- Use two different characters and confirm their JSON files and sets remain independent.
- Assign one of two upgraded/enchanted copies of the same prefab. Store or drop that exact copy and confirm the other copy is not substituted.
- Return the exact item to inventory and confirm it resolves again.
- Duplicate an item through another mod, if relevant, and check the BepInEx warning about duplicate Berserker Sets IDs.

## Safety and edge cases

- Try invalid armor, food, ammo, resource, potion, trophy, and miscellaneous items in both slot types.
- Try a shield in main hand and a weapon in off hand; both should be rejected.
- Try two different torches, torch-only, shield-only, one-handed + torch, one-handed + shield, bow, crossbow, atgeir, greatsword, and tool sets.
- Test a broken item, dropped item, chest-stored item, and destroyed item.
- Press cycle rapidly. The 0.2-second guard should prevent overlapping requests.
- Confirm cycling is blocked during inventory, map, pause, console, chat/text entry, death, sleep, teleport/loading, and intro/cutscene states.
- Hold LB and press D-pad Right to cycle forward; verify Set 5 wraps to Set 1.
- Hold LB and press D-pad Left to cycle backward; verify Set 1 wraps to Set 5.
- Test in single-player, as a multiplayer client, through death/respawn, portal travel, logout, and world changes.

## What to include in an error report

- The complete compiler error text, including file and line number, or the relevant exception plus stack trace from `BepInEx\LogOutput.log`.
- Valheim version, BepInEx version, Jötunn version, and whether the issue occurs without other inventory/equipment/UI mods.
- The exact action that triggered the problem and the two items configured in the affected set.
- For persistence issues, the affected character JSON after removing any information you consider private.

Enable `EnableDebugLogging` in the BepInEx config only when collecting diagnostics; routine operation keeps it off.
