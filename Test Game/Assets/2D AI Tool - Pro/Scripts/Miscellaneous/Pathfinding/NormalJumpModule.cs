using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI2DTool
{
    public class NormalJumpModule
    {
        protected readonly PathFollow_Base pathFollow;

        protected readonly EntityAI EntityAI;

        protected Vector2 TopCenter;

        public bool Break;

        public NormalJumpModule(PathFollow_Base pathFollow)
        {
            this.pathFollow = pathFollow;

            TopCenter = pathFollow.TopCenter;

            EntityAI = pathFollow.EntityAI;
        }

        public void Update(Vector3[] Path, Vector2 currentPos, int i)
        {
            if (i + 1 < Path.Length)
            {
                Vector2 nextPos = Path[i + 1];

                Vector2 dir = (nextPos - currentPos).normalized;
                dir.x = (float)Math.Round(dir.x, 1);
                dir.y = (float)Math.Round(dir.y, 1);

                if ((dir.x == 0f || dir.x <= -0.5f || dir.x >= 0.5f) && dir.y >= 0)
                {
                    if (pathFollow.CheckJumpGroundPosition(Path, i))
                    {
                        if (EntityAI.Rb.IsTouching(pathFollow.WallContact) && !pathFollow.WallCheck ||
                            pathFollow.feetPosition.y < EntityAI.Collider.bounds.center.y + TopCenter.y)
                            pathFollow.IsLedgeJump = true;

                        Break = true;
                    }
                }
                else if (dir.y < -0.1f)
                    Break = true;
            }
        }
    }
}