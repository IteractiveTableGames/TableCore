# TableCore Architecture Document (v0.2)

Target runtime: Godot 4.x (.NET / C#)
Target platform: Windows fullscreen kiosk on multitouch coffee table
Hardware: Intel NUC driving 32" capacitive touch foil under glass
Primary input: Multi-touch (up to 10 contacts exposed by OS)
Primary display pattern: Flat board in fixed orientation, individual per-player HUDs rotated toward each player's edge

---

## 1. Goals and Principles

### 1.1 Functional Goals
- Allow multiple players sitting around the same physical table to:
  - Join a session.
  - Claim a seat along the physical edge of the screen.
  - Enter a display name / color / avatar.
- Maintain a canonical "session" that stores all active players and their seats.
- Allow users to choose a "module" (game / experience) and launch it.
- Run that module in a runtime environment where:
  - The board is always shown upright in a fixed orientation.
  - Each player sees a rotated HUD oriented toward their edge.
  - Touch input is attributed to the correct player.
- Provide a shared framework SDK for module authors:
  - Player info (identity, color, avatar, seat).
  - Turn order helpers.
  - Dice rolling.
  - Currency / funds handling.
  - Cards, decks, hands.
  - Board abstractions (grid boards, freeform maps, etc.).
  - Animation helpers (piece movement, highlights, etc.).
  - HUD creation per player.
  - State synchronization (player state → HUD; board state → visuals).

### 1.2 Non-Functional Goals
- Touch-first: gameplay is touch-based, multi-touch capable.
- HUDs rotate; board does not. We must not rotate the board based on turns.
- Clear extensibility path:
  - Different board types across games.
  - Games with hidden/private info (card hands, secret cash).
  - GM-style control / narrative systems (e.g. D&D DM seat).
- Reasonable module sandbox: modules should talk to the framework through defined interfaces, not rewrite core systems.
- Offline-first, local play. Future network companions are not blocked by the design.

---

## 2. System Overview

### 2.1 Major Subsystems
1. Lobby System
   - Seat claiming along edges.
   - Player naming / avatar / color selection.
   - Module (game) selection and launch.

2. Session Manager
   - Holds the active list of PlayerProfiles.
   - Stores SeatZone for each player so we know their edge and HUD rotation.
   - Stores which module is currently selected.
   - Persists session config (house rules, etc.).

3. Module Runtime
   - Instantiates the chosen module.
   - Injects SessionState + Framework Services.
   - Creates the per-player HUDs for that module (using the framework HUD API).
   - Routes touches: HUD vs board.

4. Framework SDK / Primitives Layer
   - Player model, turn order, dice, currency, cards, board abstractions, token controllers, animation services.
   - Per-player HUD API.
   - State sync helpers (e.g. "player X cash changed" → update HUD; "player X drew card" → update hand HUD).
   - Board interface for both grid/track style and freeform map style.

5. Module Store / Loader
   - Discovers installable modules.
   - Surfaces module metadata to the Lobby.
   - Instantiates module scenes on demand.

6. Input + Seat Mapping Layer
   - Maps physical touch events to their logical player, based on SeatZone.ScreenRegion.
   - Distinguishes "board" touches (global playfield) from "HUD" touches (private or player-specific).

---

## 3. Core Data Types

### 3.1 TableEdge
Players can only sit on one of four canonical edges. Each edge implies a 90° rotation for that player's HUD.
```csharp
public enum TableEdge {
    Bottom, // RotationDegrees = 0
    Right,  // RotationDegrees = 90
    Top,    // RotationDegrees = 180
    Left    // RotationDegrees = 270
}
```

### 3.2 SeatZone
Represents where a player sits and where their HUD should render.
```csharp
public class SeatZone {
    public Rect2 ScreenRegion { get; set; }      // HUD area (strip along edge)
    public TableEdge Edge { get; set; }          // which side of the table
    public float RotationDegrees { get; set; }   // 0,90,180,270 derived from Edge
    public Vector2 AnchorPoint { get; set; }     // initial long-press touch point
}
```
Rules:
- When a player claims a seat, we:
  - Compute their closest edge by comparing distance to screen bounds.
  - Snap them to that TableEdge.
  - Assign the canonical RotationDegrees.
  - Generate ScreenRegion as a sub-rectangle hugging that edge.

### 3.3 PlayerProfile
Represents one human at the table.
```csharp
public class PlayerProfile {
    public Guid PlayerId { get; set; }
    public string DisplayName { get; set; }
    public Color DisplayColor { get; set; }
    public Texture2D Avatar { get; set; }

    public SeatZone Seat { get; set; }

    public bool IsGameMaster { get; set; } // e.g. DM seat in future RPG modules
}
```

### 3.4 SessionState
Session data passed from Lobby → Module Runtime.
```csharp
public class SessionState {
    public List<PlayerProfile> Players { get; set; } = new();
    public ModuleDescriptor? SelectedModule { get; set; }

    public Dictionary<string, Variant> SessionOptions { get; set; } = new();
}
```

### 3.5 ModuleDescriptor
Metadata for a module.
```csharp
public class ModuleDescriptor {
    public string ModuleId { get; set; }
    public string DisplayName { get; set; }
    public Texture2D Icon { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public PackedScene EntryScene { get; set; }
    public Dictionary<string, Variant> Capabilities { get; set; } = new();
}
```

---

## 4. Lobby System

### 4.1 Responsibilities
- Player joins by touching/holding near an edge.
- SeatZone is created (edge, rotation, HUD strip).
- Player customizes name/color/avatar.
- Player is marked "ready".
- When minimum player count is satisfied, table can pick a module.

### 4.2 Flow
1. **Seat Claim Screen**
   - Screen shows prompts like "Touch and hold to join" along all four edges.
   - Player presses and holds ~1s.
   - System:
     - Detects hold.
     - Computes the nearest edge (Bottom/Right/Top/Left) by comparing distances to edges.
     - Creates SeatZone:
       - `Edge` = chosen edge.
       - `RotationDegrees` = 0/90/180/270.
       - `ScreenRegion` = HUD strip anchored to that edge.
       - `AnchorPoint` = touch point.
     - Creates temporary PlayerProfile and adds to SessionState.

2. **Player Personalization HUD**
   - Framework spawns a HUD panel for that player inside their ScreenRegion.
   - That HUD panel is rotated RotationDegrees.
   - HUD panel includes:
     - Text entry keyboard (custom on-screen keyboard).
     - Color picker.
     - Avatar picker.
   - We only honor one active touch per HUD at a time to avoid going past OS 10-contact limit while typing.

3. **Repeat for all players**
   - Other players can join on different edges.
   - (Future: allow multiple players per edge by subdividing ScreenRegion, but for v1 we assume one per edge.)

4. **Module Selection**
   - When at least MinPlayers are ready:
     - Show list/grid of modules discovered by ModuleLoader.
     - Tapping a module shows details (icon, summary, allowed player count, capabilities).
   - Press "Start Game" to launch.

### 4.3 Implementation Notes
- Lobby is `Lobby.tscn` with `LobbyController.cs`.
- `LobbyController` owns a `SessionState` instance.
- On "Start Game":
  - Fills `SessionState.SelectedModule`.
  - Hands `SessionState` to Module Runtime.

---

## 5. Input + Seat Mapping Layer

### 5.1 Purpose
Multi-touch comes in as raw touch points. We need to know:
- Is this touch intended for the board (global play area)?
- Or is this touch intended for a specific player's HUD / private UI?

### 5.2 InputRouter
A singleton-style node available to Lobby and to Modules.
```csharp
public class InputRouter : Node {
    public SessionState Session { get; set; }

    public override void _UnhandledInput(InputEvent @event) {
        // Pseudocode:
        // if @event is InputEventScreenTouch / InputEventScreenDrag:
        //   var player = ResolvePlayerFromPosition(@event.Position);
        //   if (player != null) emit PlayerHudTouch(player, @event)
        //   else emit BoardTouch(@event)
    }

    private PlayerProfile? ResolvePlayerFromPosition(Vector2 screenPos) {
        foreach (var p in Session.Players) {
            if (p.Seat.ScreenRegion.HasPoint(screenPos)) return p;
        }
        return null;
    }
}
```

### 5.3 Board vs HUD
- If touch is inside some player's SeatZone.ScreenRegion → route to that player's HUD.
- Else → route to the global board layer.

This keeps HUD interactions private-ish and lets modules enforce rules (e.g. only the current player can move their token).

---

## 6. Module Runtime

### 6.1 What is a Module?
- A self-contained Godot scene that implements the game logic / visual board / rules.
- The root of that scene must implement `IGameModule`.
- The framework will:
  - Instantiate the module scene.
  - Inject the SessionState + framework services.
  - Create / attach per-player HUDs for that module.
  - Set up InputRouter routing.

### 6.2 Module Lifecycle Contract
```csharp
public interface IGameModule {
    // called after scene instantiation, before first frame
    void Initialize(SessionState session, IModuleServices services);

    // optional per-frame update hook if the module wants it
    void Tick(double delta);

    // called when leaving the module (back to Lobby, etc.)
    void Shutdown();
}
```

### 6.3 IModuleServices
Framework-provided API surface modules call into. This is the SDK.
```csharp
public interface IModuleServices {
    // Player and turn info
    TurnManager GetTurnManager();
    IReadOnlyList<PlayerProfile> GetPlayers();

    // Dice, money, cards
    DiceService GetDiceService();
    CurrencyBank GetBank();
    CardService GetCardService();

    // Board helpers
    IBoardManager GetBoardManager();

    // UI / HUD helpers
    IHUDService GetHUDService();

    // Animation helpers
    AnimationService GetAnimationService();

    // Lifecycle helpers
    void ReturnToLobby();
    void SaveModuleState(object stateBlob);
    object? LoadModuleState();
}
```

The point: modules depend on `IModuleServices`. They don't poke global singletons unless absolutely necessary.

---

## 7. Framework SDK: Services and Primitives

This is the part module authors actually build on. We expand it here.

### 7.1 TurnManager
Tracks whose turn it is and advances turns.
```csharp
public class TurnManager {
    private List<Guid> _order = new();
    private int _currentIndex;

    public Guid CurrentPlayerId => _order[_currentIndex];

    public void SetOrder(List<Guid> playerIds, int startIndex = 0) { ... }
    public void AdvanceTurn() { ... }
}
```
Uses:
- Board games: linear turn order.
- RPG: "initiative" order.
- Can be extended to allow GM override.

### 7.2 DiceService
Centralized dice logic + optional dice roll animation.
```csharp
public class DiceService {
    private Random _rng = new();

    public int Roll(int sides) => _rng.Next(1, sides + 1);

    public int[] RollMultiple(int diceCount, int sides) { ... }

    public async Task<int[]> RollWithAnimation(int diceCount, int sides) {
        // 1. visually roll dice in BoardLayer / HUD
        // 2. return rolled values
    }
}
```
Allows modules to request dice rolls consistently, and to trigger shared dice animations.

### 7.3 CurrencyBank
Tracks currency / funds per player.
```csharp
public class CurrencyBank {
    private Dictionary<Guid, int> _balances = new();

    public int GetBalance(Guid playerId) { ... }
    public void Add(Guid playerId, int amount) { ... }
    public bool Transfer(Guid fromPlayer, Guid toPlayer, int amount) { ... }
}
```
Framework responsibility:
- Whenever CurrencyBank changes a player's funds, it should inform HUDService so that player's HUD updates live (see HUDService below).

### 7.4 Cards, Decks, Hands
We split data and presentation.

#### CardData
```csharp
public class CardData {
    public string CardId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Texture2D FrontImage { get; set; }
    public Texture2D BackImage { get; set; }
    public Dictionary<string, Variant> Metadata { get; set; } = new();
}
```

#### Deck
```csharp
public class Deck {
    private Stack<CardData> _cards;

    public void Shuffle() { ... }
    public CardData Draw() { ... }
    public void InsertBottom(CardData card) { ... }
}
```

#### Hand
```csharp
public class Hand {
    public Guid OwnerPlayerId { get; set; }
    private List<CardData> _cards = new();

    public IReadOnlyList<CardData> Cards => _cards;

    public void Add(CardData card) { ... }
    public void Remove(CardData card) { ... }
}
```

#### CardService
The CardService is part of the SDK exposed to modules. It helps with draw/discard/update.
```csharp
public class CardService {
    private Dictionary<Guid, Hand> _handsByPlayer = new();

    public Hand GetHand(Guid playerId) { ... }
    public void GiveCardToPlayer(Guid playerId, CardData card) { ... }
    public void RemoveCardFromPlayer(Guid playerId, CardData card) { ... }
}
```
Framework responsibility:
- On hand changes, CardService notifies HUDService so the owner's HUD can update their hand view (fan of cards, face-up only to that player).

### 7.5 HUDService (Per-Player HUD Layer)
This is critical. The HUD is now **module-driven**, not just framework-driven.

Goals:
- Each module must be able to define what each player's HUD looks like.
- HUD is anchored in SeatZone.ScreenRegion.
- HUD is rotated toward that player (0/90/180/270).
- HUD can include:
  - Funds display.
  - Hand of cards.
  - Action buttons ("Roll Dice", "End Turn").
  - Private info.

#### HUDService responsibilities:
- Create a HUD container for each player.
- Give the module a handle to draw into that HUD for that player.
- Provide API calls to update HUD elements when shared state changes (money, cards, prompts).

Possible API:
```csharp
public interface IHUDService {
    // Called by module during Initialize
    IPlayerHUD CreatePlayerHUD(PlayerProfile player);

    // Convenience updates
    void UpdateFunds(Guid playerId, int newAmount);
    void UpdateHand(Guid playerId, IReadOnlyList<CardData> cards);
    void SetPrompt(Guid playerId, string message); // e.g. "Your turn! Roll dice"
}
```

`CreatePlayerHUD` returns an object that lets the module customize layout or attach custom controls:
```csharp
public interface IPlayerHUD {
    Guid PlayerId { get; }
    Control GetRootControl(); // Godot Control node already positioned+rotated

    // Module can add custom UI nodes under here:
    void AddControl(Node controlNode);
}
```

Flow:
- Framework (ModuleRuntimeController) creates a HUDLayer.
- For each PlayerProfile in SessionState:
  - HUDLayer spawns a HUD root `Control` inside that player's SeatZone.ScreenRegion, rotated Seat.SeatZone.RotationDegrees.
  - HUDService wraps that Control in an IPlayerHUD.
  - Module gets these IPlayerHUDs in Initialize().

The module can now:
- Draw money, cards, buttons, etc. however it wants for each player.
- Call HUDService later to update funds/cards/etc. automatically when state changes.

This design lets different games present totally different per-player HUDs while still leveraging shared services for currency and cards.

### 7.6 Board Abstractions
Boards differ wildly:
- Track / tile loop (Monopoly-style).
- Hex / grid map.
- Freeform battle map (RPG, tactics).
- Abstract zones/areas (settlements, markets, etc.).

We cannot hardcode a single Board type. Instead, we define an interface and a manager.

```csharp
public interface IBoardManager {
    // Register the board scene that the module is using.
    void SetBoardRoot(Node2D boardRoot);

    // Token placement / movement
    void PlaceToken(Guid playerId, TokenController token, BoardLocation location);
    Task MoveToken(Guid playerId, TokenController token, BoardPath path);

    // Info lookup
    BoardLocation GetLocation(TokenController token);
    Vector2 GetWorldPosition(BoardLocation location);
}
```

Where:
```csharp
public struct BoardLocation {
    public string RegionId;    // e.g. "Tile_12", "Hex_A3", "Room_Throne"
    public Vector2 GridCoords; // optional for grids/hexes
}

public class BoardPath {
    public List<BoardLocation> Steps { get; set; }
}
```

Key idea:
- `BoardRoot` is supplied by the module. Could be a TileMap, a custom Node2D, a hex map, etc.
- The framework does *not* assume board topology.
- The module knows how to compute legal movement, turn rules, adjacency, etc.
- But once the module says "move token along this BoardPath", IBoardManager + AnimationService handle actually animating that token smoothly tile-to-tile.

Why this matters:
- Monopoly-style move: you generate a BoardPath of N squares around the loop, hand it to `MoveToken`, and you're done.
- Tactics map: you generate a path of reachable hexes, same call.
- RPG DM map: GM drags NPC token manually; IBoardManager still tracks where it ended up.

### 7.7 TokenController (Animated Pieces)
```csharp
public class TokenController : Node2D {
    [Export] public Guid OwnerPlayerId { get; set; }
    [Export] public string TokenName { get; set; }

    private AnimatedSprite2D _anim;

    public override void _Ready() {
        _anim = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public async Task PlayMovePath(List<Vector2> worldPoints) {
        _anim.Play("walk");
        foreach (var p in worldPoints) {
            await AnimateStepTo(p);
        }
        _anim.Play("idle");
        MaybeWave();
    }

    private Task AnimateStepTo(Vector2 target) { ... }
    private void MaybeWave() { ... }
}
```

The framework / AnimationService can help implement `AnimateStepTo()` in a consistent way.

### 7.8 AnimationService
Centralized animation helpers. This keeps module authors from rewriting tweens every time.
```csharp
public class AnimationService {
    public async Task AnimateMove(Node2D node, Vector2 from, Vector2 to, double durationMs) { ... }
    public async Task AnimateBounce(Node2D node) { ... }
    public async Task AnimateHighlight(Node2D node, Color color, double durationMs) { ... }
}
```

AnimationService will probably work in tandem with IBoardManager when moving tokens along a BoardPath.

---

## 8. Module Store / Loader

### 8.1 On-Disk Layout (example)
Each module lives under `./Modules/<moduleId>/` with:
- `module.json`
- `MainScene.tscn`
- assets/

Example `module.json`:
```json
{
  "moduleId": "com.tablecore.monopolyish",
  "displayName": "Monopoly-ish",
  "minPlayers": 2,
  "maxPlayers": 6,
  "entryScene": "res://Modules/com.tablecore.monopolyish/MainScene.tscn",
  "capabilities": {
    "usesTurns": true,
    "usesCurrency": true,
    "usesDice": true
  }
}
```

### 8.2 ModuleLoader
```csharp
public class ModuleLoader {
    public List<ModuleDescriptor> DiscoverModules() { ... }
}
```
- Called by Lobby to show available modules.
- Called by Runtime to instantiate the selected module.

---

## 9. Visual Layer / Scene Structure Summary

### 9.1 High-Level Node Layout in Runtime Mode
```
ModuleRuntimeRoot
 ├─ BoardLayer (Node2D, rotation = 0)
 │    ├─ BoardRoot (Node2D/TileMap/etc. provided by module)
 │    ├─ Tokens (TokenController instances)
 │    └─ ... other shared visuals ...
 └─ HUDLayer (Control / CanvasLayer)
      ├─ HUDContainer_PlayerA
      │    └─ PlayerHUD (rotated 0/90/180/270 based on SeatZone)
      ├─ HUDContainer_PlayerB
      │    └─ PlayerHUD (...)
      └─ ...
```
- BoardLayer:
  - Doesn't rotate for different players/turns.
  - Shows shared game state.

- HUDLayer:
  - Each player's HUD is positioned in that player's SeatZone.ScreenRegion.
  - Each HUD is rotated by SeatZone.RotationDegrees.
  - Module can populate each HUD differently via HUDService.

### 9.2 Lobby Scene Layout
```
LobbyRoot
 ├─ Background / "Touch and hold to join" hints
 ├─ SeatClaimController (handles touch, picks nearest edge, builds SeatZone)
 ├─ PlayerSetupHUDs (one per joined player)
 └─ ModuleSelectView (appears after min players joined)
```

---

## 10. Turn / Highlight Behavior

- TurnManager tracks whose turn it is.
- We NEVER rotate the board or camera when turns advance.
- Instead, we:
  - Pulse or glow that player's HUD (HUDService.SetPrompt / highlight effect).
  - Expose bigger buttons (e.g. "Roll Dice") in that player's HUD during their turn.

This keeps spatial consistency for the board while still making it obvious whose action is next.

---

## 11. Extensibility / Future

### 11.1 D&D / GM Mode
- `PlayerProfile.IsGameMaster` can mark a seat as the GM seat.
- HUDService can allow the module to draw special GM-only panels/buttons in that GM's HUD.
- BoardManager can be extended for fog-of-war / hidden tokens visible only to GM.

### 11.2 Private Info
- Hand rendering:
  - HUDService.UpdateHand() only renders card faces in that player's HUD.
  - Other players either see card backs or see nothing for that hand.
- Currency display per player is inherently private-ish in HUD.

### 11.3 Network / Companion Devices (future)
- IModuleServices.GetPlayers() + CurrencyBank + CardService define a clean state model.
- In future we can expose a websocket server that mirrors each player's HUD to that player's phone.
- GM seat could get full fog-of-war controls on their own device.

### 11.4 AI GM / Narrative Layer
- IModuleServices can later gain e.g. `DescribeGameState()` which returns a structured snapshot:
  - Tokens, locations, HP/gold/etc., open quests.
- AI layer can consume that snapshot to narrate events, spawn encounters, etc.
- GM HUD can surface those suggestions.

---

## 12. Team Deliverables

### Team A: Core Framework / Lobby / Session
- Implement `SessionState`, `PlayerProfile`, `SeatZone`, `TableEdge`.
- Implement Lobby scene + SeatClaimController.
  - Edge distance calc.
  - SeatZone.ScreenRegion allocation.
  - RotationDegrees = 0/90/180/270.
- Implement player personalization HUDs for name/color/avatar.
- Implement ModuleLoader + module selection UI.
- Implement SessionManager handoff from Lobby → Runtime.

### Team B: Runtime Shell / HUD Layer / InputRouter
- Implement ModuleRuntimeRoot scene.
- Implement BoardLayer (fixed orientation) and HUDLayer.
- Implement HUDService:
  - Spawn per-player HUD roots using SeatZone.ScreenRegion and RotationDegrees.
  - Expose IPlayerHUD to module.
  - Provide UpdateFunds / UpdateHand / SetPrompt convenience.
- Implement InputRouter:
  - Route touch events to HUD vs Board.
  - Expose events like PlayerHudTouch / BoardTouch.

### Team C: SDK Primitives / Game Services
- Implement TurnManager, DiceService, CurrencyBank, CardService.
- Implement TokenController + AnimationService.
- Implement IBoardManager interface + default BoardManager implementation:
  - SetBoardRoot()
  - PlaceToken()
  - MoveToken() with animation.
- Implement IModuleServices and a concrete ModuleServices class that wires everything together.

### Team D: Sample Module (Reference Game)
- Create a "Monopoly-ish" module:
  - BoardRoot scene with a loop of tiles.
  - TokenControllers for each player.
  - TurnManager integration.
  - Dice roll to advance tokens.
  - CurrencyBank to track money / rent / passing start.
  - HUDService integration:
    - Show each player's money.
    - Show that player's "Roll Dice" / "End Turn" buttons in their own rotated HUD.
    - Show that player's hand (e.g. property cards).
- This module acts as:
  - Proof-of-concept for SDK design.
  - Golden example for future module authors.

---

## 13. Summary / Enforcement Notes

- Players claim seats via edge touch. Closest edge wins. Rotation is locked to 0°, 90°, 180°, or 270°.
- BoardLayer is never rotated. The board always stays in one canonical orientation for everyone, like a physical board game.
- Each player gets a HUD in their SeatZone.ScreenRegion. That HUD is rotated toward them. HUD shows name, money, hand, action buttons, prompts.
- Framework exposes services (IModuleServices) so modules don't reinvent:
  - Turn logic
  - Dice
  - Currency
  - Cards / hands
  - HUD creation & updates
  - Token movement & animation
  - Board token placement

- Board flexibility:
  - IBoardManager + BoardLocation/BoardPath abstracts the idea of "where pieces are" without assuming Monopoly vs hex grid vs freeform map. Module supplies the board, framework handles movement/animation and state sync.

- Future work (GM tools, private info, AI DM, networked companions) is intentionally not blocked: we've already included PlayerProfile.IsGameMaster, per-player private HUDs, and service interfaces that can later serialize game state outward.

This document is now the canonical spec for TableCore v0.2 and should be used by engineering teams to begin implementation.
