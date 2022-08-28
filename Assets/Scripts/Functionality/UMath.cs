using System;

using UnityEngine;

public static class UMath
{
    public const double goldenRatio = 1.61803398874989484820458683436;
    public const double phi = goldenRatio;

    #region Math Operations

    ///<summary>Checks if value is between a and b - where a or b could be the larget number.</summary>
    public static bool Within(float a, float b, float value, float tolerance = 0)
    {
        return (value >= a - tolerance && value <= b + tolerance) || (value >= b - tolerance && value <= a + tolerance);
    }

    public static bool WithinBounds(Vector2 pos, Vector2 bounds)
    {
        bool withinX = Mathf.Abs(pos.x) <= Mathf.Abs(bounds.x);
        bool withinY = Mathf.Abs(pos.y) <= Mathf.Abs(bounds.y);
        return Mathf.Abs(pos.x) <= Mathf.Abs(bounds.x) && Mathf.Abs(pos.y) <= Mathf.Abs(bounds.y);
    }

    ///<summary>Linear equation (y=mx+b) in code form.</summary>
    public static float GetY(Vector2 point1, Vector2 point2, float x)
    {
        float m = GetSlope(point1, point2);
        float b = point1.y - (m * point1.x);

        return m * x + b;
    }

    ///<summary>Linear equation (x=(y-b)/m) in code form.</summary>
    public static float GetX(Vector2 point1, Vector2 point2, float y)
    {
        float m = GetSlope(point1, point2);
        float b = point1.y - (m * point1.x);

        return (y - b) / m;
    }

    public static float GetSlope(Vector2 point1, Vector2 point2)
    {
        float dx = point2.x - point1.x;
        if (dx == 0)
            return float.NaN;
        return (point2.y - point1.y) / dx;
    }

    ///<summary>Calculates the linear parameter t that produces the interpolant value within</summary>
    public static float InverseLerp(Vector2 a, Vector2 b, Vector2 value)
    {
        Vector2 AB = b - a;
        Vector2 AV = value - a;
        return Vector2.Dot(AV, AB) / Vector2.Dot(AB, AB);
    }

    ///<summary>Calculates the linear parameter t that produces the interpolant value within</summary>
    public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
    {
        Vector3 AB = b - a;
        Vector3 AV = value - a;
        return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
    }

    public static Vector2 Abs(Vector2 value)
    {
        return new Vector2(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }
    public static Vector2Int Abs(Vector2Int value)
    {
        return new Vector2Int(Mathf.Abs(value.x), Mathf.Abs(value.y));
    }

    public static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    public static Vector2 Sign(Vector2 value)
    {
        return new Vector2(Mathf.Sign(value.x), Mathf.Sign(value.y));
    }
    public static Vector2Int Sign(Vector2Int value)
    {
        return new Vector2Int((int)Mathf.Sign(value.x), (int)Mathf.Sign(value.y));
    }

    public static int GetSignIfValue(float value)
    {
        return (int)(Mathf.Sign(value) * Mathf.Floor(Mathf.Min(Mathf.Abs(value), 1)));
    }

    public static Vector2Int GetSignIfValue(Vector2 value)
    {
        return new Vector2Int(GetSignIfValue(value.x), GetSignIfValue(value.y));
    }

    public static Vector3Int GetSignIfValue(Vector3 value)
    {
        return new Vector3Int(GetSignIfValue(value.x), GetSignIfValue(value.y), GetSignIfValue(value.z));
    }

    public static bool WithinRange(Vector2 start, Vector2 end, Vector2 value)
    {
        bool withinX = value.x >= start.x && value.x <= end.x;
        bool withinY = value.y >= start.y && value.y <= end.y;
        return withinX && withinY;
    }

    public static float GetPercent(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }

    public static string PercentToHexString(float percent)
    {
        int value = Mathf.RoundToInt(percent * 255);
        string hex = Convert.ToString(value, 16);

        if (hex.Length % 2 == 1)
            hex = "0" + hex;

        return hex.ToUpper();
    }

    #region Directions

    public struct DirectionPlane
    {
        public const int hor = 0;
        public const int horizontal = 0;
        public const int vert = 1;
        public const int vertical = 1;
        public const int fourWay = 4;
        public const int eightWay = 8;
    }

    public class Direction
    {
        public const int none = 0;
        public const int up = 1;
        public const int down = -1;
        public const int left = -2;
        public const int right = 2;

        public const float axisProportion = .75f; //Diagonal directions are fractioned to keep speed consistant
    }

    /// <summary>
    /// Useful for enum flags and counting how many flags have been set.
    /// </summary>
    public static int CountOnBits(int x)
    {
        int count = 0;
        while (x != 0)
        {
            if ((x & 1) != 0) count++;
            x = x >> 1;
        }
        return count;
    }

    #endregion
    public static bool RandomBool()
    {
        return (UnityEngine.Random.value > 0.5f);
    }
    public static float RandomSign()
    {
        return UnityEngine.Random.Range(0, 2) * 2 - 1;
    }

    public static Vector2 RandomDirection(int plane, float distance) // Picks a random direction based on given plane and distance
    {
        Vector2 dir = new Vector2(0, 0);

        if (plane == DirectionPlane.fourWay) //For the four way plane, we only want to move either horizontal or vertical. We quickly decide what to change our plane to at random (before we actually go to our switch case).
        {
            plane = UnityEngine.Random.Range((int)0, (int)2);
            //Debug.Log("fourWayRand " + plane);
        }

        switch (plane)
        {
            case DirectionPlane.hor:
                dir = new Vector2(RandOperand() * distance, 0); //Distance multiplied by the operand modifier (either + or -)
                break;

            case DirectionPlane.vert:
                dir = new Vector2(0, RandOperand() * distance);
                break;

            case DirectionPlane.eightWay: //for eight way movement, we can move across both the horizontal and vertical planes

                int randX = UnityEngine.Random.Range((int)0, (int)2); //Are we going to move on the horizontal plane?
                int randY = UnityEngine.Random.Range((int)0, (int)2); //Are we going to move on the vertical plane?

                if (randX == 0 && randY == 0) //Make sure the value isn't Vector2.zero
                {
                    if (UnityEngine.Random.Range((int)0, (int)2) == 1)
                        randX = 1;
                    else
                        randY = 1;
                }

                //Debug.Log("randX " + randX + " | randY " + randY);

                dir = new Vector2(RandOperand() * randX * distance, RandOperand() * randY * distance);

                if (randX == 1 && randY == 1) // if moving diagonally, we shorten the distance to seem more proportionate
                    dir = dir * Direction.axisProportion;

                break;
        }

        //Debug.Log("dir " + dir);
        return dir;
    }

    public static Vector2 RandomDirection(int plane)
    {
        return RandomDirection(plane, 1);
    }

    ///<summary>
    ///Faster way to grab distance between two vectors. Best used when comparing other sqrDistances.
    ///</summary>
    public static float SqrDistance(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude;
    }

    public static int RandOperand() //Returns either +1 or -1
    {
        int rand = UnityEngine.Random.Range((int)0, (int)2);
        //Debug.Log("Operand Value " + rand);

        if (rand == 0)
            return -1;
        else
            return 1;
    }

    public static float FindDegree(float x, float y) //does the same as above but for two float values
    {
        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;

        //float value = (float)((Mathf.Atan2(x, y) / Mathf.PI) * 180f);

        //return value % 360;
    }

    public static float FindDegree(Vector2 pos) //does the same as above but for a Vector2 (probably the most used)
    {
        return Mathf.Atan2(pos.x, pos.y) * Mathf.Rad2Deg;

        //float value = (float)((Mathf.Atan2(pos.x, pos.y) / Mathf.PI) * 180f);

        //return value % 360;
    }

    public static float GetAngleDifference(float degA, float degB, bool clockwise)
    {
        float start = clockwise && degA < degB ? degA + 360 : degA;
        float end = !clockwise && degB < degA ? degB + 360 : degB;

        return end - start;
    }

    /// <summary>Simply swaps the two values.</summary>
    public static void SwapValues(ref float a, ref float b)
    {
        float swap = b;
        b = a;
        a = b;
    }

    public static float NormalizeAngle(float a)
    {
        return a - 180f * Mathf.Floor((a + 180f) / 180f);
    }

    /// <summary>Gets difference between two angles in degrees.</summary>
    public static float GetAngleDifference(float degA, float degB)
    {
        return (degB - degA + 180 + 360) % 360 - 180;
    }

    public static float RoundByValue(float num, float roundValue)
    {
        return Mathf.Floor(num / roundValue) * roundValue;
    }

    public static Vector2 getRelativePosition(Transform origin, Vector3 position)
    {
        Vector3 distance = position - origin.position;
        Vector3 relativePosition = Vector3.zero;
        relativePosition.x = Vector3.Dot(distance, origin.right.normalized);
        relativePosition.y = Vector3.Dot(distance, origin.up.normalized);
        relativePosition.z = Vector3.Dot(distance, origin.forward.normalized);

        return relativePosition;
    }

    public static Vector2 getRelativePosition(Vector3 origin, Vector3 position)
    {
        Vector3 relativePosition = position - origin;

        return relativePosition;
    }


    public static float EaseIn(float t)
    {
        return 1 - Mathf.Cos(t * Mathf.PI) * .5f;
    }
    public static float EaseOut(float t)
    {
        return -(Mathf.Cos(t * Mathf.PI) - 1) * .5f;
    }

    public static float EaseInOut(float t)
    {
        return Mathf.Sin(t * Mathf.PI) * .5f;
    }

    //Fast and Funky 1D Nonlinear Transformations
    //https://www.youtube.com/watch?v=mr5xkf6zSzk
    public static float SmoothStart(float t, int power = 2, float min = 0, float max = 1)
    {
        t = Mathf.Clamp01(t);

        if (power < 2)
            power = 1;

        return IntPow(t, power) * (max - min) + min;
    }

    public static Vector2 SmoothStart(float t, Vector2 origin, Vector2 finish, int power = 2)
    {
        Vector2 position;
        position.x = SmoothStart(t, power, origin.x, finish.x);
        position.y = SmoothStart(t, power, origin.y, finish.y);
        return position;
    }

    public static float SmoothStop(float t, int power = 2, float min = 0, float max = 1)
    {
        t = Mathf.Clamp01(t);

        if (power < 2)
            power = 1;

        return (1 - IntPow((1 - t), power)) * (max - min) + min;
    }

    public static Vector2 SmoothStop(float t, Vector2 origin, Vector2 finish, int power = 2)
    {
        Vector2 position;
        position.x = SmoothStop(t, power, origin.x, finish.x);
        position.y = SmoothStop(t, power, origin.y, finish.y);
        return position;
    }

    ///<summary>More efficient than Mathf.Pow (in cases with short power ints) as we only allow ints for the power parameter.
    ///<para>Will still return a float value</para></summary>
    public static float IntPow(float num, int power = 2)
    {
        if (power == 0 || num == 0)
            return 0;

        for (int i = 1; i < power; i++)
            num = num * num;

        return num;
    }

    public static int BoolToNum(bool boolean)
    {
        return boolean == true ? 1 : 0;
    }

    public static Vector2 Clamp(Vector2 vector2, float min, float max)
    {
        float multiplier = vector2.magnitude;
        if (multiplier < min)
            multiplier = min;
        else if (multiplier > max)
            multiplier = max;

        return vector2.normalized * multiplier;
        //return (new Vector2(Mathf.Clamp(vector2.x, min, max), Mathf.Clamp(vector2.y, min, max)));
    }
    public static Vector2 Clamp01(Vector2 vector2)
    {
        return Clamp(vector2, 0, 1);
    }

    public static Vector2 ClampAxis(Vector2 vector2) => ClampAxis(vector2, 0, 1);
    public static Vector2 ClampAxis(Vector2 vector2, float clampMin, float clampMax)
    {
        float mag = (vector2.magnitude - clampMin) / (clampMax - clampMin);
        return Clamp(vector2.normalized * mag, 0, 1);
        //return Clamp(vector2, 0, 1);
        //return Clamp(vector2, -1, 1);
    }

    public static Vector2 Clamp8DAxis(Vector2 vector2, float threshold = .333f)
    {
        if (Mathf.Abs(vector2.y) > threshold)
            vector2.y = Mathf.Sign(vector2.y);
        else
            vector2.y = 0;

        if (Mathf.Abs(vector2.x) > threshold)
            vector2.x = Mathf.Sign(vector2.x);
        else
            vector2.x = 0;

        return vector2;
    }

    public static Vector2 Clamp4DAxis(Vector2 vector2)
    {
        Vector2[] directions = { new Vector2(0, 1), new Vector2(1, 0), new Vector2(0, -1), new Vector2(-1, 0) };

        if (vector2 == Vector2.zero)
            return Vector2.zero;

        Vector2 closestDir = directions[0];
        for (int i = 1; i < 4; i++)
        {
            float curAngleDiff = Vector2.Angle(vector2, directions[i]);
            float closestAngleDiff = Vector2.Angle(vector2, closestDir);

            if (curAngleDiff < closestAngleDiff)
                closestDir = directions[i];
        }

        return closestDir;
    }

    public static Vector2 InvertAxis(Vector2 vector2)
    {
        return new Vector2(vector2.x * -1, vector2.y * -1);
    }

    public static Vector2 RadianToVector2(float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }

    public static Vector2 DegreeToVector2(float degree)
    {
        return RadianToVector2(degree % 360 * Mathf.Deg2Rad);
    }

    public static float MinMaxPercent(float value, float min, float max, bool clamp = false)
    {
        float percent = (value - min) / (max - min);
        if (clamp)
            percent = Mathf.Clamp(percent, 0, 1);
        return percent;
    }

    public static float InverseLerpUnclamped(float a, float b, float value)
    {
        return (value - a) / (b - a);
    }

    public static Vector3 LerpAngle(Vector3 a, Vector3 b, float t)
    {
        Vector3 angle;
        angle.x = Mathf.LerpAngle(a.x, b.x, t);
        angle.y = Mathf.LerpAngle(a.y, b.y, t);
        angle.z = Mathf.LerpAngle(a.z, b.z, t);

        return angle;
    }

    public static float LerpAngleInDirection(float a, float b, float t, bool clockwise)
    {
        float start = clockwise && a < b ? a + 360 : a;
        float end = !clockwise && b < a ? b + 360 : b;

        return Mathf.Lerp(start, end, t);
    }

    public static float InverseLerpAngleInDirection(float a, float b, float value, bool clockwise)
    {
        float start = clockwise && a < b ? a + 360 : a;
        float end = !clockwise && b < a ? b + 360 : b;

        return Mathf.InverseLerp(start, end, value);
    }

    public static float SlopeFromDegree(float degree)
    {
        Vector2 vector = UMath.DegreeToVector2(degree);
        return vector.y / vector.x;
    }

    public static float Perpendicular(float angle)
    {
        return (angle - 90) % 360;
    }

    public static Vector2 LineNormal(Vector2 lineOrigin, float lineAngle, Vector2 point)
    {
        //Quite imperfect, finding the midpoint between py and px does not give us an accurate normal at all
        //Still using this anyways because it meets our purposes lol

        float m = UMath.SlopeFromDegree(lineAngle);
        float b = lineOrigin.y - m * lineOrigin.x;

        Vector2 py = new Vector2(point.x, m * point.x + b);

        if (m == 0) return py; //Makes safe from divide by zero

        Vector2 px = new Vector2((point.y - b) / m, point.y);

        return (px + py) / 2;
    }

    ///<summary>Different variant of modulo that manages negative numbers. Good for arrays.</summary>
    public static float Mod(float x, float m)
    {
        return (x % m + m) % m;
    }

    public static bool OppositeSigns(int x, int y)
    {
        return ((x ^ y) < 0);
    }

    ///<summary>Different variant of modulo that manages negative numbers. Good for arrays.</summary>
    public static int Mod(int x, int m) => (int)Mod((float)x, (float)m);

    public static float Round(float value, int digits)
    {
        float mult = Mathf.Pow(10.0f, (float)digits);
        return Mathf.Round(value * mult) / mult;
    }

    public static float Average(float[] values)
    {
        float v = 0;

        int len = values.Length;
        for (int i = 0; i < len; i++)
        {
            v += values[i];
        }

        return v / len;
    }

    public static float RandomPercent()
    {
        float p = UnityEngine.Random.Range(0, 100);
        return p / 100;
    }

    ///<summary>Gets multiple random numbers and averages them into one value.
    ///<para>https://serenesforest.net/general/true-hit/</para></summary>
    public static float WeightedRandomPercent(int averageCount = 2)
    {
        float v = 0;

        for (int i = 0; i < averageCount; i++)
            v += RandomPercent();

        return v / averageCount;
    }

    public static Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 controlP0, Vector3 controlP1, Vector3 p1)
    {
        float u = 1.0f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0; //first term
        p += 3f * uu * t * controlP0; //second term
        p += 3f * u * tt * controlP1; //third term
        p += ttt * p1; //fourth term

        return p;
    }

    public static bool PointOnLineSegment(Vector2 pt1, Vector2 pt2, Vector2 pt, double epsilon = 0.001)
    {
        if (pt.x - Math.Max(pt1.x, pt2.x) > epsilon ||
            Math.Min(pt1.x, pt2.x) - pt.x > epsilon ||
            pt.y - Math.Max(pt1.y, pt2.y) > epsilon ||
            Math.Min(pt1.y, pt2.y) - pt.y > epsilon)
            return false;

        if (Math.Abs(pt2.x - pt1.x) < epsilon)
            return Math.Abs(pt1.x - pt.x) < epsilon || Math.Abs(pt2.x - pt.x) < epsilon;
        if (Math.Abs(pt2.y - pt1.y) < epsilon)
            return Math.Abs(pt1.y - pt.y) < epsilon || Math.Abs(pt2.y - pt.y) < epsilon;

        double x = pt1.x + (pt.y - pt1.y) * (pt2.x - pt1.x) / (pt2.y - pt1.y);
        double y = pt1.y + (pt.x - pt1.x) * (pt2.y - pt1.y) / (pt2.x - pt1.x);

        return Math.Abs(pt.x - x) < epsilon || Math.Abs(pt.y - y) < epsilon;
    }

    /// <summary>
    /// Gets the coordinates of the intersection point of two lines.
    /// </summary>
    /// <param name="A1">A point on the first line.</param>
    /// <param name="A2">Another point on the first line.</param>
    /// <param name="B1">A point on the second line.</param>
    /// <param name="B2">Another point on the second line.</param>
    /// <param name="found">Is set to false of there are no solution. true otherwise.</param>
    /// <returns>The intersection point coordinates. Returns Vector2.zero if there is no solution.</returns>
    public static Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out bool found)
    {
        float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

        if (tmp == 0)
        {
            // No solution!
            found = false;
            return Vector2.zero;
        }

        float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

        found = true;

        return new Vector2(
            B1.x + (B2.x - B1.x) * mu,
            B1.y + (B2.y - B1.y) * mu
        );
    }

    #endregion
}