using UnityEngine;
using System.Collections;
using Redirection;


public class KeyboardController : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

    /// <summary>
    /// Auto-Adjust automatically counters curvature as human naturally would.
    /// </summary>
    [SerializeField]
    bool useAutoAdjust = true;

    /// <summary>
    /// Translation speed in meters per second.
    /// </summary>
    [SerializeField, Range(0.01f, 10)]
    float translationSpeed = 1f;

    /// <summary>
    /// Rotation speed in degrees per second.
    /// </summary>
    [SerializeField, Range(0.01f, 360)]
    float rotationSpeed = 90f;

    float lastCurvatureApplied = 0;
    //float lastRotationApplied = 0;
    //Vector3 lastTranslationApplied = Vector3.zero;
    

	// Use this for initialization
	void Start () {
	
	}

	// Update is called once per frame
	void Update () {
        if (!redirectionManager.simulationManager.userIsWalking || redirectionManager.MOVEMENT_CONTROLLER != RedirectionManager.MovementController.Keyboard)
            return;

        Vector3 userForward = Utilities.FlattenedDir3D(this.transform.forward);
        Vector3 userRight = Utilities.FlattenedDir3D(this.transform.right);

        if (Input.GetKey(KeyCode.W))
        {
            this.transform.Translate(translationSpeed * Time.deltaTime * userForward, Space.World);
        }
        if (Input.GetKey(KeyCode.S))
        {
            this.transform.Translate(-translationSpeed * Time.deltaTime * userForward, Space.World);
        }
        if (Input.GetKey(KeyCode.D))
        {
            this.transform.Translate(translationSpeed * Time.deltaTime * userRight, Space.World);
        }
        if (Input.GetKey(KeyCode.A))
        {
            this.transform.Translate(-translationSpeed * Time.deltaTime * userRight, Space.World);
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            this.transform.Rotate(userRight, -rotationSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.Rotate(userRight, rotationSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            this.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            this.transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime, Space.World);
        }


        if (useAutoAdjust)
        {
            this.transform.Rotate(Vector3.up, -lastCurvatureApplied, Space.World);
            lastCurvatureApplied = 0; // We set it to zero meaning we applied what was last placed. This prevents constant application of rotation when curvature isn't applied.

            //this.transform.Rotate(Vector3.up, -lastRotationApplied, Space.World);
            //lastRotationApplied = 0; // We set it to zero meaning we applied what was last placed. This prevents constant application of rotation when rotation isn't applied.

            //this.transform.Translate(-lastTranslationApplied, Space.World);
            //lastTranslationApplied = Vector3.zero; // We set it to zero meaning we applied what was last placed. This prevents constant application of translation when translation isn't applied.
        }
	}

    public void SetLastCurvature(float rotationInDegrees)
    {
        lastCurvatureApplied = rotationInDegrees;
        //if (useAutoAdjust)
        //{
        //    this.transform.Rotate(Vector3.up, -rotationInDegrees, Space.World);
        //}
    }

    public void SetLastRotation(float rotationInDegrees)
    {
        //lastRotationApplied = rotationInDegrees;
        //if (useAutoAdjust)
        //{
        //    this.transform.Rotate(Vector3.up, -rotationInDegrees, Space.World);
        //}
    }

    public void SetLastTranslation(Vector3 translation)
    {
        //lastTranslationApplied = translation;
        //if (useAutoAdjust)
        //{
        //    this.transform.Translate(-translation, Space.World);
        //}
    }
}
