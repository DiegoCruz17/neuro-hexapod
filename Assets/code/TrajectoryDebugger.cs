using System.Collections.Generic;
using UnityEngine;

public class TrajectoryDebugger : MonoBehaviour
{
    public GameObject spherePrefab; // Asigna un prefab de esfera desde el Inspector
    public AntaresController antares; // Arrastra el objeto del hexápodo aquí

    private List<GameObject> spheres = new List<GameObject>();
    private List<TextMesh> labels = new List<TextMesh>();

    void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            // Crear esfera
            GameObject sphere = Instantiate(spherePrefab);
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material.color = Color.red;
            spheres.Add(sphere);

            // Crear texto flotante encima
            GameObject label = new GameObject("Label" + (i + 1));
            TextMesh tm = label.AddComponent<TextMesh>();
            tm.text = "P" + (i + 1);
            tm.characterSize = 0.05f;
            tm.fontSize = 50;
            tm.color = Color.white;
            tm.anchor = TextAnchor.MiddleCenter;
            labels.Add(tm);
        }
    }

    void Update()
    {
        List<Vector3> points = HexapodTrajectory.CalcularTrayectoria(
            antares.d, antares.al, antares.n, antares.w,
            antares.rs, antares.ra, antares.c, antares.k,
            antares.hb, antares.wb
        );


        for (int i = 0; i < 6; i++)
        {
            Vector3 p = points[i];

            // Intercambiar Z por Y
            Vector3 converted = new Vector3(p.x, p.z, p.y);

            // Posicionar esfera relativa al hexápodo
            Vector3 worldPos = antares.transform.position + converted;
            spheres[i].transform.position = worldPos;

            // Posicionar texto encima de la esfera
            labels[i].transform.position = worldPos + Vector3.up * 0.1f;

            // Hacer que el texto mire siempre hacia la cámara
            if (Camera.main != null)
            {
                labels[i].transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
