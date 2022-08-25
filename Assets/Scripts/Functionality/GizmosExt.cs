using UnityEngine;

namespace Than
{
    public static class GizmosExt
    {
        public static void DrawCircle(Vector3 center, Quaternion rotation, float radius, float theta = .1f)
        {
            //Draw a ring
            Vector3 beginPoint = center;
            Vector3 firstPoint = center;
            for (float t = 0; t < 2 * Mathf.PI; t += theta)
            {
                Vector2 pos = new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * radius;

                //*Apply our movement values
                Vector3 endPoint = center + rotation * pos;
                if (t == 0)
                {
                    firstPoint = endPoint;
                }
                else
                {
                    Gizmos.DrawLine(beginPoint, endPoint);
                }
                beginPoint = endPoint;
            }
            //Draw the last segment
            Gizmos.DrawLine(firstPoint, beginPoint);
        }
    }

}
