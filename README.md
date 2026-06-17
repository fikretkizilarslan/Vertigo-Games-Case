# Vertigo Games — Battle Pass Case

Unity UI case study: scrollable Battle Pass road, premium unlock flow, currency fly animations, and mobile-minded rendering optimizations.

## Scenes

- `Assets/_Project/Case_1/Scenes/BattlePass Scene.unity` — main Battle Pass UI
- Case 2 — weapon showcase (separate scene)

## Startup behaviour

The road builds at runtime from tier data (~44 nodes). During build, layout driving is disabled so `HorizontalLayoutGroup` + `ContentSizeFitter` do not re-solve on every instantiate; the final layout is rebuilt once when all nodes are ready.

`startupRevealDuration` is set to **0** — the road appears instantly when ready (no fade-in).

In the Unity Editor, Play mode may still feel slightly slower than a device build because of domain reload. On a real build this cost is lower.

For production, this pattern would typically move to **object pooling** or **async preload** behind a loading screen; here it demonstrates runtime assembly and layout-safe batching for a portfolio slice.

## Notable systems

- `BattlePassVisualConfig` — ScriptableObject for card sprites, materials, and rarity colors
- `CurrencyWalletFlyAnimator` — gold / diamond / gem collect & spend animations
- Viewport-culled card glow to limit SetPass calls when premium is active
