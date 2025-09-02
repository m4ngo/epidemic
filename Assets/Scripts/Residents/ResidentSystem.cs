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
    // Monte Carlo settings
    public const int ITERATIONS = 100;
    public const int ITERATION_LENGTH = 500;
    public const int STEPS_PER_FRAME = 10;

    // World attributes
    public const int BUILDINGS_X_BOUNDS = 13;
    public const int BUILDINGS_Y_BOUNDS = 8;
    public const int AMOUNT_OF_RESIDENTS = 10000;
    public const float ROOM_SIZE = 1f;

    // Resident attributes
    public const int VIRUS_LENGTH = 7;
    public const float CHANCE_OF_INFECTION = 0.2f;
    public const float CHANCE_OF_MUTATION = 0.001f;
    public const float CHANCE_OF_DEATH = 0.5f;
    public const float CHANCE_OF_REVIVE = 0.2f;
    public const int IMMUNITY_PERIOD = 30;
    public const int MAX_MOVE = 1;
    public const int IMMUNITY_THRESHOLD = 20;
    public const float TRANSITION_TIME = 0f;
    public const bool ANIMATED = false;

    // Resident colors
    public static readonly float[] SUSCEPTIBLE = { 80f/ 255f, 212f/255f, 35f/255f, 1f };
    public static readonly float[] INFECTED = { 224f/255f, 29f/255f, 9f/255f, 1f };
    public static readonly float[] RECOVERED = { 60f/255f, 60f/255f, 60f/255f, 1f };
    public static readonly float[] VACCINATED = { 15f/255f, 138f/255f, 188f/255f, 1f };
    public static readonly float[] DEAD = { 10f/255f, 10f/255f, 10f/255f, 1f };

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

        for (int i = 0; i < STEPS_PER_FRAME; i++)
        {
            switch (currentState)
            {
                case WorldState.DECIDE_ROOMS:

                    JobHandle decideRoomHandle = default;
                    new DecideRoom
                    {
                        rand = m_Random,
                        timeElapsed = stagesElapsed
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
                    if (timer <= 0)
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
                    if (stagesElapsed > ITERATION_LENGTH)
                    {
                        stagesElapsed = 0;
                        MultiGraphHandler.Instance.CreateNewGraph();
                    }

                    roomHasInfected.Dispose();
                    break;
            }
        }
    }

    [BurstCompile]
    private partial struct MoveTowardTarget : IJobEntity
    {
        [ReadOnly] public float deltaTime;

        [BurstCompile]
        public void Execute(ref ResidentComponent res, ref LocalTransform transform)
        {
            transform.Position = ANIMATED ? math.lerp(transform.Position, new float3((float2)res.targetRoom * ROOM_SIZE + res.offset, transform.Position.z), (10f / TRANSITION_TIME) * deltaTime) : new float3((float2)res.targetRoom * ROOM_SIZE + res.offset, transform.Position.z);
        }
    }

    [BurstCompile]
    private partial struct DecideRoom : IJobEntity
    {
        [ReadOnly] public Random rand;
        [ReadOnly] public int timeElapsed;

        [BurstCompile]
        public void Execute(ref ResidentComponent res)
        {
            if(res.state == ViralState.DEAD)
            {
                return;
            }
            if (timeElapsed == ITERATION_LENGTH)
            {
                res.targetRoom = new int2(
                 rand.NextInt(-BUILDINGS_X_BOUNDS, BUILDINGS_X_BOUNDS + 1),
                 rand.NextInt(-BUILDINGS_Y_BOUNDS, BUILDINGS_Y_BOUNDS + 1)
                     );
            }
            else
            {
                int2 newPos = new int2(
                    (res.targetRoom.x + rand.NextInt(-MAX_MOVE, MAX_MOVE + 1)),
                    (res.targetRoom.y + rand.NextInt(-MAX_MOVE, MAX_MOVE + 1))
                     );
                if (newPos.x > BUILDINGS_X_BOUNDS)
                {
                    newPos.x = -BUILDINGS_X_BOUNDS;
                }
                if (newPos.x < -BUILDINGS_X_BOUNDS)
                {
                    newPos.x = BUILDINGS_X_BOUNDS;
                }
                if (newPos.y > BUILDINGS_Y_BOUNDS)
                {
                    newPos.y = -BUILDINGS_Y_BOUNDS;
                }
                if (newPos.y < -BUILDINGS_Y_BOUNDS)
                {
                    newPos.y = BUILDINGS_Y_BOUNDS;
                }
                res.targetRoom = newPos;
            }
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
            if(stagesElapsed == ITERATION_LENGTH)
            {
                if (res.original)
                {
                    transform.Position = new float3(transform.Position.xy, -0.1f);
                    res.state = ViralState.INFECTED;
                    res.timeInfected = 0;
                    res.gene = 1;
                    color.Value = new float4(INFECTED[0], 0, INFECTED[2], INFECTED[3]);
                }
                else
                {
                    transform.Position = new float3(transform.Position.xy, 0f);
                    res.state = ViralState.SUSCEPTIBLE;
                    res.gene = -999;
                    res.timeInfected = -999;
                    color.Value = new float4(SUSCEPTIBLE[0], SUSCEPTIBLE[1], SUSCEPTIBLE[2], SUSCEPTIBLE[3]);
                }
                return;
            }

            int key = HashedRoom(res.targetRoom);

            if (res.state != ViralState.INFECTED)
            {
                if (res.state == ViralState.RECOVERED && stagesElapsed - res.timeInfected >= IMMUNITY_PERIOD + VIRUS_LENGTH)
                {
                    transform.Position = new float3(transform.Position.xy, 0f);
                    res.state = ViralState.SUSCEPTIBLE;
                    res.gene = -999;
                    color.Value = new float4(SUSCEPTIBLE[0], SUSCEPTIBLE[1], SUSCEPTIBLE[2], SUSCEPTIBLE[3]);
                    return;
                }

                if (res.state == ViralState.DEAD && rand.NextFloat() <= CHANCE_OF_REVIVE)
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
                    if (rand.NextFloat() <= CHANCE_OF_DEATH)
                    {
                        transform.Position = new float3(transform.Position.xy, 0.05f);
                        res.state = ViralState.DEAD;
                        color.Value = new float4(DEAD[0], DEAD[1], DEAD[2], DEAD[3]);
                    }
                    else
                    {
                        transform.Position = new float3(transform.Position.xy, 0.1f);
                        res.state = ViralState.RECOVERED;
                        color.Value = new float4(RECOVERED[0], RECOVERED[1], RECOVERED[2], RECOVERED[3]);
                    }
                }
            }
        }
    }

    private static int HashedRoom(int2 roomPos)
    {
        return roomPos.x + BUILDINGS_X_BOUNDS + (roomPos.y + BUILDINGS_Y_BOUNDS) * (BUILDINGS_X_BOUNDS * 2 + 1);
    }
}
