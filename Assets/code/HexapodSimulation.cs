using UnityEngine;

public class HexapodSimulation : MonoBehaviour
{
    void Start()
    {
        HexapodState state = new HexapodState();
        for (int i = 0; i < 6; i++) state.LP[i] = 20f;

        float dt = 1f;
        for (int step = 0; step < 1000; step++)
        {
            Stimuli.Update(state, go: 1, bk: 0, spinL: 0, spinR: 0, left: 0, right: 0, dt);
            CPG.Update(state.CPGs, dt);

            float[] offset = new float[] { 10, 0, -10, 10, 0, -10 };
            for (int j = 0; j < 6; j++)
            {
                float cpgXY = (j % 2 == 0 ? state.CPGs[5] : state.CPGs[6]) * state.DIR1 * 5 + offset[j];
                float T = 130 + 3 * cpgXY * state.DIR1;
                float cpgZ = (j % 2 == 0 ? state.CPGs[8] : state.CPGs[9]);

                Locomotion.Update(ref state.Q1[j], ref state.Q2[j], ref state.Q3[j],
                                  ref state.E[j], ref state.LP[j], ref state.L2P[j], ref state.L3P[j],
                                  T, cpgXY, cpgZ, dt);
            }

            if (step % 100 == 0)
                Debug.Log($"[Paso {step}] CPG6: {state.CPGs[5]:F3}, DIR1: {state.DIR1:F3}, Q1_1: {state.Q1[0]:F1}");
        }
    }
}
