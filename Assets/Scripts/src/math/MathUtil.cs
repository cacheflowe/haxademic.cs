using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtil : Object
{
    public static float Map(float value, float low1, float high1, float low2, float high2)
    {
        return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
    }
}
