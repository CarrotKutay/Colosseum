using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class Converter : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject PrefabGameObject;
    public static Entity prefabEntity;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity prefabEntity = conversionSystem.GetPrimaryEntity(PrefabGameObject);
        dstManager.SetName(prefabEntity, "PrefabEntity");
        Converter.prefabEntity = prefabEntity;
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(PrefabGameObject);
    }
}
