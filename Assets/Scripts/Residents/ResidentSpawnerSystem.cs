using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

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

        for (int i = 0; i < ResidentSystem.AMOUNT_OF_RESIDENTS; i++) // Spawn residents
        {
            Entity newEntity = ecb.Instantiate(spawner.ValueRO.prefab);

            ecb.AddComponent(newEntity, new ResidentComponent {
                targetRoom = new int2(
                 m_Random.NextInt(-ResidentSystem.BUILDINGS_X_BOUNDS, ResidentSystem.BUILDINGS_X_BOUNDS + 1),
                 m_Random.NextInt(-ResidentSystem.BUILDINGS_Y_BOUNDS, ResidentSystem.BUILDINGS_Y_BOUNDS + 1)
                 ),
                state = i == 0 ? ViralState.INFECTED : ViralState.SUSCEPTIBLE,
                offset = (m_Random.NextFloat2() - new float2(ResidentSystem.ROOM_SIZE / 2)),
                gene = i == 0 ? 1 : -999
            });
            ecb.AddComponent(newEntity, new URPMaterialPropertyBaseColor
            {
                Value = i == 0 ? 
                new float4(ResidentSystem.INFECTED[0], 0, ResidentSystem.INFECTED[2], ResidentSystem.INFECTED[3]) : 
                new float4(ResidentSystem.SUSCEPTIBLE[0], ResidentSystem.SUSCEPTIBLE[1], ResidentSystem.SUSCEPTIBLE[2], ResidentSystem.SUSCEPTIBLE[3])
            });
            if (i == 0)
            {
                ecb.SetComponent(newEntity, new LocalTransform { Position = new float3(0, 0, -0.1f) });
            }
        }

        ecb.Playback(state.EntityManager);
    }

    [BurstCompile]
    public void OnStopRunning(ref SystemState state)
    {

    }
}
