using UnityEngine;
using System.Collections;
using System.IO;

public class SnapshotGenerator : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

    public static int maxResWidthOrHeight = 8192;
    [HideInInspector]
    public int resWidth = 2048;
    [HideInInspector]
    public int resHeight = 2048;
    public static string DEFAULT_SNAPSHOT_DIRECTORY = "Screenshots/";
    public Camera targetCamera;

    void Awake()
    {

    }


    public static string SnapshotDefaultName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }


    public void TakeScreenshot(string fileName)
    {
        #if !UNITY_WEBPLAYER    
        if (fileName == null)
            fileName = SnapshotDefaultName(resWidth, resHeight);
        else
            fileName = DEFAULT_SNAPSHOT_DIRECTORY + fileName + ".png";

        print("takeScreenshot NOT IMPLEMENTED.");
        return;
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        targetCamera.targetTexture = rt;
        Texture2D texture = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        targetCamera.Render();
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        targetCamera.targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(fileName, bytes);
        Debug.Log(string.Format("Saved Screenshot to: {0}", fileName));
        #endif
    }

    // The Path will already have "/" at the end
    public static string GetProjectPath()
    {
        #if UNITY_EDITOR
                return Application.dataPath.Substring(0, Application.dataPath.Length - 7) + "/";
        #else 
                return Application.dataPath;
        #endif
    }

    public static void CreateDirectoryIfNeeded(string directory)
    {
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            //Debug.LogWarning("Created Directory: " + directory);
        }
        //Debug.LogWarning("Directory Existed: " + directory);
    }
}
