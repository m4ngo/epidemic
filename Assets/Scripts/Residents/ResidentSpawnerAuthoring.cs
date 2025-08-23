using UnityEngine;
using Unity.Entities;

public class ResidentSpawnerAuthoring : MonoBehaviour
{
    public GameObject prefab;
}

class ResidentSpawnerBaker : Baker<ResidentSpawnerAuthoring>
{
    public override void Bake(ResidentSpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.WorldSpace);

        AddComponent(entity, new ResidentSpawnerComponent
        {
            prefab = GetEntity(authoring.prefab, TransformUsageFlags.WorldSpace)
        });
    }
}
