using UnityEngine;

namespace ArchCore.Utils
{
    public class DebugDraw
    {
        public static void DrawCube(Vector3 pos, Color col, Vector3 scale, float duaration = 0)
        {
            Vector3 halfScale = scale * 0.5f;

            Vector3[] points = new Vector3[]
            {
                pos + new Vector3(halfScale.x, halfScale.y, halfScale.z),
                pos + new Vector3(-halfScale.x, halfScale.y, halfScale.z),
                pos + new Vector3(-halfScale.x, -halfScale.y, halfScale.z),
                pos + new Vector3(halfScale.x, -halfScale.y, halfScale.z),
                pos + new Vector3(halfScale.x, halfScale.y, -halfScale.z),
                pos + new Vector3(-halfScale.x, halfScale.y, -halfScale.z),
                pos + new Vector3(-halfScale.x, -halfScale.y, -halfScale.z),
                pos + new Vector3(halfScale.x, -halfScale.y, -halfScale.z),
            };

            Debug.DrawLine(points[0], points[1], col, duaration);
            Debug.DrawLine(points[1], points[2], col, duaration);
            Debug.DrawLine(points[2], points[3], col, duaration);
            Debug.DrawLine(points[3], points[0], col, duaration);
        }

        public static void DrawRect(Rect rect, Color col, float duaration = 0)
        {
            Vector3 pos = new Vector3(rect.x + rect.width / 2, rect.y + rect.height / 2, 0.0f);
            Vector3 scale = new Vector3(rect.width, rect.height, 0.0f);

            DebugDraw.DrawRect(pos, col, scale, duaration);
        }

        public static void DrawRect(Vector3 pos, Color col, Vector3 scale, float duaration = 0)
        {
            Vector3 halfScale = scale * 0.5f;

            Vector3[] points = new Vector3[]
            {
                pos + new Vector3(halfScale.x, halfScale.y, halfScale.z),
                pos + new Vector3(-halfScale.x, halfScale.y, halfScale.z),
                pos + new Vector3(-halfScale.x, -halfScale.y, halfScale.z),
                pos + new Vector3(halfScale.x, -halfScale.y, halfScale.z),
            };

            Debug.DrawLine(points[0], points[1], col, duaration);
            Debug.DrawLine(points[1], points[2], col, duaration);
            Debug.DrawLine(points[2], points[3], col, duaration);
            Debug.DrawLine(points[3], points[0], col, duaration);
        }
        
        public static void DrawPoint(Vector3 pos)
        {
            DrawPoint(pos, Color.red);
        }
        
        public static void DrawPoint(Vector3 pos, Color col , float scale = 1, float duaration = 600000)
        {
            Vector3[] points = {
                pos + (Vector3.up * scale),
                pos - (Vector3.up * scale),
                pos + (Vector3.right * scale),
                pos - (Vector3.right * scale),
                pos + (Vector3.forward * scale),
                pos - (Vector3.forward * scale)
            };

            Debug.DrawLine(points[0], points[1], col, duaration);
            Debug.DrawLine(points[2], points[3], col, duaration);
            Debug.DrawLine(points[4], points[5], col, duaration);

            Debug.DrawLine(points[0], points[2], col, duaration);
            Debug.DrawLine(points[0], points[3], col, duaration);
            Debug.DrawLine(points[0], points[4], col, duaration);
            Debug.DrawLine(points[0], points[5], col, duaration);

            Debug.DrawLine(points[1], points[2], col, duaration);
            Debug.DrawLine(points[1], points[3], col, duaration);
            Debug.DrawLine(points[1], points[4], col, duaration);
            Debug.DrawLine(points[1], points[5], col, duaration);

            Debug.DrawLine(points[4], points[2], col, duaration);
            Debug.DrawLine(points[4], points[3], col, duaration);
            Debug.DrawLine(points[5], points[2], col, duaration);
            Debug.DrawLine(points[5], points[3], col, duaration);
        }
    }
}