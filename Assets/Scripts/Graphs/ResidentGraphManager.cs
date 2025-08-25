using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ResidentGraphManager : MonoBehaviour
{
    [SerializeField] private GraphHandler sLine;
    [SerializeField] private GraphHandler iLine;
    [SerializeField] private GraphHandler rLine;

    private EntityManager em;
    private EntityQuery eq;

    private void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        eq = new EntityQueryBuilder(Allocator.Temp).WithAll<ResidentComponent>().Build(em);
        InvokeRepeating("UpdateGraphs", 0f, 1.3f);
    }


    private void UpdateGraphs()
    {
        NativeArray<Entity> res = eq.ToEntityArray(Allocator.Temp);

        int recovered = 0;
        int infected = 0;

        foreach (Entity e in res)
        {
            ViralState state = em.GetComponentData<ResidentComponent>(e).state;
            if (state == ViralState.INFECTED)
            {
                infected++;
            }
            else if (state == ViralState.RECOVERED)
            {
                recovered++;
            }
        }

        int total = ResidentSystem.AMOUNT_OF_RESIDENTS;
        sLine.AddPoint((float)(total - recovered - infected) / (float)total);
        iLine.AddPoint((float)(infected) / (float)total);
        rLine.AddPoint((float)(recovered) / (float)total);

        res.Dispose();
    }
}
