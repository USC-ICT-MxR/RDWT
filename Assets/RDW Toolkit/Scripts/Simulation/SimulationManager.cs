using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Redirection;

public class SimulationManager : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

    //enum AlgorithmChoice { S2C, S2O, GreedyTransGain, S2C_GreedyTransGain, S2O_GreedyTransGain, CenterBased, CenterBasedTransGainSpeedUp, S2C_CenterBasedTransGainSpeedUp, S2O_CenterBasedTransGainSpeedUp, None };
    enum ExperimentChoice { FixedTrackedSpace, VaryingSizes, VaryingShapes };
    enum AlgorithmChoice {None, S2C, S2O, Zigzag};
    enum PathSeedChoice { Office, ExplorationSmall, ExplorationLarge, LongWalk, ZigZag };
    enum ResetChoice { None, TwoOneTurn };

    //[SerializeField]
    //bool showUserStartAndEndInLastSnapshot;

    [HideInInspector]
    public static string commandLineRunCode = "";

    // Experiment Variables
    System.Type redirector = null;
    System.Type resetter = null;
    List<VirtualPathGenerator.PathSeed> pathSeeds = new List<VirtualPathGenerator.PathSeed>();
    List<TrackingSizeShape> trackingSizes = new List<TrackingSizeShape>();
    List<InitialConfiguration> initialConfigurations = new List<InitialConfiguration>();
    List<Vector3> gainScaleFactors = new List<Vector3>();

    [SerializeField]
    bool runInSimulationMode = false;

    [SerializeField]
    AlgorithmChoice condAlgorithm;

    [SerializeField]
    ResetChoice condReset;

    [SerializeField]
    PathSeedChoice condPath;

    [SerializeField]
    ExperimentChoice condExperiment;


    [SerializeField]
    float MAX_TRIALS = 10f;
    
    [SerializeField]
    bool runAtFullSpeed = false;
    [SerializeField]
    public bool onlyRandomizeForward = true;
    [SerializeField]
    bool averageTrialResults = false;
    [SerializeField]
    public float DISTANCE_TO_WAYPOINT_THRESHOLD = 0.3f; // Maximum distance requirement to trigger waypoint

    float zigLength = 5.5f;
    float zagAngle = 140;
    int zigzagWaypointCount = 6;

    float trialsForCurrentExperiment = 5;

    bool takeScreenshot = false;
    private float framesInExperiment = 0;

    List<ExperimentSetup> experimentSetups;
    int experimentIterator = 0;
    private bool experimentComplete = false;
    [HideInInspector]
    public bool experimentInProgress = false;
    [HideInInspector]
    public List<Vector2> waypoints;
    [HideInInspector]
    public int waypointIterator = 0;
    [HideInInspector]
    public bool userIsWalking = false;

    

    public struct InitialConfiguration
    {
        public Vector2 initialPosition;
        public Vector2 initialForward;
        public bool isRandom;
        public InitialConfiguration(Vector2 initialPosition, Vector2 initialForward)
        {
            this.initialPosition = initialPosition;
            this.initialForward = initialForward;
            isRandom = false;
        }
        public InitialConfiguration(bool isRandom) // For Creating Random Configuration or just default of center/up
        {
            this.initialPosition = Vector2.zero;
            this.initialForward = Vector2.up;
            this.isRandom = isRandom;
        }
    }

    struct TrackingSizeShape
    {
        public float x, z;
        public TrackingSizeShape(float x, float z)
        {
            this.x = x;
            this.z = z;
        }
    }

    struct ExperimentSetup
    {
        public System.Type redirector;
        public System.Type resetter;
        public VirtualPathGenerator.PathSeed pathSeed;
        public TrackingSizeShape trackingSizeShape;
        public InitialConfiguration initialConfiguration;
        public Vector3 gainScaleFactor;
        public ExperimentSetup(System.Type redirector, System.Type resetter, VirtualPathGenerator.PathSeed pathSeed, TrackingSizeShape trackingSizeShape, InitialConfiguration initialConfiguration, Vector3 gainScaleFactor)
        {
            this.redirector = redirector;
            this.resetter = resetter;
            this.pathSeed = pathSeed;
            this.trackingSizeShape = trackingSizeShape;
            this.initialConfiguration = initialConfiguration;
            this.gainScaleFactor = gainScaleFactor;
        }
    }

    VirtualPathGenerator.PathSeed getPathSeedOfficeBuilding()
    {
        VirtualPathGenerator.SamplingDistribution distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 2, 8);
        VirtualPathGenerator.SamplingDistribution angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 90, 90, VirtualPathGenerator.AlternationType.Random);
        int waypointCount = 200;
        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }

    VirtualPathGenerator.PathSeed getPathSeedZigzag()
    {
        VirtualPathGenerator.SamplingDistribution distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, zigLength, zigLength);
        VirtualPathGenerator.SamplingDistribution angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, zagAngle, zagAngle, VirtualPathGenerator.AlternationType.Constant);
        int waypointCount = zigzagWaypointCount;
        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }

    VirtualPathGenerator.PathSeed getPathSeedExplorationSmall()
    {
        VirtualPathGenerator.SamplingDistribution distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 2, 6);
        VirtualPathGenerator.SamplingDistribution angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, -180, 180);
        int waypointCount = 250;
        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }

    VirtualPathGenerator.PathSeed getPathSeedExplorationLarge()
    {
        VirtualPathGenerator.SamplingDistribution distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform,8, 12);
        VirtualPathGenerator.SamplingDistribution angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, -180, 180);
        int waypointCount = 100;
        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }

    VirtualPathGenerator.PathSeed getPathSeedLongCorridor()
    {
        VirtualPathGenerator.SamplingDistribution distanceSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 1000, 1000);
        VirtualPathGenerator.SamplingDistribution angleSamplingDistribution = new VirtualPathGenerator.SamplingDistribution(VirtualPathGenerator.DistributionType.Uniform, 0, 0);
        int waypointCount = 1;
        return new VirtualPathGenerator.PathSeed(distanceSamplingDistribution, angleSamplingDistribution, waypointCount);
    }

    void setUpExperimentFixedTrackingArea(PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
            case PathSeedChoice.ZigZag:
                pathSeeds.Add(getPathSeedZigzag());
                break;
        }

        trackingSizes.Add(new TrackingSizeShape(redirectionManager.trackedSpace.localScale.x, redirectionManager.trackedSpace.localScale.z));

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        gainScaleFactors.Add(Vector3.one);
    }

    void setUpExperimentTrackingAreaSizePerformance(PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
            case PathSeedChoice.ZigZag:
                pathSeeds.Add(getPathSeedZigzag());
                break;
        }

        for (int i = 2; i <= 60; i += 1)
        {
            trackingSizes.Add(new TrackingSizeShape(i, i));
        }

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        gainScaleFactors.Add(Vector3.one);
    }

    void setUpExperimentTrackingAreaShape(PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
            case PathSeedChoice.ZigZag:
                pathSeeds.Add(getPathSeedZigzag());
                break;
        }

        for (int area = 100; area <= 200; area += 50)
        {
            for (float ratio = 1; ratio <= 2; ratio += 0.5f)
            {
                trackingSizes.Add(new TrackingSizeShape(Mathf.Sqrt(area) / Mathf.Sqrt(ratio), Mathf.Sqrt(area) * Mathf.Sqrt(ratio)));
            }
        }

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(1, 0)));
        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), Vector2.one)); // HACK: THIS NON-NORMALIZED ORIENTATION WILL INDICATE DIAGONAL AND WILL BE FIXED LATER
        gainScaleFactors.Add(Vector3.one);
    }

    /*
    void setUpExperimentGainFactors(PathSeedChoice pathSeedChoice, List<Redirector> redirectors, List<Resetter> resetters)
    {
        // Initialize Values
        this.redirectors = redirectors;
        this.resetters = resetters;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        TRIALS_PER_EXPERIMENT = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
        }

        trackingSizes.Add(new TrackingSizeShape(10, 10));

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));

        for (float g_t = 0; g_t <= 1.5f; g_t += 0.5f)
        {
            for (float g_r = 0; g_r <= 1.5f; g_r += 0.5f)
            {
                for (float g_c = 0; g_c <= 1.5f; g_c += 0.5f)
                {
                    gainScaleFactors.Add(new Vector3(g_t, g_r, g_c));
                }
            }
        }
    }
    */

    private void GenerateAllExperimentSetups()
    {
        // Here we generate the correspondign experiments
        experimentSetups = new List<ExperimentSetup>();
        foreach (VirtualPathGenerator.PathSeed pathSeed in pathSeeds)
        {
            foreach (TrackingSizeShape trackingSize in trackingSizes)
            {
                foreach (InitialConfiguration initialConfiguration in initialConfigurations)
                {
                    foreach (Vector3 gainScaleFactor in gainScaleFactors)
                    {
                        for (int i = 0; i < trialsForCurrentExperiment; i++)
                        {
                            experimentSetups.Add(new ExperimentSetup(redirector, resetter, pathSeed, trackingSize, initialConfiguration, gainScaleFactor));
                        }
                    }
                }
            }
        }
    }

    void startNextExperiment()
    {
        Debug.Log("---------- EXPERIMENT STARTED ----------");

        ExperimentSetup setup = experimentSetups[experimentIterator];

        printExperimentDescriptor(getExperimentDescriptor(setup));

        // Setting Gain Scale Factors
        //RedirectionManager.SCALE_G_T = setup.gainScaleFactor.x;
        //RedirectionManager.SCALE_G_R = setup.gainScaleFactor.y;
        //RedirectionManager.SCALE_G_C = setup.gainScaleFactor.z;

        // Enabling/Disabling Redirectors
        redirectionManager.UpdateRedirector(setup.redirector);
        redirectionManager.UpdateResetter(setup.resetter);

        // Setup Trail Drawing
        redirectionManager.trailDrawer.enabled = !runAtFullSpeed;
        
        // Enable User Rendering
        SetUserBodyVisibility(true);
        
        // Enable Waypoint
        redirectionManager.targetWaypoint.gameObject.SetActive(true);

        // Resetting User and World Positions and Orientations
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;
        // ESSENTIAL BUG FOUND: If you set the user first and then the redirection recipient, then the user will be moved, so you have to make sure to do it afterwards!
        //Debug.Log("Target User Position: " + setup.initialConfiguration.initialPosition.ToString("f4"));
        redirectionManager.headTransform.position = Utilities.UnFlatten(setup.initialConfiguration.initialPosition, redirectionManager.headTransform.position.y);
        //Debug.Log("Result User Position: " + redirectionManager.userHeadTransform.transform.position.ToString("f4"));
        redirectionManager.headTransform.rotation = Quaternion.LookRotation(Utilities.UnFlatten(setup.initialConfiguration.initialForward), Vector3.up);

        // Set up Tracking Area Dimensions
        redirectionManager.UpdateTrackedSpaceDimensions(setup.trackingSizeShape.x, setup.trackingSizeShape.z);
        
        // Adjust Top View Camera Size
        AdjustCameraSizes();
        AdjustTrailWidth();
        
        // Adjust Screenshot Generator Dimensions
        AdjustSnapshotGeneratorDimensions();

        // Set up Virtual Path
        float sumOfDistances, sumOfRotations;
        waypoints = VirtualPathGenerator.generatePath(setup.pathSeed, setup.initialConfiguration.initialPosition, setup.initialConfiguration.initialForward, out sumOfDistances, out sumOfRotations);
        Debug.Log("sumOfDistances: " + sumOfDistances);
        Debug.Log("sumOfRotations: " + sumOfRotations);
        if (setup.redirector == typeof(ZigZagRedirector))
        {
            // Create Fake POIs
            Transform poiRoot = (new GameObject()).transform;
            poiRoot.name = "ZigZag Redirector Waypoints";
            poiRoot.localPosition = Vector3.zero;
            poiRoot.localRotation = Quaternion.identity;
            Transform poi0 = (new GameObject()).transform;
            poi0.localPosition = Vector3.zero;
            poi0.parent = poiRoot;
            List<Transform> zigzagRedirectorWaypoints = new List<Transform>();
            zigzagRedirectorWaypoints.Add(poi0);
            foreach (Vector2 waypoint in waypoints)
            {
                Transform poi = (new GameObject()).transform;
                poi.localPosition = Utilities.UnFlatten(waypoint);
                poi.parent = poiRoot;
                zigzagRedirectorWaypoints.Add(poi);
            }
            ((ZigZagRedirector)redirectionManager.redirector).waypoints = zigzagRedirectorWaypoints;
        }

        // NO LONGER SUPPORTING DRAWING FULL VIRTUAL PATH AT BEGINNING
        //if (drawVirtualPath)
        //    virtualPath = redirectionManager.realTrailDrawer.drawPath(setup.initialConfiguration.initialPosition, waypoints, virtualPathColor, null);

        // Set First Waypoint Position and Enable It
        redirectionManager.targetWaypoint.position = new Vector3(waypoints[0].x, redirectionManager.targetWaypoint.position.y, waypoints[0].y);
        waypointIterator = 0;

        // POSTPONING THESE FOR SAFETY REASONS!
        //// Allow Walking
        //UserController.allowWalking = true;

        //// Start Logging
        //redirectionManager.redirectionStatistics.beginLogging();
        //redirectionManager.statisticsLogger.beginLogging();

        //lastExperimentRealStartTime = Time.realtimeSinceStartup;
        experimentInProgress = true;
    }

    void endExperiment()
    {
        //Debug.LogWarning("Last Experiment Length: " + (Time.realtimeSinceStartup - lastExperimentRealStartTime));

        ExperimentSetup setup = experimentSetups[experimentIterator];

        // Stop Trail Drawing
        redirectionManager.trailDrawer.enabled = false;

        // Delete Virtual Path
        // THIS CAN BE MADE OPTIONAL IF NECESSARY
        redirectionManager.trailDrawer.ClearTrail(TrailDrawer.VIRTUAL_TRAIL_NAME);

        // Disable User Rendering
        SetUserBodyVisibility(false);

        // Disable Waypoint
        redirectionManager.targetWaypoint.gameObject.SetActive(true);

        // Disallow Walking
        userIsWalking = false;

        // Stop Logging
        redirectionManager.statisticsLogger.EndLogging();

        // Gather Summary Statistics
        redirectionManager.statisticsLogger.experimentResults.Add(redirectionManager.statisticsLogger.GetExperimentResultForSummaryStatistics(getExperimentDescriptor(setup)));

        // Log Sampled Metrics
        if (redirectionManager.statisticsLogger.logSampleVariables)
        {
            Dictionary<string, List<float>> oneDimensionalSamples;
            Dictionary<string, List<Vector2>> twoDimensionalSamples;
            redirectionManager.statisticsLogger.GetExperimentResultsForSampledVariables(out oneDimensionalSamples, out twoDimensionalSamples);
            redirectionManager.statisticsLogger.LogAllExperimentSamples(experimentDescriptorToString(getExperimentDescriptor(setup)), oneDimensionalSamples, twoDimensionalSamples);
        }

        // Take Snapshot In Next Frame (After User and Virtual Path Is Disabled)
        if (!runAtFullSpeed)
            takeScreenshot = true;

        // Show User Beging and End
        // We are doing this hackingly by abusing the user and waypoint's default color
        //if (showUserStartAndEndInLastSnapshot)
        //{
        //    // Place User Body At End Point (Becuase of Red) (Already There By Default)
        //    redirectionManager.userBody.gameObject.SetActive(true);
        //    redirectionManager.userOrientationIndicator.gameObject.SetActive(false);
        //    // Place Waypoint At Initial Position
        //    redirectionManager.getNextWaypointTransform().gameObject.SetActive(true);
        //    Vector3 waypointPosition = redirectionManager.simulatedFreezeReset.trackingAreaCoordsToWorldCoordsForPosition(setup.initialConfiguration.initialPosition);
        //    redirectionManager.getNextWaypointTransform().position = new Vector3(waypointPosition.x, redirectionManager.getNextWaypointTransform().position.y, waypointPosition.z);
        //}

        // Prepared for new experiment
        experimentIterator++;
        //lastExperimentEndTime = Time.time;
        experimentInProgress = false;

        // Log All Summary Statistics To File
        if (experimentIterator == experimentSetups.Count)
        {
            if (averageTrialResults)
                redirectionManager.statisticsLogger.experimentResults = mergeTrialSummaryStatistics(redirectionManager.statisticsLogger.experimentResults);
            //redirectionManager.statisticsLogger.LogExperimentSummaryStatisticsResults(redirectionManager.statisticsLogger.experimentResults);
            redirectionManager.statisticsLogger.LogExperimentSummaryStatisticsResultsSCSV(redirectionManager.statisticsLogger.experimentResults);
            Debug.Log("Last Experiment Complete");
            experimentComplete = true;
            if (redirectionManager.runInTestMode)
                Application.Quit();
        }

        // Disabling Redirectors
        redirectionManager.RemoveRedirector();
        redirectionManager.RemoveResetter();
    }

    void InstantiateSimulationPrefab()
    {
        Transform waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Destroy(waypoint.GetComponent<SphereCollider>());
        redirectionManager.targetWaypoint = waypoint;
        waypoint.name = "Simulated Waypoint";
        waypoint.position = 1.2f * Vector3.up + 1000 * Vector3.forward;
        waypoint.localScale = 0.3f * Vector3.one;
        waypoint.GetComponent<Renderer>().material.color = new Color(0, 1, 0);
        waypoint.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, 0.12f, 0));
    }
    
    public void Initialize()
    {
        redirectionManager.runInTestMode = runInSimulationMode;
        userIsWalking = !(redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.AutoPilot);
        if (redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.AutoPilot)
            DISTANCE_TO_WAYPOINT_THRESHOLD = 0.05f;// 0.0001f;
        
        if (redirectionManager.MOVEMENT_CONTROLLER != RedirectionManager.MovementController.Tracker)
        {
            InstantiateSimulationPrefab();
        }

        if (redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.Tracker)
            return;

        //redirectionManager.simulationDataLogger.generateBatchFiles();

        // Read From Command Line
        //if (redirectionManager.runInTestMode)
        //{
        //    if (System.Environment.GetCommandLineArgs().Length > 1)
        //    {
        //        commandLineRunCode = System.Environment.GetCommandLineArgs()[1].Substring(0, 1) == "-" ? System.Environment.GetCommandLineArgs()[2] : System.Environment.GetCommandLineArgs()[1];
        //        Debug.Log("Run Code: " + commandLineRunCode);
        //    }
        //    else
        //        redirectionManager.runInTestMode = false;
        //}

        // Setting Random Seed
        Random.seed = VirtualPathGenerator.RANDOM_SEED;

        // Make sure VSync doesn't slow us down
        
        //Debug.Log("Application.targetFrameRate: " + Application.targetFrameRate);

        if (runAtFullSpeed && this.enabled)
        {
            //redirectionManager.topViewCamera.enabled = false;
            //drawVirtualPath = false;
            QualitySettings.vSyncCount = 0;
        }

        // Also Determine Time Scale
        //if (this.enabled)
        //{
        //    //Time.timeScale = timeScale;
        //    //Time.fixedDeltaTime *= timeScale;
        //}

        // Initialization
        experimentIterator = 0;
        //if (this.enabled)
        //    redirectionManager.userMovementManager.activateSimulatedWalker();

        /*
        // Here we manually determine what we want to run
        //algorithms.Add(AlgorithmChoice.GreedyTransGain);
        //algorithms.Add(AlgorithmChoice.CenterBased);
        //algorithms.Add(AlgorithmChoice.None);
        algorithms.Add(AlgorithmChoice.S2C);
        //algorithms.Add(AlgorithmChoice.S2O);
        
        //pathSeeds.Add(new SimulationPathGenerator.PathSeed(new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, false, 100, 100), new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, true, 0, 0), 1));
        pathSeeds.Add(new SimulationPathGenerator.PathSeed(new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, false, 1000, 1000), new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, true, 0, 0), 1));

        //SimulationPathGenerator.SamplingDistribution distanceDistribution = new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, false, 8, 10);
        //SimulationPathGenerator.SamplingDistribution angleDistribution = new SimulationPathGenerator.SamplingDistribution(SimulationPathGenerator.DistributionType.Uniform, true, Mathf.PI / 4, 3 * Mathf.PI / 4);
        //pathSeeds.Add(new SimulationPathGenerator.PathSeed(distanceDistribution, angleDistribution, 1));

        //for (int i = 2; i <= 30; i += 2)
        //{
        //    trackingSizes.Add(new TrackingSizeShape(i, i));
        //}

        //trackingSizes.Add(new TrackingSizeShape(20, 20));
        //trackingSizes.Add(new TrackingSizeShape(5f, 1.25f));
        //trackingSizes.Add(new TrackingSizeShape(20, 5));
        //trackingSizes.Add(new TrackingSizeShape(100, 20));
        //trackingSizes.Add(new TrackingSizeShape(5, 5));
        //trackingSizes.Add(new TrackingSizeShape(5, 20));
        //trackingSizes.Add(new TrackingSizeShape(10, 10));
        trackingSizes.Add(new TrackingSizeShape(32, 32));
        //trackingSizes.Add(new TrackingSizeShape(50, 50));
        //trackingSizes.Add(new TrackingSizeShape(8, 8));
        //trackingSizes.Add(new TrackingSizeShape(10, 10));

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        //initialConfigurations.Add(new InitialConfiguration(new Vector2(7.5f, 0), new Vector2(0, 1)));
        //initialConfigurations.Add(new InitialConfiguration(true)); // Random Config
        //initialConfigurations.Add(new InitialConfiguration(new Vector2(40, -40), new Vector2(0, 1)));
        */

        //// MANUAL TESTING
        //redirectionManager.runInTestMode = true;
        //commandLineRunCode = "1421";
        //commandLineRunCode = "2531";
        //commandLineRunCode = "242";
        //commandLineRunCode = "244";

        
        if (redirectionManager.runInTestMode)
        {
            //print("EXP SETUP");
            //int expCode = int.Parse(commandLineRunCode.Substring(0, 1));
            //int pathCode = int.Parse(commandLineRunCode.Substring(1, 1));
            //int algoCode = int.Parse(commandLineRunCode.Substring(2, 1));
            //int resetCode = int.Parse(commandLineRunCode.Substring(3, 1));

            
            System.Type redirectorType = null;
            System.Type resetterType = null;
            switch (condAlgorithm)
            {
                case AlgorithmChoice.None:
                    redirectorType = typeof(NullRedirector);
                    break;
                case AlgorithmChoice.S2C:
                    redirectorType = typeof(S2CRedirector);
                    break;
                case AlgorithmChoice.S2O:
                    redirectorType = typeof(S2ORedirector);
                    break;
                case AlgorithmChoice.Zigzag:
                    redirectorType = typeof(ZigZagRedirector);
                    break;
                //case 4:
                //    algorithmChoice = AlgorithmChoice.CenterBasedTransGainSpeedUp;
                //    break;
                //case 5:
                //    algorithmChoice = AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp;
                //    break;
                //case 6:
                //    algorithmChoice = AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp;
                //    break;
            }
            switch (condReset)
            {
                case ResetChoice.None:
                    resetterType = typeof(NullResetter);
                    break;
                case ResetChoice.TwoOneTurn:
                    resetterType = typeof(TwoOneTurnResetter);
                    break;
            }
            // BY DEFAULT ONLY ONE TYPE
            //resetterType = typeof(TwoOneTurnResetter);

            //Debug.Log("Algorithm: " + algoCode);
            //switch (pathCode)
            //{
            //    case 1:
            //        pathSeedChoice = PathSeedChoice.Office;
            //        break;
            //    case 2:
            //        pathSeedChoice = PathSeedChoice.ExplorationSmall;
            //        break;
            //    case 3:
            //        pathSeedChoice = PathSeedChoice.ExplorationLarge;
            //        break;
            //    case 4:
            //        pathSeedChoice = PathSeedChoice.LongWalk;
            //        break;
            //    case 5:
            //        pathSeedChoice = PathSeedChoice.ZigZag;
            //        break;
            //}
            //Debug.Log("PathSeed: " + pathSeedChoice);
            switch (condExperiment)
            {
                case ExperimentChoice.FixedTrackedSpace:
                    setUpExperimentFixedTrackingArea(condPath, redirectorType, resetterType);
                    break;
                case ExperimentChoice.VaryingSizes:
                    setUpExperimentTrackingAreaSizePerformance(condPath, redirectorType, resetterType);
                    break;
                case ExperimentChoice.VaryingShapes:
                    setUpExperimentTrackingAreaShape(condPath, redirectorType, resetterType);
                    break;
                //case 3:
                //    setUpExperimentGainFactors(pathSeedChoice, redirectors, resetters);
                //    break;
            }

        }

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.Office, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaSizePerformance(PathSeedChoice.LongWalk, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);



        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.Office, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.None);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2C);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2O);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentTrackingAreaShape(PathSeedChoice.LongWalk, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);



        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.Office, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationSmall, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.ExplorationLarge, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);

        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.None);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2C);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2O);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2C_CenterBasedTransGainSpeedUp);
        //setUpExperimentGainFactors(PathSeedChoice.LongWalk, AlgorithmChoice.S2O_CenterBasedTransGainSpeedUp);


        GenerateAllExperimentSetups();
        
        // Determine Initial Configurations If Random
        determineInitialConfigurations(ref experimentSetups);
    }
	
    // Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
    void Update()
    {
        if (redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.Tracker)
            return;
        //framesGoneBy++;
        //if (firstUpdateRealTime == 0)
        //    firstUpdateRealTime = Time.realtimeSinceStartup;
        //if (Time.realtimeSinceStartup - firstUpdateRealTime > 1)
        //{
        //    Debug.Log("Frames Per Second: " + (framesGoneBy / 1.0f));
        //    firstUpdateRealTime = 0;
        //    framesGoneBy = 0;
        //}

        updateSimulatedWaypointIfRequired();

        // First Take Care of Snapshot, so the time it take to generate it doesn't effect newly beginning experiment
        if (takeScreenshot)
        {
            //Debug.Log("Frames In Experiment: " + framesInExperiment);
            framesInExperiment = 0;
            float start = Time.realtimeSinceStartup;
            redirectionManager.snapshotGenerator.TakeScreenshot(experimentDescriptorToString(getExperimentDescriptor(experimentSetups[experimentIterator - 1]))); // Snapshot pertains to the previous experiment
            Debug.Log("Time Spent For Snapshot Generation: " + (Time.realtimeSinceStartup - start));
            takeScreenshot = false;
            if (experimentIterator == experimentSetups.Count)
                Debug.Log("---------- EXPERIMENTS COMPLETE ----------");
        }
        //if (!experimentInProgress && ((Time.time - lastExperimentEndTime) / timeScale > EXPERIMENT_WAIT_TIME) && experimentIterator < experimentSetups.Count)
        if (!experimentInProgress && experimentIterator < experimentSetups.Count)
        {
            startNextExperiment();
            //experimentStartTime = Time.time;
        }
        //if (experimentInProgress && !userStartedWalking && ((Time.time - experimentStartTime) / timeScale > WALKING_WAIT_TIME))
        if (experimentInProgress && !userIsWalking)
        {
            userIsWalking = true;
            //// Allow Walking
            //UserController.allowWalking = true;
            // Start Logging
            redirectionManager.statisticsLogger.BeginLogging();
        }

        if (experimentInProgress && userIsWalking)
        {
            //Debug.Log("User At: " + redirectionManager.userHeadTransform.position.ToString("f4"));
            framesInExperiment++;
        }

    }
    void OnGUI()
    {
        //GUI.Box(new Rect((int)(0.5f * Screen.width) - 75, (int)(0.5f * Screen.height) - 14, 150, 28), (1 / (60 * Time.deltaTime)).ToString("f1"));
        if (experimentComplete)
            GUI.Box(new Rect((int)(0.5f * Screen.width) - 75, (int)(0.5f * Screen.height) - 14, 150, 28), "Experiment Complete");
    }


    Dictionary<string, string> getExperimentDescriptor(ExperimentSetup setup)
    {
        Dictionary<string, string> descriptor = new Dictionary<string, string>();

        descriptor["redirector"] = setup.redirector.ToString();
        descriptor["resetter"] = setup.resetter == null ? "no_reset" : setup.resetter.ToString();
        descriptor["tracking_size_x"] = setup.trackingSizeShape.x.ToString();
        descriptor["tracking_size_z"] = setup.trackingSizeShape.z.ToString();

        // OLDER VERBOSE MODE
        //descriptor["redirector"] = setup.redirector.ToString();
        //descriptor["resetter"] = setup.resetter == null ? "no_reset" : setup.resetter.ToString();
        //descriptor["path_waypoint_count"] = setup.pathSeed.waypointCount.ToString();
        //if (setup.pathSeed.distanceDistribution.distributionType == VirtualPathGenerator.DistributionType.Uniform)
        //    descriptor["path_distance_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(min = " + setup.pathSeed.distanceDistribution.min + ", max = " + setup.pathSeed.distanceDistribution.max + ")";
        //if (setup.pathSeed.distanceDistribution.distributionType == VirtualPathGenerator.DistributionType.Normal)
        //    descriptor["path_distance_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(mu = " + setup.pathSeed.distanceDistribution.mu + ", sigma = " + setup.pathSeed.distanceDistribution.sigma + ", min = " + setup.pathSeed.distanceDistribution.min + ", max = " + setup.pathSeed.distanceDistribution.max + ")";
        //if (setup.pathSeed.angleDistribution.distributionType == VirtualPathGenerator.DistributionType.Uniform)
        //    descriptor["path_angle_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(min = " + setup.pathSeed.angleDistribution.min + ", max = " + setup.pathSeed.angleDistribution.max + ")";
        //if (setup.pathSeed.angleDistribution.distributionType == VirtualPathGenerator.DistributionType.Normal)
        //    descriptor["path_angle_distribution"] = setup.pathSeed.distanceDistribution.distributionType + "(mu = " + setup.pathSeed.angleDistribution.mu + ", sigma = " + setup.pathSeed.angleDistribution.sigma + ", min = " + setup.pathSeed.angleDistribution.min + ", max = " + setup.pathSeed.angleDistribution.max + ")";
        //descriptor["tracking_size_x"] = setup.trackingSizeShape.x.ToString();
        //descriptor["tracking_size_z"] = setup.trackingSizeShape.z.ToString();
        //descriptor["initial_position"] = setup.initialConfiguration.initialPosition.ToString();
        //descriptor["initial_forward"] = setup.initialConfiguration.initialForward.ToString();
        //descriptor["random_initial_position"] = setup.initialConfiguration.isRandom.ToString();
        //descriptor["trials"] = trialsForCurrentExperiment.ToString();
        //descriptor["g_t_scale_factor"] = setup.gainScaleFactor.x.ToString();
        //descriptor["g_r_scale_factor"] = setup.gainScaleFactor.y.ToString();
        //descriptor["g_c_scale_factor"] = setup.gainScaleFactor.z.ToString();
        return descriptor;
    }

    void printExperimentDescriptor(Dictionary<string, string> experimentDescriptor)
    {
        foreach (KeyValuePair<string, string> pair in experimentDescriptor)
        {
            Debug.Log(pair.Key + ": " + pair.Value);
        }
    }

    string experimentDescriptorToString(Dictionary<string, string> experimentDescriptor)
    {
        string retVal = "";
        int i = 0;
        foreach (KeyValuePair<string, string> pair in experimentDescriptor)
        {
            retVal += pair.Value;
            if (i != experimentDescriptor.Count - 1)
                retVal += "+";
            i++;
        }
        return retVal;
    }

    void SetUserBodyVisibility(bool isVisible)
    {
        print("SetUserBodyVisibility NOT IMPLEMENTED.");
    }

    void AdjustCameraSizes()
    {
        //redirectionManager.topViewCamera.orthographicSize = 0.5f * (setup.trackingSizeShape.z + SCREENSHOT_EXTRA_COVERAGE_BUFFER);
        print("AdjustCameraSizes NOT IMPLEMENTED.");
    }

    void AdjustTrailWidth()
    {
        //redirectionManager.realTrailDrawer.PATH_WIDTH = 0.003f * Mathf.Max(setup.trackingSizeShape.x, setup.trackingSizeShape.z);
        print("AdjustTrailWidth NOT IMPLEMENTED.");
    }

    void AdjustSnapshotGeneratorDimensions()
    {
        //if (setup.trackingSizeShape.x > setup.trackingSizeShape.z)
        //{
        //    redirectionManager.screenshotGenerator.resWidth = ScreenshotGenerator.maxResWidthOrHeight;
        //    redirectionManager.screenshotGenerator.resHeight = (int)Mathf.Ceil(ScreenshotGenerator.maxResWidthOrHeight * ((setup.trackingSizeShape.z + SCREENSHOT_EXTRA_COVERAGE_BUFFER) / (setup.trackingSizeShape.x + SCREENSHOT_EXTRA_COVERAGE_BUFFER)));
        //}
        //else if (setup.trackingSizeShape.x < setup.trackingSizeShape.z)
        //{
        //    redirectionManager.screenshotGenerator.resHeight = ScreenshotGenerator.maxResWidthOrHeight;
        //    redirectionManager.screenshotGenerator.resWidth = (int)Mathf.Ceil(ScreenshotGenerator.maxResWidthOrHeight * ((setup.trackingSizeShape.x + SCREENSHOT_EXTRA_COVERAGE_BUFFER) / (setup.trackingSizeShape.z + SCREENSHOT_EXTRA_COVERAGE_BUFFER)));
        //}
        //else
        //{
        //    redirectionManager.screenshotGenerator.resHeight = ScreenshotGenerator.maxResWidthOrHeight;
        //    redirectionManager.screenshotGenerator.resWidth = ScreenshotGenerator.maxResWidthOrHeight;
        //}
        print("AdjustSnapshotGeneratorDimensions NOT IMPLEMENTED.");
    }

    public List<Dictionary<string, string>> mergeTrialSummaryStatistics(List<Dictionary<string, string>> experimentResults)
    {
        List<Dictionary<string, string>> mergedResults = new List<Dictionary<string, string>>();
        Dictionary<string, string> mergedResult = null;
        float tempValue = 0;
        Vector2 tempVectorValue = Vector2.zero;
        for (int i = 0; i < experimentResults.Count; i++)
        {
            if (i % trialsForCurrentExperiment == 0)
            {
                mergedResult = new Dictionary<string, string>(experimentResults[i]);
            }
            else
            {
                foreach (KeyValuePair<string, string> pair in experimentResults[i])
                {
                    if (float.TryParse(pair.Value, out tempValue))
                    {
                        //Debug.Log("Averaged Float Values: " + pair.Value + ", " + mergedResult[pair.Key]);
                        mergedResult[pair.Key] = (i % trialsForCurrentExperiment == trialsForCurrentExperiment - 1) ? ((float.Parse(mergedResult[pair.Key]) + tempValue) / ((float)trialsForCurrentExperiment)).ToString() : (float.Parse(mergedResult[pair.Key]) + tempValue).ToString();
                    }
                    else if (TryParseVector2(pair.Value, out tempVectorValue))
                    {
                        //Debug.Log("Averaged Vector Values: " + pair.Value + ", " + mergedResult[pair.Key]);
                        mergedResult[pair.Key] = (i % trialsForCurrentExperiment == trialsForCurrentExperiment - 1) ? ((ParseVector2(mergedResult[pair.Key]) + tempVectorValue) / ((float)trialsForCurrentExperiment)).ToString() : (ParseVector2(mergedResult[pair.Key]) + tempVectorValue).ToString();
                    }
                }
            }
            if (i % trialsForCurrentExperiment == trialsForCurrentExperiment - 1)
                mergedResults.Add(mergedResult);
        }
        return mergedResults;
    }

    bool TryParseVector2(string value, out Vector2 result)
    {
        result = Vector2.zero;
        if (!(value[0] == '(' && value[value.Length - 1] == ')' && value.Contains(",")))
            return false;
        result.x = float.Parse(value.Substring(1, value.IndexOf(",") - 1));
        result.y = float.Parse(value.Substring(value.IndexOf(",") + 2, value.IndexOf(")") - (value.IndexOf(",") + 2)));
        return true;
    }

    Vector2 ParseVector2(string value)
    {
        Vector2 result = Vector2.zero;
        result.x = float.Parse(value.Substring(1, value.IndexOf(",") - 1));
        result.y = float.Parse(value.Substring(value.IndexOf(",") + 2, value.IndexOf(")") - (value.IndexOf(",") + 2)));
        return result;
    }

    void determineInitialConfigurations(ref List<ExperimentSetup> experimentSetups)
    {
        for (int i = 0; i < experimentSetups.Count; i++)
        {
            ExperimentSetup setup = experimentSetups[i];
            if (setup.initialConfiguration.isRandom)
            {
                if (!onlyRandomizeForward)
                    setup.initialConfiguration.initialPosition = VirtualPathGenerator.getRandomPositionWithinBounds(-0.5f * setup.trackingSizeShape.x, 0.5f * setup.trackingSizeShape.x, -0.5f * setup.trackingSizeShape.z, 0.5f * setup.trackingSizeShape.z);
                setup.initialConfiguration.initialForward = VirtualPathGenerator.getRandomForward();
                //Debug.LogWarning("Random Initial Configuration for size (" + trackingSizeShape.x + ", " + trackingSizeShape.z + "): Pos" + initialConfiguration.initialPosition.ToString("f2") + " Forward" + initialConfiguration.initialForward.ToString("f2"));
                experimentSetups[i] = setup;
            }
            else if (Mathf.Abs(setup.initialConfiguration.initialPosition.x) > 0.5f * setup.trackingSizeShape.x || Mathf.Abs(setup.initialConfiguration.initialPosition.y) > 0.5f * setup.trackingSizeShape.z)
            {
                Debug.LogError("Invalid beginning position selected. Defaulting Initial Configuration to (0, 0) and (0, 1).");
                setup.initialConfiguration.initialPosition = Vector2.zero;
                setup.initialConfiguration.initialForward = Vector2.up;
                experimentSetups[i] = setup;
            }
            if (!setup.initialConfiguration.isRandom)
            {
                // Deal with diagonal hack
                if (setup.initialConfiguration.initialForward == Vector2.one)
                {
                    setup.initialConfiguration.initialForward = (new Vector2(setup.trackingSizeShape.x, setup.trackingSizeShape.z)).normalized;
                    experimentSetups[i] = setup;
                }
            }
        }
    }

    void updateSimulatedWaypointIfRequired()
    {
        if ((redirectionManager.currPos - Utilities.FlattenedPos3D(redirectionManager.targetWaypoint.position)).magnitude < DISTANCE_TO_WAYPOINT_THRESHOLD)
        {
            redirectionManager.simulationManager.updateWaypoint();
        }
    }

    public void updateWaypoint()
    {
        if (!experimentInProgress)
            return;
        if (waypointIterator == waypoints.Count - 1)
        {
            if (experimentIterator < experimentSetups.Count)
                endExperiment();
        }
        else
        {
            waypointIterator++;
            redirectionManager.targetWaypoint.position = new Vector3(waypoints[waypointIterator].x, redirectionManager.targetWaypoint.position.y, waypoints[waypointIterator].y);
        }
    }
}
