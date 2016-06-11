using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Redirection;

public class ZigZagRedirector : Redirector
{
    [SerializeField]
    [Tooltip("Virtual path represented as series of waypoints in a zig-zag arrangement.")]
    public List<Transform> waypoints;
    // We assume these points
    [SerializeField]
    [Tooltip("Two points parented to RedirectedUser that are targets in the real world. RealtTarget0 is the starting point.")]
    public Transform realTarget0, realTarget1;

    [SerializeField]
    Vector3 RealTarget0DefaultPosition = Vector3.zero, RealTarget1DefaultPosition = new Vector3(3f, 0, 3f);

    /// <summary>
    ///  How close you need to get to the waypoint for it to be considered reached.
    /// </summary>
    float WAYPOINT_UPDATE_DISTANCE = 0.4f;
    /// <summary>
    /// How slow you need to be walking to trigger next waypoint when in proximity to current target.
    /// </summary>
    float SLOW_DOWN_VELOCITY_THRESHOLD = 0.25f;

    bool headingToTarget0 = false;
    int waypointIndex = 1;

    bool initialized = false;

    /**
     * Two big realizations:
     * 1. Expecting curvature to do most of the work is dangerous because when you do that, you'll need more than 180 in the real world for the next waypoint!
     * 2. When you want curvature to do the work, you're not planning correctly. You actually want more rotation then you think. Double actually. If you look at the arc, you end up rotating inward at the end, and you actually peak at the center, and that's when you are aiming in the direction of the line that connects the two real targets
     * So the best thing really to do is to put as much work as possible on rotation, and if there's anything left crank up curvature to max until goal is reached.
     */


    // FOR TESTING
    //public Vector3 virtualTargetPosition;
    //public Vector3 realTargetPosition;
    //public Vector3 realTargetPositionRelative;
    //public float angleToRealTarget;
    //public float angleToVirtualTarget;
    //public Vector3 userToVirtualTarget;
    //public Vector3 userToRealTarget;
    //public float distanceToRealTarget;
    //public float expectedRotationFromCurvature;
    //public float requiredAngleInjection;
    //public float requiredAngleInjectionFromRotationGain;
    //public float requiredTranslationInjection;

    //public float g_c;
    //public float g_r;
    //public float g_t;


    // Use this for initialization
    void Start()
    {
        //initialize();
        //Debug.LogWarning("ZIG ZAG INITIALIZED");
        // FOR TESTING PURPOSES
        //user.rotation = Quaternion.identity;
        //redirectionRecipient.rotation = Quaternion.identity;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.LogWarning("UPDATE");
        updateWaypoint();
    }



    void Initialize()
    {
        print("INIT SHIT");
        Vector3 point0 = Utilities.FlattenedPos3D(waypoints[0].position);
        Vector3 point1 = Utilities.FlattenedPos3D(waypoints[1].position);

        if (realTarget0 == null)
        {
            realTarget0 = InstantiateDefaultRealTarget(0, RealTarget0DefaultPosition);
            realTarget1 = InstantiateDefaultRealTarget(1, RealTarget1DefaultPosition);
        }

        Vector3 realDesiredDirection =  Utilities.FlattenedDir3D(Utilities.GetRelativePosition(realTarget1.position, this.transform) - Utilities.GetRelativePosition(realTarget0.position, this.transform));
        Quaternion pointToPointRotation = Quaternion.LookRotation(point1 - point0, Vector3.up);
        Quaternion desiredDirectionToForwardRotation = Quaternion.FromToRotation(realDesiredDirection, Vector3.forward);
        Quaternion desiredRotation = desiredDirectionToForwardRotation * pointToPointRotation;

        Vector3 pinnedPointRelativePosition = Utilities.FlattenedPos3D(Utilities.GetRelativePosition(realTarget0.position, this.transform));
        Vector3 pinnedPointPositionRotationCorrect = desiredRotation * pinnedPointRelativePosition;

        // Align first two waypoints to be on two real points (rather pin first one, and then get alignment towards the second one)
        this.transform.rotation = desiredRotation;
        this.transform.position = point0 + this.transform.position.y * Vector3.up - pinnedPointPositionRotationCorrect;

        if (redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.AutoPilot)
        {
            WAYPOINT_UPDATE_DISTANCE = 0.1f;
            SLOW_DOWN_VELOCITY_THRESHOLD = 100f;
        }

        // FOR TESTING PURPOSES
        //redirectionRecipient.rotation = Quaternion.identity;
        //redirectionRecipient.position = unFlatten(point0 - pinnedPointRelativePosition, redirectionRecipient.position.y);

        //user.position = unFlatten(point0, user.position.y);
        //user.rotation = pointToPointRotation;

        // Failed attempt to fix case when user is near waypoint, but reset is triggered. Then again resets shouldn't even be fired to begin with in this scenario.
        //if (redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.AutoPilot)
        //    WAYPOINT_UPDATE_DISTANCE = redirectionManager.simulationManager.DISTANCE_TO_WAYPOINT_THRESHOLD;
    }


    void updateWaypoint()
    {
        bool userIsNearTarget = Utilities.FlattenedPos3D(redirectionManager.currPos - waypoints[waypointIndex].position).magnitude < WAYPOINT_UPDATE_DISTANCE;
        bool userHasSlownDown = redirectionManager.deltaPos.magnitude / redirectionManager.GetDeltaTime() < SLOW_DOWN_VELOCITY_THRESHOLD;
        bool userHasMoreWaypointsLeft = waypointIndex < waypoints.Count - 1;
        if (userIsNearTarget && userHasSlownDown && userHasMoreWaypointsLeft && !redirectionManager.inReset)
        {
            waypointIndex++;
            headingToTarget0 = !headingToTarget0;
            Debug.LogWarning("WAYPOINT UDPATED");
        }
    }

    public override void ApplyRedirection()
    {

        //print("ZIGZAG REDIRECTION YAW");
        if (!initialized)
        {
            Initialize();
            initialized = true;
        }


        Vector3 virtualTargetPosition;
        Vector3 realTargetPosition;
        Vector3 realTargetPositionRelative;
        float angleToRealTarget;
        Vector3 userToVirtualTarget;
        float angleToVirtualTarget;
        Vector3 userToRealTarget;
        float distanceToRealTarget;
        float expectedRotationFromCurvature;
        float requiredAngleInjection;
        float requiredTranslationInjection;

        float g_c;
        float g_r;
        float g_t;


        virtualTargetPosition = Utilities.FlattenedPos3D(waypoints[waypointIndex].position);
        realTargetPosition = headingToTarget0 ? Utilities.FlattenedPos3D(realTarget0.position) : Utilities.FlattenedPos3D(realTarget1.position);
        realTargetPositionRelative = headingToTarget0 ? Utilities.FlattenedPos3D(Utilities.GetRelativePosition(realTarget0.position, this.transform)) : Utilities.FlattenedPos3D(Utilities.GetRelativePosition(realTarget1.position, this.transform));
        angleToRealTarget = Utilities.GetSignedAngle(redirectionManager.currDir, realTargetPosition - redirectionManager.currPos);
        angleToVirtualTarget = Utilities.GetSignedAngle(redirectionManager.currDir, virtualTargetPosition - redirectionManager.currPos);
        distanceToRealTarget = (realTargetPositionRelative - redirectionManager.currPosReal).magnitude;
        userToVirtualTarget = virtualTargetPosition - redirectionManager.currPos;
        userToRealTarget = realTargetPosition - redirectionManager.currPos;
        //requiredAngleInjection = angleToTarget - angleToRealTarget;
        requiredAngleInjection = Utilities.GetSignedAngle(userToRealTarget, userToVirtualTarget);
        
        float minimumRealTranslationRemaining = userToVirtualTarget.magnitude / (1 + redirectionManager.MAX_TRANS_GAIN);
        float minimumRealRotationRemaining = angleToVirtualTarget; // / (1 + redirectionManager.MIN_ROT_GAIN);

        // This can slightly be improved by expecting more from rotation when you know the user is rotating in a direction that now requires positive rotation gain instead!
        float expectedRotationFromRotationGain = Mathf.Sign(requiredAngleInjection) * Mathf.Min(Mathf.Abs(requiredAngleInjection), Mathf.Abs(minimumRealRotationRemaining * redirectionManager.MIN_ROT_GAIN));
        float remainingRotationForCurvatureGain = requiredAngleInjection - expectedRotationFromRotationGain;
        expectedRotationFromCurvature = Mathf.Sign(requiredAngleInjection) * Mathf.Min(minimumRealTranslationRemaining * (Mathf.Rad2Deg / redirectionManager.CURVATURE_RADIUS), Mathf.Abs(2 * remainingRotationForCurvatureGain));


        //if (!allRotGains)
        //{
        //    // Instead of estimating with real target (which may be very off, estimate with remaining distance to virtual target, in worst case scenario of rotation to target 
        //    //expectedRotationFromCurvature = Mathf.Sign(requiredAngleInjection) * Mathf.Min(distanceToRealTarget * (Mathf.Rad2Deg / redirectionManager.CURVATURE_RADIUS), Mathf.Abs(requiredAngleInjection));
        //    expectedRotationFromCurvature = Mathf.Sign(requiredAngleInjection) * Mathf.Min(minimumRealTranslationRemaining * (Mathf.Rad2Deg / redirectionManager.CURVATURE_RADIUS), Mathf.Abs(requiredAngleInjection));
        //    //requiredAngleInjectionFromRotationGain = requiredAngleInjection - 0.5f * expectedRotationFromCurvature; // Expect it to do half of what it's capable of
        //    requiredAngleInjectionFromRotationGain = requiredAngleInjection - expectedRotationFromCurvature;
        //}

        //// TESTING ALL PRESSURE ON ROTATION GAIN
        ////requiredAngleInjectionFromRotationGain = requiredAngleInjection;


        ////requiredTranslationInjection = distanceToTarget - distanceToRealTarget;
        requiredTranslationInjection = (realTargetPosition - virtualTargetPosition).magnitude;

        //if (!allRotGains)
        //{
        //    //g_c = distanceToRealTarget < 0.1f ? 0 : (expectedRotationFromCurvature / distanceToRealTarget); // Rotate in the opposite direction so when the user counters the curvature, the intended direction is achieved
        //    //g_c = distanceToRealTarget < 0.1f ? 0 : (expectedRotationFromCurvature / minimumRealTranslationRemaining); // Rotate in the opposite direction so when the user counters the curvature, the intended direction is achieved
        //    g_c = Mathf.Abs(expectedRotationFromCurvature) > 1 ? Mathf.Sign(requiredAngleInjection) * (Mathf.Rad2Deg / redirectionManager.CURVATURE_RADIUS) : 0;
        //    g_r = distanceToRealTarget < 0.1f || Mathf.Abs(angleToRealTarget) < Mathf.Deg2Rad * 1 ? 0 : requiredAngleInjectionFromRotationGain / Mathf.Abs(angleToRealTarget);
        //}
        //else
        //{
        //    requiredAngleInjectionFromRotationGain = requiredAngleInjection;
        //    g_c = 0;
        //    g_r = distanceToRealTarget < 0.1f || Mathf.Abs(angleToRealTarget) < Mathf.Deg2Rad * 1 ? 0 : requiredAngleInjectionFromRotationGain / Mathf.Abs(angleToRealTarget);
        //}

        g_c = distanceToRealTarget < 0.1f ? 0 : (expectedRotationFromCurvature / minimumRealTranslationRemaining); // Rotate in the opposite direction so when the user counters the curvature, the intended direction is achieved
        g_r = distanceToRealTarget < 0.1f || Mathf.Abs(angleToRealTarget) < Mathf.Deg2Rad * 1 ? 0 : expectedRotationFromRotationGain / Mathf.Abs(minimumRealRotationRemaining);
        g_t = distanceToRealTarget < 0.1f ? 0 : requiredTranslationInjection / distanceToRealTarget;


        // New Secret Sauce! Focusing on alignment!
        // Determine Translation Gain Sign and intensity!
        // CAREFUL ABOUT SIGNED ANGLE BETWEEN BEING IN RADIANS!!!
        g_t = Mathf.Cos(Mathf.Deg2Rad * Utilities.GetSignedAngle(redirectionManager.deltaPos, (virtualTargetPosition - realTargetPosition))) * Mathf.Abs(g_t);
        g_r *= Mathf.Sign(redirectionManager.deltaDir);
        // CONSIDER USING SIN NOW FOR ANGLES!

        // Put Caps on Gain Values
        /*
        g_t = Mathf.Sign(g_t) * Mathf.Min(Mathf.Abs(g_t), 0.25f);
        //g_t = Mathf.Sign(g_t) * Mathf.Min(Mathf.Abs(g_t), 1f);
        g_r = Mathf.Sign(g_r) * Mathf.Min(Mathf.Abs(g_r), 0.5f);
        */
        g_t = g_t > 0 ? Mathf.Min(g_t, redirectionManager.MAX_TRANS_GAIN) : Mathf.Max(g_t, redirectionManager.MIN_TRANS_GAIN);
        g_r = g_r > 0 ? Mathf.Min(g_r, redirectionManager.MAX_ROT_GAIN) : Mathf.Max(g_r, redirectionManager.MIN_ROT_GAIN);

        // Don't do translation if you're still checking out the previous target
        if ((redirectionManager.currPos - Utilities.FlattenedPos3D(waypoints[waypointIndex - 1].position)).magnitude < WAYPOINT_UPDATE_DISTANCE)
            g_t = 0;

        // Translation Gain
        InjectTranslation(g_t * redirectionManager.deltaPos);
        // Rotation Gain
        InjectRotation(g_r * redirectionManager.deltaDir);
        // Curvature Gain
        InjectCurvature(g_c * redirectionManager.deltaPos.magnitude);

        //if (redirectionManager.deltaPos.magnitude / redirectionManager.GetDeltaTime() < SLOW_DOWN_VELOCITY_THRESHOLD)
            //print("REPORTED USER SPEED: " + (redirectionManager.deltaPos.magnitude / redirectionManager.GetDeltaTime()).ToString("F4"));
    }

    Transform InstantiateDefaultRealTarget(int targetID, Vector3 position)
    {
        Transform waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Destroy(waypoint.GetComponent<SphereCollider>());
        waypoint.parent = this.transform;
        waypoint.name = "Real Target "+targetID;
        waypoint.position = position;
        waypoint.localScale = 0.3f * Vector3.one;
        waypoint.GetComponent<Renderer>().material.color = new Color(1, 1, 1);
        waypoint.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, 0.12f, 0));
        waypoint.GetComponent<Renderer>().enabled = false;
        return waypoint;
    }

}
