using UnityEngine;
using System.Collections;

public class ResetTrigger : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;
    [HideInInspector]
    public Collider bodyCollider;

    [SerializeField, Range(0f, 1f)]
    public float RESET_TRIGGER_BUFFER = 0.5f;

    [HideInInspector]
    public float xLength, zLength;

    public void Initialize()
    {
        // Set Size of Collider
        float trimAmountOnEachSide = bodyCollider.transform.localScale.x + 2 * RESET_TRIGGER_BUFFER;
        this.transform.localScale = new Vector3(1 - (trimAmountOnEachSide / this.transform.parent.localScale.x), 2 / this.transform.parent.localScale.y, 1 - (trimAmountOnEachSide / this.transform.parent.localScale.z));
        xLength = this.transform.parent.localScale.x - trimAmountOnEachSide;
        zLength = this.transform.parent.localScale.z - trimAmountOnEachSide;
    }

    void OnTriggerEnter(Collider other)
    {

    }

    void OnTriggerExit(Collider other)
    {
        if (other == bodyCollider)
        {
            redirectionManager.OnResetTrigger();
        }
    }

    

}
