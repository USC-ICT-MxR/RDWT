using UnityEngine;
using System.Collections;
using Redirection;

public abstract class Redirector : MonoBehaviour
{
    [HideInInspector]
    public RedirectionManager redirectionManager;

    
      
    /// <summary>
    /// Applies redirection based on the algorithm.
    /// </summary>
    public abstract void ApplyRedirection();

    /// <summary>
    /// Applies rotation to Redirected User. The neat thing about calling it this way is that we can keep track of gains applied.
    /// </summary>
    /// <param name="rotationInDegrees"></param>
    protected void InjectRotation(float rotationInDegrees)
    {
        if (rotationInDegrees != 0)
        {
            this.transform.RotateAround(Utilities.FlattenedPos3D(redirectionManager.headTransform.position), Vector3.up, rotationInDegrees);
            this.GetComponentInChildren<KeyboardController>().SetLastRotation(rotationInDegrees);
            redirectionManager.statisticsLogger.Event_Rotation_Gain(rotationInDegrees / redirectionManager.deltaDir, rotationInDegrees);
        }
    }


    /// <summary>
    /// Applies curvature to Redirected User. The neat thing about calling it this way is that we can keep track of gains applied.
    /// </summary>
    /// <param name="rotationInDegrees"></param>
    protected void InjectCurvature(float rotationInDegrees)
    {
        if (rotationInDegrees != 0)
        {
            this.transform.RotateAround(Utilities.FlattenedPos3D(redirectionManager.headTransform.position), Vector3.up, rotationInDegrees);
            this.GetComponentInChildren<KeyboardController>().SetLastCurvature(rotationInDegrees);
            redirectionManager.statisticsLogger.Event_Curvature_Gain(rotationInDegrees / redirectionManager.deltaPos.magnitude, rotationInDegrees);
        }
    }

    /// <summary>
    /// Applies rotation to Redirected User. The neat thing about calling it this way is that we can keep track of gains applied.
    /// </summary>
    /// <param name="translation"></param>
    protected void InjectTranslation(Vector3 translation)
    {
        if (translation.magnitude > 0)
        {
            this.transform.Translate(translation, Space.World);
            this.GetComponentInChildren<KeyboardController>().SetLastTranslation(translation);
            redirectionManager.statisticsLogger.Event_Translation_Gain(Mathf.Sign(Vector3.Dot(translation, redirectionManager.deltaPos)) * translation.magnitude / redirectionManager.deltaPos.magnitude, Utilities.FlattenedPos2D(translation));
            if (double.IsNaN(Mathf.Sign(Vector3.Dot(translation, redirectionManager.deltaPos)) * translation.magnitude / redirectionManager.deltaPos.magnitude))
                print("wtf");
        }
    }


    
}
