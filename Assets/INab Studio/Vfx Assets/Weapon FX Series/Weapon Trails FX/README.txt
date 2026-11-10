Hi!
Thanks for purchasing Procedural Weapon Trails! Here are some useful links to help you get started:

Discord Support Server
https://discord.com/invite/K88zmyuZFD

Online Documentation
https://inabstudios.gitbook.io/procedural-weapon-trails/

Import & Setup Video
https://www.youtube.com/watch?v=GUPurjGuC-Y

How To Use Video 
https://www.youtube.com/watch?v=PIU04UBQosM

If you have questions, feedback, or run into issues, reach out on Discord. 
Enjoy using Procedural Weapon Trails in your project!​

##Offline Instructions:


#Requirements

- Unity 6.0 or newer.​
- HDRP or URP render pipeline.​
- Editor Coroutines package 
- URP only: Visual Effect Graph package installed.​
- URP only: Opaque Texture enabled in your URP asset.​


#Before Importing (required)

- Install required packages via Window → Package Manager → Unity Registry.​
- URP: Install Visual Effect Graph.​
- Install Editor Coroutines (com.unity.editorcoroutines).​
- URP setting: enable Opaque Texture in your URP Asset.​


#Before Importing (optional, for demo scenes)

- Install Cinemachine via Package Manager
- Project Settings → Player → Other Settings → Active Input Handling = Input Manager (Old) or Both.​
- Window → TextMeshPro → Import TMP Essential Resources.​


#Setup by Pipeline

URP

- No extra steps; import the asset and it should work out of the box.​
- rebuild graphs: Edit → VFX → Rebuild and Save All VFX Graphs.​

HDRP

- Locate HDRP.unitypackage at: INab Studio/Vfx Assets/Weapon FX Series/Weapon Trails FX.​
- Double‑click HDRP.unitypackage to import all files.​
- Rebuild graphs after import: Edit → VFX → Rebuild and Save All VFX Graphs.​


#Demo Scenes
Path: INab Studio/Vfx Assets/Weapon FX Series/Weapon Trails FX/Demo Scenes.​

Included scenes:

- All Trails Showcase: Browse all trail prefabs.​
- All Trails Showcase – Animated Character: See trails on a character.​
- Animation Events: Trails driven by animation events.​
- Manual Mode – API: C# API examples.​
- Multiple Character Instances: Correct setup for shared clips across characters.​

Each scene contains a short summary and simple on‑screen controls.​


#Quick Start


A) Add the component
Add WeaponTrailEffect.cs to your character GameObject. Ensure the character has an Animator.​

B) Set up trail transforms
Trails use two points: Line Tip and Line Bottom. In the component, set Weapon Mount to your weapon transform, then click Setup Line Transforms to auto‑create them as children.​

C) Position the transforms
Select Line Tip and move it (typically +Z) to the weapon’s tip.​
Select Line Bottom and position it at the weapon’s base. Hide gizmos later if desired.​

D) Choose animation and trail
Select the animation clip to apply the trail to.​
Click Load New and choose a trail prefab. Optionally enable Auto Preview on Load to auto‑play the animation when loading different prefabs.​

E) Enable the trail
In Trail Settings, enable the trail for the selected animation. Trails can be toggled per animation. Press Play to preview.​

F) Set trail time range
Adjust Start Time and End Time to control when the trail spawns within the clip. The character pose updates live while scrubbing, matching the selected range. Use Play with Pause to stop mid‑animation and fine‑tune.​

G) Per‑animation tuning
Set Fade In and Fade Out durations to control smoothness.​
Adjust trail Length and Lifetime to match the motion style of the clip.​

H) Done
When the animation plays and the trail is enabled for that clip, the trail spawns and follows the weapon in real time.​

### Third Party Assets
All assets in (INab Studio\Demo Assets\CC0) directory are licensed under CC0 License.

https://polyhaven.com/license