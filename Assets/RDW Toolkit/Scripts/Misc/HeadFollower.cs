using UnityEngine;
using System.Collections;

public class HeadFollower : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.position = redirectionManager.currPos;
        if (redirectionManager.currDir != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(redirectionManager.currDir, Vector3.up);
	}
}
