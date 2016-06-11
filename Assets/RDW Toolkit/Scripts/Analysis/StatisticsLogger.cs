using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using Redirection;
public class StatisticsLogger : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

    public bool logSampleVariables = false;
    public bool appendToFile = false;

    [HideInInspector]
    public List<Dictionary<string, string>> experimentResults = new List<Dictionary<string, string>>();
    public float samplingFrequency = 10; // How often we will gather data we have and log it in hertz
    // The way this works is that we wait 1 / samplingFrequency time to transpire before we attempt to clean buffers and gather samples
    // And since we always get a buffer value right before collecting samples, we'll have at least 1 buffer value to get an average from
    // The only problem with this is that overall we'll be gathering less than the expected frequency since the "lateness" of sampling will accumulate

    // THE FOLLOWING PARAMETERS MUST BE SENSITIVE TO TIME SCALE

    // TEMPORARILY SETTING ALL TO PUBLIC FOR TESTING

    // Redirection Single Parameters
    float sumOfInjectedTranslation = 0; // Overall amount of displacement (IN METERS) of redirection reference due to translation gain (always positive)
    float sumOfInjectedRotationFromRotationGain = 0; // Overall amount of rotation (IN RADIANS) (around user) of redirection reference due to rotation gain (always positive)
    float sumOfInjectedRotationFromCurvatureGain = 0; // Overall amount of rotation (IN RADIANS) (around user) of redirection reference due to curvature gain (always positive)
    float maxTranslationGain = float.MinValue;
    float minTranslationGain = float.MaxValue;
    float maxRotationGain = float.MinValue;
    float minRotationGain = float.MaxValue;
    float maxCurvatureGain = float.MinValue;
    float minCurvatureGain = float.MaxValue;

    // Reset Single Parameters
    float resetCount = 0;
    float sumOfVirtualDistanceTravelled = 0; // Based on user movement controller plus redirection movement
    float sumOfRealDistanceTravelled = 0; // Based on user movement controller
    float experimentBeginningTime = 0;
    float experimentEndingTime = 0;

    // Reset Sample Parameters
    List<float> virtualDistancesTravelledBetweenResets = new List<float>(); // this will be measured also from beginning to first reset, and from last reset to end (?)
    float virtualDistanceTravelledSinceLastReset;
    List<float> timeElapsedBetweenResets = new List<float>(); // this will be measured also from beginning to first reset, and from last reset to end (?)
    float timeOfLastReset = 0;

    // Sampling Paramers: These parameters are first read per frame/value update and stored in their buffer, and then 1/samplingFrequency time goes by, the values in the buffer will be averaged and logged to the list
    // The buffer variables for gains will be multiplied by time and at sampling time divided by time since last sample to get a proper average (since the functions aren't guaranteed to be called every frame)
    // Actually we can do this for all parameters just for true weighted average!
    List<Vector2> userRealPositionSamples = new List<Vector2>();
    List<Vector2> userRealPositionSamplesBuffer = new List<Vector2>();
    List<Vector2> userVirtualPositionSamples = new List<Vector2>();
    List<Vector2> userVirtualPositionSamplesBuffer = new List<Vector2>();
    List<float> translationGainSamples = new List<float>();
    List<float> translationGainSamplesBuffer = new List<float>();
    List<float> injectedTranslationSamples = new List<float>();
    List<float> injectedTranslationSamplesBuffer = new List<float>();
    List<float> rotationGainSamples = new List<float>();
    List<float> rotationGainSamplesBuffer = new List<float>();
    // NOTE: IN THE FUTURE, WE MIGHT WANT TO LOG THE INJECTED VALUES DIVIDED BY TIME, SO IT'S MORE CONSISTENT AND NO DEPENDENT ON THE FRAMERATE
    List<float> injectedRotationFromRotationGainSamples = new List<float>();
    List<float> injectedRotationFromRotationGainSamplesBuffer = new List<float>();
    List<float> curvatureGainSamples = new List<float>();
    List<float> curvatureGainSamplesBuffer = new List<float>();
    List<float> injectedRotationFromCurvatureGainSamples = new List<float>();
    List<float> injectedRotationFromCurvatureGainSamplesBuffer = new List<float>();
    List<float> injectedRotationSamples = new List<float>();
    List<float> injectedRotationSamplesBuffer = new List<float>();
    List<float> distanceToNearestBoundarySamples = new List<float>();
    List<float> distanceToNearestBoundarySamplesBuffer = new List<float>();
    List<float> distanceToCenterSamples = new List<float>();
    List<float> distanceToCenterSamplesBuffer = new List<float>();
    List<float> samplingIntervals = new List<float>();

    float lastSamplingTime = 0;

    //private bool testing = true;

    // Helper Varialbles
    //float startTime = float.MaxValue;
    enum LoggingState { not_started, logging, paused, complete };
    LoggingState state = LoggingState.not_started;


    // MAKE SURE TO INITIALIZE ALL VALUES HERE
    // FOR NOW I JUST CARE ABOUT RESETS
    void InitializeAllValues()
    {
        sumOfInjectedTranslation = 0;
        sumOfInjectedRotationFromRotationGain = 0;
        sumOfInjectedRotationFromCurvatureGain = 0;
        maxTranslationGain = float.MinValue;
        minTranslationGain = float.MaxValue;
        maxRotationGain = float.MinValue;
        minRotationGain = float.MaxValue;
        maxCurvatureGain = float.MinValue;
        minCurvatureGain = float.MaxValue;

        resetCount = 0;
        sumOfVirtualDistanceTravelled = 0;
        sumOfRealDistanceTravelled = 0;
        experimentBeginningTime = redirectionManager.GetTime();

        virtualDistancesTravelledBetweenResets = new List<float>();
        virtualDistanceTravelledSinceLastReset = 0;
        timeElapsedBetweenResets = new List<float>();
        timeOfLastReset = redirectionManager.GetTime(); // Technically a reset didn't happen here but we want to remember this time point

        userRealPositionSamples = new List<Vector2>();
        userRealPositionSamplesBuffer = new List<Vector2>();
        userVirtualPositionSamples = new List<Vector2>();
        userVirtualPositionSamplesBuffer = new List<Vector2>();
        translationGainSamples = new List<float>();
        translationGainSamplesBuffer = new List<float>();
        injectedTranslationSamples = new List<float>();
        injectedTranslationSamplesBuffer = new List<float>();
        rotationGainSamples = new List<float>();
        rotationGainSamplesBuffer = new List<float>();
        injectedRotationFromRotationGainSamples = new List<float>();
        injectedRotationFromRotationGainSamplesBuffer = new List<float>();
        curvatureGainSamples = new List<float>();
        curvatureGainSamplesBuffer = new List<float>();
        injectedRotationFromCurvatureGainSamples = new List<float>();
        injectedRotationFromCurvatureGainSamplesBuffer = new List<float>();
        injectedRotationSamples = new List<float>();
        injectedRotationSamplesBuffer = new List<float>();
        distanceToNearestBoundarySamples = new List<float>();
        distanceToNearestBoundarySamplesBuffer = new List<float>();
        distanceToCenterSamples = new List<float>();
        distanceToCenterSamplesBuffer = new List<float>();
        samplingIntervals = new List<float>();

        lastSamplingTime = redirectionManager.GetTime();
    }


    // IMPORTANT! The gathering of values has to be in LateUpdate to make sure the "Time.deltaTime" that's used by the gain sampling functions is the same ones that are considered when dividing by time elapsed 
    // that we do when gatherin the samples from buffers. Otherwise it can be that we get the buffers from a deltaTime, then the same deltaTime is used later to calculate a buffer value for a gain, and then 
    // later on the division won't be fair!
    public void UpdateStats()
    {
        if (state == LoggingState.logging)
        {
            // Average and Log Sampled Values If It's Time To
            UpdateFrameBasedValues();
            if (redirectionManager.GetTime() - lastSamplingTime > (1 / samplingFrequency))
            {
                GenerateSamplesFromBufferValuesAndClearBuffers();
                samplingIntervals.Add(redirectionManager.GetTime() - lastSamplingTime);
                lastSamplingTime = redirectionManager.GetTime();
            }
        }
    }

    public void BeginLogging()
    {
        if (state == LoggingState.not_started || state == LoggingState.complete)
        {
            state = LoggingState.logging;
            InitializeAllValues();
        }
    }

    // IF YOU PAUSE, YOU HAVE TO BE CAREFUL ABOUT TIME ELAPSED BETWEEN PAUSES!
    public void PauseLogging()
    {
        if (state == LoggingState.logging)
        {
            state = LoggingState.paused;
        }
    }

    public void ResumeLogging()
    {
        if (state == LoggingState.paused)
        {
            state = LoggingState.logging;
        }
    }

    // Experiment Descriptors are given and 
    public void EndLogging()
    {
        if (state == LoggingState.logging)
        {
            Event_Experiment_Ended();
            state = LoggingState.complete;
        }
    }

    // Experiment Descriptors are given and we add the logged data as a full experiment result bundle
    public Dictionary<string, string> GetExperimentResultForSummaryStatistics(Dictionary<string, string> experimentDescriptor)
    {
        Dictionary<string, string> experimentResults = new Dictionary<string, string>(experimentDescriptor);

        experimentResults["reset_count"] = resetCount.ToString();
        experimentResults["virtual_distance_between_resets_median"] = GetMedian(virtualDistancesTravelledBetweenResets).ToString();
        experimentResults["time_elapsed_between_resets_median"] = GetMedian(timeElapsedBetweenResets).ToString();

        experimentResults["sum_injected_translation"] = sumOfInjectedTranslation.ToString();
        experimentResults["sum_injected_rotation_g_r"] = sumOfInjectedRotationFromRotationGain.ToString();
        experimentResults["sum_injected_rotation_g_c"] = sumOfInjectedRotationFromCurvatureGain.ToString();
        experimentResults["sum_real_distance_travelled"] = sumOfRealDistanceTravelled.ToString();
        experimentResults["sum_virtual_distance_travelled"] = sumOfVirtualDistanceTravelled.ToString();
        experimentResults["min_g_t"] = minTranslationGain < float.MaxValue ? minTranslationGain.ToString() : "N/A";
        experimentResults["max_g_t"] = maxTranslationGain > float.MinValue ? maxTranslationGain.ToString() : "N/A";
        experimentResults["min_g_r"] = minRotationGain < float.MaxValue ? minRotationGain.ToString() : "N/A";
        experimentResults["max_g_r"] = maxRotationGain > float.MinValue ? maxRotationGain.ToString() : "N/A";
        experimentResults["min_g_c"] = minCurvatureGain < float.MaxValue ? minCurvatureGain.ToString() : "N/A";
        experimentResults["max_g_c"] = maxCurvatureGain > float.MinValue ? maxCurvatureGain.ToString() : "N/A";
        experimentResults["g_t_average"] = GetAverageOfAbsoluteValues(translationGainSamples).ToString();
        experimentResults["injected_translation_average"] = GetAverage(injectedTranslationSamples).ToString();
        experimentResults["g_r_average"] = GetAverageOfAbsoluteValues(rotationGainSamples).ToString();
        experimentResults["injected_rotation_from_rotation_gain_average"] = GetAverage(injectedRotationFromRotationGainSamples).ToString();
        experimentResults["g_c_average"] = GetAverageOfAbsoluteValues(curvatureGainSamples).ToString();
        experimentResults["injected_rotation_from_curvature_gain_average"] = GetAverage(injectedRotationFromCurvatureGainSamples).ToString();
        experimentResults["injected_rotation_average"] = GetAverage(injectedRotationSamples).ToString();

        experimentResults["real_position_average"] = GetAverage(userRealPositionSamples).ToString();
        experimentResults["virtual_position_average"] = GetAverage(userVirtualPositionSamples).ToString();
        experimentResults["distance_to_boundary_average"] = GetAverage(distanceToNearestBoundarySamples).ToString();
        experimentResults["distance_to_center_average"] = GetAverage(distanceToCenterSamples).ToString();
        experimentResults["normalized_distance_to_boundary_average"] = GetTrackingAreaNormalizedValue(GetAverage(distanceToNearestBoundarySamples)).ToString();
        experimentResults["normalized_distance_to_center_average"] = GetTrackingAreaNormalizedValue(GetAverage(distanceToCenterSamples)).ToString();

        experimentResults["experiment_duration"] = (experimentEndingTime - experimentBeginningTime).ToString();
        experimentResults["average_sampling_interval"] = GetAverage(samplingIntervals).ToString();

        return experimentResults;
    }

    public void GetExperimentResultsForSampledVariables(out Dictionary<string, List<float>> oneDimensionalSamples, out Dictionary<string, List<Vector2>> twoDimensionalSamples)
    {
        oneDimensionalSamples = new Dictionary<string, List<float>>();
        twoDimensionalSamples = new Dictionary<string, List<Vector2>>();

        oneDimensionalSamples.Add("distances_to_boundary", distanceToNearestBoundarySamples);
        oneDimensionalSamples.Add("normalized_distances_to_boundary", GetTrackingAreaNormalizedList(distanceToNearestBoundarySamples));
        oneDimensionalSamples.Add("distances_to_center", distanceToCenterSamples);
        oneDimensionalSamples.Add("normalized_distances_to_center", GetTrackingAreaNormalizedList(distanceToCenterSamples));
        oneDimensionalSamples.Add("g_t", translationGainSamples);
        oneDimensionalSamples.Add("injected_translations", injectedTranslationSamples);
        oneDimensionalSamples.Add("g_r", rotationGainSamples);
        oneDimensionalSamples.Add("injected_rotations_from_rotation_gain", injectedRotationFromRotationGainSamples);
        oneDimensionalSamples.Add("g_c", curvatureGainSamples);
        oneDimensionalSamples.Add("injected_rotations_from_curvature_gain", injectedRotationFromCurvatureGainSamples);
        oneDimensionalSamples.Add("injected_rotations", injectedRotationSamples);
        oneDimensionalSamples.Add("virtual_distances_between_resets", virtualDistancesTravelledBetweenResets);
        oneDimensionalSamples.Add("time_elapsed_between_resets", timeElapsedBetweenResets);
        oneDimensionalSamples.Add("sampling_intervals", samplingIntervals);

        twoDimensionalSamples.Add("user_real_positions", userRealPositionSamples);
        twoDimensionalSamples.Add("user_virtual_positions", userVirtualPositionSamples);
    }

    public void Event_User_Translated(Vector3 deltaPosition2D)
    {
        if (state == LoggingState.logging)
        {
            sumOfVirtualDistanceTravelled += deltaPosition2D.magnitude;
            sumOfRealDistanceTravelled += deltaPosition2D.magnitude;
            virtualDistanceTravelledSinceLastReset += deltaPosition2D.magnitude;
        }
    }

    public void Event_User_Rotated(float rotationInDegrees)
    {
        if (state == LoggingState.logging)
        {

        }
    }

    public void Event_Translation_Gain(float g_t, Vector3 translationApplied)
    {
        if (state == LoggingState.logging)
        {
            //if (testing)
            //{
            //    g_t = 1;
            //    translationApplied = Vector2.up;
            //}

            sumOfInjectedTranslation += translationApplied.magnitude;
            maxTranslationGain = Mathf.Max(maxTranslationGain, g_t);
            minTranslationGain = Mathf.Min(minTranslationGain, g_t);
            sumOfVirtualDistanceTravelled += Mathf.Sign(g_t) * translationApplied.magnitude; // if gain is positive, redirection reference moves with the user, thus increasing the virtual displacement, and if negative, decreases
            virtualDistanceTravelledSinceLastReset += Mathf.Sign(g_t) * translationApplied.magnitude;
            //translationGainSamplesBuffer.Add(Mathf.Abs(g_t) * redirectionManager.userMovementManager.lastDeltaTime);
            // The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            // But we'll artificially use this current delta time instead!
            //translationGainSamplesBuffer.Add(g_t * redirectionManager.userMovementManager.lastDeltaTime);
            //print("Translation Gain: " + g_t + "\tInterval: " + redirectionManager.getDeltaTime());
            translationGainSamplesBuffer.Add(g_t * redirectionManager.GetDeltaTime());
            //injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.userMovementManager.lastDeltaTime);
            injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.GetDeltaTime());
        }
    }

    public void Event_Translation_Gain_Reorientation(float g_t, Vector3 translationApplied)
    {
        if (state == LoggingState.logging)
        {
            throw new System.NotImplementedException();
            ////if (testing)
            ////{
            ////    g_t = 1;
            ////    translationApplied = Vector2.up;
            ////}

            //sumOfInjectedTranslation += translationApplied.magnitude;
            //maxTranslationGain = Mathf.Max(maxTranslationGain, g_t);
            //minTranslationGain = Mathf.Min(minTranslationGain, g_t);
            //sumOfVirtualDistanceTravelled += Mathf.Sign(g_t) * translationApplied.magnitude; // if gain is positive, redirection reference moves with the user, thus increasing the virtual displacement, and if negative, decreases
            //virtualDistanceTravelledSinceLastReset += Mathf.Sign(g_t) * translationApplied.magnitude;
            ////translationGainSamplesBuffer.Add(Mathf.Abs(g_t) * redirectionManager.userMovementManager.lastDeltaTime);
            //// The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            //// But we'll artificially use this current delta time instead!
            ////translationGainSamplesBuffer.Add(g_t * redirectionManager.userMovementManager.lastDeltaTime);
            ////print("Translation Gain: " + g_t + "\tInterval: " + redirectionManager.getDeltaTime());
            //translationGainSamplesBuffer.Add(g_t * redirectionManager.getDeltaTime());
            ////injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.userMovementManager.lastDeltaTime);
            //injectedTranslationSamplesBuffer.Add(translationApplied.magnitude * redirectionManager.getDeltaTime());
        }
    }

    public void Event_Rotation_Gain(float g_r, float rotationApplied)
    {
        if (state == LoggingState.logging)
        {
            //if (testing)
            //{
            //    g_r = 1;
            //    rotationApplied = 1;
            //}
            sumOfInjectedRotationFromRotationGain += Mathf.Abs(rotationApplied);
            maxRotationGain = Mathf.Max(maxRotationGain, g_r);
            minRotationGain = Mathf.Min(minRotationGain, g_r);
            //rotationGainSamplesBuffer.Add(Mathf.Abs(g_r) * redirectionManager.userMovementManager.lastDeltaTime);
            // The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            // But we'll artificially use this current delta time instead!
            //rotationGainSamplesBuffer.Add(g_r * redirectionManager.userMovementManager.lastDeltaTime);
            rotationGainSamplesBuffer.Add(g_r * redirectionManager.GetDeltaTime());
            //injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
            //injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
        }
    }

    public void Event_Rotation_Gain_Reorientation(float g_r, float rotationApplied)
    {
        if (state == LoggingState.logging)
        {
            print("event_rotation_gain_reorientation NOT IMPLEMENTED.");
            //throw new System.NotImplementedException();

            ////if (testing)
            ////{
            ////    g_r = 1;
            ////    rotationApplied = 1;
            ////}
            //sumOfInjectedRotationFromRotationGain += Mathf.Abs(rotationApplied);
            //maxRotationGain = Mathf.Max(maxRotationGain, g_r);
            //minRotationGain = Mathf.Min(minRotationGain, g_r);
            ////rotationGainSamplesBuffer.Add(Mathf.Abs(g_r) * redirectionManager.userMovementManager.lastDeltaTime);
            //// The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            //// But we'll artificially use this current delta time instead!
            ////rotationGainSamplesBuffer.Add(g_r * redirectionManager.userMovementManager.lastDeltaTime);
            //rotationGainSamplesBuffer.Add(g_r * redirectionManager.getDeltaTime());
            ////injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            //injectedRotationFromRotationGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.getDeltaTime());
            ////injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            //injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.getDeltaTime());
        }
    }

    public void Event_Curvature_Gain(float g_c, float rotationApplied)
    {
        if (state == LoggingState.logging)
        {
            //if (testing)
            //{
            //    g_c = 1;
            //    rotationApplied = 1;
            //}
            sumOfInjectedRotationFromCurvatureGain += Mathf.Abs(rotationApplied);
            maxCurvatureGain = Mathf.Max(maxCurvatureGain, g_c);
            minCurvatureGain = Mathf.Min(minCurvatureGain, g_c);
            //curvatureGainSamplesBuffer.Add(Mathf.Abs(g_c) * redirectionManager.userMovementManager.lastDeltaTime);
            //// The proper way is using redirectionManager.userMovementManager.lastDeltaTime which is the true time the gain was applied for, but this causes problems when we have a long frame and then a short frame
            // But we'll artificially use this current delta time instead!
            //curvatureGainSamplesBuffer.Add(g_c * redirectionManager.userMovementManager.lastDeltaTime);
            curvatureGainSamplesBuffer.Add(g_c * redirectionManager.GetDeltaTime());
            //injectedRotationFromCurvatureGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            injectedRotationFromCurvatureGainSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
            //injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.userMovementManager.lastDeltaTime);
            injectedRotationSamplesBuffer.Add(Mathf.Abs(rotationApplied) * redirectionManager.GetDeltaTime());
        }
    }

    public void Event_Reset_Triggered()
    {
        if (state == LoggingState.logging)
        {
            resetCount++;
            virtualDistancesTravelledBetweenResets.Add(virtualDistanceTravelledSinceLastReset);
            virtualDistanceTravelledSinceLastReset = 0;
            timeElapsedBetweenResets.Add(redirectionManager.GetTime() - timeOfLastReset);
            timeOfLastReset = redirectionManager.GetTime(); // Technically a reset didn't happen here but we want to remember this time point
        }
    }

    void UpdateFrameBasedValues()
    {
        ////if (testing)
        ////{
        ////    userRealPositionSamplesBuffer.Add(redirectionManager.getDeltaTime() * Vector2.one);
        ////    distanceToNearestBoundarySamplesBuffer.Add(redirectionManager.getDeltaTime() * 1);
        ////    distanceToCenterSamplesBuffer.Add(redirectionManager.getDeltaTime() * 1);
        ////}
        //else
        //{
        
        // Now we are letting the developer determine the movement manually in update, and we pull the info from redirector
        Event_User_Rotated(redirectionManager.deltaDir);
        Event_User_Translated(Utilities.FlattenedPos2D(redirectionManager.deltaPos));
        
        userRealPositionSamplesBuffer.Add(redirectionManager.GetDeltaTime() * Utilities.FlattenedPos2D(redirectionManager.currPosReal));
        userVirtualPositionSamplesBuffer.Add(redirectionManager.GetDeltaTime() * Utilities.FlattenedPos2D(redirectionManager.currPos));
        distanceToNearestBoundarySamplesBuffer.Add(redirectionManager.GetDeltaTime() * redirectionManager.resetter.getDistanceToNearestBoundary());
        distanceToCenterSamplesBuffer.Add(redirectionManager.GetDeltaTime() * redirectionManager.currPosReal.magnitude);
        //}
    }

    void GenerateSamplesFromBufferValuesAndClearBuffers()
    {
        GetSampleFromBuffer(ref userRealPositionSamples, ref userRealPositionSamplesBuffer);
        GetSampleFromBuffer(ref userVirtualPositionSamples, ref userVirtualPositionSamplesBuffer);
        GetSampleFromBuffer(ref translationGainSamples, ref translationGainSamplesBuffer);
        GetSampleFromBuffer(ref injectedTranslationSamples, ref injectedTranslationSamplesBuffer);
        GetSampleFromBuffer(ref rotationGainSamples, ref rotationGainSamplesBuffer);
        GetSampleFromBuffer(ref injectedRotationFromRotationGainSamples, ref injectedRotationFromRotationGainSamplesBuffer);
        GetSampleFromBuffer(ref curvatureGainSamples, ref curvatureGainSamplesBuffer);
        GetSampleFromBuffer(ref injectedRotationFromCurvatureGainSamples, ref injectedRotationFromCurvatureGainSamplesBuffer);
        GetSampleFromBuffer(ref injectedRotationSamples, ref injectedRotationSamplesBuffer);
        GetSampleFromBuffer(ref distanceToNearestBoundarySamples, ref distanceToNearestBoundarySamplesBuffer);
        GetSampleFromBuffer(ref distanceToCenterSamples, ref distanceToCenterSamplesBuffer);
    }

    void GetSampleFromBuffer(ref List<float> samples, ref List<float> buffer, bool verbose = false)
    {
        float sampleValue = 0;
        foreach (float bufferValue in buffer)
        {
            sampleValue += bufferValue;
        }
        //samples.Add(sampleValue / (redirectionManager.GetTime() - lastSamplingTime));
        // OPTIONALLY WE CAN NOT LOG ANYTHING AT ALL IN THIS CASE!
        samples.Add(buffer.Count != 0 ? sampleValue / buffer.Count : 0);
        if (verbose)
        {
            print("sampleValue: " + sampleValue);
            print("samplingInterval: " + (redirectionManager.GetTime() - lastSamplingTime));
        }
        buffer.Clear();
    }

    void GetSampleFromBuffer(ref List<Vector2> samples, ref List<Vector2> buffer)
    {
        Vector2 sampleValue = Vector2.zero;
        foreach (Vector2 bufferValue in buffer)
        {
            sampleValue += bufferValue;
        }
        //samples.Add(sampleValue / (redirectionManager.GetTime() - lastSamplingTime));
        samples.Add(sampleValue / buffer.Count);
        buffer.Clear();
    }

    void Event_Experiment_Ended()
    {
        virtualDistancesTravelledBetweenResets.Add(virtualDistanceTravelledSinceLastReset);
        timeElapsedBetweenResets.Add(redirectionManager.GetTime() - timeOfLastReset);
        experimentEndingTime = redirectionManager.GetTime();
    }

    // This function introduces lots of floating point error and I'd rather see clean values than noisy accurate weighted measurements
    //float getTimeWeightedSampleAverage(List<float> sampleArray, List<float> sampleDurationArray)
    //{
    //    float valueSum = 0;
    //    float timeSum = 0;
    //    for (int i = 0; i < sampleArray.Count; i++)
    //    {
    //        valueSum += sampleArray[i] * sampleDurationArray[i];
    //        timeSum += sampleDurationArray[i];
    //    }
    //    return valueSum / timeSum;
    //}

    Vector2 GetTimeWeightedSampleAverage(List<Vector2> sampleArray, List<float> sampleDurationArray)
    {
        Vector2 valueSum = Vector2.zero;
        float timeSum = 0;
        for (int i = 0; i < sampleArray.Count; i++)
        {
            valueSum += sampleArray[i] * sampleDurationArray[i];
            timeSum += sampleDurationArray[i];
        }
        return sampleArray.Count != 0 ? valueSum / timeSum : Vector2.zero;
    }

    float GetAverage(List<float> array)
    {
        float sum = 0;
        foreach (float value in array)
        {
            sum += value;
        }
        return array.Count != 0 ? sum / array.Count : 0;
    }

    float GetAverageOfAbsoluteValues(List<float> array)
    {
        float sum = 0;
        foreach (float value in array)
        {
            sum += Mathf.Abs(value);
        }
        return array.Count != 0 ? sum / array.Count : 0;
    }

    Vector2 GetAverage(List<Vector2> array)
    {
        Vector2 sum = Vector2.zero;
        foreach (Vector2 value in array)
        {
            sum += value;
        }
        return sum / array.Count;
    }

    // We're not providing a time-based version of this at this time
    float GetMedian(List<float> array)
    {
        if (array.Count == 0)
        {
            Debug.LogError("Empty Array");
            return 0;
        }
        List<float> sortedArray = array.OrderBy(item => item).ToList<float>();
        if (sortedArray.Count % 2 == 1)
            return sortedArray[(int)(0.5f * sortedArray.Count)];
        else
            return 0.5f * (sortedArray[(int)(0.5f * sortedArray.Count)] + sortedArray[(int)(0.5f * sortedArray.Count) - 1]);
    }

    // This would make more sense in the context of square-shaped environments
    // Normalizing by dividing by diameter
    float GetTrackingAreaNormalizedValue(float distance)
    {
        return distance / redirectionManager.resetter.getTrackingAreaHalfDiameter();
    }

    List<float> GetTrackingAreaNormalizedList(List<float> distances)
    {
        List<float> retVal = new List<float>(distances);
        for (int i = 0; i < distances.Count; i++)
        {
            retVal[i] = retVal[i] / redirectionManager.resetter.getTrackingAreaHalfDiameter();
        }
        return retVal;
    }

    ////////////// LOGGING TO FILE

    string RESULT_DIRECTORY = "Experiment Results/";
    string SUMMARY_STATISTICS_DIRECTORY = "Summary Statistics/";
    string SAMPLED_METRICS_DIRECTORY = "Sampled Metrics/";

    XmlWriter xmlWriter;
    public string SUMMARY_STATISTICS_XML_FILENAME = "SimulationResults";
    const string XML_ROOT = "Experiments";
    const string XML_ELEMENT = "Experiment";

    StreamWriter csvWriter;

    void Awake()
    {
        RESULT_DIRECTORY = SnapshotGenerator.GetProjectPath() + RESULT_DIRECTORY;
        SUMMARY_STATISTICS_DIRECTORY = RESULT_DIRECTORY + SUMMARY_STATISTICS_DIRECTORY;
        SAMPLED_METRICS_DIRECTORY = RESULT_DIRECTORY + SAMPLED_METRICS_DIRECTORY;
        SnapshotGenerator.DEFAULT_SNAPSHOT_DIRECTORY = RESULT_DIRECTORY + SnapshotGenerator.DEFAULT_SNAPSHOT_DIRECTORY;
        SnapshotGenerator.CreateDirectoryIfNeeded(RESULT_DIRECTORY);
        SnapshotGenerator.CreateDirectoryIfNeeded(SUMMARY_STATISTICS_DIRECTORY);
        SnapshotGenerator.CreateDirectoryIfNeeded(SAMPLED_METRICS_DIRECTORY);
        SnapshotGenerator.CreateDirectoryIfNeeded(SnapshotGenerator.DEFAULT_SNAPSHOT_DIRECTORY);
    }

    // Writes all summary statistics for a batch of experiments
    public void LogExperimentSummaryStatisticsResults(List<Dictionary<string, string>> experimentResults)
    {
        // Settings
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.IndentChars = ("\t");
        settings.CloseOutput = true;

        // Create XML File
        //xmlWriter = redirectionManager.runInTestMode ? XmlWriter.Create(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + "_" + SimulationManager.commandLineRunCode + ".xml", settings) : XmlWriter.Create(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + "_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".xml", settings);
        //xmlWriter = XmlWriter.Create(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + " - " + redirectionManager.startTimeOfProgram + ".xml", settings);
        xmlWriter = XmlWriter.Create(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + ".xml", settings);
        xmlWriter.Settings.Indent = true;
        xmlWriter.WriteStartDocument();
        xmlWriter.WriteStartElement(XML_ROOT);

        // HACK: If there's only one element, Excel won't show the lablels, so we're duplicating for now
        if (experimentResults.Count == 1)
        {
            experimentResults.Add(experimentResults[0]);
        }

        foreach (Dictionary<string, string> experimentResult in experimentResults)
        {
            xmlWriter.WriteStartElement(XML_ELEMENT);
            foreach (KeyValuePair<string, string> entry in experimentResult)
            {
                xmlWriter.WriteElementString(entry.Key, entry.Value);
            }
            xmlWriter.WriteEndElement();
        }

        xmlWriter.WriteEndElement();
        xmlWriter.WriteEndDocument();
        xmlWriter.Flush();
        xmlWriter.Close();
    }

    public void LogExperimentSummaryStatisticsResultsSCSV(List<Dictionary<string, string>> experimentResults)
    {
        csvWriter = new StreamWriter(SUMMARY_STATISTICS_DIRECTORY + SUMMARY_STATISTICS_XML_FILENAME + ".csv", appendToFile);
        csvWriter.WriteLine("sep=;");
        if (experimentResults.Count > 0)
        {
            // Set up the headers
            csvWriter.Write("experiment_start_time;");
            foreach (string header in experimentResults[0].Keys)
            {
                csvWriter.Write(header + ";");
            }
            csvWriter.WriteLine();
            // Write Values
            csvWriter.Write(redirectionManager.startTimeOfProgram +";");
            foreach (Dictionary<string, string> experimentResult in experimentResults)
            {
                foreach (string value in experimentResult.Values)
                {
                    csvWriter.Write(value + ";");
                }
                csvWriter.WriteLine();
            }
            
        }
        csvWriter.Flush();
        csvWriter.Close();
    }


    public void LogOneDimensionalExperimentSamples(string experimentDecriptorString, string measuredMetric, List<float> values)
    {
        //csvWriter = new StreamWriter(SAMPLED_METRICS_DIRECTORY + measuredMetric +"_" + experimentDecriptorString +".csv");
        string experimentSamplesDirectory = SAMPLED_METRICS_DIRECTORY + experimentDecriptorString + "/";
        SnapshotGenerator.CreateDirectoryIfNeeded(experimentSamplesDirectory);
        csvWriter = new StreamWriter(experimentSamplesDirectory + measuredMetric + ".csv");
        foreach (float value in values)
        {
            csvWriter.WriteLine(value);
        }
        csvWriter.Flush();
        csvWriter.Close();
    }

    public void LogTwoDimensionalExperimentSamples(string experimentDecriptorString, string measuredMetric, List<Vector2> values)
    {
        //csvWriter = new StreamWriter(measuredMetric + "_" + experimentDecriptorString + ".csv");
        string experimentSamplesDirectory = SAMPLED_METRICS_DIRECTORY + experimentDecriptorString + "/";
        SnapshotGenerator.CreateDirectoryIfNeeded(experimentSamplesDirectory);
        csvWriter = new StreamWriter(experimentSamplesDirectory + measuredMetric + ".csv");
        foreach (Vector2 value in values)
        {
            csvWriter.WriteLine(value.x + ", " + value.y);
        }
        csvWriter.Flush();
        csvWriter.Close();
    }

    public void LogAllExperimentSamples(string experimentDecriptorString, Dictionary<string, List<float>> oneDimensionalSamplesMap, Dictionary<string, List<Vector2>> twoDimensionalSamplesMap)
    {
        foreach (KeyValuePair<string, List<float>> oneDimensionalSamples in oneDimensionalSamplesMap)
        {
            LogOneDimensionalExperimentSamples(experimentDecriptorString, oneDimensionalSamples.Key, oneDimensionalSamples.Value);
        }
        foreach (KeyValuePair<string, List<Vector2>> twoDimensionalSamples in twoDimensionalSamplesMap)
        {
            LogTwoDimensionalExperimentSamples(experimentDecriptorString, twoDimensionalSamples.Key, twoDimensionalSamples.Value);
        }
    }

    public void GenerateBatchFiles()
    {
        StreamWriter batchFileWriter;
        for (int pathCode = 1; pathCode <= 4; pathCode++)
        {
            string experimentSamplesDirectory = "batch" + pathCode + ".bat";
            batchFileWriter = new StreamWriter(experimentSamplesDirectory);
            for (int expCode = 1; expCode <= 3; expCode++)
            {

                for (int algoCode = 1; algoCode <= 6; algoCode++)
                {
                    batchFileWriter.WriteLine("start Simulation.exe -batchmode " + expCode + pathCode + algoCode);
                }
            }
            batchFileWriter.Flush();
            batchFileWriter.Close();
        }
    }
}
