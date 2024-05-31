using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingHandler : MonoBehaviour
{
    [Header("Buildings")]
    [SerializeField] private MeshRenderer[] cityBuildings;
    [SerializeField] private MeshRenderer[] suburbBuildings;

    [Header("Camera")]
    [SerializeField] private Transform cam;
    [SerializeField] private float rotateSpeed;

    private void Start()
    {
        //recolor and reheight buildings
        foreach (MeshRenderer bul in cityBuildings)
        {
            bul.material.color = bul.material.color + new Color(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
            float y = Random.Range(2.5f, 7f);
            bul.transform.localScale = new Vector3(1.6f, y, 1.6f);
            Vector3 pos = bul.transform.position;
            bul.transform.position = new Vector3(pos.x, y / 2, pos.z);
        }

        foreach (MeshRenderer bul in suburbBuildings)
        {
            bul.material.color = bul.material.color + new Color(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
            float y = Random.Range(1.5f, 2f);
            bul.transform.localScale = new Vector3(1.6f, y, 1.6f);
            Vector3 pos = bul.transform.position;
            bul.transform.position = new Vector3(pos.x, y / 2, pos.z);
        }
    }

    private void Update()
    {
        float input = Input.GetAxisRaw("Horizontal");
        cam.Rotate(0, rotateSpeed * Time.deltaTime * -input, 0);
    }
}
