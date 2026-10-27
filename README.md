# Block Blaster AR

Unity Android project based on [the PRD](Block%20Blaster%20AR%20%E2%80%94%20PRD.md). The same app offers **Practice** without a camera and **Play on my table** on an ARCore-supported phone. Gameplay runs on a 60 × 80 cm board in the arena's local coordinates.

## Current status

The project compiles and builds. `Builds/BlockBlaster.apk` (33 MB, development build) was produced on 8 Oct 2026, the rule self-check passes, and the gameplay smoke check passes in editor Play mode. The APK installs and launches on a Samsung Galaxy S25 Ultra (SM-S938B), where AR starts, shows the camera feed, and detects surfaces. **A full game in AR, and the hardware checks under Verify, have not been run yet.**

Building on Ubuntu 25.10 needed three workarounds, all handled by the scripts: the editor lives outside this folder (Android's SDK tools fail on paths with spaces), `Tools/compat-libs/` supplies the `libxml2.so.2` the editor links against, and two .NET settings stop Unity's IL2CPP converter from crashing intermittently. To open the editor by hand on that OS:

```bash
LD_LIBRARY_PATH="$PWD/Tools/compat-libs" ~/Unity/Hub/Editor/6000.0.58f2/Editor/Unity -projectPath "$PWD"
```

## Set up and run

Use Unity **6000.0.58f2** with Android Build Support, Android SDK & NDK Tools, and OpenJDK. The project pins AR Foundation and ARCore to **6.0.8**. It uses the built-in render pipeline, legacy touch input, portrait orientation, OpenGL ES 3, and ARM64.

From this folder:

```bash
bash scripts/setup-unity.sh hub
```

Sign in to Unity Hub and activate an eligible Unity license. Close Hub, then:

```bash
bash scripts/setup-unity.sh editor
bash scripts/build-android.sh
bash scripts/install-phone.sh
```

The Hub helper downloads Unity's official portable Linux AppImage into `Tools/`, without installing a system package. It installs the editor and Android modules in `~/Unity/Hub/Editor/`, outside this folder, because Android's SDK tools fail when their path contains spaces. Downloads can take several GB. If you already have Unity elsewhere, pass its executable:

```bash
bash scripts/build-android.sh "/path/to/Editor/Unity"
```

For interactive development, add **this folder** as an existing project in Hub. After package import, choose **Block Blaster → Open game scene**, then press Play and choose Practice. Android configuration runs automatically on first open and can be repeated with **Block Blaster → Configure project**.

The Android APK is written to `Builds/BlockBlaster.apk`; the build log is `Builds/build.log`. If multiple devices are connected, pass a device serial to the installer, for example `bash scripts/install-phone.sh 10c0fc02`.

## Play

- **Practice:** tap Practice, optionally pinch to resize, then Start Game. Camera permission is not needed.
- **AR:** allow camera access, move slowly over a textured horizontal table, tap a highlighted surface, pinch before starting, then Start Game. The arena stays anchored; losing tracking pauses the game until tracking returns and you tap Resume.
- The ship fires on its own, four shots per second. Hold and drag anywhere on the lower half to steer. In the editor, use mouse dragging or A/D/arrow keys. Mouse wheel resizes before starting.
- Each cube earns 25 points; four cubes total 100. A missed shape costs 10 points, floored at zero. Ship contact removes one life and clears that shape; immunity lasts 1.5 seconds.
- A glowing gold-faced shape drops a pickup when fully destroyed. S = Shield, + = Extra Life, 3 = Triple Shot for 8 seconds, ~ = Slow-Mo for 5 seconds. Collect by touching with the ship.
- Waves last 30 seconds. Fall speed increases 10% and spawn rate 15% per wave; difficulty is capped after wave 16. There are at most 20 live shapes.
- Pause offers Resume, Restart, and Quit. Returning from the background requires a Resume tap. Play Again keeps the arena and resets the game.

Your Redmi can be used for Practice. Confirm your friend's exact Samsung model in **Settings → About phone** before checking [Google's supported-device list](https://developers.google.com/ar/devices). "Note 23/24" may refer to an S23/S24 model.

## Verify

In Unity, run **Block Blaster → Check game rules** to verify all 28 shape rotations, connectivity, valid spawn columns, score/life limits, and wave calculations. **Run gameplay smoke check** exercises a real bullet collision, cube damage, score, shield, hit immunity, pickups, pause/resume, game over, and restart. It restores the saved high score after the check. Both passed on 8 Oct 2026.

Before calling the project ready to submit, test on hardware:

1. Finish a three-life game in Practice; verify final score, saved best score after relaunch, and replay.
2. Confirm that the ship auto-fires four shots per second and every visible contact removes one cube.
3. Collect each pickup; verify durations and the three-life cap.
4. Background and return to the app; confirm no automatic resume.
5. Place AR on a supported phone, resize, walk around the table, lose and regain tracking, and replay without moving the arena.
6. Check 30+ fps in a busy wave and approximate anchor drift below 5 cm in good lighting. These are hardware acceptance criteria, not verified performance claims.

## Code map for the viva

| Concept | Working code |
| --- | --- |
| AR plane detection and placement raycast | `ArenaPlacer`, `ARPlaneManager`, `ARRaycastManager` |
| Anchoring and tracking loss | `ArenaPlacer.PlaceAnchor`, `HasTracking` |
| Finger-to-arena raycast | `ShipController.ReadPointer` |
| Physics and collision | `Bullet.OnCollisionEnter`, `ShipController.OnTriggerEnter` |
| Projectile motion and scaled gravity | `PowerUp.Drop`, `PowerUp.FixedUpdate` |
| Pooling | `BlockSpawner`, `GameEffects` |
| Score, lives, waves, pause | `GameManager`, `GameRules` |
| HUD and saved best score | `UIManager`, `GameManager` |

P0 and P1 have source implementations. P2 armoured cubes and charged shots are deferred, as allowed by the PRD. All geometry, icons, effects, and audio are generated in code. The only third-party files are two open-licensed fonts (Orbitron and Rajdhani, SIL Open Font License) in `Assets/Resources/`, with their licences in `Assets/Licenses/`.

Official setup references: [Unity Hub on Linux](https://docs.unity.com/en-us/hub/install-hub-linux), [Hub CLI](https://docs.unity.com/en-us/hub/use-hub-cli), [Unity release and changeset](https://unity.com/releases/editor/whats-new/6000.0.58f2), [Android build dependencies](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/android/getting-started/sdksetup/install-dependencies).
