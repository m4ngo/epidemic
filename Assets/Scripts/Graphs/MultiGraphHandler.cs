using UnityEngine;

public class MultiGraphHandler : MonoBehaviour
{
    public static MultiGraphHandler Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject graphGroup;
    [SerializeField] private float graphGroupOffset;

    [SerializeField] private float[,] points = new float[4, 150];
    [SerializeField] private GraphHandler[] graphHandlers;

    private ResidentGraphManager currentGraph;
    private int count;

    private void Start()
    {
        CreateNewGraph();
        InvokeRepeating("UpdateGraphHandlers", 0f, 0.5f);
    }

    public void CreateNewGraph()
    {
        if (currentGraph != null)
        {
            currentGraph.enabled = false;
        }
        count++;
        GameObject newGraph = Instantiate(graphGroup, spawnPoint.position + new Vector3(0, 0, count * graphGroupOffset), Quaternion.identity);
        currentGraph = newGraph.GetComponent<ResidentGraphManager>();
    }

    public void LogValue(float value, int graph, int index)
    {
        if (index >= points.GetLength(1))
        {
            return;
        }
        if (graph >= points.GetLength(0))
        {
            return;
        }

        points[graph, index] += value;
    }

    public void UpdateGraphHandlers()
    {
        for (int i = 0; i < points.GetLength(0); i++)
        {
            GraphHandler handler = graphHandlers[i];
            for (int j = 0; j < points.GetLength(1); j++)
            {
                handler.AddOrSetPoint(points[i, j] / count, j);
            }
        }
    }
}
