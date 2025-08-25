using System.Collections.Generic;
using UnityEngine;

public class GraphHandler : MonoBehaviour
{
    [SerializeField] private LineRenderer line;
    [SerializeField] private float maxLength;
    [SerializeField] private float scaledHeight;
    private List<float> points = new List<float>();

    public void AddPoint(float value)
    {
        line.positionCount++;
        points.Add(value);
        Vector3[] pos = new Vector3[points.Count];
        float increment = maxLength / points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            pos[i] = new Vector2(increment * i, points[i] * scaledHeight);
        }
        line.SetPositions(pos);
    }
}
