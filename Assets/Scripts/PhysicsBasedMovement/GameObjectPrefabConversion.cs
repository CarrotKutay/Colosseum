using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class GameObjectPrefabConversion : MonoBehaviour, IDeclareReferencedPrefabs
{
    [SerializeField]
    private GameObject PrefabGameObject;
    private EntityManager entityManager;
    private Entity PrefabEntity;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(PrefabGameObject);
    }

    private void Start()
    {
        entityManager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;

        var blobAssetStore = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<ConvertToEntitySystem>()
            .BlobAssetStore;

        PrefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(PrefabGameObject,
            GameObjectConversionSettings.FromWorld(
                World.DefaultGameObjectInjectionWorld, blobAssetStore
            ));
        entityManager.SetName(PrefabEntity, PrefabGameObject.name);


        entityManager.Instantiate(PrefabEntity);
    }
}
