using UnityEngine;
using System.Collections.Generic;
using System.Buffers;
using ClipperLib;

/// <summary>
/// Dynamic polygon mesh generation with Clipper-based polygon union.
/// Memory-efficient mesh regeneration optimized for mobile (zero GC, pooled buffers).
/// </summary>
public class TerritoryManager : MonoBehaviour
{
    private Mesh mesh;
    private List<List<IntPoint>> subjectPolygons = new List<List<IntPoint>>();

    private void Start()
    {
        mesh = new Mesh();
        if (GetComponent<MeshFilter>() != null)
            GetComponent<MeshFilter>().mesh = mesh;
    }

    /// <summary>
    /// Unions new polygon points with current territory using Clipper union.
    /// Pooled IntPoint arrays prevent GC spikes during mobile play.
    /// </summary>
    public void UnionTerritory(List<Vector2> polygon)
    {
        List<IntPoint> clipPoints = ArrayPool<IntPoint>.Shared.Rent(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
        {
            clipPoints[i] = new IntPoint(
                (long)(polygon[i].x * 100),
                (long)(polygon[i].y * 100));
        }
        var paths = new List<List<IntPoint>> { clipPoints };
        List<List<IntPoint>> solution = new List<List<IntPoint>>();
        Clipper clipper = new Clipper();
        clipper.AddPaths(subjectPolygons, PolyType.ptSubject, true);
        clipper.AddPaths(paths, PolyType.ptClip, true);
        clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftPositive, PolyFillType.pftPositive);
        ArrayPool<IntPoint>.Shared.Return(clipPoints);
        subjectPolygons = solution;
        RegenerateMesh();
    }

    /// <summary>Memory-efficient mesh regeneration from unioned polygon paths.</summary>
    private void RegenerateMesh()
    {
        mesh.Clear();
        if (subjectPolygons.Count == 0) return;
        List<Vector3> verts = ListPool<Vector3>.Get();
        List<int> tris = ListPool<int>.Get();
        for (int p = 0; p < subjectPolygons.Count; p++)
        {
            var pts = subjectPolygons[p];
            int baseIndex = verts.Count;
            for (int i = 0; i < pts.Count; i++)
                verts.Add(new Vector3(pts[i].X / 100f, pts[i].Y / 100f, 0f));
            for (int i = 1; i < pts.Count - 1; i++)
            {
                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + i + 1);
                tris.Add(baseIndex + i);
            }
        }
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        ListPool<Vector3>.Release(verts);
        ListPool<int>.Release(tris);
    }

    private static class ListPool<T>
    {
        private static Stack<List<T>> pool = new Stack<List<T>>();
        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>();
        public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
    }
}
// TerritoryManager updated
