All notable changes to this project will be documented on Devlog posts and the ChangeLog file. This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). 
This changelog refers to the Pro version of the tool.

## [2.0.0] - 2025-01-27
**Changed**
* Updated dependencies package and removed them from the Assets folder. They are now fetched using Unity's package manager.
* Improved 2D Platform Jumping AI.
* QoL changes.

## [1.3.3] - 2022-03-08
**Changed**
* Fixed issues with Pathfollow script where flying enemies weren't flipping to directions.
* Fixed bug where AI movement setup was not done correctly for non-tiled games.


## [1.3.2] - 2022-02-06
**Changed**
* Fixed AI Jump issue with non-tiled games


## [1.3.1] - 2022-01-08

**Changed**
* Fixed error preventing from building
* Fixed AITool noodles arrows rendering issue

## [1.3.0] - 2021-12-04

**Added**
* Special node called "Cooldown Alternate Node", acts like a normal state but only has a logic to branch transition between two states based on cooldown.
* Special node called "Check Targets Detected", acts like a normal state but only has a logic to branch transition between two states based on target detection.
* Special node called "Check Health Amount", acts like a normal state but only has a logic to branch transition between two states based on the entity's current health.
* New optional "Aggro mechanic" for all states.
* New options of animations for "Patrolling" state.
* New options of missile movement for "MissileProjectile" script.
* New options for combat states for knockback, now each state can have its own knockback values.
* New option for tag selection on "E_Attacks" state.
* New script called "Grenade" that can be used by the new state "Throw grenade".
* New State called "Throw grenade" that uses the new script Grenade, suitable for platformer 2D entities.
* New State called "Hit scan" that shoots to a target using hit scene technique.
* New State called "Avoid Target" that can be used by flying or Top-down 2D entities, it uses a steering behavior and calculations to avoid targets around the scene.
* New State called "Keep Distance", similar to the "Avoid target" but it is better suitable for Platformer 2D entities, it uses a simple but smart technique to keep an amount of distance from the target.
* New State called "Wandering state" that can be used by flying or Top-down 2D entities to auto set target destination around the scene with tons of settings to be tweaked.
* New State called "Block damage state" that allows entities to block damage based on direction.
* New State called "Heal state" that allows entities to heal themselves using animation and spawn particle system.
* New pathfinding solution for Platformer 2D characters that use auto jump destination calculation, support for timed animation, and ledge climb. It's available in states that use pathfinding algorithms.
* New script to optimize NavMesh (or Gridgraph mesh for Astar) that can make the pathfinding for platformer 2D characters more suitable.
* New slope movement for Platformer 2D characters.
* New noodle rendering for the Node-editor.
* Update xNode dependency scripts

**Changed**
* Fixed issues with "E_DodgeState" script.
* Fixed issue with all the event-based nodes scripts, the ones that start with "On..".
* Fixed bug with "Node.cs" from the xNode dependency.
* Replaced "Damage hop" with "knockback" logics for combat states.
* Replaced "Pathfollow" class with a robust and more efficient one called "Pathfollow_base".


## [1.2.1] - 2021-06-11

**Added**
* New option for the E_DodgeState to flip the character if he detects a ledge behind it, to prevent it from falling when dodging too close to a platform ledge.
* New option for the E_DashAttack to stop the character if he detects a ledge in front of it, to prevent it from falling when dashing too close to a platform ledge.
* New option for the E_Attacks to flip the character to target directions or not.
* New option for the Projectiles script to use Tags alongside the layers.

**Changed**
* Fixed a bug where the A.I. wasn't working on Editor if the experimental option 'Reload Domain' is disabled in Project settings.
* Fixed a bug where the A.I. wasn't working on Build.
* Fixed projectile script bug that was hitting the target some frames ahead.
* Changed how the E_DashAttack damage targets throughout the state.


## [1.2.0] - 2021-06-01

**Added**
* Special node called "PlayAnimation", acts just like a normal state but only plays animations.
* Special node called "OnPlayerInput" that is triggered by an internal event when the player press the specified input, work with both Old and New Input managers.
* Special node called "OnObjectsClose" that is triggered by an internal event when the entity is inside a specified collider's trigger or inside the OverlapCircle.
* Special node called "OnEntityHealth" that is triggered by an event when the entity's health is bellow or equal to a specified percentage or value.
* Special node called "OnDamageDirection" that is triggered by an event when the entity receives damage from a specified direction.
* An entity state node called "E_ChangeObjectsStats", that can be used to change sprites, materials, layers, tags, and some others stats of objects in the scene with specified tags.
* A component called "EntityInteractable" that is required for triggering the OnObjectsClose nodes, also uses Unity events to trigger logic in the inspector.
* A component called "EntityInput" that is required for triggering the OnPlayerInput nodes, also uses Unity events to trigger logic in the inspector.
* New API calls for the EntityGraph, which retrieves the new event-based nodes.
* Internal methods to the EntityAI that is used to handle the new special nodes.
* New API call for the Entity for checking the facing direction.
* The demo scenes now supports both New and Old Input manager.

**Changed**
* Add a new layer set up for the demo scenes: "Player Projectiles". To split both projectiles so they can collider with both ground and enemies.
* Refactor of Projectile script, now it uses OnTrigger events to damage targets and an internal OverlapCircle to detect the next frame position, preventing the projectile to pass through colliders.
* Optimized the EntityGraph APIs to retrieve nodes from the graph, by adding a dictionary call instead of Linq queries.
* Fixed issue with the entities facing directions when they return from the pool.
* Fixed issue with rotation transform of entities in Top-down mode.
* Fixed issue with FollowTarget, and FollowPartner states where they could lose the target and return a generic error.
* Fixed the name of the EntityGraph windows, where they were not displaying the correct name of the file.
* Fixed animations of demo scene enemies.
* Changed the min value of the rotation speed on EntityData.
* Changed the path to the Editors windows of EntitySystemEditor and Help file.
* Changed folder name of the Editor windows.
* Removed unused Odin serialized directives.

## [1.1.0] - 2021-05-21

**Added**
* A special node called "MultiTransitionalNode" allows states to transition to multiple states in a specified order.
* New Type of projectiles called MissileProjectile, that follows the target and can be damageable if layers are properly set.
* New sprites in demo scenes for projectiles.
* Better debugs warning and error logs in NavMesh-based states to better inform problems with missing NavMesh, and targets outside NavMesh.
* New bool type option on E_ShootProjectile.cs called "instantFirstShoot", where if "true" the state will shoot the projectile on state enter instead of waiting for "delayBetweenShoots".
* New example of how to use the MultipleTransitionalNode on the "Shooter" entity in the Platformer demo scene.

**Changed**
* Some Projectiles.cs Private variables encapsulation to Protected for using they on Parent classes.
* Optimization on E_DodgeState.cs, like caching, internal class, and cleaning code.
* Clamped value of the float variable "delayToExitState" to 0f minimum in E_SpawnObject.cs.
* Clamped value of the float variable "delayBetweenShoots" to 0f minimum in E_ShootProjectile.cs.
* Clamped value of the float variable "delayToExitState" to 0f minimum in E_ShootProjectile.cs.
* Clamped value of the int variable "maxShootsInState" to 1 minimum in E_ShootProjectile.cs.
* Fix missing initialization of the targets' layer mask on Entity.cs OnEnable method.
* Fix bug where the state E_ShootProjectile.cs could be stuck if the animation played too fast.
* The delay between shoots now will only start when the shooting animation finishes.

## [1.0.0] - 2021-05-17

* First Release