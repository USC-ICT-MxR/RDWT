using UnityEngine;
using System.Collections;

public class PoseFixer : MonoBehaviour {

    Vector3 fixedPosition;
    Quaternion fixedRotation;

    [SerializeField]
    bool fixPosition = true, fixRotation = true;


    void OnEnable()
    {
        fixedPosition = this.transform.position;
        fixedRotation = this.transform.rotation;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (fixPosition)
            this.transform.position = fixedPosition;
        if (fixRotation)
            this.transform.rotation = fixedRotation;
	}
}
