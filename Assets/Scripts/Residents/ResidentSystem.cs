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
    public const int BUILDINGS_X_BOUNDS = 26;
    public const int BUILDINGS_Y_BOUNDS = 15;
    public const int AMOUNT_OF_RESIDENTS = 100000;

    // Resident attributes
    public const int VIRUS_LENGTH = 4;
    public const int VIRUS_DAYS_PER_MUTATION = 60;
    public const int RECOVERED_LENGTH = 30;
    public const float CHANCE_OF_INFECTION = 0.1f;
    public const float CHANCE_OF_VACCINATION = 0.005f;
    public const float TRANSITION_TIME = 0.1f;
    public const bool ANIMATED = false;

    // Resident colors
    public static readonly float[] SUSCEPTIBLE = { 80f/ 255f, 212f/255f, 35f/255f, 1f };
    public static readonly float[] INFECTED = { 224f/255f, 29f/255f, 9f/255f, 1f };
    public static readonly float[] RECOVERED = { 90f/255f, 106f/255f, 111f/255f, 1f };
    public static readonly float[] VACCINATED = { 15f/255f, 138f/255f, 188f/255f, 1f };

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
    private int timeUntilMutation;

    public void OnCreate(ref SystemState state)
    {
        residentQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<ResidentComponent>().WithAll<URPMaterialPropertyBaseColor>().Build();
        currentState = WorldState.DECIDE_ROOMS;
        timer = 0f;
        stagesElapsed = 0;
        numOfRooms = (BUILDINGS_X_BOUNDS * 2 + 1) * (BUILDINGS_Y_BOUNDS * 2 + 1);
        timeUntilMutation = VIRUS_DAYS_PER_MUTATION;
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

                timeUntilMutation--;
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
                    mutated = timeUntilMutation <= 0
                }.ScheduleParallel(residentQuery, infectionStateHandle).Complete();

                currentState = WorldState.DECIDE_ROOMS;
                // Set the in-world time elapsed
                stagesElapsed++;
                if(timeUntilMutation <= 0)
                {
                    timeUntilMutation = VIRUS_DAYS_PER_MUTATION;
                }

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
            transform.Position = ANIMATED ? math.lerp(transform.Position, new float3(res.targetRoom + res.offset, transform.Position.z), (10f / TRANSITION_TIME) * deltaTime) : new float3(res.targetRoom + res.offset, transform.Position.z);
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
        [ReadOnly] public bool mutated;

        [BurstCompile]
        public void Execute(ref ResidentComponent res, ref URPMaterialPropertyBaseColor color, ref LocalTransform transform)
        {
            int key = HashedRoom(res.targetRoom);

            if (res.state != ViralState.INFECTED) // Only non-infected residents have a chance to become vaccinated
            {
                if (mutated)
                {
                    transform.Position = new float3(transform.Position.xy, 0f);
                    res.state = ViralState.SUSCEPTIBLE;
                    color.Value = new float4(SUSCEPTIBLE[0], SUSCEPTIBLE[1], SUSCEPTIBLE[2], SUSCEPTIBLE[3]);
                }
                else if (rand.NextFloat() <= CHANCE_OF_VACCINATION)
                {
                    transform.Position = new float3(transform.Position.xy, 0.05f);
                    res.state = ViralState.VACCINATED;
                    color.Value = new float4(VACCINATED[0], VACCINATED[1], VACCINATED[2], VACCINATED[3]);
                }
            }

            if (res.state == ViralState.SUSCEPTIBLE)
            {
                if (array[key] > 0 && rand.NextFloat() <= CHANCE_OF_INFECTION)
                {
                    transform.Position = new float3(transform.Position.xy, -0.1f);
                    res.state = ViralState.INFECTED;
                    res.timeInfected = stagesElapsed;
                    color.Value = new float4(INFECTED[0], INFECTED[1], INFECTED[2], INFECTED[3]);
                }
            }
            else if (res.state == ViralState.INFECTED)
            {
                if (stagesElapsed - res.timeInfected >= VIRUS_LENGTH)
                {
                    transform.Position = new float3(transform.Position.xy, 0.1f);
                    res.state = ViralState.RECOVERED;
                    color.Value = new float4(RECOVERED[0], RECOVERED[1], RECOVERED[2], RECOVERED[3]);
                }
            }
            else if (res.state == ViralState.RECOVERED)
            {
                if (stagesElapsed - (res.timeInfected + VIRUS_LENGTH) >= RECOVERED_LENGTH)
                {
                    transform.Position = new float3(transform.Position.xy, 0f);
                    res.state = ViralState.SUSCEPTIBLE;
                    color.Value = new float4(SUSCEPTIBLE[0], SUSCEPTIBLE[1], SUSCEPTIBLE[2], SUSCEPTIBLE[3]);
                }
            }
        }
    }

    private static int HashedRoom(int2 roomPos)
    {
        return roomPos.x + BUILDINGS_X_BOUNDS + (roomPos.y + BUILDINGS_Y_BOUNDS) * (BUILDINGS_X_BOUNDS * 2 + 1);
    }
}
