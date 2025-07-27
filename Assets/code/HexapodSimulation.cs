using UnityEngine;

public class HexapodSimulation : MonoBehaviour
{
    public HexapodState state = new HexapodState();  // lo dejamos público para acceder desde otros scripts
    public float dt = 0.01f;
    public float D = 2f;
    public float T = 100f;
    public float[] RangoOPQ1_offset = new float[] { 50, 0, -50, -50, 0, 50 };

    void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            state.LP[i] = 20f;
            state.Ei[i] = 0f;
        }

        for (int step = 0; step < 1000; step++)
        {
            // Actualizar estímulos y CPGs
            Stimuli.Update(state, go: 1, bk: 0, spinL: 0, spinR: 0, left: 0, right: 0, dt);
            CPG.Update(state.CPGs, dt);

            // === Actualización de las 6 patas ===
            // Pierna 1
            Locomotion.Update(ref state.Q1[0], ref state.Q2[0], ref state.Q3[0],
                              ref state.E[0], ref state.Ei[0], ref state.LP[0], ref state.L2P[0], ref state.L3P[0],
                              T + state.CPGs[5] * D * (state.DIR3 - 0.1f * state.DIR4),
                              state.CPGs[5] * D * state.DIR1 + RangoOPQ1_offset[0],
                              3f * state.CPGs[8], dt);

            // Pierna 3
            Locomotion.Update(ref state.Q1[2], ref state.Q2[2], ref state.Q3[2],
                              ref state.E[2], ref state.Ei[2], ref state.LP[2], ref state.L2P[2], ref state.L3P[2],
                              T + state.CPGs[5] * D * (state.DIR3 + 0.1f * state.DIR4),
                              state.CPGs[5] * D * state.DIR1 + RangoOPQ1_offset[2],
                              3f * state.CPGs[8], dt);

            // Pierna 5
            Locomotion.Update(ref state.Q1[4], ref state.Q2[4], ref state.Q3[4],
                              ref state.E[4], ref state.Ei[4], ref state.LP[4], ref state.L2P[4], ref state.L3P[4],
                              T + state.CPGs[6] * D * state.DIR3,
                              state.CPGs[6] * D * state.DIR1 + RangoOPQ1_offset[4],
                              3f * state.CPGs[9], dt);

            // Pierna 2
            Locomotion.Update(ref state.Q1[1], ref state.Q2[1], ref state.Q3[1],
                              ref state.E[1], ref state.Ei[1], ref state.LP[1], ref state.L2P[1], ref state.L3P[1],
                              T - state.CPGs[5] * D * state.DIR3,
                              -state.CPGs[5] * D * state.DIR2 + RangoOPQ1_offset[1],
                              3f * state.CPGs[8], dt);

            // Pierna 4
            Locomotion.Update(ref state.Q1[3], ref state.Q2[3], ref state.Q3[3],
                              ref state.E[3], ref state.Ei[3], ref state.LP[3], ref state.L2P[3], ref state.L3P[3],
                              T - state.CPGs[6] * D * (state.DIR3 - 0.1f * state.DIR4),
                              -state.CPGs[6] * D * state.DIR2 + RangoOPQ1_offset[3],
                              3f * state.CPGs[9], dt);

            // Pierna 6
            Locomotion.Update(ref state.Q1[5], ref state.Q2[5], ref state.Q3[5],
                              ref state.E[5], ref state.Ei[5], ref state.LP[5], ref state.L2P[5], ref state.L3P[5],
                              T - state.CPGs[6] * D * (state.DIR3 + 0.1f * state.DIR4),
                              -state.CPGs[6] * D * state.DIR2 + RangoOPQ1_offset[5],
                              3f * state.CPGs[9], dt);

            // Debug cada 100 pasos
            if (step % 100 == 0)
                Debug.Log($"[Paso {step}] CPG6: {state.CPGs[5]:F3}, DIR1: {state.DIR1:F3}, Q1_1: {state.Q1[0]:F1}");
        }
    }
}

