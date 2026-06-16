# HoopFlow — Editor Tools & Runtime Systems Reference

> **Project:** HoopFlow (Mobile Unity game, URP, Android / iOS)  
> **Last updated:** May 2026

---

## Table of Contents

1. [Menu Layout](#menu-layout)
2. [HoopFlow / Performance](#hoopflow--performance)
3. [HoopFlow / Diagnostics](#hoopflow--diagnostics)
4. [HoopFlow / Build](#hoopflow--build)
5. [HoopFlow / Sprites](#hoopflow--sprites)
6. [HoopFlow / Level](#hoopflow--level)
7. [HoopFlow / Scene](#hoopflow--scene)
8. [Paxie / Case](#paxie--case)
9. [Runtime Systems](#runtime-systems)
10. [Optimization Roadmap Summary](#optimization-roadmap-summary)

---

## Menu Layout

```
Unity Menu Bar
├── HoopFlow/
│   ├── Performance/    ← Rendering & CPU optimization tools
│   ├── Diagnostics/    ← Real-time analysis windows
│   ├── Build/          ← Pre-build checks & shader tools
│   ├── Sprites/        ← Sprite Atlas management
│   ├── Level/          ← Level-design & scene setup tools
│   └── Scene/          ← Scene-specific binding helpers
└── Paxie/
    └── Case/           ← Content pipeline & puzzle workflow tools
```

> **Convention:** HoopFlow = technical / optimization tools.  Paxie = game-content workflow tools.

---

## HoopFlow / Performance

---

### Strip Unused Material Keywords
**File:** `Assets/_Project/Scripts/Editor/MaterialKeywordStripper.cs`

Removes shader keywords from every material in `Assets/_Project` when the
corresponding texture or property is not actually assigned.

**Why it matters:** Each enabled keyword on a material creates a unique shader *variant*.
More variants → more SetPass calls → slower GPU state switches.

**Keywords checked:**

| Keyword | Removed when |
|---|---|
| `_NORMALMAP` | No normal map texture is assigned |
| `_EMISSION` | Emission color is black / zero |
| `_METALLICSPECGLOSSMAP` | No metallic/smoothness texture |
| `_SPECGLOSSMAP` | No specular texture |
| `_DETAIL_MULX2` | No detail albedo texture |
| `_PARALLAXMAP` | No height map texture |

**When to run:** After importing new assets or before every release build.  
**Expected gain:** SetPass Calls typically drop 10–30 %.

---

### Texture Audit (ASTC)
**File:** `Assets/_Project/Scripts/Editor/HoopFlowTextureAuditor.cs`

Scans all textures for mobile-specific import setting problems and can auto-fix them all with one click.

**Detected issues:**

| Issue | Description | Auto-fix applied |
|---|---|---|
| No ASTC (Android) | GPU cannot decompress efficiently | ASTC 6×6 override applied |
| No ASTC (iOS) | PVRTC is larger & lower quality | ASTC 6×6 override applied |
| UI Sprite max size > 1024 | Wastes VRAM | Clamped to 1024 |
| Mipmaps off on 3D texture | Causes aliasing at distance | Mipmaps enabled |
| Mipmaps on UI sprite | Wastes GPU memory — UI is always full-res | Mipmaps disabled |
| Read/Write enabled | Keeps a CPU-side copy (doubles RAM usage) | Disabled |

**How to use:** Tick "Search _Project folder only" → **Scan** → **Auto-Fix All**.

> **Note:** Auto-Fix triggers a reimport on every affected texture. Unity may appear frozen for a few seconds.

---

### Audio Optimizer
**File:** `Assets/_Project/Scripts/Editor/HoopFlowAudioOptimizer.cs`

Audits every audio clip under `Assets/_Project` and enforces mobile-optimal compression settings.

**Rule set:**

| Clip length | Load Type | Format | Mono forced |
|---|---|---|---|
| > 5 s (BGM) | Streaming | Vorbis q = 0.5 | No |
| 2–5 s (mid SFX) | CompressedInMemory | Vorbis q = 0.5 | Yes |
| < 2 s (short SFX) | DecompressOnLoad | ADPCM | Yes |

**Sample rate:** BGM → 44 100 Hz · SFX → 22 050 Hz  
(Indistinguishable to the human ear; file size drops ~50 %.)

**Expected gain:** Total audio memory typically reduced by 50–70 %.

---

### Mark Scene Decorations As Batching-Static
**File:** `Assets/_Project/Scripts/Editor/MarkDecorationsStaticTool.cs`

Marks every `MeshRenderer` in the active scene that is not a dynamic gameplay object
(cubes, ducks) with `StaticEditorFlags.BatchingStatic`.

**How static batching works:** Unity bakes marked meshes into a single large vertex buffer at
build time or scene load. At runtime they are all drawn in one or a few draw calls instead of one per mesh.

**Steps:**
1. Open the game scene in edit mode.
2. Run **Mark Scene Decorations As Batching-Static**.
3. Press `Ctrl + S` to save.
4. Enter Play mode — check the Stats window.

**Expected gain:** Environment render cost drops from ~428 draw calls to ~10–20.

**Undo:** "Clear Batching-Static From Active Scene" in the same menu.

---

### Enable GPU Instancing On All Materials
**File:** `Assets/_Project/Scripts/Editor/ForceEnableGpuInstancingTool.cs`

Enables `GPU Instancing` on every material found under `Assets/_Project`.

**When to run:** After importing a new asset pack or after any bulk material change.

**Note:** When SRP Batcher is active, GPU Instancing is bypassed for SRP-compatible shaders.
Use `CubeGridInstancer` (`Graphics.DrawMeshInstanced`) for objects that need guaranteed instancing.

---

### Enable GPU Instancing On Selected
**File:** `Assets/_Project/Scripts/Editor/ForceEnableGpuInstancingTool.cs`

Same as above but only for materials selected in the Project window.

---

### Optimize Draw Calls NOW
**File:** `Assets/_Project/Scripts/Editor/DrawCallOptimizer.cs`

One-click shortcut that applies several optimizations in sequence:
1. Enables GPU Instancing on all materials.
2. Marks static objects as `BatchingStatic`.
3. Removes redundant light components.

**Warning:** Makes broad changes. Save the scene before running.

---

### Open Player Settings (verify Static Batching)
**File:** `Assets/_Project/Scripts/Editor/StaticBatchingPlayerSettingsCheck.cs`

Opens **Edit → Project Settings → Player** and prints a reminder to the Console.
Verify that *Static Batching* is checked under **Other Settings** for the Android build target.

---

## HoopFlow / Diagnostics

---

### Batch Diagnostic
**File:** `Assets/_Project/Scripts/Editor/BatchDiagnosticTool.cs`

An editor window that analyses the current rendering state in Play mode.

**Displayed information:**

| Field | Meaning | Target |
|---|---|---|
| Active Renderers | Total enabled MeshRenderers | — |
| With PropertyBlock | Renderers using MaterialPropertyBlock | **0** |
| enableInstancing=OFF | Materials with GPU Instancing disabled | **0** |
| Estimated Saves | Draw calls saved if instancing were fully working | as high as possible |
| Top Materials | Materials shared by the most renderers | instancing candidates |

**How to use:** Enter Play mode → **HoopFlow / Diagnostics / Batch Diagnostic** → click **Refresh**.

> **Key insight:** Any renderer with a `MaterialPropertyBlock` blocks GPU Instancing for its
> entire material group. The goal is "With PropertyBlock = 0".

---

### Canvas Auditor
**File:** `Assets/_Project/Scripts/Editor/HoopFlowCanvasAuditor.cs`

Scans every Canvas in all loaded scenes and reports UI performance problems.

**Detected issues:**

| Issue | Description |
|---|---|
| GraphicRaycaster on non-interactive Canvas | Wastes CPU every frame for pointer hit-testing |
| LayoutGroup count > 0 | Recalculates layout every frame — replace with anchors |
| Excess raycastTargets (> 5) | GraphicRaycaster builds a hitbox list each frame |

**Auto-Fix:** "Remove Unnecessary GraphicRaycasters" removes the `GraphicRaycaster` component
from every Canvas that has no `Selectable` children.

**Canvas splitting rule of thumb:**
- Background / decoration → one Canvas, never touched again.
- Score, timer, HUD → one Canvas, rebuilds only when those values change.
- World-space UI (name tags, tooltips) → one Canvas.

---

### RaycastTarget Auditor
**File:** `Assets/_Project/Scripts/Editor/RaycastTargetAuditor.cs`

Lists every `Graphic` (Image, Text, etc.) that has `raycastTarget = true` but has
no `Selectable` component (Button, Toggle, Slider, etc.).

**Why it matters:** `GraphicRaycaster` builds a sorted list of all raycast targets every frame.
Large lists measurably increase CPU cost on mobile.

**How to use:** Scan → select entries → "Disable Selected" → Save.

---

## HoopFlow / Build

---

### Shader Variant Analyzer
**File:** `Assets/_Project/Scripts/Editor/HoopFlowShaderVariantTool.cs`

Analyses how many unique keyword combinations (shader variants) are actually in use across
all materials in the project.

**Background:** URP compiles every keyword combination as a separate program that must be
loaded into GPU memory. Unused variants inflate the build and may cause a stutter spike on
first load when Unity compiles them on demand.

**Create ShaderVariantCollection:** Writes only the keyword combinations that are actually
used to `Assets/_Project/Settings/HoopFlow_ShaderVariants.shadervariants`.

**Integration:** Add the generated file to  
**Edit → Project Settings → Graphics → Preloaded Shaders**.  
Unity will compile those variants during the loading screen instead of during gameplay.

---

### Mobile Build Checklist
**File:** `Assets/_Project/Scripts/Editor/HoopFlowBuildChecklist.cs`

Validates 13 critical settings before you ship a release build. Items marked ⚡ can be auto-fixed.

| # | Check | Why |
|---|---|---|
| 1 | Scripting Backend: IL2CPP ⚡ | Mono is 3–5× slower on mobile |
| 2 | IL2CPP Code Stripping ≥ Medium ⚡ | Reduces binary size |
| 3 | Android Graphics API | Vulkan should be first |
| 4 | URP HDR Disabled ⚡ | HDR requires extra GPU pass on mobile |
| 5 | URP Shadow Distance ≤ 25 ⚡ | Shadow rendering is GPU-heavy |
| 6 | URP Dynamic Batching Enabled ⚡ | Merges small meshes sharing a material |
| 7 | SRP Batcher Enabled ⚡ | Single most impactful render setting |
| 8 | Texture Compression: ASTC ⚡ | Required for mobile GPU decompression |
| 9 | Development Build Disabled ⚡ | Development builds are significantly slower |
| 10 | Min API ≥ 24 | ASTC support guaranteed above API 24 |
| 11 | Fixed Timestep reminder | Manual check — 0.02 s (50 Hz) recommended |
| 12 | VSync Disabled ⚡ | Use `Application.targetFrameRate` on mobile |
| 13 | targetFrameRate reminder | Confirm set to 60 in your bootstrapper |

---

## HoopFlow / Sprites

---

### Create Sprite Atlas From Selected Folder
**File:** `Assets/_Project/Scripts/Editor/SpriteAtlasCreationHelper.cs`

Packs all sprites inside the currently-selected Project folder into a single `SpriteAtlas` asset.

**Settings applied automatically:**
- Texture Format: ASTC 6×6
- Max Size: 2048
- Tight Packing: On
- Allow Rotation: Off *(keeps sprite pivots correct at runtime)*
- Generate Mipmaps: Off

**How to use:** Select a sprite folder in the Project window → **HoopFlow / Sprites / Create Sprite Atlas From Selected Folder**.

---

### Create All UI Sprite Atlases (Auto)
**File:** `Assets/_Project/Scripts/Editor/SpriteAtlasCreationHelper.cs`

Automatically scans `Assets/_Project/Art/UI` and creates one atlas per sub-folder.
Run this after adding new UI sprites to keep atlases up to date.

---

## HoopFlow / Level

---

### Cube Level Builder
**File:** `Assets/_Project/Scripts/Editor/CubeGridPlacer.cs`

Drag-and-drop grid editor for placing cube puzzle levels in the scene.
Works with `CubeLevelPuzzleData` ScriptableObject assets.

---

### Particle Panel
**File:** `Assets/_Project/Scripts/Editor/ParticleSystemPanel.cs`

Inspector panel for quickly managing and previewing ParticleSystem components in the scene.

---

### Setup Full Duck Animator
**File:** `Assets/_Project/Scripts/Editor/DuckScaleAnimationGenerator.cs`

One-click setup that creates the Animator Controller and scale animation clips
on the selected Duck prefab.

---

### Setup Pool Path Manager
**File:** `Assets/_Project/Scripts/Editor/PoolPathSetupTool.cs`

Configures the `PoolPathManager` component and its required scene references automatically.

---

## HoopFlow / Scene

Scene-specific binding helpers. Each menu item performs a targeted setup operation for a particular scene:

| Menu Item | Purpose |
|---|---|
| Save Settings UI to Menu scene | Bakes the settings UI prefab into S_Menu |
| Add Settings button bindings | Wires button callbacks in S_Menu |
| Add music area to Menu scene | Sets up BGM for S_Menu |
| S_Water_Splash — add music area | Sets up BGM for the gameplay scene |
| S_Water_Splash — save Settings UI | Bakes the settings UI into the gameplay scene |

---

## Paxie / Case

Game-content pipeline tools. These are tied to the puzzle and level authoring workflow
and should remain under the Paxie menu.

| Menu Item | File | Purpose |
|---|---|---|
| New Cube Level Puzzle | PaxieCaseCreatePuzzleMenu.cs | Creates a new `CubeLevelPuzzleData` asset |
| Generate Palette Materials | PaletteMaterialGenerator.cs | Creates one material asset per palette color for the selected puzzle |
| Build Menu Stack Cards | MenuStackBuilder.cs | Rebuilds the card stack in the Menu scene |
| Setup Level Flow | LevelFlowSetupMenu.cs | Configures scene order, level catalog, and build settings |
| Reset Last Played Level | LevelFlowSetupMenu.cs | Clears last-played PlayerPrefs value |
| Clear Level Progress | LevelFlowSetupMenu.cs | Resets all level progress in PlayerPrefs |
| Repair Catalog | LevelFlowSetupMenu.cs | Resets the level catalog to a known-good state |
| Remove Missing Scripts | PaxieRemoveMissingScripts.cs | Strips null script references from all open scenes |

---

## Runtime Systems

---

### CubeGridInstancer
**File:** `Assets/_Project/Scripts/Level/CubeGridInstancer.cs`

Renders all registered cube renderers using `Graphics.DrawMeshInstanced`, completely
bypassing the SRP Batcher and Unity's per-object draw call overhead.

**How it works:**
1. `AttackReactiveCube.Awake()` calls `CubeGridInstancer.Register(renderer, material)`.
2. The instancer groups renderers by material and calls `DrawMeshInstanced` each frame,
   drawing up to 1 023 cubes per draw call.
3. Each registered cube's own `MeshRenderer` is disabled to prevent double-rendering.
4. When a cube is destroyed: `Deregister()` re-enables its `MeshRenderer` → DOTween animation plays normally.

**Performance:** 400 cubes × 4 colors = 400 draw calls without this system → **4 draw calls** with it.

---

### AttackReactiveCube — PrepareForSpawn pattern
**File:** `Assets/_Project/Scripts/AttackReactiveCube.cs`

Avoids `MaterialPropertyBlock` on spawned cubes (which blocks GPU Instancing) by using a
static pre-configuration pattern:

1. `CubeLevelRuntimeSpawner` calls `AttackReactiveCube.PrepareForSpawn(color, material)`.
2. `Instantiate(prefab)` triggers `Awake()`.
3. `Awake()` reads the static state and assigns the material directly to `sharedMaterial`
   — no `MaterialPropertyBlock` is ever set, so `CubeGridInstancer` can batch freely.

**Rule:** Never call `SetPropertyBlock` on a renderer that needs to be instanced.

---

### GcAllocationMonitor
**File:** `Assets/_Project/Scripts/Runtime/Performance/GcAllocationMonitor.cs`  
**Namespace:** `HoopFlow.Performance`

Periodically samples the managed heap and logs a warning when allocations within
the interval exceed a configurable threshold.

Only compiled in Development Builds and the Editor — zero overhead in release builds.

```csharp
// Start from any MonoBehaviour:
GcAllocationMonitor.Begin(this, intervalSec: 1f, thresholdKb: 5f);

// Stop when the scene unloads:
GcAllocationMonitor.End();
```

---

### HoopFlowObjectPool\<T\> and HoopFlowGoPool\<T\>
**File:** `Assets/_Project/Scripts/Runtime/Performance/HoopFlowObjectPool.cs`  
**Namespace:** `HoopFlow.Performance`

Zero-GC object pool for any class (`HoopFlowObjectPool<T>`) and a convenience wrapper
for Unity Components (`HoopFlowGoPool<T>`).

```csharp
// --- Generic pool ---
var pool = new HoopFlowObjectPool<ParticleSystem>(
    factory:     () => Instantiate(particlePrefab),
    onGet:       p  => p.gameObject.SetActive(true),
    onReturn:    p  => { p.Stop(); p.gameObject.SetActive(false); },
    onClear:     p  => Destroy(p.gameObject),
    initialSize: 8);

var vfx = pool.Get();       // borrow
pool.Return(vfx);           // return
pool.Clear();               // call in OnDestroy

// --- Component pool (shorter) ---
var duckPool = new HoopFlowGoPool<DuckJump>(duckPrefab, poolRoot, initialSize: 5);
var duck = duckPool.Get();
duckPool.Return(duck);
```

**Internals:** Stack-based → O(1) Get and Return. No per-frame allocation.

---

## Optimization Roadmap Summary

### Completed work

| Week | Improvement | Expected gain |
|---|---|---|
| 1 | URP settings (HDR off, shadow 20 m, dynamic batching on) | SetPass ↓ |
| 1 | Legacy Text → TextMeshPro migration | UI batch ↓ |
| 1 | MaterialKeywordStripper | SetPass ↓ 10–30 % |
| 1 | GPU Instancing enabled on duck / env / particle materials | Batch ↓ |
| 2 | CubeGridInstancer (DrawMeshInstanced) | 400 → 4 draw calls |
| 2 | Mark Scene Decorations As Batching-Static | 428 → ~15 draw calls |
| 2 | Canvas Auditor + GraphicRaycaster cleanup | CPU ↓ |
| 3 | Texture Audit — ASTC 6×6 on all textures | RAM ↓ 40–60 % |
| 3 | Audio Optimizer — Vorbis / ADPCM compression | RAM ↓ 50–70 % |
| 3 | GcAllocationMonitor + HoopFlowObjectPool | GC Alloc ↓ |
| 4 | ShaderVariantAnalyzer + prewarming collection | Load spike ↓ |
| 4 | Mobile Build Checklist (IL2CPP, ASTC, SRP Batcher) | Release readiness |

### Target metrics (mobile)

| Metric | Baseline | Target |
|---|---|---|
| Batches | 895 | < 150 |
| SetPass Calls | 50 | < 20 |
| Triangles | 48 k | < 50 k |
| CPU Main | 16 ms | < 12 ms (60 fps) |
| RAM | — | < 300 MB |

---

*Document location: `Assets/_Project/Docs/HoopFlow_Tools_Documentation.md`*
