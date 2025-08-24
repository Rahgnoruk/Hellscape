You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI:
- Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1

# Feature: UI Toolkit — Main Menu, HUD, Pause, Vignette, Inventory placeholders

## Goals
- Main Menu (Host/Join via Relay).
- In-Game HUD: Team Score, Personal Score, Player Health.
- Red damage vignette that flashes on hit.
- Pause/In-Game Menu (Resume + Exit to Main Menu).
- Inventory strip placeholders (slot0 Base weapon always; slots 1–3 empty for now).
- No gameplay logic changes. Light glue only to read existing net vars.

---

## A) Assets & Structure
Create folders:
- `Assets/Hellscape/UI/UXML/`
- `Assets/Hellscape/UI/USS/`
- `Assets/Hellscape/Scripts/Presentation/UI/`

Add these UI assets:

### 1) `Assets/Hellscape/UI/UXML/MainMenu.uxml`
A centered column:
- Title label “HELLSCAPE”
- Button **Host via Relay**
- Row: TextField (placeholder “JOIN CODE”) + Button **Join**
- Small status text (e.g., errors)
- Footer small text with build info

### 2) `Assets/Hellscape/UI/UXML/HUD.uxml`
- Top-left group: “Team: 000000” (team score), “You: 0000” (personal score)
- Top-right: Button **Menu**
- Bottom-left: Health bar + numeric “HP 100/100”
- Bottom-center: Inventory strip with 4 slots (slot0 labeled “Base”, slots 1–3 “Empty”)
- Fullscreen child `#DamageVignette` (display:none; style opacity driven)

### 3) `Assets/Hellscape/UI/UXML/PauseMenu.uxml`
A centered panel overlay:
- Label “Paused”
- Button **Resume**
- Button **Exit to Main Menu**

### 4) `Assets/Hellscape/UI/USS/ui-common.uss`
- Clean minimal theme; readable fonts; dark panels; rounded corners; spacing.
- Classes: `.panel`, `.button`, `.slot`, `.slot--active`, `.badge`, etc.
- `#DamageVignette` with red radial gradient, initially `opacity: 0; display: flex;`

---

## B) Scripts (Presentation/UI)

### 1) `MainMenuUI.cs`
Path: `Assets/Hellscape/Scripts/Presentation/UI/MainMenuUI.cs`

Responsibilities:
- Drives `MainMenu.uxml` via `UIDocument`.
- Wires **Host** to `RelayBootstrap.StartHostAsync()` and **Join** to `RelayBootstrap.StartClientAsync(joinCode)`.
- Uppercases/join-code sanitize.
- Hides itself when NetworkManager becomes server or client.
- Optional: expose a public `ShowError(string)`.

Implementation notes:
- `RequireComponent(typeof(UIDocument))`
- Look up elements by name: `#BtnHost`, `#JoinCode`, `#BtnJoin`, `#StatusText`.
- Reference a `RelayBootstrap` in scene via `[SerializeField]`.

### 2) `HudUI.cs`
Path: `Assets/Hellscape/Scripts/Presentation/UI/HudUI.cs`

Responsibilities:
- Controls `HUD.uxml` via `UIDocument`.
- Reads server net vars to display:
  - Team score: from `SimGameServer.netTeamScore`
  - Personal score: find entry in `SimGameServer.netScores` where `clientId == NetworkManager.Singleton.LocalClientId`
  - Wave index optional later (ignore for this pass)
- Health: for now, show a placeholder health provider (100/100). Provide a public API for later wiring:
  - `public void SetLocalHealth(int current, int max)` → updates bar & text.
- Inventory placeholders:
  - Slot0 label “Base” always highlighted.
  - Slots 1–3 show “Empty”.
  - Provide public methods (no logic yet): `SetSlot(int slotIndex, string label, int ammo, bool active)`
- **Menu** button toggles Pause overlay.

### 3) `PauseMenuUI.cs`
Path: `Assets/Hellscape/Scripts/Presentation/UI/PauseMenuUI.cs`

Responsibilities:
- Controls `PauseMenu.uxml` via `UIDocument`. Starts hidden.
- Buttons:
  - **Resume** → hide overlay.
  - **Exit to Main Menu**:
    - If Network running: `NetworkManager.Singleton.Shutdown();`
    - Show MainMenu; hide HUD; (no scene change required).
- Provide `Show(bool)`.

### 4) `VignetteFxUI.cs`
Path: `Assets/Hellscape/Scripts/Presentation/UI/VignetteFxUI.cs`

Responsibilities:
- Gets the `#DamageVignette` `VisualElement` from HUD doc.
- Method `public void Flash(float intensity = 1f, float hold = 0.06f, float fade = 0.25f)`
  - Sets opacity to `Mathf.Clamp01(intensity)` then fades to 0 over `fade` seconds.
- Expose `OnLocalPlayerDamaged(int amount)` helper to call `Flash()`.

> NOTE: We are not wiring real damage events in this pass. We only expose public methods. Hook later from whatever health component you use.

### 5) `UIBootstrap.cs`
Path: `Assets/Hellscape/Scripts/Presentation/UI/UIBootstrap.cs`

Responsibilities:
- Holds references to the three `UIDocument` components (MainMenu, HUD, Pause).
- Ensures a single persistent UI root:
  - `DontDestroyOnLoad(gameObject)`.
  - On app start: show **Main Menu**; hide HUD & Pause.
  - When network connects (server or client): hide **Main Menu**, show **HUD**.
  - When network stops: show **Main Menu**, hide others.
- Finds `SimGameServer` (if present) and subscribes to its net vars to update HUD (poll per-frame is fine to start).

---

## C) Minimal Glue (safe)

1) **Expose last join code for host (optional):**
Modify `RelayBootstrap.cs`:
- Add a public read-only property `public string LastJoinCode => lastJoinCode;`
- (No other changes.)

2) **SimGameServer accessor:**
If not present already, add a static helper:
```csharp
// in SimGameServer.cs
public static SimGameServer Active => Instance;
3. **HUD polling:** In `HudUI.Update()`, if `SimGameServer.Active != null` and `IsSpawned`, read `netTeamScore.Value` and scan `netScores` for local entry.

---

## **D) UXML element names (for code queries)**

* MainMenu: `#BtnHost`, `#JoinCode`, `#BtnJoin`, `#StatusText`

* HUD:

  * Scores: `#TeamScoreText`, `#PersonalScoreText`

  * Health: `#HealthBarFill`, `#HealthText`

  * Inventory slots: `#InvSlot0`, `#InvSlot1`, `#InvSlot2`, `#InvSlot3` (each contains `Label` name \+ ammo text `Label`)

  * Menu: `#BtnMenu`

  * Vignette: `#DamageVignette`

* Pause: `#BtnResume`, `#BtnExit`

---

## **E) Styling (USS essentials)**

`ui-common.uss`:

* Base font size 16; title 36–48.

* `.panel` padded, rgba(0,0,0,0.6), rounded 12, backdrop blur if available.

* `.button` hover/active states.

* `.slot` square 64–80px, border, center content; `.slot--active` brighter border.

* `#DamageVignette`: full stretch, pointer-events: none; background radial-gradient(red transparent); opacity 0\.

---

## **F) Scene Setup**

* Create a `UIRoot` GameObject in your bootstrap scene:

  * Add `UIBootstrap` (script).

  * Add 3 children with `UIDocument`: `MainMenuDoc`, `HudDoc`, `PauseDoc`.

    * Assign their source assets to the UXML files.

    * Assign the USS to each document (ui-common.uss).

  * Add `MainMenuUI` to `MainMenuDoc`, `HudUI` \+ `VignetteFxUI` to `HudDoc`, `PauseMenuUI` to `PauseDoc`.

  * Drag a reference to `RelayBootstrap` into `MainMenuUI`.

No need to split scenes; UI just hides/shows based on network state.

---

## **G) Acceptance**

* App boots to **Main Menu** with Host/Join; join code uppercased.

* Host starts game → menu hides, HUD shows. Client joins → menu hides, HUD shows.

* HUD shows **Team score** (0...), **Personal score** (0...), **HP 100/100** with a bar.

* Press **Menu** button → Pause overlay with **Resume** and **Exit**.

  * Resume closes overlay.

  * Exit shuts down network and returns to Main Menu.

* Call `HudUI.SetLocalHealth(57,100)` (via temporary debug key or script) updates health bar.

* Call `VignetteFxUI.Flash()` shows a brief red overlay.

* Inventory strip shows “Base” \+ 3 “Empty” slots; no logic yet.

---

## **H) Files to create/modify**

* `Assets/Hellscape/UI/UXML/MainMenu.uxml` (new)

* `Assets/Hellscape/UI/UXML/HUD.uxml` (new)

* `Assets/Hellscape/UI/UXML/PauseMenu.uxml` (new)

* `Assets/Hellscape/UI/USS/ui-common.uss` (new)

* `Assets/Hellscape/Scripts/Presentation/UI/MainMenuUI.cs` (new)

* `Assets/Hellscape/Scripts/Presentation/UI/HudUI.cs` (new)

* `Assets/Hellscape/Scripts/Presentation/UI/PauseMenuUI.cs` (new)

* `Assets/Hellscape/Scripts/Presentation/UI/VignetteFxUI.cs` (new)

* `Assets/Hellscape/Scripts/Presentation/UI/UIBootstrap.cs` (new)

* `Assets/Hellscape/Scripts/Net/RelayBootstrap.cs` (modify: `LastJoinCode` getter)