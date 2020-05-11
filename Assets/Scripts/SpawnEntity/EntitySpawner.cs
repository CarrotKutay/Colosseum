using Unity.Entities;
using UnityEngine;

public class EntitySpawner : MonoBehaviour
{
    private EntityManager manager;
    private EntityArchetype SpawnArchetype;
    // Start is called before the first frame update
    void Start()
    {
        manager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;

        SpawnArchetype = manager.CreateArchetype(
            typeof(SpawnComponent)
        );
    }

    // Update is called once per frame
    void Update()
    {
        if (Converter.prefabEntity != Entity.Null)
        {
            var spawnObj = manager.CreateEntity(SpawnArchetype);
            manager.SetComponentData(spawnObj,
                new SpawnComponent
                {
                    Value = Converter.prefabEntity
                }
            );
            Converter.prefabEntity = Entity.Null;
        }
    }
}

public struct SpawnComponent : IComponentData
{
    public Entity Value;
}
