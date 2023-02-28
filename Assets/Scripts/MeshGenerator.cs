using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class MeshGenerator : MonoBehaviour
{
    private Mesh _mesh;
    private Vector3[] _verticles;
    private int[] _triangles;
    private Color[] _colors;

    [SerializeField] private Material _material;
    [SerializeField] private PhysicMaterial _physicMaterial;
    [SerializeField] private Gradient _gradient;
    private int _xSize = 255;
    private int _ySize = 255;
    private float _squareSide = 1f;

    // Start is called before the first frame update
    void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        CreateTerrain();
        UpdateMesh();

        GetComponent<MeshCollider>().sharedMesh = _mesh;
        GetComponent<MeshCollider>().material = _physicMaterial;
        GetComponent<MeshRenderer>().material = _material;
    }

    float[][] GetRandomTerrain()
    {
        var max = 100f;
        var min = 0f;
        float[][] heights = new float[_xSize + 1][];
        for (var x = 0; x <= _xSize; x++)
        {
            heights[x] = new float[_ySize + 1];
            for (var y = 0; y <= _ySize; y++)
            {
                if (x == 0 || x == _xSize || y == 0 || y == _ySize)
                {
                    heights[x][y] = 0f; // Borders of the map
                }
                else
                {
                    heights[x][y] = Random.Range(min, max);
                }
            }
        }
        return heights;
    }

    float[][] GenerateHeights()
    {
        var terrain1 = GetRandomTerrain();

        var blurringIterations = 200; // Can be exposed as a parameter.
        float[][] blurringMemoryBuffer = new float[terrain1.Length][];
        for (var i=0; i< blurringIterations; i++)
        {
            BlurTerrain(terrain1, blurringMemoryBuffer, 3, false, 0.035f);
        }

        var terrain2 = GetRandomTerrain();
        blurringIterations = 200; // Can be exposed as a parameter.
        for (var i = 0; i < blurringIterations; i++)
        {
            BlurTerrain(terrain2, blurringMemoryBuffer, 50, true, 0.015f);
        }

        Rescale(terrain2, blurringMemoryBuffer,
            50, 50, 71, 71,
            0, 0, _xSize+1, _ySize+1);

        for (var x=0; x<_xSize+1; x++) for (var y=0; y<_ySize+1; y++) blurringMemoryBuffer[x][y] += terrain1[x][y];

        // TODO: Add terrain1 and terrain2
        return blurringMemoryBuffer;
    }

    void Rescale(float[][] source, float[][] destination, 
        int x1src, int y1src, int x2src, int y2src,
        int x1dst, int y1dst, int x2dst, int y2dst)
    {
        float srcWidth = x2src - x1src;
        float dstWidth = x2dst - x1dst;
        float srcHeight = y2src - y1src;
        float dstHeight = y2dst - y1dst;
        for (float xDst=x1dst; xDst<x2dst; xDst++) for (float yDst = y1dst; yDst < y2dst; yDst++)
        {
            var xDstProportion = (xDst-x1dst) / dstWidth;
            var yDstProportion = (yDst-y1dst) / dstHeight;
            var srcX = x1src + xDstProportion * srcWidth;
            var srcY = y1src + yDstProportion * srcHeight;

            // destination[(int)xDst][(int)yDst] = source[(int)srcX][(int)srcY]; not interpolated

            var proportionX = srcX - (int)srcX;
            var proportionY = srcY - (int)srcY;

            destination[(int)xDst][(int)yDst] =
                    source[(int)srcX][(int)srcY] * (1f-proportionX)*(1f-proportionY)
                    + source[(int)Mathf.Ceil(srcX)][(int)srcY] * proportionX * (1f - proportionY)
                    + source[(int)srcX][(int)Mathf.Ceil(srcY)] * (1f - proportionX) * proportionY
                    + source[(int)Mathf.Ceil(srcX)][(int)Mathf.Ceil(srcY)] * proportionX * proportionY
                    ;
        }
    }

    float DistanceBetween(float x1, float y1, float x2, float y2)
    {
        var xdiff = x1 - x2;
        var ydiff = y1 - y2;
        return Mathf.Sqrt(xdiff * xdiff + ydiff * ydiff);
    }

    //(int x, int y, float distance)[][] ClosestPoints(int sizeX, int sizeY, HashSet<(int, int)> points)
    //{
    //    // BFS from multiple points.
    //    var resultBuffer = new (int x, int y, float distance)[sizeX][];
    //    for (var x = 0; x < sizeX; x++)
    //    {
    //        resultBuffer[x] = new (int x, int y, float distance)[sizeY];
    //    }
    //    var visited = new HashSet<(int, int)>();
    //    Queue<(int x, int y)> pointsToVisit = new();
    //    foreach (var point in points)
    //    {
    //        pointsToVisit.Enqueue(point);
    //    }
    //    while (pointsToVisit.Count > 0)
    //    {
    //        var current = pointsToVisit.Dequeue();
    //        visited.Add((current.x, current.y));
    //        (int x, int y, float distance)? closestNeighbour = null;
    //        var bestDistance = float.PositiveInfinity;
    //        void checkNeighbour(int offsetX, int offsetY)
    //        {
    //            var x = current.x + offsetX;
    //            var y = current.y + offsetY;
    //            if (x < 0 || y < 0 || x >= sizeX || y >= sizeY || visited.Contains((x, y))) return;
    //            var bestPointCandidate = resultBuffer[x][y];
    //            var bestDistanceCandidate = DistanceBetween(current.x, current.y, bestPointCandidate.x, bestPointCandidate.y);
    //            if (bestDistanceCandidate < bestDistance)
    //            {
    //                closestNeighbour = bestPointCandidate;
    //                bestDistance = bestDistanceCandidate;
    //            }
    //            pointsToVisit.Enqueue((x, y));
    //        }
    //        checkNeighbour(current.x-1, current.y);
    //        checkNeighbour(current.x+1, current.y);
    //        checkNeighbour(current.x, current.y-1);
    //        checkNeighbour(current.x, current.y+1);
    //        resultBuffer[current.x][current.y] = (bestDistance == float.PositiveInfinity)
    //            ? (current.x,current.y,0.0f)
    //            : (closestNeighbour.Value.x, closestNeighbour.Value.y, bestDistance);
    //    }
    //    return resultBuffer;
    //}

    void BlurTerrain(float[][] heights, float[][] resultBuffer, int blurringMatrixSize = 3, bool symmetricBlurring = true, float coefficient = 0.035f)
    {
        for (var x = 0; x < _xSize+1; x++)
        {
            if (resultBuffer[x] == null) resultBuffer[x] = new float[heights[x].Length];
            for (var y = 0; y < _ySize+1; y++)
            {
                // First loop to find the sums of the terrain
                resultBuffer[x][y] = heights[x][y]
                    + (x>0 ? heights[x-1][y] : 0f)
                    + (y>0 ? heights[x][y-1] : 0f)
                    - (x>0 && y>0 ? heights[x-1][y-1] : 0f);
            }
        }


        for (var x=0; x<_xSize; x++)
        {
            for (var y=0; y < _ySize; y++)
            {
                var minusSumDist = symmetricBlurring ? blurringMatrixSize + 1 : blurringMatrixSize;

                resultBuffer[x][y] =
                        ((x<_xSize-blurringMatrixSize && y<_ySize-blurringMatrixSize && x>minusSumDist && y>minusSumDist
                        ? (resultBuffer[x+blurringMatrixSize][y+blurringMatrixSize] - resultBuffer[x-minusSumDist][y-minusSumDist])
                        : resultBuffer[x][y]) // This means edges are not blurred correctly! (but it's still cool :D)
                    / 9f)
                    * coefficient
                    + heights[x][y] * (1f - coefficient);
            }
        }

        for (var x = 0; x < _xSize; x++)
        {
            for (var y = 0; y < _ySize; y++)
            {
                heights[x][y] = resultBuffer[x][y];
            }
        }
    }

    //HashSet<(int,int)> GetPointsSlowerEroded(float[][] heights)
    //{
    //    var numberOfPeaksToLeave = _xSize / 10; // 10 is arbitrary - could be exposed as a parameter; it adjusts the density of higher peaks in the map
    //    var peaks = heights.SelectMany((row, x) => row.Select((height, y) => new { x, y, height })).OrderByDescending(x => x.height).Take(3 * numberOfPeaksToLeave).ToArray(); // 3 should be enough in most of the cases, but experiment with it later!
    //    // Below is O(n^2) but for small "n" it's good enough.
    //    var distanceMatrix = new float[peaks.Length][];
    //    for (var i=0; i<peaks.Length; i++)
    //    {
    //        distanceMatrix[i] = new float[i];
    //        for (var j=0; j<i; j++)
    //        {
    //            distanceMatrix[i][j] = DistanceBetween(peaks[i].x, peaks[i].y, peaks[j].x, peaks[j].y);
    //        }
    //    }
    //    var result = distanceMatrix.SelectMany((row, x) => row.Select((dist, y)=> new { x, y, dist })).OrderBy(_ => _.dist).Select(_ => (_.x, _.y))
    //        .Distinct().Reverse().Take(numberOfPeaksToLeave).ToHashSet();
    //    return result;
    //}

    void CreateTerrain()
    {
        var heights = GenerateHeights();
        var pointsPerRowX = (_xSize + 1);
        _verticles = Enumerable.Range(0, pointsPerRowX * (_ySize + 1))
            .Select(i =>
            {
                var x = (int)(i % pointsPerRowX);
                var y = (int)(i / pointsPerRowX);
                return new Vector3(
                    _squareSide * x,
                    heights[x][y],
                    _squareSide * y
                    );
            })
            .ToArray();

        _triangles = Enumerable.Range(0, pointsPerRowX * _ySize)
            .Where(i => i % pointsPerRowX != _xSize)
            .SelectMany(i => new int[]
            {
                i, i+_xSize+1, i+1,
                i+_xSize+1, i+_xSize+2, i+1
            }).ToArray();

        var minHeight = _verticles.Min(x => x.y);
        var maxHeight = _verticles.Max(x => x.y);
        _colors = new Color[_verticles.Length];
        for (var i=0; i < _verticles.Length; i++)
        {
            var height = Mathf.InverseLerp(minHeight, maxHeight, _verticles[i].y);
            _colors[i] = _gradient.Evaluate(height);
        }
    }

    void UpdateMesh()
    {
        _mesh.Clear();
        _mesh.vertices = _verticles;
        _mesh.triangles = _triangles;
        _mesh.colors = _colors;
        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
    }
}
