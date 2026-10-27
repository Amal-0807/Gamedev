# Block Blaster AR — Product Requirements Document

Oct 7, 2026 · @Amal

## Overview

Block Blaster AR is a mobile AR shooter: a small spaceship slides along the bottom of a virtual arena placed on a real table, shooting tetromino-shaped blocks that fall from the top. A block that touches the ship costs one life; three lives per game.

| Item | Detail |
| --- | --- |
| Project no. | 18 (originally listed as "AR Target Shooter") |
| Team | Amal Thomas (URK23CS7097), Basil Shaji (URK23CS7013) |
| Engine | Unity with AR Foundation |
| Concepts covered | Projectile motion, collision detection, raycasting, AR plane detection |

**Why AR, not a normal phone game:** the arena is anchored to a real surface, so the player can walk around it and view the falling blocks from any angle. The camera viewpoint is part of play: shapes are easier to judge from the side, so moving your body is a skill.

**Naming note:** "Tetris" is a trademark, so the game and UI use "blocks" or "tetrominoes", never the brand name.

## Goals and success criteria

The project succeeds if a first-time player can place the arena, play a full 3-life game, and see a final score, on a real phone, without a crash.

| Goal | Success measure |
| --- | --- |
| Stable AR placement | Arena stays anchored while the player walks around it; drift under \~5 cm in a lit room |
| Smooth play | 30+ fps on the target test phone during a busy wave |
| Fair hit detection | Every visible bullet-to-block contact registers; no bullets passing through blocks |
| Clear rules | A new player understands lives and power-ups within the first game, without explanation |
| Concept coverage | Each listed concept (projectile motion, collision, raycast) maps to working code you can point to in the viva |

**Non-goals (not in this version):** multiplayer, online leaderboards, in-app purchases, custom block editor, iOS and Android builds both (one platform only).

## Assumptions to confirm

These were chosen as defaults because they weren't decided yet. Each can change without breaking the rest of the design.

| Decision | Default chosen | Why |
| --- | --- | --- |
| Shooter position | Option B: virtual ship on a table-top arena | Confirmed by the team |
| Block reaching the bottom (missed, not hitting the ship) | Costs 10 points, no life lost | Keeps shooting meaningful without making misses feel unfair |
| Platform | Android with ARCore | Most common student device; no Mac or Apple developer account needed |
| Unity version | Unity 6 LTS with AR Foundation 6 | Current long-term-support pairing; confirm against lab machines |
| Arena size | 60 cm wide × 80 cm tall, scalable with pinch | Fits a desk; small enough to see whole arena on screen |
| Deliverable format | This PRD as the planning document | Check whether the college needs a specific template |
| Deadline | Not given; milestones below use relative weeks | Replace weeks with real dates once known |

## Core gameplay and rules

One game = survive falling waves of blocks for as long as possible with 3 lives, scoring points for every block destroyed.

**The loop, every second of play:**

1. A block spawns at the top of the arena at a random column and starts falling.
2. The player slides the ship left or right and fires upward.
3. A bullet that hits a block damages it; enough damage destroys it and adds points.
4. A block that touches the ship costs 1 life and is destroyed.
5. A block that reaches the bottom without touching the ship costs 10 points.
6. Every 30 seconds a new wave starts: blocks fall faster and spawn more often.
7. At 0 lives the game ends and shows the score and high score.

**Rules in detail:**

| Rule | Value |
| --- | --- |
| Lives per game | 3 (maximum 3, even with Extra Life drops) |
| Damage taken | 1 life per block that touches the ship |
| Invincibility after a hit | 1.5 s, ship flashes; prevents losing two lives to one cluster |
| Score per block | 4 cubes × 25 points = 100 per tetromino |
| Missed block penalty | −10 points, score never below 0 |
| Wave length | 30 s; fall speed +10% and spawn rate +15% per wave |
| Fire rate | 4 shots per second, auto-fire while holding |

**Edge cases to handle:**

- Bullet hits two blocks at once: only the first contact counts; the bullet is destroyed on hit.
- Block touches the ship during invincibility: block is destroyed, no life lost.
- AR tracking lost mid-game: game auto-pauses and shows "Move phone slowly to find the table".
- App sent to background: game pauses; resumes only on player tap.

## Features

P0 features make the game playable and must ship; P1 make it good; P2 only if time allows.

| Feature | Priority | Description |
| --- | --- | --- |
| Table placement | P0 | Detect a horizontal plane, tap to place the arena |
| Ship movement | P0 | Drag to slide the ship left and right along the arena floor |
| Shooting | P0 | Auto-fire upward while holding; physics bullets with collision |
| Falling tetrominoes | P0 | The 7 standard shapes (I, O, T, S, Z, J, L), random column and rotation |
| Lives and game over | P0 | 3 lives, hit flash, game-over screen with score |
| Score and high score | P0 | Live score on screen; best score saved on the device |
| Waves | P1 | Speed and spawn rate rise every 30 s |
| Chip-off damage | P1 | Each cube of a tetromino is hit separately, so shapes break apart |
| Power-ups | P1 | Dropped by glowing blocks; see below |
| Effects and sound | P1 | Particle burst on hit, explosion on destroy, shot and hit sounds |
| Armoured blocks | P2 | Metallic cubes that take 2 hits |
| Charged shot | P2 | Hold-to-charge arcing shot with area damage (shows projectile motion) |

**Power-ups (P1)** — about 1 in 8 blocks glows; destroying it drops a power-up the ship collects by touching it.

| Power-up | Effect | Duration |
| --- | --- | --- |
| Shield | Absorbs the next block hit, no life lost | Until used |
| Extra Life | +1 life, capped at 3 | Instant |
| Triple Shot | Fires 3 bullets in a spread | 8 s |
| Slow-Mo | All falling blocks move at half speed | 5 s |

## Controls and player journey

All input is one finger on the screen, so the other hand can hold the phone steady.

| Action | Input |
| --- | --- |
| Place arena | Tap on a highlighted table surface |
| Resize arena | Pinch, only before the game starts |
| Move ship | Drag finger left or right anywhere on the lower half of the screen |
| Shoot | Auto-fires while the finger is down |
| Pause | Pause button, top-right |

**Screens, in order:**

1. **Start** — title, Play button, high score.
2. **Scan** — "Point at a table and move slowly"; detected surfaces glow.
3. **Place** — tap to drop the arena; pinch to resize; Start button.
4. **Countdown** — 3, 2, 1 above the arena.
5. **Play** — lives (3 hearts), score, active power-up timer.
6. **Pause** — Resume, Restart, Quit.
7. **Game over** — final score, high score, Play Again (keeps the arena in place), Menu.

## Technical design

The arena is one parent object anchored to the table, and all gameplay runs in the arena's local space, so the game works the same at any position or scale.

**Where each listed concept lives in the code** (what to show in the viva):

| Concept | Where it is used |
| --- | --- |
| Raycast | (1) `ARRaycastManager` turns the placement tap into a point on the real table. (2) While playing, a ray from the finger hits the arena's floor plane, and its x position becomes the ship's target x. |
| Collision | Bullets and blocks use colliders with `OnCollisionEnter`; ship-block contact uses a trigger collider. Bullets use Continuous collision detection so fast shots can't pass through thin cubes. |
| Rigidbody | Bullets get a fixed upward velocity; falling blocks are kinematic and moved at a set speed so fall speed stays exact per wave. |
| Projectile motion | Power-up drops fall and arc under scaled gravity; the P2 charged shot uses a launch angle and gravity. Plain bullets fly straight, as in a space shooter. |
| Particle System | Hit sparks, block explosions, ship hit flash. |

**Main scripts:**

| Script | Job |
| --- | --- |
| `ArenaPlacer` | Plane detection, placement tap, pinch to scale, hides plane visuals after placement |
| `GameManager` | Game states (scan, place, countdown, play, pause, over), lives, score, waves |
| `ShipController` | Touch-to-ray input, smooth sliding, firing, invincibility flash |
| `BlockSpawner` | Picks shape, column and rotation; spawn timing per wave |
| `Tetromino` | Holds 4 `BlockCube` children; destroyed when all cubes are gone |
| `BlockCube` | Health, takes damage, reports hits, spawns particles |
| `Bullet` | Velocity, lifetime, destroy on hit; pooled for performance |
| `PowerUp` | Drop physics, pickup, timed effects |
| `UIManager` | HUD, menus, high score via `PlayerPrefs` |

**Performance rules:** pool bullets and cubes instead of creating and destroying them; cap live blocks at 20; keep one directional light and simple unlit or mobile shaders.

## Milestones and team split

Eight weeks in four 2-week phases; Amal owns AR and UI, Basil owns gameplay, and both meet at each gate to merge and test on the phone.

&#91;embedded content: 8-week plan · 2 tracks, 3 gates\]

The split keeps both members unblocked: gameplay can be built in a normal Unity scene first and dropped into the AR arena at gate 1. Weeks are relative; swap in real dates once the deadline is known.

## Risks and open questions

The biggest risk is AR setup on the test phone, so it is tested in week 1, before any gameplay is built.

| Risk | Mitigation |
| --- | --- |
| Test phone doesn't support ARCore | Check Google's ARCore supported-devices list in week 1; borrow a supported phone if needed |
| Arena drifts or jumps | Use an AR anchor on placement; test in good lighting on a textured table |
| Finger blocks the view of the ship | Drag input works anywhere on the lower half, not on the ship itself |
| Small blocks hard to see at desk distance | Bright outlined cubes; arena scales with pinch |
| Frame drops in late waves | Object pooling and a 20-block cap from the start |
| Viva question: "where is projectile motion?" | Power-up drops and charged shot use real gravity arcs; explain why plain bullets fly straight |

**Open questions:**

- [ ] Is the missed-block penalty (−10 points) right, or should a miss cost a life?
- [ ] Which test phone, and is it on the ARCore supported list?
- [ ] Unity version installed on the lab machines?
- [ ] Submission deadline and required report format?
- [ ] Final game name?
