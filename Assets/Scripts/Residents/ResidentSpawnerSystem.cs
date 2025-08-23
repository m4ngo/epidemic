using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;

partial struct ResidentSpawnerSystem : ISystem, ISystemStartStop
{
    [BurstCompile]
    public void OnStartRunning(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<ResidentSpawnerComponent>(out Entity spawnerEntity))
        {
            return;
        }

        RefRW<ResidentSpawnerComponent> spawner = SystemAPI.GetComponentRW<ResidentSpawnerComponent>(spawnerEntity);

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        Random m_Random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000));
        float4 susceptible = new float4(0.33f, 1f, 0f, 1f);
        float4 infected = new float4(1f, 0.16f, 0.16f, 1f);

        for (int i = 0; i < ResidentSystem.AMOUNT_OF_RESIDENTS; i++) // Spawn residents
        {
            Entity newEntity = ecb.Instantiate(spawner.ValueRO.prefab);

            ecb.AddComponent(newEntity, new ResidentComponent { 
                state = i == 0 ? ViralState.INFECTED : ViralState.SUSCEPTIBLE,
                offset = (m_Random.NextFloat2() - new float2(0.5f)) * 0.5f
            });
            ecb.AddComponent(newEntity, new URPMaterialPropertyBaseColor
            {
                Value = i == 0 ? infected : susceptible
            });
        }

        ecb.Playback(state.EntityManager);
    }

    [BurstCompile]
    public void OnStopRunning(ref SystemState state)
    {

    }
}
