# Project

This is a small educational 3D game made with Godot and C#.

The project is being developed for two university internships.
The priority is simplicity, stability and demonstrability rather than
commercial-level complexity.

## Game concept

The player controls a small futuristic delivery drone.

Core gameplay loop:

1. Receive a delivery mission.
2. Fly to the pickup point.
3. Pick up cargo.
4. Fly to the destination.
5. Avoid obstacles and damage.
6. Deliver cargo.
7. Receive score based on time and damage.

## Technical requirements

Use Godot with C#.

Keep systems small and independent.

Main systems:

- PlayerController
- PlayerHealth
- MissionManager
- CargoSystem
- ScoreManager
- DifficultyManager
- WeatherManager
- GameManager
- UIManager
- SaveManager

The game must contain:

- one playable 3D level;
- several delivery missions;
- health and damage;
- win and lose conditions;
- timer;
- score;
- three difficulty settings;
- simple weather;
- main menu;
- HUD;
- pause menu;
- results screen;
- settings;
- local statistics saved as JSON.

## Development constraints

Do not overengineer.

Do not add features unless they directly help satisfy the university assignment.

Prefer readable C# over complex abstractions.

Keep the game playable after every milestone.

Before finishing a task:

- build the C# project;
- check for errors;
- update documentation when appropriate.

Do not replace working systems unnecessarily.
