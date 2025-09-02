using System.Collections.Generic;
using UnityEngine;

public class GraphHandler : MonoBehaviour
{
    [SerializeField] private LineRenderer line;
    [SerializeField] private float maxLength;
    [SerializeField] private float scaledHeight;
    private List<float> points = new List<float>();

    public int AddPoint(float value)
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
        return line.positionCount - 1;
    }

    public void SetPoint(float value, int index)
    {
        line.SetPosition(index, new Vector2(line.GetPosition(index).x, value * scaledHeight));
    }

    public void AddOrSetPoint(float value, int index)
    {
        if (index >= points.Count)
        {
            AddPoint(value);
        }
        else
        {
            SetPoint(value, index);
        }
    }
}
