using MaykerStudio.Types;
using UnityEngine;

namespace MaykerStudio
{
    public static class ExtDebug
    {
        public static void DrawPathFinding(Vector3[] corners)
        {
#if UNITY_EDITOR
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 c1 = corners[i];

                if (i + 1 < corners.Length)
                {
                    Vector3 c2 = corners[i + 1];

                    Debug.DrawLine(c1, c2, Color.green, 0f);
                }
            }
#endif
        }

        static private Vector2 DirFromAngle(float angleInDegrees, AI2DTool.EntityAI entity)
        {
            angleInDegrees -= entity.targetCheck.eulerAngles.z;

            return new Vector2(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad) * entity.FacingDirection, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        public static void DrawDetectionType(AI2DTool.EntityAI entity, DetectionType type, float FOV, float distance, Color c = default)
        {
#if UNITY_EDITOR
            if (c == default)
                c = Color.blue;

            switch (type)
            {
                case DetectionType.Circle:
                    DrawEllipse(entity.targetCheck.position, distance, distance, 16, c);
                    break;
                case DetectionType.Ray:
                    Debug.DrawRay(entity.targetCheck.position, Vector2.right * distance * entity.FacingDirection, c);
                    break;
                case DetectionType.FOV:
                    Vector2 viewAngleA = DirFromAngle(-FOV / 2, entity);
                    Vector2 viewAngleB = DirFromAngle(FOV / 2, entity);

                    Debug.DrawLine(entity.targetCheck.position, (Vector2)entity.targetCheck.position + viewAngleA * distance);
                    Debug.DrawLine(entity.targetCheck.position, (Vector2)entity.targetCheck.position + viewAngleB * distance);

                    DrawEllipse(entity.targetCheck.position, distance, distance, 32, c);
                    break;
                default:
                    break;
            }
#endif
        }

        public static RaycastHit2D BoxCast(Vector2 origen, Vector2 size, float angle, Vector2 direction, float distance, int mask, float duration = 0f)
        {
#if UNITY_EDITOR
            RaycastHit2D hit = Physics2D.BoxCast(origen, size, angle, direction, distance, mask);

            //Setting up the points to draw the cast
            Vector2 p1, p2, p3, p4, p5, p6, p7, p8;
            float w = size.x * 0.5f;
            float h = size.y * 0.5f;
            p1 = new Vector2(-w, h);
            p2 = new Vector2(w, h);
            p3 = new Vector2(w, -h);
            p4 = new Vector2(-w, -h);

            Quaternion q = Quaternion.AngleAxis(angle, new Vector3(0, 0, 1));
            p1 = q * p1;
            p2 = q * p2;
            p3 = q * p3;
            p4 = q * p4;

            p1 += origen;
            p2 += origen;
            p3 += origen;
            p4 += origen;

            Vector2 realDistance = direction.normalized * distance;
            p5 = p1 + realDistance;
            p6 = p2 + realDistance;
            p7 = p3 + realDistance;
            p8 = p4 + realDistance;


            //Drawing the cast
            Color castColor = hit ? Color.red : Color.magenta;
            Debug.DrawLine(p1, p2, castColor, duration);
            Debug.DrawLine(p2, p3, castColor, duration);
            Debug.DrawLine(p3, p4, castColor, duration);
            Debug.DrawLine(p4, p1, castColor, duration);

            Debug.DrawLine(p5, p6, castColor, duration);
            Debug.DrawLine(p6, p7, castColor, duration);
            Debug.DrawLine(p7, p8, castColor, duration);
            Debug.DrawLine(p8, p5, castColor, duration);

            Debug.DrawLine(p1, p5, Color.white, duration);
            Debug.DrawLine(p2, p6, Color.white, duration);
            Debug.DrawLine(p3, p7, Color.white, duration);
            Debug.DrawLine(p4, p8, Color.white, duration);
            if (hit)
            {
                Debug.DrawLine(hit.point, hit.point + hit.normal.normalized * 0.2f, Color.green);
            }

            return hit;
#else
            return default;
#endif
        }


        //Draws just the box at where it is currently hitting.
        public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color)
        {
            origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
            DrawBox(origin, halfExtents, orientation, color);
        }

        //Draws the full box from start of cast to its end distance. Can also pass in hitInfoDistance instead of full distance
        public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color)
        {
            direction.Normalize();
            Box bottomBox = new Box(origin, halfExtents, orientation);
            Box topBox = new Box(origin + direction * distance, halfExtents, orientation);

            Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color);
            Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
            Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
            Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color);
            Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color);
            Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
            Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
            Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color);

            DrawBox(bottomBox, color);
            DrawBox(topBox, color);
        }

        public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color)
        {
            DrawBox(new Box(origin, halfExtents, orientation), color);
        }
        public static void DrawBox(Box box, Color color)
        {
#if UNITY_EDITOR
            Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color);
            Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color);
            Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color);
            Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color);

            Debug.DrawLine(box.backTopLeft, box.backTopRight, color);
            Debug.DrawLine(box.backTopRight, box.backBottomRight, color);
            Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color);
            Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color);

            Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color);
            Debug.DrawLine(box.frontTopRight, box.backTopRight, color);
            Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color);
            Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color);
#endif
        }

        public static void DrawEllipse(Vector3 pos, float radiusX, float radiusY, int segments, Color color, float duration = 0)
        {
#if UNITY_EDITOR
            float angle = 0f;

            Vector3 lastPoint = Vector3.zero;
            Vector3 thisPoint = Vector3.zero;

            for (int i = 0; i < segments + 1; i++)
            {
                thisPoint.x = Mathf.Sin(Mathf.Deg2Rad * angle) * radiusX;
                thisPoint.y = Mathf.Cos(Mathf.Deg2Rad * angle) * radiusY;

                if (i > 0)
                {
                    Debug.DrawLine(lastPoint + pos, thisPoint + pos, color, duration);
                }

                lastPoint = thisPoint;
                angle += 360f / segments;
            }
#endif
        }

        public struct Box
        {
            public Vector3 localFrontTopLeft { get; private set; }
            public Vector3 localFrontTopRight { get; private set; }
            public Vector3 localFrontBottomLeft { get; private set; }
            public Vector3 localFrontBottomRight { get; private set; }
            public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
            public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
            public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
            public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

            public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
            public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
            public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
            public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
            public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
            public Vector3 backTopRight { get { return localBackTopRight + origin; } }
            public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
            public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

            public Vector3 origin { get; private set; }

            public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
            {
                Rotate(orientation);
            }
            public Box(Vector3 origin, Vector3 halfExtents)
            {
                localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
                localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
                localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
                localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

                this.origin = origin;
            }


            public void Rotate(Quaternion orientation)
            {
                localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
                localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
                localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
                localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
            }
        }

        //This should work for all cast types
        static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
        {
            return origin + direction.normalized * hitInfoDistance;
        }

        static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 direction = point - pivot;
            return pivot + rotation * direction;
        }
    }
}