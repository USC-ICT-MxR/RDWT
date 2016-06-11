using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Redirection;

public class TrailDrawer : MonoBehaviour {

    //[SerializeField]
    
    LayerMask trailLayer;

    [SerializeField]
    bool drawRealTrail = true, drawVirtualTrail = true;

    [SerializeField, Range(0.01f, 1)]
    float MIN_DIST = 0.1f;
    [SerializeField, Range(0.01f, 0.5f)]
    float PATH_WIDTH = 0.05f;
    const float PATH_HEIGHT = 0.0001f;
    
    [SerializeField]
    Color realTrailColor = new Color(1, 1, 0, 0.5f), virtualPathColor = new Color(0, 0, 1, 0.5f);

    List<Vector3> realTrailVertices = new List<Vector3>(), virtualTrailVertices = new List<Vector3>();

    public const string REAL_TRAIL_NAME = "Real Trail", VIRTUAL_TRAIL_NAME = "Virtual Trail";

    Transform trailParent = null, realTrail = null, virtualTrail = null;
    Mesh realTrailMesh, virtualTrailMesh;

    [HideInInspector]
    public RedirectionManager redirectionManager;

    bool isLogging;

    void Awake()
    {
        trailParent = new GameObject("Trails").transform;
        trailParent.parent = this.transform;
        trailParent.position = Vector3.zero;
        trailParent.rotation = Quaternion.identity;
        trailLayer = LayerMask.NameToLayer("Redirection");
        // Find the next available layer ID and use it for Redirection
        if (trailLayer == -1)
        {
            for (int layerID = 8; layerID < 32; layerID++)
            {
                if (LayerMask.LayerToName(layerID).Length == 0)
                {
                    trailLayer = layerID;
                    //print("trailLayer: " + layerID);
                    break;
                }
            }
        }
    }

    void OnEnable()
    {
        BeginTrailDrawing();
    }

    void OnDisable()
    {
        StopTrailDrawing();
        if (drawRealTrail)
            ClearTrail(REAL_TRAIL_NAME);
        if (drawVirtualTrail)
            ClearTrail(VIRTUAL_TRAIL_NAME);
    }

    public void BeginTrailDrawing()
    {
        if (drawRealTrail)
            Initialize(REAL_TRAIL_NAME, realTrailColor, realTrailVertices, out realTrail, out realTrailMesh);
        if (drawVirtualTrail)
            Initialize(VIRTUAL_TRAIL_NAME, virtualPathColor, virtualTrailVertices, out virtualTrail, out virtualTrailMesh);
        isLogging = true;
    }

    public void StopTrailDrawing()
    {
        isLogging = false;
    }

    public void ClearTrail(string trailName)
    {
        Transform trail;
        if ((trail = trailParent.FindChild(trailName)) != null)
            Destroy(trail.gameObject);
    }

    void Initialize(string trailName, Color trailColor, List<Vector3> vertices, out Transform trail, out Mesh trailMesh)
    {
        vertices.Clear();
        //Material pathMaterial = new Material(Shader.Find("GUI/Text Shader"));
        Material pathMaterial = new Material(Shader.Find("Standard"));
        pathMaterial.color = trailColor;
        ClearTrail(trailName);
        trail = new GameObject(trailName).transform;
        trail.gameObject.AddComponent<MeshFilter>();
        trail.gameObject.AddComponent<MeshRenderer>();
        trail.gameObject.GetComponent<MeshRenderer>().sharedMaterial = pathMaterial; // USING SHARED MATERIAL, OTHERWISE UNITY WILL INSTANTIATE ANOTHER MATERIAL?
        MeshFilter meshFilter = trail.gameObject.GetComponent<MeshFilter>();
        trailMesh = new Mesh();
        trailMesh.hideFlags = HideFlags.DontSave; // destroys the mesh object when the application is terminated
        meshFilter.mesh = trailMesh;
        trailMesh.Clear(); // Good practice before modifying mesh verts or tris
        trail.parent = trailParent;
        trail.localPosition = Vector3.zero;
        trail.localRotation = Quaternion.identity;
        trail.gameObject.layer = trailLayer;
    }

	
	// Update is called once per frame
	void LateUpdate () {

        if (isLogging)
        {
            if (drawRealTrail)
                UpdateTrailPoints(realTrailVertices, realTrail, realTrailMesh);
            if (drawVirtualTrail)
            {
                // Reset Position of Virtual Trail
                virtualTrail.position = Vector3.zero;
                virtualTrail.rotation = Quaternion.identity;

                UpdateTrailPoints(virtualTrailVertices, virtualTrail, virtualTrailMesh, 2 * PATH_HEIGHT);
            }
        }
    }

    void UpdateTrailPoints(List<Vector3> vertices, Transform relativeTransform, Mesh mesh, float pathHeight = PATH_HEIGHT)
    {
        Vector3 currentPoint = Utilities.FlattenedPos3D(redirectionManager.headTransform.position, pathHeight);
        currentPoint = Utilities.GetRelativePosition(currentPoint, relativeTransform);
        if (vertices.Count == 0)
        {
            vertices.Add(currentPoint);
        }
        else if (Vector3.Distance(vertices[vertices.Count - 1], currentPoint) > MIN_DIST)
        {
            vertices.Add(currentPoint);
            UpdateLine(mesh, vertices.ToArray(), Vector3.up, PATH_WIDTH);
        }
    }

    /// <summary>
    /// Function that can be used for drawing virtual path that is predicted or implied by a series of waypoints.
    /// </summary>
    /// <param name="points3D"></param>
    /// <param name="pathColor"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    GameObject DrawPath(List<Vector3> points3D, Color pathColor, Transform parent, LayerMask pathLayer)
    {
        //Material pathMaterial = new Material(Shader.Find("GUI/Text Shader"));
        Material pathMaterial = new Material(Shader.Find("GUI/Text Shader"));
        pathMaterial.color = pathColor;
        GameObject path = new GameObject("Path");
        path.AddComponent<MeshFilter>();
        path.AddComponent<MeshRenderer>();
        path.GetComponent<MeshRenderer>().sharedMaterial = pathMaterial; // USING SHARED MATERIAL, OTHERWISE UNITY WILL INSTANTIATE ANOTHER MATERIAL?
        MeshFilter meshFilter = path.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.hideFlags = HideFlags.DontSave; // destroys the mesh object when the application is terminated
        meshFilter.mesh = mesh;
        mesh.Clear(); // Good practice before modifying mesh verts or tris
        path.transform.parent = parent;
        path.transform.localPosition = Vector3.zero;
        path.transform.localRotation = Quaternion.identity;
        path.layer = pathLayer;
        UpdateLine(mesh, points3D.ToArray(), Vector3.up, PATH_WIDTH);
        return path;
    }


    #region MeshUtils
    public static void UpdateLine(Mesh mesh, Vector3[] points,
        Vector3 norm, float width, bool closedLoop = false,
        float aspect = 1.0f)
    {
        Vector3[] widePts = GenerateLinePoints(points, norm,
            width, closedLoop);
        int[] wideTris = GenerateLineTris(points.Length,
            closedLoop);
        // calculate UVs for the new line
        Vector2[] uvs = GenerateUVs(points, width, aspect);
        Vector3[] normals = new Vector3[uvs.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = norm;
        }

        mesh.Clear();
        mesh.vertices = widePts;
        mesh.triangles = wideTris;
        mesh.normals = normals;
        mesh.uv = uvs;
    }

    /// <summary>
    /// Generates the necessary points to create a constant-width line
    /// along a series of points with surface normal to some vector,
    /// optionally forming a closed loop (last points connect to first 
    /// points).
    /// </summary>
    /// <param name="points">A list of points defining the line.</param>
    /// <param name="lineNormal">The normal vector of the polygons created.
    /// </param>
    /// <param name="lineWidth">The width of the line.</param>
    /// <param name="closedLoop">Is the line a closed loop?</param>
    /// <returns></returns>
    public static Vector3[] GenerateLinePoints(Vector3[] points,
        Vector3 lineNormal, float lineWidth, bool closedLoop = false)
    {
        Vector3[] output;
        float angleBetween, distance;
        Vector3 fromPrev, toNext, perp, prevToNext, ptA, ptB;

        lineNormal.Normalize();

        output = new Vector3[points.Length * 2];

        for (int i = 0; i < points.Length; i++)
        {

            GetPrevAndNext(points, i, out fromPrev, out toNext,
                closedLoop);

            prevToNext = (toNext + fromPrev);

            perp = Vector3.Cross(prevToNext, lineNormal);

            perp.Normalize();

            angleBetween = Vector3.Angle(perp, fromPrev);

            distance = (lineWidth / 2) / Mathf.Sin(angleBetween
                * Mathf.Deg2Rad);

            distance = Mathf.Clamp(distance, 0, lineWidth * 2);

            ptA = points[i] + distance * perp * -1;
            ptB = points[i] + distance * perp;

            output[i * 2] = ptA;
            output[i * 2 + 1] = ptB;
        }

        return output;

    }

    /// <summary>
    /// Generates an array of point indices defining triangles for a line 
    /// strip as generated by GenerateLinePoints.
    /// </summary>
    /// <param name="numPoints">The number of points in the input line.
    /// </param>
    /// <param name="closedLoop">Is it a closed loop?</param>
    /// <returns></returns>
    public static int[] GenerateLineTris(int numPoints,
        bool closedLoop = false)
    {
        int triIdxSize = (numPoints * 6);
        int[] triArray = new int[triIdxSize + ((closedLoop) ? 6 : 0)];
        int modulo = numPoints * 2;
        int j = 0;
        for (int i = 0; i < triArray.Length - 6; i += 6)
        {
            triArray[i + 0] = (j + 2) % modulo;
            triArray[i + 1] = (j + 1) % modulo;
            triArray[i + 2] = (j + 0) % modulo;
            triArray[i + 3] = (j + 2) % modulo;
            triArray[i + 4] = (j + 3) % modulo;
            triArray[i + 5] = (j + 1) % modulo;
            j += 2;
        }
        return triArray;
    }

    public static Vector2[] GenerateUVs(Vector3[] pts, float width = 20,
    float aspect = 1.0f)
    {
        Vector2[] uvs = new Vector2[pts.Length * 2];
        float lastV = 0.0f;
        for (int i = 0; i < pts.Length; i++)
        {
            float v;
            // if aspect were 1, then difference between last V and new V 
            // would be delta between points / width?
            if (i > 0)
            {
                float delta = (pts[i] - pts[i - 1]).magnitude;
                v = (delta / width) * aspect;
            }
            else
            {
                v = 0;
            }
            lastV += v;
            uvs[2 * i] = new Vector2(0, lastV);
            uvs[2 * i + 1] = new Vector2(1, lastV);
        }
        return uvs;
    }

    private static void GetPrevAndNext(Vector3[] verts, int index,
        out Vector3 fromPrev, out Vector3 toNext, bool closedLoop = false)
    {
        // handle edge cases
        if (index == 0)
        {
            toNext = verts[index] - verts[index + 1];
            if (!closedLoop)
            {
                fromPrev = toNext;
            }
            else
            {
                fromPrev = verts[verts.Length - 1] - verts[index];
            }
        }
        else if (index == verts.Length - 1)
        {
            fromPrev = verts[index - 1] - verts[index];
            if (!closedLoop)
            {
                toNext = fromPrev;
            }
            else
            {
                toNext = verts[index] - verts[0];
            }
        }
        else
        {
            toNext = verts[index] - verts[index + 1];
            fromPrev = verts[index - 1] - verts[index];
        }

        fromPrev.Normalize();
        toNext.Normalize();
    }
    #endregion MeshUtils


}
