using Unity.Entities;
using Unity.Mathematics;
[GenerateAuthoringComponent]
public struct CameraTag : IComponentData
{
    [UnityEngine.Header("Position")]
    public float3 offset; // recommended (0, 2, 0)
    public float3 orbitMultiplier; // recommended (4, 1, 4)
    [UnityEngine.Range(0, 1)] public float movementSpeedAdjustment; // recommended 0.3
    [UnityEngine.Header("Rotation")]
    [UnityEngine.Range(1, 89)] public float turnInputThreshold; //  threshold in degree celsius, recommended 40
    [UnityEngine.Range(1, 3)] public float rotationSpeed; // recommended 1.5
    public float maxRotationXAxis; // recommended about 60
}