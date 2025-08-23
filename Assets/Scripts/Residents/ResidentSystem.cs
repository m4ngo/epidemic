using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Rendering;

[BurstCompile]
partial struct ResidentSystem : ISystem
{
    // World attributes
    public const int BUILDINGS_X_BOUNDS = 19;
    public const int BUILDINGS_Y_BOUNDS = 10;
    public const int AMOUNT_OF_RESIDENTS = 50000;

    // Resident attributes
    public const int VIRUS_LENGTH = 2;
    public const float CHANCE_OF_INFECTION = 0.1f;
    public const float TRANSITION_SPEED = 15.0f;
    public const float TRANSITION_TIME = 1.5f;

    // Resident colors
    public float4 susceptible;
    public float4 infected;
    public float4 recovered;

    private EntityQuery residentQuery;

    private enum WorldState
    {
        DECIDE_ROOMS,
        TRANSITION,
        AWAIT_STAGE_END
    }
    
    private WorldState currentState;
    private float timer;
    private int stagesElapsed;
    private int numOfRooms;

    public void OnCreate(ref SystemState state)
    {
        residentQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<ResidentComponent>().WithAll<URPMaterialPropertyBaseColor>().Build();
        currentState = WorldState.DECIDE_ROOMS;
        timer = 0f;
        stagesElapsed = 0;
        numOfRooms = (BUILDINGS_X_BOUNDS * 2 + 1) * (BUILDINGS_Y_BOUNDS * 2 + 1);

        susceptible = new float4(0.33f, 1f, 0f, 1f);
        infected = new float4(1f, 0.16f, 0.16f, 1f);
        recovered = new float4(0.5f, 0.5f, 0.5f, 1f);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityManager entityManager = state.EntityManager;

        Random m_Random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000));

        switch (currentState)
        {
            case WorldState.DECIDE_ROOMS:

                JobHandle decideRoomHandle = default;
                new DecideRoom
                {
                    rand = m_Random
                }.ScheduleParallel(residentQuery, decideRoomHandle).Complete();
                currentState = WorldState.TRANSITION;
                timer = TRANSITION_TIME;

                break;
            case WorldState.TRANSITION:

                // Don't immediately move; gives viewer time to see which residents got infected
                if (timer <= TRANSITION_TIME / 2f)
                {
                    JobHandle moveTowardTargetHandle = default;
                    new MoveTowardTarget
                    {
                        deltaTime = SystemAPI.Time.DeltaTime
                    }.ScheduleParallel(residentQuery, moveTowardTargetHandle).Complete();
                }

                // Move to next stage after a slight delay
                timer -= SystemAPI.Time.DeltaTime;
                if(timer <= 0)
                {
                    currentState = WorldState.AWAIT_STAGE_END;
                }

                break;
            case WorldState.AWAIT_STAGE_END:

                NativeArray<int> roomHasInfected = new NativeArray<int>(numOfRooms, Allocator.TempJob);

                JobHandle setInfectedHandle = default;
                new SetInfectedRoom
                {
                    array = roomHasInfected
                }.Schedule(residentQuery, setInfectedHandle).Complete();

                JobHandle infectionStateHandle = default;
                new InfectionState
                {
                    array = roomHasInfected,
                    rand = m_Random,
                    stagesElapsed = stagesElapsed,
                    susceptible = susceptible,
                    infected = infected,
                    recovered = recovered
                }.ScheduleParallel(residentQuery, infectionStateHandle).Complete();

                currentState = WorldState.DECIDE_ROOMS;
                // Set the in-world time elapsed
                stagesElapsed++;

                roomHasInfected.Dispose();
                break;
        }
    }

    [BurstCompile]
    private partial struct MoveTowardTarget : IJobEntity
    {
        [ReadOnly] public float deltaTime;

        [BurstCompile]
        public void Execute(ref ResidentComponent res, ref LocalTransform transform)
        {
            transform.Position = math.lerp(transform.Position, new float3(res.targetRoom + res.offset, transform.Position.z), TRANSITION_SPEED * deltaTime);
        }
    }

    [BurstCompile]
    private partial struct DecideRoom : IJobEntity
    {
        [ReadOnly] public Random rand;

        [BurstCompile]
        public void Execute(ref ResidentComponent res)
        {
            res.targetRoom = new int2(
                 rand.NextInt(-BUILDINGS_X_BOUNDS, BUILDINGS_X_BOUNDS + 1),
                 rand.NextInt(-BUILDINGS_Y_BOUNDS, BUILDINGS_Y_BOUNDS + 1)
                 );
        }
    }

    [BurstCompile]
    private partial struct SetInfectedRoom : IJobEntity
    {
        public NativeArray<int> array;

        [BurstCompile]
        public void Execute(ref ResidentComponent res)
        {
            int key = HashedRoom(res.targetRoom);
            if (res.state == ViralState.INFECTED)
            {
                array[key]++;
            }
        }
    }

    [BurstCompile]
    private partial struct InfectionState : IJobEntity
    {
        [ReadOnly] public NativeArray<int> array;
        [ReadOnly] public Random rand;
        [ReadOnly] public int stagesElapsed;
        [ReadOnly] public float4 susceptible;
        [ReadOnly] public float4 infected;
        [ReadOnly] public float4 recovered;

        [BurstCompile]
        public void Execute(ref ResidentComponent res, ref URPMaterialPropertyBaseColor color, ref LocalTransform transform)
        {
            int key = HashedRoom(res.targetRoom);
            if (res.state == ViralState.SUSCEPTIBLE)
            {
                if (array[key] > 0 && rand.NextFloat() <= CHANCE_OF_INFECTION)
                {
                    transform.Position = new float3(transform.Position.xy, -0.1f);
                    res.state = ViralState.INFECTED;
                    res.timeInfected = stagesElapsed;
                    color.Value = infected;
                }
            }
            else if (res.state == ViralState.INFECTED)
            {
                if (stagesElapsed - res.timeInfected >= VIRUS_LENGTH)
                {
                    transform.Position = new float3(transform.Position.xy, 0.1f);
                    res.state = ViralState.RECOVERED;
                    color.Value = recovered;
                }
            }
        }
    }

    private static int HashedRoom(int2 roomPos)
    {
        return roomPos.x + BUILDINGS_X_BOUNDS + (roomPos.y + BUILDINGS_Y_BOUNDS) * (BUILDINGS_X_BOUNDS * 2 + 1);
    }
}
