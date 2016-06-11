using UnityEngine;
using System.Collections;
using Redirection;

public class S2ORedirector : SteerToRedirector {


    private const float S2O_TARGET_GENERATION_ANGLE_IN_DEGREES = 60;
    public float S2O_TARGET_RADIUS = 5.0f; //Target orbit radius for Steer-to-Orbit algorithm (meters)


    public override void PickRedirectionTarget()
    {
        Vector3 trackingAreaPosition = Utilities.FlattenedPos3D(redirectionManager.trackedSpace.position);
        Vector3 userToCenter = trackingAreaPosition - redirectionManager.currPos;

        //Compute steering target for S2O
        if (noTmpTarget)
        {
            tmpTarget = new GameObject("S2O Target");
            currentTarget = tmpTarget.transform;
            noTmpTarget = false;
        }

        //Step One: Compute angles for direction from center to potential targets
        float alpha;
        //Where is user relative to desired orbit?
        if (userToCenter.magnitude < S2O_TARGET_RADIUS) //Inside the orbit
        {
            alpha = S2O_TARGET_GENERATION_ANGLE_IN_DEGREES;
        }
        else
        {
            //Use tangents of desired orbit
            alpha = Mathf.Acos(S2O_TARGET_RADIUS / userToCenter.magnitude) * Mathf.Rad2Deg;
        }
        //Step Two: Find directions to two petential target positions
        Vector3 dir1 = Quaternion.Euler(0, alpha, 0) * -userToCenter.normalized;
        Vector3 targetPosition1 = trackingAreaPosition + S2O_TARGET_RADIUS * dir1;
        Vector3 dir2 = Quaternion.Euler(0, -alpha, 0) * -userToCenter.normalized;
        Vector3 targetPosition2 = trackingAreaPosition + S2O_TARGET_RADIUS * dir2;

        //Step Three: Evaluate difference in direction
        // We don't care about angle sign here
        float angle1 = Vector3.Angle(redirectionManager.currDir, targetPosition1 - redirectionManager.currPos);
        float angle2 = Vector3.Angle(redirectionManager.currDir, targetPosition2 - redirectionManager.currPos);

        currentTarget.transform.position = (angle1 <= angle2) ? targetPosition1 : targetPosition2;
    }


}
