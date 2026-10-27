# 0007. Encrypted, tamper-evident saves

Status: accepted. Saves are written atomically today; encryption is still to be added.

## Decision
The local save file is encrypted and carries an integrity check, with a key kept in the platform's secure storage (Android Keystore, iOS Keychain). Writes stay atomic (write a temporary file, then replace), with a previous copy kept for recovery. The cloud copy is encrypted in transit and at rest by the service.

## Why
- Coins, purchased items and race records are worth protecting from casual editing.
- A damaged file (power loss, full storage) must never wipe a player's progress.
- The save can hold account link data; it should not be readable by other apps on a rooted device.

## Rejected alternatives
- Plain JSON: easy to read and debug, but trivial to edit.
- Obfuscation such as base64 or XOR: gives a false sense of safety.
- A key stored in the app: anyone can extract it, so it protects nothing.
- Relying on the server alone: the game must work offline.

## Consequences
- Local encryption deters casual edits only; anything with real value (online points, purchases) is still checked on the server.
- A save version number and a migration path are needed, because old plain saves must still load once.
- Debugging needs a tool or editor menu that decrypts a save.
- If the key is lost (new device, wiped storage), the local save is gone; the cloud save is the recovery path.
