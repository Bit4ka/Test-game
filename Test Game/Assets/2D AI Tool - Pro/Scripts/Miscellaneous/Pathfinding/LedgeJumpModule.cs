using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI2DTool
{
    public class LedgeJumpModule
    {
        protected readonly PathFollow_Base pathFollow;

        protected Vector2 BottomCenter;

        public bool Break;

        public LedgeJumpModule(PathFollow_Base pathFollow)
        {
            this.pathFollow = pathFollow;

            BottomCenter = pathFollow.BottomCenter;
        }

        public void Update(Vector3[] Path, Vector2 currentPos, int i)
        {
            if (i + 1 < Path.Length)
            {
                Vector2 next = Path[i + 1];

                Vector2 dir = (next - currentPos).normalized;
                dir.x = (float)Math.Round(dir.x, 1);
                dir.y = (float)Math.Round(dir.y, 1);

                if (dir.y > -0.5f && dir.y < 1f && currentPos.y > pathFollow.Center.y + BottomCenter.y)
                {
                    if (pathFollow.CheckJumpGroundPosition(Path, i))
                    {
                        if (pathFollow.feetPosition.y < pathFollow.EntityAI.Collider.bounds.center.y + pathFollow.TopCenter.y)
                            pathFollow.IsLedgeJump = true;

                        Break = true;
                        return;
                    }
                }
            }
        }
    }
}