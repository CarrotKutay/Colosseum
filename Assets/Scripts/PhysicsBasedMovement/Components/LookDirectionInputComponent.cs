using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct LookDirectionInputComponent : IComponentData
{
    public float3 Value;
}