using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ResidentGraphManager : MonoBehaviour
{
    [SerializeField] private GraphHandler sLine;
    [SerializeField] private GraphHandler iLine;
    [SerializeField] private GraphHandler rLine;
    [SerializeField] private GraphHandler dLine;

    private EntityManager em;
    private EntityQuery eq;

    private void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        eq = new EntityQueryBuilder(Allocator.Temp).WithAll<ResidentComponent>().Build(em);
        //InvokeRepeating("UpdateGraphs", 0f, ResidentSystem.TRANSITION_TIME / 1.1f);
    }

    private void Update()
    {
        UpdateGraphs();
    }

    private void UpdateGraphs()
    {
        NativeArray<Entity> res = eq.ToEntityArray(Allocator.Temp);

        int recovered = 0;
        int infected = 0;
        int dead = 0;

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
            else if (state == ViralState.DEAD)
            {
                dead++;
            }
        }

        int total = ResidentSystem.AMOUNT_OF_RESIDENTS;

        float sVal = (float)(total - recovered - infected - dead) / (float)total;
        int count = sLine.AddPoint(sVal);
        MultiGraphHandler.Instance.LogValue(sVal, 0, count);

        float iVal = (float)(infected) / (float)total;
        iLine.AddPoint(iVal);
        MultiGraphHandler.Instance.LogValue(iVal, 1, count);

        float rVal = (float)(recovered) / (float)total;
        rLine.AddPoint(rVal);
        MultiGraphHandler.Instance.LogValue(rVal, 2, count);

        float dVal = (float)(dead) / (float)total;
        dLine.AddPoint(dVal);
        MultiGraphHandler.Instance.LogValue(dVal, 3, count);

        res.Dispose();
    }
}
