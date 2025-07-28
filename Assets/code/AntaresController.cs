using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;  

public class AntaresController : MonoBehaviour
{
    public bool disableGravity = false;

    public Transform Cooxa1;
    public Transform Cooxa2;
    public Transform Cooxa3;
    public Transform Cooxa4;
    public Transform Cooxa5;
    public Transform Cooxa6;

    public float L0 = 86.0f;
    public float L1 = 74.28f;
    public float L2 = 140.85f;

    public float d = 40f, al = 60f, n = 20f, w = 1f, rs = 0f, ra = 0f, c = 0f;
    public float k = 0f;

    public float hb = -20f;
    public float wb = 80f;

    // VARIABLES DE LA RED//
    public float go;
    public float bk;
    public float left;
    public float right;
    public float spinl;
    public float spinr;
    
    // Interpolation variables for smooth al transitions
    private float targetAl = 60f;
    public float alInterpolationSpeed = 20f; // Units per second

    private Vector3[] mountPoints;

    private Transform[] coxas;
    private Transform[] femurs;
    private Transform[] tibias;

    public enum ControlMode { InverseKinematics, NeuralCircuit }
    public ControlMode controlMode = ControlMode.InverseKinematics;

    private Sensors sensors;
    private HexapodState neuralState = new HexapodState();
    public float dt = 0.01f; // Simulation timestep for neural circuit
        // VARIABLES DE LA RED//
    public float go = 0f;
    public float bk = 0f;
    public float left = 0f;
    public float right = 0f;

    public float spinL = 0f;
    public float spinR = 0f;

    public float[] RangoOPQ1_offset = new float[] { 40, 0, -40, -40, 0, 40 };
    public float[] T = new float[] { 90, 130, 90, 90, 130, 90 };

    private ControlPlay controls;
    private Vector2 moveInput; // Guardará el valor del analógico izquierdo
    private bool girandoDerecha = false;
    private bool girandoIzquierda = false;
    private ControlMode previousMode;
    private float previousD = 0f;
    private Vector2 FlechasInput;
    public bool useGamepadControl = true;
    private float analogMagnitude = 0f;
    private float dtOffset = 0f;

    void Start()
    {
        if (disableGravity)
        {
            var rootBody = GetComponent<ArticulationBody>();
            if (rootBody != null)
                rootBody.useGravity = false;

            foreach (var body in GetComponentsInChildren<ArticulationBody>())
            {
                body.useGravity = false;
            }
        }

        // Reordenar coxas para hacer coincidir el orden MATLAB → Unity
        coxas = new Transform[] { Cooxa4, Cooxa5, Cooxa6, Cooxa1, Cooxa2, Cooxa3 };

        mountPoints = new Vector3[]
        {
            new Vector3(62.77f,  90.45f, 123.83f),
            new Vector3(86f,     0f,     123.83f),
            new Vector3(65.89f, -88.21f, 123.83f),
            new Vector3(-65.89f, 88.21f, 123.83f),
            new Vector3(-86f,    0f,     123.83f),
            new Vector3(-62.77f, -90.45f, 123.83f)
        };

        femurs = new Transform[6];
        tibias = new Transform[6];
        for (int i = 0; i < 6; i++)
        {
            femurs[i] = coxas[i].GetChild(0);
            tibias[i] = femurs[i].GetChild(0);
        }
        sensors = GetComponent<Sensors>();
        previousMode = controlMode;
        
        // Initialize targetAl to current al value to prevent initial jump
        targetAl = al;
    }
    void OnEnable()
    {
        controls = new ControlPlay();
        controls.Enable();
        controls.Move.Mover.performed += OnMovePerformed;
        controls.Move.Mover.canceled += OnMoveCanceled;
        controls.Move.GiroDer.started += OnGiroDerStarted;
        controls.Move.GiroDer.canceled += OnGiroDerCanceled;
        controls.Move.GiroIzq.started += OnGiroIzqStarted;
        controls.Move.GiroIzq.canceled += OnGiroIzqCanceled;
        controls.Move.Flechas.performed += OnFlechasPerformed;
        controls.Move.Flechas.canceled += OnFlechasCanceled;
        controls.Move.AumentarDt.started += OnR2Pressed;
        controls.Move.DisminuirDt.started += OnL2Pressed;


    }
    
    void OnDisable()
    {
        controls.Move.Mover.performed -= OnMovePerformed;
        controls.Move.Mover.canceled -= OnMoveCanceled;
        controls.Move.GiroDer.started -= OnGiroDerStarted;
        controls.Move.GiroDer.canceled -= OnGiroDerCanceled;
        controls.Move.GiroIzq.started -= OnGiroIzqStarted;
        controls.Move.GiroIzq.canceled -= OnGiroIzqCanceled;
        controls.Move.Flechas.performed -= ctx => FlechasInput = ctx.ReadValue<Vector2>();
        controls.Move.Flechas.canceled -= ctx => FlechasInput = Vector2.zero;
        controls.Move.Flechas.performed -= OnFlechasPerformed;
        controls.Move.Flechas.canceled -= OnFlechasCanceled;
        controls.Move.AumentarDt.started -= OnR2Pressed;
        controls.Move.DisminuirDt.started -= OnL2Pressed;

        controls.Disable();
    }

    void Update()
    {
        if (controlMode == ControlMode.InverseKinematics)
        {
            if (girandoDerecha || girandoIzquierda)
            {
                d = 40f;           // velocidad constante
                targetAl = 50f;    // amplitud de paso (target)
                w = -1f;           // dirección de paso invertida
                ra = 1f;           // orientación radial
                c = 20f;           // radio de curvatura del giro
                rs = girandoDerecha ? 0f : Mathf.PI;  // 0 = giro horario, PI = antihorario
            }
            else
            {
                ra = 0f;
                c = 0f;
                w = 1f;

                if (useGamepadControl)
                {
                    float magnitude = moveInput.magnitude;
                    float angleRad = Mathf.Atan2(-moveInput.x, moveInput.y);
                    if (angleRad < 0)
                        angleRad += 2 * Mathf.PI;

                    d = Mathf.Clamp01(magnitude) * 40f;

                    if (d < 1f)
                    {
                        d = 0.001f;
                        targetAl = 0f;
                    }
                    else
                    {
                        targetAl = 20f;
                    }

                    rs = angleRad;
                }
                else
                {
                    // Control manual desde el Inspector: d, al, rs ya están definidos en el editor
                    // targetAl uses the current al value from the inspector
                    targetAl = al;
                }
            }
            
            // Smoothly interpolate al towards targetAl
            al = Mathf.MoveTowards(al, targetAl, alInterpolationSpeed * Time.deltaTime);

            // Reiniciar k si el modo cambió
            if (previousMode != controlMode)
            {
                k = 0f;
                previousMode = controlMode;
            }

            // Reiniciar k si se pasa de quieto a movimiento
            if (previousD < 1f && d >= 1f)
            {
                k = 0f;
            }
            previousD = d;

            // Debug opcional
            Debug.Log($"Input: {moveInput}, D: {d:F2}, Rs: {rs:F2} rad");

            var targets = HexapodTrajectory.CalcularTrayectoria(d, al, n, w, rs, ra, c, k, hb, wb);
            for (int i = 0; i < 6; i++)
            {
                var angleModifier = 1f;
                if (i < 3) angleModifier = -1f;
                Vector3 basePos = mountPoints[i];
                Vector3 target = targets[i];
                Vector3 angles = HexapodKinematics.InverseKinematics(basePos, target, L0, L1, L2);

                var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                var coxaDrive = coxaBody.xDrive;
                if (i >= 3)
                {
                    angles.x = 180 - (((angles.x) + 720) % 360);
                }
                coxaDrive.target = angles.x * angleModifier;
                coxaBody.xDrive = coxaDrive;

                var femurBody = femurs[i].GetComponent<ArticulationBody>();
                var femurDrive = femurBody.xDrive;
                femurDrive.target = angles.y * angleModifier;
                femurBody.xDrive = femurDrive;

                var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                var tibiaDrive = tibiaBody.xDrive;
                tibiaDrive.target = angles.z * angleModifier;
                tibiaBody.xDrive = tibiaDrive;
            }
            k += 60 * Mathf.PI / 100 * Time.deltaTime;
            if (k > 60 * Mathf.PI) k = 0;
        }
        else if (controlMode == ControlMode.NeuralCircuit)
        {
            //CONTROL POR MEDIO DEL PS4, HACE PARTE DE LA SEGUNDA VERSION 
            
            // Reseteo inputs
            /* go = 0f;
            bk = 0f;
            left = 0f;
            right = 0f;
            spinL = 0f;
            spinR = 0f; */

            // Leer input del analógico
            float joystickX = moveInput.x;
            float joystickY = moveInput.y;

            // Umbral mínimo para evitar ruidos pequeños
            float threshold = 0.2f;

            if (joystickY > threshold)
                go = joystickY;
            else if (joystickY < -threshold)
                bk = -joystickY;

            if (joystickX > threshold)
                right = joystickX;
            else if (joystickX < -threshold)
                left = -joystickX;

            // Leer spin con L2 y R2
            float l2 = controls.Move.GiroIzq.ReadValue<float>();
            float r2 = controls.Move.GiroDer.ReadValue<float>();

            if (l2 > 0.2f)
                spinL = l2;
            if (r2 > 0.2f)
                spinR = r2;
            //////////////////////////////////////////////////////
            go = 0; bk = 0; left = 0; right = 0;
            float D = 4, T = 90;


            // Calcular dirección del analógico
            float angle = Mathf.Atan2(moveInput.x, moveInput.y); // Y es adelante
            if (angle < 0) angle += 2 * Mathf.PI; // Aseguramos 0–2π

            // Normalizar ángulo en rangos direccionales (tipo brújula)
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Mapeo direccional proporcional (0–10)
            go = Mathf.Clamp01(cos) * 10f;
            bk = Mathf.Clamp01(-cos) * 10f;
            right = Mathf.Clamp01(sin) * 10f;
            left = Mathf.Clamp01(-sin) * 10f;
            // Aplicar deadzone: go y bk deben ser mayores a 3 para activarse
            if (go < 3f) go = 0f;
            if (bk < 3f) bk = 0f;

            // Magnitud del stick (0–1), escalada a dt (0–0.5)
            analogMagnitude = Mathf.Clamp01(moveInput.magnitude);
            float dynamicDt = analogMagnitude * 0.5f;
            


            // Si no se está girando, usar dt dinámico
            if (!girandoDerecha && !girandoIzquierda)
            {
                dt = dynamicDt;
            }
            if (girandoDerecha)
            {
                spinr = 10f;
                spinl = 0f;
                dt = 0.5f;
            }
            else if (girandoIzquierda)
            {
                spinl = 10f;
                spinr = 0f;
                dt = 0.5f;
            }
            else
            {
                spinl = 0f;
                spinr = 0f;
            }

            dt = Mathf.Clamp(dt + dtOffset, 0f, 0.9f);
            for (int j = 0; j < 50; j++)
            {
                Stimuli.Update(neuralState, go, bk, spinl, spinr, left, right, dt);
                CPG.Update(neuralState.CPGs, dt);

                // 6 patas (0-5)
                Locomotion.Update(ref neuralState.Q1[0], ref neuralState.Q2[0], ref neuralState.Q3[0], ref neuralState.E[0], ref neuralState.Ei[0], ref neuralState.LP[0], ref neuralState.L2P[0], ref neuralState.L3P[0],
                    T[0] + neuralState.CPGs[5] * D * (neuralState.DIR3 - 0.1f * neuralState.DIR4), -neuralState.CPGs[5] * D * neuralState.DIR1 + RangoOPQ1_offset[0], 3f * neuralState.CPGs[8]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[4], ref neuralState.Q2[4], ref neuralState.Q3[4], ref neuralState.E[4], ref neuralState.Ei[4], ref neuralState.LP[4], ref neuralState.L2P[4], ref neuralState.L3P[4],
                    T[4] - neuralState.CPGs[5] * D * neuralState.DIR3, -neuralState.CPGs[5] * D * neuralState.DIR2 + RangoOPQ1_offset[4], 3f * neuralState.CPGs[8]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[2], ref neuralState.Q2[2], ref neuralState.Q3[2], ref neuralState.E[2], ref neuralState.Ei[2], ref neuralState.LP[2], ref neuralState.L2P[2], ref neuralState.L3P[2],
                    T[2] + neuralState.CPGs[5] * D * (neuralState.DIR3 + 0.1f * neuralState.DIR4), -neuralState.CPGs[5] * D * neuralState.DIR1 + RangoOPQ1_offset[2], 3f * neuralState.CPGs[8]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[3], ref neuralState.Q2[3], ref neuralState.Q3[3], ref neuralState.E[3], ref neuralState.Ei[3], ref neuralState.LP[3], ref neuralState.L2P[3], ref neuralState.L3P[3],
                    T[3] - neuralState.CPGs[6] * D * (neuralState.DIR3 - 0.1f * neuralState.DIR4), -neuralState.CPGs[6] * D * neuralState.DIR2 + RangoOPQ1_offset[3], 3f * neuralState.CPGs[9]* neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[1], ref neuralState.Q2[1], ref neuralState.Q3[1], ref neuralState.E[1], ref neuralState.Ei[1], ref neuralState.LP[1], ref neuralState.L2P[1], ref neuralState.L3P[1],
                    T[1] + neuralState.CPGs[6] * D * neuralState.DIR3, -neuralState.CPGs[6] * D * neuralState.DIR1 + RangoOPQ1_offset[1], 3f * neuralState.CPGs[9]* neuralState.MOV, dt);
                
                Locomotion.Update(ref neuralState.Q1[5], ref neuralState.Q2[5], ref neuralState.Q3[5], ref neuralState.E[5], ref neuralState.Ei[5], ref neuralState.LP[5], ref neuralState.L2P[5], ref neuralState.L3P[5],
                    T[5] - neuralState.CPGs[6] * D * (neuralState.DIR3 + 0.1f * neuralState.DIR4), -neuralState.CPGs[6] * D * neuralState.DIR2 + RangoOPQ1_offset[5], 3f * neuralState.CPGs[9]* neuralState.MOV, dt);

                // Aplicar ángulos
                for (int i = 0; i < 6; i++)
                {
                    var angleModifier = -1f;
                    var angleModifierCOX = 1f;
                    if (i < 3) { angleModifier = 1f; angleModifierCOX = 1f; }
                    var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                    var coxaDrive = coxaBody.xDrive;
                    coxaDrive.target = neuralState.Q1[i] * angleModifier * angleModifierCOX;
                    coxaBody.xDrive = coxaDrive;

                    var femurBody = femurs[i].GetComponent<ArticulationBody>();
                    var femurDrive = femurBody.xDrive;
                    femurDrive.target = neuralState.Q2[i] * angleModifier;
                    femurBody.xDrive = femurDrive;

                    var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                    var tibiaDrive = tibiaBody.xDrive;
                    tibiaDrive.target = neuralState.Q3[i] * angleModifier;
                    tibiaBody.xDrive = tibiaDrive;
                }
            }
            // Resetear dtOffset si no hay movimiento ni giro
            if (moveInput.magnitude < 0.01f && !girandoDerecha && !girandoIzquierda)
            {
                if (dtOffset != 0f)
                {
                    dtOffset = 0f;
                    Debug.Log("dtOffset reseteado a 0 por estar quieto");
                }
            }
        }
    }
    private ArticulationDrive ConfigureDrive(float target, float stiffness = 1000f, float damping = 500f, float forceLimit = 100f)
    {
        ArticulationDrive drive = new ArticulationDrive();
        drive.stiffness = stiffness;
        drive.damping = damping;
        drive.forceLimit = forceLimit;
        drive.target = target;
        drive.targetVelocity = 0f; // No se usa cuando stiffness > 0
        return drive;
    }
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
        n = 0;
    }
    private void OnGiroDerStarted(InputAction.CallbackContext ctx) => girandoDerecha = true;
    private void OnGiroDerCanceled(InputAction.CallbackContext ctx){girandoDerecha = false; n = 0;}
    private void OnGiroIzqStarted(InputAction.CallbackContext ctx) => girandoIzquierda = true;
    private void OnGiroIzqCanceled(InputAction.CallbackContext ctx){girandoIzquierda = false; n = 0;}
    private void OnFlechasPerformed(InputAction.CallbackContext ctx)
    {
        FlechasInput = ctx.ReadValue<Vector2>();

        if (FlechasInput.y > 0.5f)
        {
            hb -= 5;
            Debug.Log("flecha arriba hb -= 5 => " + hb);
        }
        else if (FlechasInput.y < -0.5f)
        {
            hb += 5;
            Debug.Log("flecha abajo hb += 5 => " + hb);
        }

        if (FlechasInput.x > 0.5f)
        {
            wb += 5;
            Debug.Log("flecha derecha wb += 5 => " + wb);
        }
        else if (FlechasInput.x < -0.5f)
        {
            wb -= 5;
            Debug.Log("flecha izquierda wb -= 5 => " + wb);
        }
    }

    private void OnFlechasCanceled(InputAction.CallbackContext ctx)
    {
        FlechasInput = Vector2.zero;
    }

    // Method to get joint forces and torques
    public void LogJointForcesTorques()
    {
        for (int i = 0; i < 6; i++)
        {
            // Coxa joint
            var coxaBody = coxas[i].GetComponent<ArticulationBody>();
            if (coxaBody != null)
            {
                var coxaJointForce = coxaBody.jointForce;
                string coxaForceStr = "Joint Forces: ";
                for (int dof = 0; dof < coxaJointForce.dofCount; dof++)
                {
                    coxaForceStr += $"DOF{dof}: {coxaJointForce[dof]:F2} ";
                }
                
                Debug.Log($"Leg {i} Coxa - {coxaForceStr}");
            }

            // Femur joint
            var femurBody = femurs[i].GetComponent<ArticulationBody>();
            if (femurBody != null)
            {
                var femurJointForce = femurBody.jointForce;
                string femurForceStr = "Joint Forces: ";
                for (int dof = 0; dof < femurJointForce.dofCount; dof++)
                {
                    femurForceStr += $"DOF{dof}: {femurJointForce[dof]:F2} ";
                }
                
                Debug.Log($"Leg {i} Femur - {femurForceStr}");
            }

            // Tibia joint
            var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
            if (tibiaBody != null)
            {
                var tibiaJointForce = tibiaBody.jointForce;
                string tibiaForceStr = "Joint Forces: ";
                for (int dof = 0; dof < tibiaJointForce.dofCount; dof++)
                {
                    tibiaForceStr += $"DOF{dof}: {tibiaJointForce[dof]:F2} ";
                }
                
                Debug.Log($"Leg {i} Tibia - {tibiaForceStr}");
            }
        }
    }

    // Method to get total joint load for a specific leg
    public float GetLegTotalForce(int legIndex)
    {
        if (legIndex < 0 || legIndex >= 6) return 0f;
        
        float totalForce = 0f;
        
        var coxaBody = coxas[legIndex].GetComponent<ArticulationBody>();
        if (coxaBody != null)
        {
            var jointForce = coxaBody.jointForce;
            for (int dof = 0; dof < jointForce.dofCount; dof++)
            {
                totalForce += Mathf.Abs(jointForce[dof]);
            }
        }
            
        var femurBody = femurs[legIndex].GetComponent<ArticulationBody>();
        if (femurBody != null)
        {
            var jointForce = femurBody.jointForce;
            for (int dof = 0; dof < jointForce.dofCount; dof++)
            {
                totalForce += Mathf.Abs(jointForce[dof]);
            }
        }
            
        var tibiaBody = tibias[legIndex].GetComponent<ArticulationBody>();
        if (tibiaBody != null)
        {
            var jointForce = tibiaBody.jointForce;
            for (int dof = 0; dof < jointForce.dofCount; dof++)
            {
                totalForce += Mathf.Abs(jointForce[dof]);
            }
        }
            
        return totalForce;
    }

    // Method to get joint torque for a specific joint
    public float GetJointTorque(ArticulationBody joint)
    {
        if (joint == null) return 0f;
        
        var jointForce = joint.jointForce;
        if (jointForce.dofCount > 0)
        {
            // For revolute joints, there's typically 1 DOF representing torque
            return jointForce[0];
        }
        return 0f;
    }
    private void OnR2Pressed(InputAction.CallbackContext ctx)
    {
        if (HexapodIsMoving())
        {
            dtOffset = Mathf.Min(dtOffset + 0.1f, 0.9f); // límite superior controlado por Clamp en Update
            Debug.Log($"[R2] dtOffset aumentado a {dtOffset:F2}");
        }
    }

    private void OnL2Pressed(InputAction.CallbackContext ctx)
    {
        if (HexapodIsMoving())
        {
            dtOffset = Mathf.Max(dtOffset - 0.1f, -0.4f); // para que no baje más allá del mínimo dt=0.1
            Debug.Log($"[L2] dtOffset disminuido a {dtOffset:F2}");
        }
    }

    private bool HexapodIsMoving()
    {
        return moveInput.magnitude > 0.1f || girandoDerecha || girandoIzquierda;
    }

}