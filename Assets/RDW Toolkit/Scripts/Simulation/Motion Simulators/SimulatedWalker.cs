using UnityEngine;
using System.Collections;
using Redirection;

public class SimulatedWalker : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

    /// <summary>
    /// Translation speed in meters per second.
    /// </summary>
    [SerializeField, Range(0.01f, 10)]
    public float translationSpeed = 1f;

    /// <summary>
    /// Rotation speed in degrees per second.
    /// </summary>
    [SerializeField, Range(0.01f, 360)]
    public float rotationSpeed = 90;



    const float MINIMUM_DISTANCE_TO_WAYPOINT_FOR_ROTATION = 0.0001f;
    const float ROTATIONAL_ERROR_ACCEPTED_IN_DEGRESS = 1;//0.2f; // If user's angular deviation from target is more than this value, we won't move (until we face the target better) - If you go low sometimes it can stop close to target
    const float EXTRA_WALK_TO_ENSURE_RESET = 0.01f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	//public void WalkUpdate () {
    public void Update()
    {
        if (redirectionManager.simulationManager.userIsWalking && redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.AutoPilot)
        {
            if (!redirectionManager.inReset)
                TurnAndWalkToWaypoint();
            else
                redirectionManager.resetter.SimulatedWalkerUpdate();
        }
	}

    public void TurnAndWalkToWaypoint()
    {
        Vector3 userToTargetVectorFlat;
        float rotationToTargetInDegrees;
        GetDistanceAndRotationToWaypoint(out rotationToTargetInDegrees, out userToTargetVectorFlat);
        //print("ROTATION NEEDED: " + rotationToTargetInDegrees);
        RotateIfNecessary(rotationToTargetInDegrees, userToTargetVectorFlat);
        GetDistanceAndRotationToWaypoint(out rotationToTargetInDegrees, out userToTargetVectorFlat);
        WalkIfPossible(rotationToTargetInDegrees, userToTargetVectorFlat);
    }
     
    public void RotateIfNecessary(float rotationToTargetInDegrees, Vector3 userToTargetVectorFlat)
    {
        // Handle Rotation To Waypoint
        float rotationToApplyInDegrees = Mathf.Sign(rotationToTargetInDegrees) * Mathf.Min(redirectionManager.GetDeltaTime() * rotationSpeed, Mathf.Abs(rotationToTargetInDegrees));
        // Only rotate if you have a reasonable distance to target
        // I'm not happy with this hack and I'm not sure how to explain the behavior because it keeps trying to rotate until the user faces southeast and then it stops!
        //if (!UserController.Approximately(userToWaypointVector2D, Vector2.zero))
        // Preventing Rotation When At Waypoint By Checking If Distance Is Sufficient
        //print("rotationToApplyInDegrees: " + rotationToApplyInDegrees);
        if (userToTargetVectorFlat.magnitude > MINIMUM_DISTANCE_TO_WAYPOINT_FOR_ROTATION)
            transform.Rotate(Vector3.up, rotationToApplyInDegrees, Space.World);
    }

    // Rotates rightward in place
    public void RotateInPlace()
    {
        transform.Rotate(Vector3.up, redirectionManager.GetDeltaTime() * rotationSpeed, Space.World);
    }

    

    public void WalkIfPossible(float rotationToTargetInDegrees, Vector3 userToTargetVectorFlat)
    {
        // Handle Translation To Waypoint
        // Luckily once we get near enough to the waypoint, the following condition stops us from shaking in place
        if (Mathf.Abs(rotationToTargetInDegrees) < ROTATIONAL_ERROR_ACCEPTED_IN_DEGRESS)
        {
            //print("ALLOWED WALKING DISTANCE: "+redirectionManager.resetter.getMaxWalkableDistanceBeforeReset());
            // Ensuring we don't overshoot the waypoint, and we don't go out of boundary
            float distanceToTravel = Mathf.Min(Mathf.Min(redirectionManager.GetDeltaTime() * translationSpeed, userToTargetVectorFlat.magnitude), EXTRA_WALK_TO_ENSURE_RESET + redirectionManager.resetter.getMaxWalkableDistanceBeforeReset());
            //Debug.Log("User Position: " + transform.position.ToString("f4"));
            //print("distanceToTravel: " + distanceToTravel);
            //Debug.Log("Expected Translation: " + (distanceToTravel * RedirectionManager.flatten3D(redirectionManager.getUserForward3D()).normalized).ToString("F4"));
            //print("WALK AMOUNT: " + distanceToTravel);
            //print("distance to travel: "+distanceToTravel);
            transform.Translate(distanceToTravel * Utilities.FlattenedPos3D(redirectionManager.currDir).normalized, Space.World);
            //Debug.Log("Travelled: " + distanceToTravel);
        }
        else
        {
            //Debug.Log("Not Travelling");
            //Debug.Log("rotationToWaypointInDegrees: " + rotationToWaypointInDegrees);
        }
    }

    void GetDistanceAndRotationToWaypoint(out float rotationToTargetInDegrees, out Vector3 userToTargetVectorFlat)
    {
        userToTargetVectorFlat = Utilities.FlattenedPos3D(redirectionManager.targetWaypoint.position - redirectionManager.currPos);
        rotationToTargetInDegrees = Utilities.GetSignedAngle(Utilities.FlattenedDir3D(redirectionManager.currDir), userToTargetVectorFlat);
    }

}
