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
    public const float CHANCE_OF_INFECTION = 0.1f;
    public const float CHANCE_OF_MUTATION = 0.0001f;
    public const int RECOVERY_TIME = 40;
    public const int IMMUNITY_THRESHOLD = 20;
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

    public void OnCreate(ref SystemState state)
    {
        residentQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<ResidentComponent>().WithAll<URPMaterialPropertyBaseColor>().Build();
        currentState = WorldState.DECIDE_ROOMS;
        timer = 0f;
        stagesElapsed = 0;
        numOfRooms = (BUILDINGS_X_BOUNDS * 2 + 1) * (BUILDINGS_Y_BOUNDS * 2 + 1);
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

                NativeParallelMultiHashMap<int, int> roomHasInfected = new NativeParallelMultiHashMap<int, int>(AMOUNT_OF_RESIDENTS, Allocator.TempJob);

                JobHandle setInfectedHandle = default;
                new SetInfectedRoom
                {
                    roomMap = roomHasInfected.AsParallelWriter()
                }.Schedule(residentQuery, setInfectedHandle).Complete();

                JobHandle infectionStateHandle = default;
                new InfectionState
                {
                    roomMap = roomHasInfected,
                    rand = m_Random,
                    stagesElapsed = stagesElapsed,
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
                math.clamp(res.targetRoom.x + rand.NextInt(-1, 2), -BUILDINGS_X_BOUNDS, BUILDINGS_X_BOUNDS),
                math.clamp(res.targetRoom.y + rand.NextInt(-1, 2), -BUILDINGS_Y_BOUNDS, BUILDINGS_Y_BOUNDS)
                 );
        }
    }

    [BurstCompile]
    private partial struct SetInfectedRoom : IJobEntity
    {
        public NativeParallelMultiHashMap<int, int>.ParallelWriter roomMap;

        [BurstCompile]
        public void Execute(ref ResidentComponent res)
        {
            int key = HashedRoom(res.targetRoom);
            if (res.state == ViralState.INFECTED)
            {
                roomMap.Add(key, res.gene);
            }
        }
    }

    private static bool SetInfectedState(ref ResidentComponent res, ref URPMaterialPropertyBaseColor color, ref LocalTransform transform, ref Random rand, int stagesElapsed, int gene)
    {
        if (gene - res.gene >= IMMUNITY_THRESHOLD)
        {
            if (rand.NextFloat() <= CHANCE_OF_INFECTION)
            {
                int mutatedGene = gene;
                if (rand.NextFloat() <= CHANCE_OF_MUTATION)
                {
                    mutatedGene = gene + 20;
                }
                transform.Position = new float3(transform.Position.xy, -(mutatedGene % 256 / 255f));
                res.state = ViralState.INFECTED;
                res.timeInfected = stagesElapsed;
                res.gene = mutatedGene;
                color.Value = new float4((1.0f - (mutatedGene % 256) / 255f), INFECTED[1], (mutatedGene % 256) / 255f, INFECTED[3]);
            }
            return true;
        }
        return false;
    }

    [BurstCompile]
    private partial struct InfectionState : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, int> roomMap;
        [ReadOnly] public Random rand;
        [ReadOnly] public int stagesElapsed;

        [BurstCompile]
        public void Execute(ref ResidentComponent res, ref URPMaterialPropertyBaseColor color, ref LocalTransform transform)
        {
            int key = HashedRoom(res.targetRoom);

            if (res.state != ViralState.INFECTED)
            {
                if (res.state == ViralState.RECOVERED && stagesElapsed - res.timeInfected >= RECOVERY_TIME + VIRUS_LENGTH)
                {
                    transform.Position = new float3(transform.Position.xy, 0f);
                    res.state = ViralState.SUSCEPTIBLE;
                    res.gene = -999;
                    color.Value = new float4(SUSCEPTIBLE[0], SUSCEPTIBLE[1], SUSCEPTIBLE[2], SUSCEPTIBLE[3]);
                    return;
                }

                if (roomMap.TryGetFirstValue(key, out int gene, out var iterator))
                {
                    if (SetInfectedState(ref res, ref color, ref transform, ref rand, stagesElapsed, gene))
                    {
                        return;
                    }
                    while (roomMap.TryGetNextValue(out gene, ref iterator))
                    {
                        if (SetInfectedState(ref res, ref color, ref transform, ref rand, stagesElapsed, gene))
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                if (stagesElapsed - res.timeInfected >= VIRUS_LENGTH)
                {
                    transform.Position = new float3(transform.Position.xy, 0.1f);
                    res.state = ViralState.RECOVERED;
                    color.Value = new float4(RECOVERED[0], RECOVERED[1], RECOVERED[2], RECOVERED[3]);
                }
            }
        }
    }

    private static int HashedRoom(int2 roomPos)
    {
        return roomPos.x + BUILDINGS_X_BOUNDS + (roomPos.y + BUILDINGS_Y_BOUNDS) * (BUILDINGS_X_BOUNDS * 2 + 1);
    }
}
