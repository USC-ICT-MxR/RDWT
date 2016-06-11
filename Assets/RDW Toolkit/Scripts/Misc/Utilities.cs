using UnityEngine;
using System.Collections;


namespace Redirection
{
    public static class Utilities
    {

        public static Vector3 FlattenedPos3D(Vector3 vec, float height = 0)
        {
            return new Vector3(vec.x, height, vec.z);
        }

        public static Vector2 FlattenedPos2D(Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }

        public static Vector3 FlattenedDir3D(Vector3 vec)
        {
            return (new Vector3(vec.x, 0, vec.z)).normalized;
        }

        public static Vector2 FlattenedDir2D(Vector3 vec)
        {
            return new Vector2(vec.x, vec.z).normalized;
        }

        public static Vector3 UnFlatten(Vector2 vec, float height = 0)
        {
            return new Vector3(vec.x, height, vec.y);
        }

        /// <summary>
        /// Gets angle from prevDir to currDir in degrees, assuming the vectors lie in the xz plane (with left handed coordinate system).
        /// </summary>
        /// <param name="currDir"></param>
        /// <param name="prevDir"></param>
        /// <returns></returns>
        public static float GetSignedAngle(Vector3 prevDir, Vector3 currDir)
        {
            return Mathf.Sign(Vector3.Cross(prevDir, currDir).y) * Vector3.Angle(prevDir, currDir);
        }

        public static Vector3 GetRelativePosition(Vector3 pos, Transform origin)
        {
            return Quaternion.Inverse(origin.rotation) * (pos - origin.position);
        }

        public static Vector3 GetRelativeDirection(Vector3 dir, Transform origin)
        {
            return Quaternion.Inverse(origin.rotation) * dir;
        }

        // Based on: http://stackoverflow.com/questions/4780119/2d-euclidean-vector-rotations
        // FORCED LEFT HAND ROTATION AND DEGREES
        public static Vector2 RotateVector(Vector2 fromOrientation, float thetaInDegrees)
        {
            Vector2 ret = Vector2.zero;
            float cos = Mathf.Cos(-thetaInDegrees * Mathf.Deg2Rad);
            float sin = Mathf.Sin(-thetaInDegrees * Mathf.Deg2Rad);
            ret.x = fromOrientation.x * cos - fromOrientation.y * sin;
            ret.y = fromOrientation.x * sin + fromOrientation.y * cos;
            return ret;
        }

        public static bool Approximately(Vector2 v0, Vector2 v1)
        {
            return Mathf.Approximately(v0.x, v1.x) && Mathf.Approximately(v0.y, v1.y);
        }
    }
}