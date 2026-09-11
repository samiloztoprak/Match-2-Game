# Match-2 Blast Puzzle

A Toon Blast / Toy Blast–style match-2 blast puzzle, built in Unity as a case study project. Tap a group of two or more same-color blocks to clear them; large groups create power-ups (Rocket, Bomb, Ball) that combo and chain-react with each other for bigger effects. Reach the target score before you run out of moves to win the level.

## Gameplay Video

<!--
GitHub only renders an inline, playable video player for files uploaded through its own
attachment pipeline (a github.com/user-attachments/assets/... or user-images.githubusercontent.com
URL) — a <video> tag pointing at a plain committed repo file will NOT play inline. To finish this:

  1. Push GameVideo.mp4 to the repo (a normal `git add` + commit + push — GitHub Pages/attachments
     are not required for the file to exist in the repo, just for the inline player below).
  2. On github.com, open this README in the web editor (pencil icon) — or open any Issue/PR/Discussion.
  3. Drag GameVideo.mp4 from your file explorer directly into the comment/edit text box.
  4. GitHub uploads it and inserts a line like:
       https://github.com/user-attachments/assets/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
  5. Replace the src="..." URL below with that one, then delete this comment block.
-->
<video src="https://github.com/user-attachments/assets/REPLACE_WITH_UPLOADED_VIDEO_ID" controls width="600"></video>

Until the step above is done, the player above won't render — [GameVideo.mp4](GameVideo.mp4) is still viewable by clicking through (GitHub will show its own basic file-preview player, or download it) as a working fallback.

## Requirements

- **Unity 6000.4.4f1** (see `ProjectSettings/ProjectVersion.txt`) — open and run with this exact editor version.
- No external services, accounts, or API keys are required. The project is fully self-contained.

## Getting Started

1. Open the project folder in Unity Hub (Unity 6000.4.4f1).
2. Open `Assets/Scenes/MainMenu.unity` (or just press Play — the game boots into the main menu first, per Build Settings).
3. Press **Play**. Tap **Level 1** to start the level.

## How to Play

- **Tap a group of 2+ same-color blocks** to blast them. Score increases by 10 points per block cleared; a small "+10" flies from every blasted block up to the score display.
- **Match 6–7 blocks** of one color → creates a **Rocket** (clears its whole row or column when tapped).
- **Match 8–9 blocks** → creates a **Bomb** (clears a 3×3 area when tapped).
- **Match 10+ blocks** → creates a **Ball** (clears every block of one color, anywhere on the board, when tapped).
- A small icon previews which cells are "one tap away" from creating a power-up.
- **Tap a power-up** to activate it. If it's touching other power-ups, they combine into one bigger effect instead of firing separately:
  | Combo | Effect |
  |---|---|
  | Bomb + Bomb | 5×5 area |
  | Rocket + Rocket | full row + full column ("+") |
  | Bomb + Rocket | 3 rows + 3 columns (thick "+") |
  | Ball + Bomb | every block of the Ball's color turns into a Bomb and explodes |
  | Ball + Rocket | every block of the Ball's color turns into a Rocket and fires |
  | Ball + Ball | clears the entire board |

  Clusters of 3+ of the same kind still produce the same fixed-size effect as a pair (they don't scale up). If a blast happens to reveal another power-up, that one chain-activates too, even if it was never tapped directly.
- **Win** by reaching the level's target score before moves run out — a banner flies in, then the game returns to the main menu with the next level unlocked.
- **Lose** by running out of moves first — the game returns directly to the main menu and you retry the same level.
- Some levels scatter **Box** obstacles across the board — they block their cell and are never part of a color match. Tapping one directly does nothing; it only breaks when a power-up's blast (including a chained one) reaches it.
- There are 3 levels with an increasing difficulty curve (bigger board, fewer moves, higher target score); progressing past the last one keeps replaying the hardest.

## Project Architecture

Layered MVC, fully data-driven, with no game-rule logic living in a `MonoBehaviour`:

```
Assets/Scripts/Runtime/
├── Model/        Pure C# — no UnityEngine.MonoBehaviour dependency, fully unit-testable.
│   GridModel (1D-flattened board, BFS flood-fill, gravity, refill)
│   GameStateModel (score, moves, win/lose)
│   IGridPiece implementations: ColorBlockPiece, RocketPiece, BombPiece, BallPiece, BoxPiece
├── Data/         ScriptableObjects: BlockTypeData, BlockPaletteData, LevelData, LevelSet, PowerUpTypeData
├── View/         MonoBehaviours that only render/animate — GridView, BlockView, HudView,
│                 ScorePopupView, LevelWinView, LevelLoseView, BackgroundFitter, BoardCameraFitter
├── Controller/   GameFlowController (all game-rule orchestration), GridInputController, MainMenuController
├── Systems/
│   ├── Pooling   BlockPoolService (UnityEngine.Pool.ObjectPool<T>)
│   ├── Events    GameEvents — typed pub/sub (Observer pattern) decoupling Model from View
│   ├── Tween     IBlockAnimator seam — DoTweenBlockAnimator (real) / InstantBlockAnimator
│   │             (synchronous, used by tests) so DOTween is the only tween dependency, isolated
│   │             to one file
│   ├── Audio     SfxController — reacts to GameEvents to play procedurally-generated placeholder SFX
│   └── LevelProgress   PlayerPrefs-backed level counter
└── GameBootstrapper   Scene-root wiring (plain manual DI, no framework)
```

Design notes:
- The grid is a flattened 1D array (`GridUtility` owns all index↔(x,y) conversion) — chosen for cache-friendliness and simplicity over a 2D array.
- Power-ups and obstacles are just another `IGridPiece` implementation (see `BoxPiece`); `GridModel`'s core algorithms (flood-fill, gravity, refill) never need to change to add one, since anything non-matchable is automatically excluded from color matching and treated as solid for gravity purposes.
- All animation goes through `IBlockAnimator`, so gameplay logic can be tested completely deterministically (`InstantBlockAnimator` resolves every animation synchronously) without needing Play Mode or waiting on real tween timing.
- The board fits any device aspect ratio at runtime (`BoardCameraFitter`), and colors/difficulty/obstacle count are tunable per level via `LevelData` against a single shared `BlockPaletteData` — no per-level art duplication needed. `LevelSet` orders the levels the game progresses through.

See `CLAUDE.md` for a much more detailed, code-reference-level architecture breakdown (originally written as working notes, kept for anyone extending the project).

## Testing

35 EditMode unit tests in `Assets/Scripts/Tests/EditMode/`:
- `GridModelTests`/`GridUtilityTests` (21) cover the core grid rules — flood-fill matching, gravity, refill, power-up area/line/cross queries, group-size computation.
- `GameFlowControllerTests` (14) drive the full turn pipeline end to end with a real `GridView` and the deterministic `InstantBlockAnimator` — match scoring, every power-up creation threshold, every 2-piece combo, same-kind cluster sizing, multi-hop chain reactions, the Box obstacle, and win/lose events.

Run them via **Window → General → Test Runner → EditMode → Run All** in the Unity Editor, or headless:

```
Unity.exe -batchmode -projectPath <path> -runTests -testPlatform EditMode -testResults results.xml -quit
```

## What's Not Implemented (Known Scope Cuts)

This is a time-boxed case study; the following were deliberately left out rather than rushed:

- **More obstacle variety** (e.g. a multi-hit crate, a balloon that floats) — only a single-hit Box exists so far; the `IGridPiece` seam already supports adding more the same way.
- **Real audio** — the current SFX are procedurally synthesized placeholder tones (see `AudioClipGenerator`), not sourced/composed audio, and there's no music.
- **Settings/pause menu**, leaderboards, or any backend/analytics integration.

## Platform

Configured for **Android** (`applicationId com.match2.match2game`, min SDK 25, IL2CPP, ARM64, portrait-locked), but runs and iterates fine directly in the Unity Editor — switching platform only affects build output.
