# HoopFlow — Unity Diagnostic Export
**Tarih:** 15 Mayıs 2026  
**Kaynak:** Unity Editor tanılama pencereleri (Cube Level Builder hariç)

---

## 1. RaycastTarget Auditor

**Amaç:** Etkileşimli bileşeni olmayan ancak `raycastTarget` açık UI Graphic’leri bulur.

| Metrik | Değer |
|--------|-------|
| Şüpheli öğe | **0** |

---

## 2. Batch Diagnostic

### Genel istatistikler

| Metrik | Değer |
|--------|-------|
| Toplam renderer | 459 |
| Aktif renderer | 38 |
| Benzersiz paylaşılan material | 23 |
| Singleton material | 9 |
| Instancable material (x2+) | 12 |

### Sorunlar

- **12 renderer** `enableInstancing=OFF` durumunda.

### Tahmini kazanç

GPU instancing düzeltilirse yaklaşık **~25 draw call** tasarrufu.

### En çok instancable material’lar (potansiyel tasarruf)

| Material | Renderer sayısı | Potansiyel |
|----------|-----------------|------------|
| M_Circle_Float_2 | 5 | -4 draw |
| M_Foam | 5 | -4 draw |
| M_Duck_Beak | 5 | -4 draw |
| M_Duck_Eye | 5 | -4 draw |

---

## 3. Canvas Auditor

**Hiyerarşi:** UI → [Canvas] → [ScreenSpaceOverlay] (Graphics: 25)

**Uyarı:** Canvas rebuild CPU maliyetini azaltmak için `LayoutGroup(s)` yerine anchor kullanmayı düşünün.

---

## 4. Shader Variants

**Toplam benzersiz shader-keyword kombinasyonu:** 19

| Shader | Variant | Material |
|--------|---------|----------|
| Universal Render Pipeline/Lit | 3 | 33 |
| Universal Render Pipeline/Unlit | 2 | 2 |
| Legacy Shaders/Particles/Alpha Blended | 1 | 1 |
| Shader Graphs/SH_Flipbook | 2 | 2 |
| Shader Graphs/SH_Foam | 1 | 2 |
| Shader Graphs/SH_Water_Conveyor | 1 | 1 |
| Particles/Standard Unlit | 1 | 1 |
| Universal Render Pipeline/Particles/Unlit | 1 | 1 |
| Mobile/Particles/Additive | 1 | 1 |

---

## 5. Audio Optimizer

**Odak:** Mobil RAM optimizasyonu

| Öğe | Sample rate |
|-----|-------------|
| Courtside Drift | 22050 |

---

## 6. Texture Audit

**Odak:** ASTC formatı ve Read/Write ayarları (mobil optimizasyon)

### Sorunlar

| Texture | Sorun |
|---------|-------|
| T_Circle_Float | UI texture — mipmaps kapalı |
| T_Bubble_Sprite | UI texture — mipmaps kapalı |
| Tx_Ball | UI sprite max size > 1024 |
| Tx_Bg | UI sprite max size > 1024 |
| Tx_Sfx | UI sprite max size > 1024 |
| Tx_Sound Icon | UI sprite max size > 1024 |

---

## 7. Build Checklist

### Uyarı (turuncu)

| Kontrol | Durum |
|---------|-------|
| Android Graphics API | Manuel override tespit edildi — listede **Vulkan**’ın birinci olduğundan emin olun |

### Geçen kontroller (yeşil)

| Kontrol | Değer / durum |
|---------|----------------|
| Scripting Backend | IL2CPP |
| IL2CPP Code Stripping | Medium |
| URP: HDR | Disabled |
| URP: Shadow Distance | < 25 (mevcut: **20**) |
| URP: Dynamic Batching | Enabled |
| SRP Batcher | Enabled |
| Android Texture Compression | ASTC |
| Development Build | Disabled |
| Android Min API | >= 24 (mevcut: **25**) |
| Physics: Fixed Timestep | 0.02 / 50 Hz |
| VSync | Disabled (`Application.targetFrameRate` kullanılıyor) |
| Application.targetFrameRate | **60** |
| Editor scripts | 35 adet — Editor klasörlerinde doğru konumda |

---

*Not: Cube Level Builder penceresi bu export’a dahil edilmemiştir.*
