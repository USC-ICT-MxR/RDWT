using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Redirection;

public class VirtualPathGenerator
{

    public static int RANDOM_SEED = 3041;

    public enum DistributionType { Normal, Uniform };
    public enum AlternationType { None, Random, Constant };

    public struct SamplingDistribution
    {
        public DistributionType distributionType;
        public float min, max;
        public float mu, sigma;
        public AlternationType alternationType; // Used typicaly for the case of generating angles, where we want the value to be negated at random
        public SamplingDistribution(DistributionType distributionType, float min, float max, AlternationType alternationType = AlternationType.None, float mu = 0, float sigma = 0)
        {
            this.distributionType = distributionType;
            this.min = min;
            this.max = max;
            this.mu = mu;
            this.sigma = sigma;
            this.alternationType = alternationType;
        }
    }

    public struct PathSeed
    {
        public int waypointCount;
        public SamplingDistribution distanceDistribution;
        public SamplingDistribution angleDistribution;
        public PathSeed(SamplingDistribution distanceDistribution, SamplingDistribution angleDistribution, int waypointCount)
        {
            this.distanceDistribution = distanceDistribution;
            this.angleDistribution = angleDistribution;
            this.waypointCount = waypointCount;
        }
    }

    static float sampleUniform(float min, float max)
    {
        //return a + Random.value * (b - a);
        return Random.Range(min, max);
    }

    static float sampleNormal(float mu = 0, float sigma = 1, float min = float.MinValue, float max = float.MaxValue)
    {
        // From: http://stackoverflow.com/questions/218060/random-gaussian-variables
        float r1 = Random.value;
        float r2 = Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(r1)) * Mathf.Sin(2.0f * Mathf.PI * r2); // Random Normal(0, 1)
        float randNormal = mu + randStdNormal * sigma;
        return Mathf.Max(Mathf.Min(randNormal, max), min);
    }

    static float sampleDistribution(SamplingDistribution distribution)
    {
        float retVal = 0;
        if (distribution.distributionType == DistributionType.Uniform)
        {
            retVal = sampleUniform(distribution.min, distribution.max);
        }
        else if (distribution.distributionType == DistributionType.Normal)
        {
            retVal = sampleNormal(distribution.mu, distribution.sigma, distribution.min, distribution.max);
        }
        if (distribution.alternationType == AlternationType.Random && Random.value < 0.5f)
            retVal = -retVal;
        return retVal;
    }

    // The angular sampling distribution must be 
    public static List<Vector2> generatePath(PathSeed pathSeed, Vector2 initialPosition, Vector2 initialForward, out float sumOfDistances, out float sumOfRotations)
    {
        // THE GENERATION RULE IS WALK THEN TURN! SO THE LAST TURN IS TECHNICALLY REDUNDANT!
        // I'M DOING THIS TO MAKE SURE WE WALK STRAIGHT ALONG THE INITIAL POSITION FIRST BEFORE WE EVER TURN
        List<Vector2> waypoints = new List<Vector2>(pathSeed.waypointCount);
        Vector2 position = initialPosition;
        Vector2 forward = initialForward.normalized;
        Vector2 nextPosition, nextForward;
        float sampledDistance, sampledRotation;
        sumOfDistances = 0;
        sumOfRotations = 0;
        int alternator = 1;
        for (int i = 0; i < pathSeed.waypointCount; i++)
        {
            sampledDistance = sampleDistribution(pathSeed.distanceDistribution);
            sampledRotation = sampleDistribution(pathSeed.angleDistribution);
            if (pathSeed.angleDistribution.alternationType == AlternationType.Constant)
                sampledRotation *= alternator;
            nextPosition = position + sampledDistance * forward;
            nextForward = Utilities.RotateVector(forward, sampledRotation).normalized; // Normalizing for extra protection in case error accumulates over time
            waypoints.Add(nextPosition);
            position = nextPosition;
            forward = nextForward;
            sumOfDistances += sampledDistance;
            sumOfRotations += Mathf.Abs(sampledRotation); // The last one might seem redundant to add
            alternator *= -1;
        }
        return waypoints;
    }

    public static Vector2 getRandomPositionWithinBounds(float minX, float maxX, float minZ, float maxZ)
    {
        return new Vector2(sampleUniform(minX, maxX), sampleUniform(minZ, maxZ));
    }

    public static Vector2 getRandomForward()
    {
        float angle = sampleUniform(0, 360);
        return Utilities.RotateVector(Vector2.up, angle).normalized; // Over-protective with the normalizing
    }

}
