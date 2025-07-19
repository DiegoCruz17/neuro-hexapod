using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject suelo; //
    public float alturaExtra = 0.1f; // 
    public float minSpacing = 0.3f;
    private List<Vector3> posicionesUsadas = new List<Vector3>();
    private Vector3 areaSize;

void Start()
{
    BoxCollider col = suelo.GetComponent<BoxCollider>();

    // Tamaño real del área (fijado manualmente)
    Vector3 size = new Vector3(28.5f, col.size.y * suelo.transform.localScale.y, 29f);
    Vector3 center = suelo.transform.position + col.center;

    float minX = center.x - size.x / 2f + minSpacing;
    float maxX = center.x + size.x / 2f - minSpacing;
    float minZ = center.z - size.z / 2f + minSpacing;
    float maxZ = center.z + size.z / 2f - minSpacing;
    float spawnY = suelo.transform.position.y + size.y / 2f + alturaExtra;

    List<Vector3> usedPositions = new List<Vector3>();

    foreach (Transform obstacle in transform)
    {
        bool placed = false;
        int maxTries = 100;

        for (int i = 0; i < maxTries; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(minX, maxX),
                spawnY,
                Random.Range(minZ, maxZ)
            );

            if (usedPositions.TrueForAll(pos => Vector3.Distance(pos, randomPos) >= minSpacing))
            {
                obstacle.position = randomPos;
                usedPositions.Add(randomPos);

                Rigidbody rb = obstacle.GetComponent<Rigidbody>();
                if (rb != null) rb.Sleep();

                placed = true;
                break;
            }
        }

        if (!placed)
        {
            Debug.LogWarning($"No se encontró espacio para el objeto {obstacle.name}, lo pondré en el centro.");
            obstacle.position = center;
        }
    }
}

    Vector3 GenerarPosicionSinColision(float spawnY)
    {
        int intentos = 100;
        while (intentos-- > 0)
        {
            float x = Random.Range(-areaSize.x / 2f + 0.2f, areaSize.x / 2f - 0.2f);
            float z = Random.Range(-areaSize.z / 2f + 0.2f, areaSize.z / 2f - 0.2f);
            Vector3 nuevaPos = new Vector3(x, spawnY, z);

            bool muyCerca = false;
            foreach (var pos in posicionesUsadas)
            {
                if (Vector3.Distance(pos, nuevaPos) < minSpacing)
                {
                    muyCerca = true;
                    break;
                }
            }

            if (!muyCerca)
            {
                posicionesUsadas.Add(nuevaPos);
                return nuevaPos;
            }
        }

        return new Vector3(0, spawnY, 0); // fallback
    }
}
