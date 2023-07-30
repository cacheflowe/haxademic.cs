using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElasticValue : Object
{
    // Hooke's law: F = -kx
    // .75/.40 = bouncy elastic
    // .50/.90 = short elastic
    // .50/.50 = smooth elastic
    // .50/.30 = easing
    // .50/.30 = slow easing

    private float fric;
    private float accel;
    private float speed;
    private float val;
    private float target;

    public ElasticValue(float val, float fric, float accel)
    {
        this.fric = fric;
        this.accel = accel;
        this.val = val;
        this.target = val;
    }

    public float Value()
    {
        return val;
    }

    public ElasticValue SetCurrent(float val)
    {
        this.val = val;
        return this;
    }

    public ElasticValue SetTarget(float target)
    {
        this.target = target;
        return this;
    }

    public ElasticValue SetFriction(float fric)
    {
        this.fric = fric;
        return this;
    }

    public ElasticValue SetAccel(float accel)
    {
        this.accel = accel;
        return this;
    }

    public void Update()
    {
        // update elastic point based on current target position vs current position
        speed = ((target - val) * accel + speed) * fric;
        val += speed;
    }
}
