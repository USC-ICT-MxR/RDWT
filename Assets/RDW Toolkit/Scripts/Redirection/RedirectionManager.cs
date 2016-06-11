using UnityEngine;
using System.Collections;
using Redirection;
public class RedirectionManager : MonoBehaviour {

    public enum MovementController { Keyboard, AutoPilot, Tracker };

    [Tooltip("Select if you wish to run simulation from commandline in Unity batchmode.")]
    public bool runInTestMode = false;

    [Tooltip("How user movement is controlled.")]
    public MovementController MOVEMENT_CONTROLLER = MovementController.Tracker;
    
    [Tooltip("Maximum translation gain applied")]
    [Range(0, 5)]
    public float MAX_TRANS_GAIN = 0.26F;
    
    [Tooltip("Minimum translation gain applied")]
    [Range(-0.99F, 0)]
    public float MIN_TRANS_GAIN = -0.14F;
    
    [Tooltip("Maximum rotation gain applied")]
    [Range(0, 5)]
    public float MAX_ROT_GAIN = 0.49F;
    
    [Tooltip("Minimum rotation gain applied")]
    [Range(-0.99F, 0)]
    public float MIN_ROT_GAIN = -0.2F;

    [Tooltip("Radius applied by curvature gain")]
    [Range(1, 23)]
    public float CURVATURE_RADIUS = 7.5F;

    [Tooltip("The game object that is being physically tracked (probably user's head)")]
    public Transform headTransform;

    [Tooltip("Use simulated framerate in auto-pilot mode")]
    public bool useManualTime = false;

    [Tooltip("Target simulated framerate in auto-pilot mode")]
    public float targetFPS = 60;
    
    

    
    [HideInInspector]
    public Transform body;
    [HideInInspector]
    public Transform trackedSpace;
    [HideInInspector]
    public Transform simulatedHead;

    [HideInInspector]
    public Redirector redirector;
    [HideInInspector]
    public Resetter resetter;
    [HideInInspector]
    public ResetTrigger resetTrigger;
    [HideInInspector]
    public TrailDrawer trailDrawer;
    [HideInInspector]
    public SimulationManager simulationManager;
    [HideInInspector]
    public SimulatedWalker simulatedWalker;
    [HideInInspector]
    public KeyboardController keyboardController;
    [HideInInspector]
    public SnapshotGenerator snapshotGenerator;
    [HideInInspector]
    public StatisticsLogger statisticsLogger;
    [HideInInspector]
    public HeadFollower bodyHeadFollower;

    [HideInInspector]
    public Vector3 currPos, currPosReal, prevPos, prevPosReal;
    [HideInInspector]
    public Vector3 currDir, currDirReal, prevDir, prevDirReal;
    [HideInInspector]
    public Vector3 deltaPos;
    [HideInInspector]
    public float deltaDir;
    [HideInInspector]
    public Transform targetWaypoint;


    [HideInInspector]
    public bool inReset = false;

    [HideInInspector]
    public string startTimeOfProgram;

    private float simulatedTime = 0;

    void Awake()
    {
        startTimeOfProgram = System.DateTime.Now.ToString("yyyy MM dd HH:mm:ss");

        GetBody();
        GetTrackedSpace();
        GetSimulatedHead();

        GetSimulationManager();
        SetReferenceForSimulationManager();
        simulationManager.Initialize();

        GetRedirector();
        GetResetter();
        GetResetTrigger();
        GetTrailDrawer();
        
        GetSimulatedWalker();
        GetKeyboardController();
        GetSnapshotGenerator();
        GetStatisticsLogger();
        GetBodyHeadFollower();
        SetReferenceForRedirector();
        SetReferenceForResetter();
        SetReferenceForResetTrigger();
        SetBodyReferenceForResetTrigger();
        SetReferenceForTrailDrawer();
        
        SetReferenceForSimulatedWalker();
        SetReferenceForKeyboardController();
        SetReferenceForSnapshotGenerator();
        SetReferenceForStatisticsLogger();
        SetReferenceForBodyHeadFollower();

        // The rule is to have RedirectionManager call all "Awake"-like functions that rely on RedirectionManager as an "Initialize" call.
        resetTrigger.Initialize();
        // Resetter needs ResetTrigger to be initialized before initializing itself
        if (resetter != null)
            resetter.Initialize();

        if (runInTestMode)
        {
            MOVEMENT_CONTROLLER = MovementController.AutoPilot;
        }
        if (MOVEMENT_CONTROLLER != MovementController.Tracker)
        {
            headTransform = simulatedHead;
        }

    }

	// Use this for initialization
	void Start () {
        simulatedTime = 0;
        UpdatePreviousUserState();
	}
	
	// Update is called once per frame
	void Update () {

	}

    void LateUpdate()
    {
        simulatedTime += 1.0f / targetFPS;

        //if (MOVEMENT_CONTROLLER == MovementController.AutoPilot)
        //    simulatedWalker.WalkUpdate();

        UpdateCurrentUserState();
        CalculateStateChanges();

        // BACK UP IN CASE UNITY TRIGGERS FAILED TO COMMUNICATE RESET (Can happen in high speed simulations)
        if (resetter != null && !inReset && resetter.IsUserOutOfBounds())
        {
            Debug.LogWarning("Reset Aid Helped!");
            OnResetTrigger();
        }

        if (inReset)
        {
            if (resetter != null)
            {
                resetter.ApplyResetting();
            }
        }
        else
        {
            if (redirector != null)
            {
                redirector.ApplyRedirection();
            }
        }

        statisticsLogger.UpdateStats();

        UpdatePreviousUserState();

        UpdateBodyPose();
    }

    public float GetDeltaTime()
    {
        if (useManualTime)
            return 1.0f / targetFPS;
        else
            return Time.deltaTime;
    }

    public float GetTime()
    {
        if (useManualTime)
            return simulatedTime;
        else
            return Time.time;
    }

    void UpdateBodyPose()
    {
        body.position = Utilities.FlattenedPos3D(headTransform.position);
        body.rotation = Quaternion.LookRotation(Utilities.FlattenedDir3D(headTransform.forward), Vector3.up);
    }

    void SetReferenceForRedirector()
    {
        if (redirector != null)
            redirector.redirectionManager = this;
    }

    void SetReferenceForResetter()
    {
        if (resetter != null)
            resetter.redirectionManager = this;
    }

    void SetReferenceForResetTrigger()
    {
        if (resetTrigger != null)
            resetTrigger.redirectionManager = this;
    }

    void SetBodyReferenceForResetTrigger()
    {
        if (resetTrigger != null && body != null)
        {
            // NOTE: This requires that getBody gets called before this
            resetTrigger.bodyCollider = body.GetComponentInChildren<CapsuleCollider>();
        }
    }

    void SetReferenceForTrailDrawer()
    {
        if (trailDrawer != null)
        {
            trailDrawer.redirectionManager = this;
        }
    }

    void SetReferenceForSimulationManager()
    {
        if (simulationManager != null)
        {
            simulationManager.redirectionManager = this;
        }
    }

    void SetReferenceForSimulatedWalker()
    {
        if (simulatedWalker != null)
        {
            simulatedWalker.redirectionManager = this;
        }
    }

    void SetReferenceForKeyboardController()
    {
        if (keyboardController != null)
        {
            keyboardController.redirectionManager = this;
        }
    }

    void SetReferenceForSnapshotGenerator()
    {
        if (snapshotGenerator != null)
        {
            snapshotGenerator.redirectionManager = this;
        }
    }

    void SetReferenceForStatisticsLogger()
    {
        if (statisticsLogger != null)
        {
            statisticsLogger.redirectionManager = this;
        }
    }

    void SetReferenceForBodyHeadFollower()
    {
        if (bodyHeadFollower != null)
        {
            bodyHeadFollower.redirectionManager = this;
        }
    }

    void GetRedirector()
    {
        redirector = this.gameObject.GetComponent<Redirector>();
        if (redirector == null)
            this.gameObject.AddComponent<NullRedirector>();
        redirector = this.gameObject.GetComponent<Redirector>();
    }

    void GetResetter()
    {
        resetter = this.gameObject.GetComponent<Resetter>();
        if (resetter == null)
            this.gameObject.AddComponent<NullResetter>();
        resetter = this.gameObject.GetComponent<Resetter>();
    }

    void GetResetTrigger()
    {
        resetTrigger = this.gameObject.GetComponentInChildren<ResetTrigger>();
    }

    void GetTrailDrawer()
    {
        trailDrawer = this.gameObject.GetComponent<TrailDrawer>();
    }

    void GetSimulationManager()
    {
        simulationManager = this.gameObject.GetComponent<SimulationManager>();
    }

    void GetSimulatedWalker()
    {
        simulatedWalker = simulatedHead.GetComponent<SimulatedWalker>();
    }

    void GetKeyboardController()
    {
        keyboardController = simulatedHead.GetComponent<KeyboardController>();
    }

    void GetSnapshotGenerator()
    {
        snapshotGenerator = this.gameObject.GetComponent<SnapshotGenerator>();
    }

    void GetStatisticsLogger()
    {
        statisticsLogger = this.gameObject.GetComponent<StatisticsLogger>();
    }

    void GetBodyHeadFollower()
    {
        bodyHeadFollower = body.GetComponent<HeadFollower>();
    }

    void GetBody()
    {
        body = transform.FindChild("Body");
    }

    void GetTrackedSpace()
    {
        trackedSpace = transform.FindChild("Tracked Space");
    }

    void GetSimulatedHead()
    {
        simulatedHead = transform.FindChild("Simulated User").FindChild("Head");
    }

    void GetTargetWaypoint()
    {
        targetWaypoint = transform.FindChild("Target Waypoint").gameObject.transform;
    }

    void UpdateCurrentUserState()
    {
        currPos = Utilities.FlattenedPos3D(headTransform.position);
        currPosReal = Utilities.GetRelativePosition(currPos, this.transform);
        currDir = Utilities.FlattenedDir3D(headTransform.forward);
        currDirReal = Utilities.FlattenedDir3D(Utilities.GetRelativeDirection(currDir, this.transform));
    }

    void UpdatePreviousUserState()
    {
        prevPos = Utilities.FlattenedPos3D(headTransform.position);
        prevPosReal = Utilities.GetRelativePosition(prevPos, this.transform);
        prevDir = Utilities.FlattenedDir3D(headTransform.forward);
        prevDirReal = Utilities.FlattenedDir3D(Utilities.GetRelativeDirection(prevDir, this.transform));
    }

    void CalculateStateChanges()
    {
        deltaPos = currPos - prevPos;
        deltaDir = Utilities.GetSignedAngle(prevDir, currDir);
    }

    public void OnResetTrigger()
    {
        //print("RESET TRIGGER");
        if (inReset)
            return;
        //print("NOT IN RESET");
        //print("Is Resetter Null? " + (resetter == null));
        if (resetter != null && resetter.IsResetRequired())
        {
            //print("RESET WAS REQUIRED");
            resetter.InitializeReset();
            inReset = true;
        }
    }

    public void OnResetEnd()
    {
        //print("RESET END");
        resetter.FinalizeReset();
        inReset = false;
    }

    public void RemoveRedirector()
    {
        this.redirector = this.gameObject.GetComponent<Redirector>();
        if (this.redirector != null)
            Destroy(redirector);
        redirector = null;
    }

    public void UpdateRedirector(System.Type redirectorType)
    {
        RemoveRedirector();
        this.redirector = (Redirector) this.gameObject.AddComponent(redirectorType);
        //this.redirector = this.gameObject.GetComponent<Redirector>();
        SetReferenceForRedirector();
    }

    public void RemoveResetter()
    {
        this.resetter = this.gameObject.GetComponent<Resetter>();
        if (this.resetter != null)
            Destroy(resetter);
        resetter = null;
    }

    public void UpdateResetter(System.Type resetterType)
    {
        RemoveResetter();
        if (resetterType != null)
        {
            this.resetter = (Resetter) this.gameObject.AddComponent(resetterType);
            //this.resetter = this.gameObject.GetComponent<Resetter>();
            SetReferenceForResetter();
            if (this.resetter != null)
                this.resetter.Initialize();
        }
    }

    public void UpdateTrackedSpaceDimensions(float x, float z)
    {
        trackedSpace.localScale = new Vector3(x, 1, z);
        resetTrigger.Initialize();
        if (this.resetter != null)
            this.resetter.Initialize();
    }
}
