# Don't Let the Wisp Die

> **Survive the darkness. Feed the light. Complete the fear**

**Don't Let the Wisp Die** is a survival horror game built in Unity. The player explores a dark forest , guided only by a fading companion Wisp. The goal is to retrieve four objects on map and restore them to the central Beacons.

---

## Gameplay Features

### The Companion Wisp & Light Management
*   **Your Lifeline:** The Wisp follows the player, providing a dynamic blue light source.
*   **Fading Hope:** The Wisp's energy decays over time, reducing the light radius.
*   **Emotion System:** The Wisp’s facial expression changes based on proximity to objectives and fear levels.
*   **Recharge:** Players must locate **Tombs** scattered across the map to collect souls and restore the Wisp's energy.
*   **Flashlight:** A secondary, directional light source controlled by the player for precise visibility.

### Exploration & Objectives
*   **The Hub:** The game begins at a central **Beacon**.
*   **Sub-Scene Dimensions:** Upon reaching a objective area (Gateway), the player is teleported to a separate sub-area (loaded additively) to retrieve the artifact.
*   **The Ritual:** Artifacts must be physically carried back to the Beacon one by one.

### Escalating Threat System
*   **Dynamic Spawning:** The forest starts empty.
*   **Evolution:** Every time an objective is completed and returned to the Beacon, a **new type of monster** is released into the map.
*   **Punishing Death:** If the player dies, they respawn at the Beacon, but **one random completed objective is reset**, forcing the player to retrieve it again.

---

## Technical Architecture

### Core Systems
*   **ScriptableObject Architecture:** Heavy use of SOs for **Variables** (State, Energy), **Event Channels** (Game Events, Audio Events), and **Runtime Sets** (Tracking active 
objectives/monsters).
*   **Dependency Injection:** Systems communicate via abstract data containers (Anchors/Channels) rather than direct Singleton references (`transform.Find` is strictly avoided).
*   **Additive Scene Management:** Interior objective areas are loaded/unloaded asynchronously to manage performance and world state.

### AI & Behavior
*   **Unity Behavior (Behavior Graphs):** Used for complex monster logic (e.g., The **DirtyMonster** which crawls on the ground, climbs trees, watches for the player, and pounces).
*   **GOAP (Goal Oriented Action Planning):** Used for the **Drunk** enemy to dynamically choose between patrolling, investigating noises, or chasing the player based on sensory data.
*   **Sensory System:** AI utilizes sight cones (dot product/raycast) and sound detection (listening to the Trace System).

### Visuals & Polish
*   **2.5D Rendering:** 2D Sprite characters reacting to 3D lighting and physics.
*   **Procedural Animation:** Monsters (DrunkMonster) use procedural movement for grabing surrond tree and moving.
*   **Trace System:** Players leave "Traces" (footsteps/noise) that fade over time, which the AI tracks.

---

## Controls

| Key | Action |
| :--- | :--- |
| **W, A, S, D** | Movement |
| **Shift** | Sprint (Consumes Energy) |
| **Space** | Jump |
| **F** | Toggle Flashlight |
| **E** | Interact (Open Doors, Pick up Items) |
| **Mouse** | Aim Flashlight / Look |

---

## Where to play

Download from itch.io: https://khoaitayden.itch.io/dont-let-wisp-die

---

## Enemy Types for now

*   **Drunk Monster:** Patrols the forest floor. Investigates sounds and tracks player footsteps.
*   **Eye Monster:** Spawns based on light levels. Looks for the player; if spotted, alerts other monsters.
*   **Dirty Monster:** Climbs trees and waits in ambush. Pounces on the player from above, shaking the camera and blocking vision.

---

## Credits

*   **Developer:** Nguyen Khoai
*   **Art and model:** Asset from itch.io and selfmade
*   **Engine:** Unity
*   **Architecture Pattern:** SO-Architecture / Event-Driven Design

## Heads up
*   Asset is all free loyalty and self made (No AI involed)
*   Big chunk of the code is make by AI 
