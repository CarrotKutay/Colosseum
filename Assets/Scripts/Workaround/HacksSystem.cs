using Unity.Entities;
using Unity.Rendering;

[DisableAutoCreation]
public class HacksSystem : SystemBase
{
    protected override void OnUpdate()
    {
        /* Suppresses the error: "ArgumentException: A component with type:BoneIndexOffset has not been added to the entity.", until the Unity bug is fixed. */
        //World.GetOrCreateSystem<CopySkinnedEntityDataToRenderEntity>().Enabled = false;
    }
}
