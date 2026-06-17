# Technical Artist Case — Vertigo Games

Bu proje, Vertigo Games Technical Artist case çalışmasını kapsamaktadır.

**Unity sürümü:** `6000.3.10f1`

## Sahneler

| Case | Sahne |
|---|---|
| Case 1 — Battle Pass UI | `Assets/_Project/Case_1/Scenes/BattlePass Scene.unity` |
| Case 2 — Weapon VFX | `Assets/_Project/Case_2/Scenes/Weapon_VFX_Scene.unity` |

İlgili sahneyi açarak **Play** düğmesine basmanız yeterlidir.

## Klasör yapısı

- **Case 1** — `Assets/_Project/Case_1/` — `Scripts/`, `ScriptableObjects/`, `Shaders/Hlsl/`, `Prefabs/UI/`, `Materials/`
- **Case 2** — `Assets/_Project/Case_2/` — `Scripts/`, `Materials/`, `Models/FBX/`, `Textures/`, `Atlas/`

---

## Case 1 — Battle Pass UI

Play başında premium kapalı gelir. Bazı düşük seviye ödüller test kolaylığı için önceden alınmış olabilir.

**Test edilmesi gerekenler:**

- **Kaydırma** — Yolu sol tuşla sürükleyiniz. Görünür kartlarda glow/pulse açık, dışarıdakilerde kapalı olmalıdır.
- **Claim** — Açık kartlara tıklayınız; tik, cüzdan güncellemesi ve kısa shine efekti çalışmalıdır.
- **Premium** — Sol paneldeki **GET** → offer burst → **CLAIM** ile premium açılır.
- **XP Skip** — `Btn_XP_Skip` ile elmas harcayıp bir seviye atlayınız; yol o seviyeye kaymalıdır.
- **Seviye & XP** — `BattlePassManager` üzerinden `Current Level` ve `Current XP` değiştirerek ilerlemeyi test edebilirsiniz.

Ödül ve yol verileri `ScriptableObjects/` altındaki asset'lerde tutulur (`Season1Data`, `DefaultPlayerProfile`, `Reward_*`).

---

## Case 2 — Weapon VFX

Silah showcase sahnesi; etrafındaki VFX efektleri bu case'de yer alır.

**Test edilmesi gerekenler:**

- **Kamera** — `CaseCameraController` ile sol tık döndürme, tekerlek zoom. Dokunmatikte pinch zoom desteklenir.
- **Weapon VFX** — `spcl_rif_mcx_topscorer` etrafındaki flow, wind, glow ve particle efektlerini inceleyiniz.
- **Arka plan** — `BackGround` gradient animasyonu ve `Weapon_Title` metni düzgün görünmelidir.

---

## Performans & Geliştirme

Viewport dışı kartlarda efektler kapatılır, yol batch spawn ile kurulur. VFX texture'ları Sprite Atlas'ta toplanmıştır; build'de draw call düşük kalır.

Kod tasarımı aşamasında yapay zeka desteğinden yararlanılmıştır. Geliştirme sürecinde **Cursor** ve **Antigravity** programları kullanılmıştır.
