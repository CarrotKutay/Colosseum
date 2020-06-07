using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct MovementDirectionInputComponent : IComponentData
{
    public float2 NewValue;
    public float2 OldValue;
}
