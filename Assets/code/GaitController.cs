using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.UrdfImporter.Control;

public class GaitController : MonoBehaviour
{
    [System.Serializable]
    public class Leg
    {
        public ArticulationBody coxa;   // eje Y
        public ArticulationBody femur;  // eje X
        public ArticulationBody tibia;  // eje X

        public float coxaOffset;
        public float femurOffset;
        public float tibiaOffset;
    }

    public List<Leg> legs = new List<Leg>();

    public float stepDuration = 1.0f; // tiempo de cada fase
    private float timer;

    public float coxaMin = 45f;
    public float coxaMax = 130f;

    public float femurMin = 0f;
    public float femurMax = 75f;

    public float tibiaMin = 0f;
    public float tibiaMax = 130f;

    private bool steppingPhase = false;

    void Start()
    {
        timer = 0;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= stepDuration)
        {
            steppingPhase = !steppingPhase;
            timer = 0;
        }

        for (int i = 0; i < legs.Count; i++)
        {
            bool isStepping = (i % 2 == 0) ? steppingPhase : !steppingPhase;
            StepLeg(legs[i], isStepping);
        }
    }

    void StepLeg(Leg leg, bool isStepping)
    {
        float t = timer / stepDuration;
        float coxaTarget, femurTarget, tibiaTarget;

        if (isStepping)
        {
            // fase de levantamiento y avance
            femurTarget = Mathf.Lerp(femurMin, femurMax, Mathf.Sin(t * Mathf.PI));
            tibiaTarget = Mathf.Lerp(tibiaMin, tibiaMax, Mathf.Sin(t * Mathf.PI));
            coxaTarget = Mathf.Lerp(coxaMin, coxaMax, t);
        }
        else
        {
            // fase de apoyo
            femurTarget = femurMin;
            tibiaTarget = tibiaMin;
            coxaTarget = Mathf.Lerp(coxaMax, coxaMin, t);
        }

        SetJointTarget(leg.femur, femurTarget + leg.femurOffset);
        SetJointTarget(leg.tibia, tibiaTarget + leg.tibiaOffset);
        SetJointTarget(leg.coxa, coxaTarget + leg.coxaOffset);
    }

    void SetJointTarget(ArticulationBody joint, float target)
    {
        var drive = joint.xDrive;
        drive.target = target;
        joint.xDrive = drive;
    }

    // 💡 Asigná los joints manualmente desde el inspector o hacé una función para hacerlo por nombre
    // Ejemplo para buscar joints por nombre:
    // leg.coxa = transform.Find("NombreCoxa").GetComponent<ArticulationBody>();
    // leg.femur = transform.Find("NombreFemur").GetComponent<ArticulationBody>();
    // leg.tibia = transform.Find("NombreTibia").GetComponent<ArticulationBody>();
}

