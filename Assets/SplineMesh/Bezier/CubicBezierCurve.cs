﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class CubicBezierCurve {

    private const int STEP_COUNT = 30;
    private const float T_STEP = 1.0f/STEP_COUNT;

    public SplineNode n1, n2;
    public float Length { get; private set; }
    private readonly List<CurveSample> samples = new List<CurveSample>(STEP_COUNT);

    public UnityEvent Changed = new UnityEvent();

    public CubicBezierCurve(SplineNode n1, SplineNode n2)
    {
        this.n1 = n1;
        this.n2 = n2;
        n1.Changed.AddListener(() => ComputePoints());
        n2.Changed.AddListener(() => ComputePoints());
        ComputePoints();
    }

    public void ConnectStart(SplineNode n1) {
        this.n1.Changed.RemoveListener(() => ComputePoints());
        this.n1 = n1;
        n1.Changed.AddListener(() => ComputePoints());
        ComputePoints();
    }

    public void ConnectEnd(SplineNode n2) {
        this.n2.Changed.RemoveListener(() => ComputePoints());
        this.n2 = n2;
        n2.Changed.AddListener(() => ComputePoints());
        ComputePoints();
    }

    public Vector3 GetInverseDirection() {
        return (2 * n2.Position) - n2.Direction;
    }

    public Vector3 GetLocation(float t)
    {
        if (t < 0 || t > 1)
            throw new ArgumentException("Time must be between 0 and 1. Given time was " + t);
        float omt = 1f - t;
        float omt2 = omt * omt;
        float t2 = t * t;
        return
            n1.position * (omt2 * omt) +
            n1.direction * (3f * omt2 * t) +
            GetInverseDirection() * (3f * omt * t2) +
            n2.position * (t2 * t);
    }

    public Vector3 GetTangent(float t)
    {
        if (t < 0 || t > 1)
            throw new ArgumentException("Time must be between 0 and 1. Given time was " + t);
        float omt = 1f - t;
        float omt2 = omt * omt;
        float t2 = t * t;
        Vector3 tangent =
            n1.position * (-omt2) +
            n1.direction * (3 * omt2 - 2 * omt) +
            GetInverseDirection() * (-3 * t2 + 2 * t) +
            n2.position * (t2);
        return tangent.normalized;
    }

    private void ComputePoints()
    {
        samples.Clear();
        Length = 0;
        Vector3 previousPosition = GetLocation(0);
        for (float t = 0; t < 1; t += T_STEP)
        {
            CurveSample sample = new CurveSample();
            sample.location = GetLocation(t);
            sample.tangent = GetTangent(t);
            Length += Vector3.Distance(previousPosition, sample.location);
            sample.distance = Length;

            previousPosition = sample.location;
            samples.Add(sample);
        }
        CurveSample lastSample = new CurveSample();
        lastSample.location = GetLocation(1);
        lastSample.tangent = GetTangent(1);
        Length += Vector3.Distance(previousPosition, lastSample.location);
        lastSample.distance = Length;
        samples.Add(lastSample);

        if (Changed != null)
            Changed.Invoke();
    }

    private CurveSample getCurvePointAtDistance(float d)
    {
        if (d < 0 || d > Length)
            throw new ArgumentException("Distance must be positive and less than curve length. Length = " + Length + ", given distance was " + d);

        CurveSample previous = samples[0];
        CurveSample next = null;
        foreach (CurveSample cp in samples)
        {
            if (cp.distance >= d) {
                next = cp;
                break;
            }
            previous = cp;
        }
        if(next == null) {
            throw new Exception("Can't find curve samples.");
        }
        float t = next == previous ? 0 : (d - previous.distance) / (next.distance - previous.distance);

        CurveSample res = new CurveSample();
        res.distance = d;
        res.location = Vector3.Lerp(previous.location, next.location, t);
        res.tangent = Vector3.Lerp(previous.tangent, next.tangent, t).normalized;
        return res;
    }

    public Vector3 GetLocationAtDistance(float d) {
        return getCurvePointAtDistance(d).location;
    }

    public Vector3 GetTangentAtDistance(float d)
    {
        return getCurvePointAtDistance(d).tangent;
    }

    private class CurveSample
    {
        public Vector3 location;
        public Vector3 tangent;
        public float distance;
    }

    public static Quaternion GetRotationFromTangent(Vector3 Tangent) {
        if (Tangent == Vector3.zero)
            return Quaternion.identity;
        return Quaternion.LookRotation(Tangent, Vector3.Cross(Tangent, Vector3.Cross(Vector3.up, Tangent).normalized));
    }
}
