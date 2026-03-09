using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MaykerStudio.Types;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;

namespace AI2DTool
{
    public class PathFollow_NavMesh :  PathFollow_Base
    {
        public PathFollow_NavMesh(EntityAI entity, MovementType movementType, float nextWaypointDistance, float JumpDuration, float DistanceToSlowDown, float SlowDownDuration, float Thrust, float MaxJumpDistanceX, float MaxJumpDistanceY, bool WaitTrigger1ForJump, bool StopMovementOnlanding, bool UseLedgeClimb, bool JumpLowHeightObstacles, bool UseTurnAroundAnim) : base(entity, movementType, nextWaypointDistance, JumpDuration, DistanceToSlowDown, SlowDownDuration, Thrust, MaxJumpDistanceX, MaxJumpDistanceY, WaitTrigger1ForJump, StopMovementOnlanding, UseLedgeClimb, JumpLowHeightObstacles, UseTurnAroundAnim)
        {
        }
    }
}