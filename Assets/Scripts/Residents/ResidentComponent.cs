using Unity.Entities;
using Unity.Mathematics;

public enum ViralState
{
    SUSCEPTIBLE,
    INFECTED,
    RECOVERED,
    VACCINATED
}

public struct ResidentComponent : IComponentData
{
    public ViralState state;
    public int2 targetRoom;
    public float2 offset;
    public int timeInfected;
}