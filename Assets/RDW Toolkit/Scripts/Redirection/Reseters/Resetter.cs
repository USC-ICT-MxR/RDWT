using UnityEngine;
using System.Collections;
using Redirection;

public abstract class Resetter : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

    enum Boundary { Top, Bottom, Right, Left };

    float maxX, maxZ;

    /// <summary>
    /// Function called when reset trigger is signaled, to see if resetter believes resetting is necessary.
    /// </summary>
    /// <returns></returns>
    public abstract bool IsResetRequired();

    public abstract void InitializeReset();

    public abstract void ApplyResetting();

    public abstract void FinalizeReset();

    public abstract void SimulatedWalkerUpdate();



    public void InjectRotation(float rotationInDegrees)
    {
        this.transform.RotateAround(Utilities.FlattenedPos3D(redirectionManager.headTransform.position), Vector3.up, rotationInDegrees);
        this.GetComponentInChildren<KeyboardController>().SetLastRotation(rotationInDegrees);
        redirectionManager.statisticsLogger.Event_Rotation_Gain_Reorientation(rotationInDegrees / redirectionManager.deltaDir, rotationInDegrees);
    }

    public void Initialize()
    {
        maxX = 0.5f * (redirectionManager.trackedSpace.localScale.x) - redirectionManager.resetTrigger.RESET_TRIGGER_BUFFER;// redirectionManager.resetTrigger.xLength);// + USER_CAPSULE_COLLIDER_DIAMETER);
        maxZ = 0.5f * (redirectionManager.trackedSpace.localScale.z) - redirectionManager.resetTrigger.RESET_TRIGGER_BUFFER;
        //print("PRACTICAL MAX X: " + maxX);
    }

    public bool IsUserOutOfBounds()
    {
        return Mathf.Abs(redirectionManager.currPosReal.x) >= maxX || Mathf.Abs(redirectionManager.currPosReal.z) >= maxZ;
    }


    Boundary getNearestBoundary()
    {
        Vector3 position = redirectionManager.currPosReal;
        if (position.x >= 0 && Mathf.Abs(maxX - position.x) <= Mathf.Min(Mathf.Abs(maxZ - position.z), Mathf.Abs(-maxZ - position.z))) // for a very wide rectangle, you can find that the first condition is actually necessary
            return Boundary.Right;
        if (position.x <= 0 && Mathf.Abs(-maxX - position.x) <= Mathf.Min(Mathf.Abs(maxZ - position.z), Mathf.Abs(-maxZ - position.z)))
            return Boundary.Left;
        if (position.z >= 0 && Mathf.Abs(maxZ - position.z) <= Mathf.Min(Mathf.Abs(maxX - position.x), Mathf.Abs(-maxX - position.x)))
            return Boundary.Top;
        return Boundary.Bottom;
    }

    Vector3 getAwayFromNearestBoundaryDirection()
    {
        Boundary nearestBoundary = getNearestBoundary();
        switch (nearestBoundary)
        {
            case Boundary.Top:
                return -Vector3.forward;
            case Boundary.Bottom:
                return Vector3.forward;
            case Boundary.Right:
                return -Vector3.right;
            case Boundary.Left:
                return Vector3.right;
        }
        return Vector3.zero;
    }

    float getUserAngleWithNearestBoundary() // Away from Wall is considered Zero
    {
        return Utilities.GetSignedAngle(redirectionManager.currDirReal, getAwayFromNearestBoundaryDirection());
    }

    protected bool isUserFacingAwayFromWall()
    {
        return Mathf.Abs(getUserAngleWithNearestBoundary()) < 90;
    }

    public float getTrackingAreaHalfDiameter()
    {
        return Mathf.Sqrt(maxX * maxX + maxZ * maxZ);
    }

    public float getDistanceToCenter()
    {
        return redirectionManager.currPosReal.magnitude;
    }

    public float getDistanceToNearestBoundary()
    {
        Vector3 position = redirectionManager.currPosReal;
        Boundary nearestBoundary = getNearestBoundary();
        switch (nearestBoundary)
        {
            case Boundary.Top:
                return Mathf.Abs(maxZ - position.z);
            case Boundary.Bottom:
                return Mathf.Abs(-maxZ - position.z);
            case Boundary.Right:
                return Mathf.Abs(maxX - position.x);
            case Boundary.Left:
                return Mathf.Abs(-maxX - position.x);
        }
        return 0;
    }

    public float getMaxWalkableDistanceBeforeReset()
    {
        Vector3 position = redirectionManager.currPosReal;
        Vector3 direction = redirectionManager.currDirReal;
        float tMaxX = direction.x != 0 ? Mathf.Max((maxX - position.x) / direction.x, (-maxX - position.x) / direction.x) : float.MaxValue;
        float tMaxZ = direction.z != 0 ? Mathf.Max((maxZ - position.z) / direction.z, (-maxZ - position.z) / direction.z) : float.MaxValue;
        //print("MaxX: " + maxX);
        //print("MaxZ: " + maxZ);
        return Mathf.Min(tMaxX, tMaxZ);
    }

}
