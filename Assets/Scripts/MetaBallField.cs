using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class MetaBallField
{
    public Transform[] Balls = new Transform[0];
    public float BallRadius = 1;

    private Vector3[] _ballPositions;
    
    /// <summary>
    /// Call Field.Update to react to ball position and parameters in run-time.
    /// </summary>
    public void Update()
    {
        _ballPositions = Balls.Select(x => x.position).ToArray();
    }
    
    /// <summary>
    /// Calculate scalar field value at point
    /// </summary>
    public float F(Vector3 position)
    {
        float f = 0;
        // Naive implementation, just runs for all balls regardless the distance.
        // A better option would be to construct a sparse grid specifically around 
        foreach (var center in _ballPositions)
        {
            f += 1 / Vector3.SqrMagnitude(center - position);
        }

        f *= BallRadius * BallRadius;

        return f - 1;
    }

    public FieldBorders Borders()
    {
        float minX = _ballPositions.Select(b => b.x).Min() - 2 * BallRadius;
        float maxX = _ballPositions.Select(b => b.x).Max() + 2 * BallRadius;
        float minY = _ballPositions.Select(b => b.y).Min() - 2 * BallRadius;
        float maxY = _ballPositions.Select(b => b.y).Max() + 2 * BallRadius;
        float minZ = _ballPositions.Select(b => b.z).Min() - 2 * BallRadius;
        float maxZ = _ballPositions.Select(b => b.z).Max() + 2 * BallRadius;
        return new FieldBorders(minX, minY, minZ, maxX, maxY, maxZ);
    }
    
    public readonly struct FieldBorders
    {
        public FieldBorders(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
        }

        public float MinX { get; }
        public float MinY { get; }
        public float MinZ { get; }
        public float MaxX { get; }
        public float MaxY { get; }
        public float MaxZ { get; }
    }
}