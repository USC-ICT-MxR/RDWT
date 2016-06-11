using UnityEngine;
using System.Collections;
using Redirection;

public class S2CRedirector : SteerToRedirector {


    // Testing Parameters
    bool dontUseTempTargetInS2C = false;
    

    private const float S2C_BEARING_ANGLE_THRESHOLD_IN_DEGREE = 160;
    private const float S2C_TEMP_TARGET_DISTANCE = 4;

    public override void PickRedirectionTarget()
    {
        Vector3 trackingAreaPosition = Utilities.FlattenedPos3D(redirectionManager.trackedSpace.position);
        Vector3 userToCenter = trackingAreaPosition - redirectionManager.currPos;

        //Compute steering target for S2C
        float bearingToCenter = Vector3.Angle(userToCenter, redirectionManager.currDir);
        float directionToCenter = Utilities.GetSignedAngle(redirectionManager.currDir, userToCenter);
        if (bearingToCenter >= S2C_BEARING_ANGLE_THRESHOLD_IN_DEGREE && !dontUseTempTargetInS2C)
        {
            //Generate temporary target
            if (noTmpTarget)
            {
                tmpTarget = new GameObject("S2C Temp Target");
                tmpTarget.transform.position = redirectionManager.currPos + S2C_TEMP_TARGET_DISTANCE * (Quaternion.Euler(0, directionToCenter * 90, 0) * redirectionManager.currDir);
                tmpTarget.transform.parent = transform;
                noTmpTarget = false;
            }
            currentTarget = tmpTarget.transform;
        }
        else
        {
            currentTarget = redirectionManager.trackedSpace;
            if (!noTmpTarget)
            {
                GameObject.Destroy(tmpTarget);
                noTmpTarget = true;
            }
        }
    }

}
