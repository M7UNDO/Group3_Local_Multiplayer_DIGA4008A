using UnityEngine;

public static class ArcPullUtility
{
    public static Vector3 CalculateArcMidPoint(Vector3 start, Vector3 end, float arcHeight)
    {
        Vector3 mid = (start + end) * 0.5f;
        mid.y += arcHeight;
        return mid;
    }

    public static Vector3 EvaluateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // Quadratic Bezier
        return Mathf.Pow(1 - t, 2) * p0 +
               2 * (1 - t) * t * p1 +
               Mathf.Pow(t, 2) * p2;
    }
}