using UnityEngine;

public static class HMathf
{
    /// <summary>
    /// Segment intersect with segment. 
    /// Line1 start with p0, end with p1. Line2 start with p2, end with p3.
    /// </summary>
    /// <remarks>
    /// See https://github.com/jvanverth/essentialmath/blob/master/src/common/IvMath/IvLineSegment3.cpp method "DistanceSquared"
    /// </remarks>
    /// <returns>True means have intersect, and the reVal give the intersect point.</returns>
    public static bool SegmentIntersect (Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, ref Vector2 reVal)
    {
        Vector2 dir1 = p1 - p0;
        Vector2 dir2 = p3 - p2;

        Vector2 w0 = p0 - p2;
        float a = Vector2.Dot (dir1, dir1);
        float b = Vector2.Dot (dir1, dir2);
        float c = Vector2.Dot (dir2, dir2);
        float d = Vector2.Dot (dir1, w0);
        float e = Vector2.Dot (dir2, w0);

        float denom = a * c - b * b;

        // parameters to compute s_c, t_c
        float sn, sd, tn, td;
        float s_c, t_c;

        // if denom is zero, try finding closest point on segment1 to origin0
        if (Mathf.Abs (denom) < 1e-5)
        {
            // clamp s_c to 0
            sd = td = c;
            sn = 0.0f;
            tn = e;
        }
        else
        {
            // clamp s_c within [0,1]
            sd = td = denom;
            sn = b * e - c * d;
            tn = a * e - b * d;

            // clamp s_c to 0
            if (sn < 0.0f)
            {
                sn = 0.0f;
                tn = e;
                td = c;
            }
            // clamp s_c to 1
            else if (sn > sd)
            {
                sn = sd;
                tn = e + b;
                td = c;
            }
        }

        // clamp t_c within [0,1]
        // clamp t_c to 0
        if (tn < 0.0f)
        {
            t_c = 0.0f;
            // clamp s_c to 0
            if (-d < 0.0f)
            {
                s_c = 0.0f;
            }
            // clamp s_c to 1
            else if (-d > a)
            {
                s_c = 1.0f;
            }
            else
            {
                s_c = -d / a;
            }
        }
        // clamp t_c to 1
        else if (tn > td)
        {
            t_c = 1.0f;
            // clamp s_c to 0
            if ((-d + b) < 0.0f)
            {
                s_c = 0.0f;
            }
            // clamp s_c to 1
            else if ((-d + b) > a)
            {
                s_c = 1.0f;
            }
            else
            {
                s_c = (-d + b) / a;
            }
        }
        else
        {
            t_c = tn / td;
            s_c = sn / sd;
        }

        Vector2 wc = w0 + s_c * dir1 - t_c * dir2;

        if (Mathf.Abs (wc.sqrMagnitude) < 1e-3)
        {
            reVal = p0 + s_c * dir1;
            return true;
        }

        return false;
    }

}