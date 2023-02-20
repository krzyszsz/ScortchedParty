using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    private int _xSize = 100;
    private int _ySize = 100;
    private float _squareSide = 10f;

    // Start is called before the first frame update
    void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        CreateShape();
        UpdateMesh();

        GetComponent<MeshCollider>().sharedMesh = _mesh;
        GetComponent<MeshCollider>().material = _physicMaterial;
        GetComponent<MeshRenderer>().material = _material;
    }

    void CreateShape()
    {
        var pointsPerRowX = (_xSize + 1);
        _verticles = Enumerable.Range(0, pointsPerRowX * (_ySize + 1))
            .Select(i =>
                new Vector3(
                    _squareSide * (int)(i % pointsPerRowX),
                    Random.Range(0f, 5f), // TODO: Placeholder
                    _squareSide * (int)(i / pointsPerRowX)
                    )
                )
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
