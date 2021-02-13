using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class CollisionEventsReceiverAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public bool UseCollisionDetails = true;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CollisionEventsReceiverProperties { UsesCollisionDetails = UseCollisionDetails });
        dstManager.AddBuffer<BufferCollisionEventElement>(entity);
    }
}
