using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtil : Object
{
    public static float Map(float value, float low1, float high1, float low2, float high2)
    {
        return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
    }

    public static float Saw( float rads ) {
        float pi = Mathf.PI;
        float twoPi = pi * 2;
        rads += pi / 2;             // offset to sync up with sin(0)
        var percent = ( rads % pi ) / pi;
        var dir = ( rads % (twoPi) > pi ) ? -1 : 1;
        percent *= 2 * dir;
        percent -= dir;
        return percent;
	}

}
