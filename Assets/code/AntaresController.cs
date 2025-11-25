using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;


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

    public float d = 40f, al = 60f, n = 20f, w = -1f, rs = 0f, ra = 1f, c = 0f;
    public float k = 0f;

    public float hb = -20f;
    public float wb = 80f;

    // VARIABLES DE LA RED//
    public float go;
    public float bk;
    public float left;
    public float right;
    public float spinL;
    public float spinR;

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
    public float dtManual = 0.6f;  // Editable en el Inspector

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

    //////////////////////////////
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Vector3> velocityHistory = new List<Vector3>();
    private List<Vector3> angularVelocityHistory = new List<Vector3>();
    private List<float[]> jointTorquesHistory = new List<float[]>(); // <- Aportado por tu amigo
    private float totalDistance = 0f;
    private Vector3 lastPosition;

    /// <summary>
    /// Variables para captura de datos
    /// </summary>
    private float dataCaptureStartTime;
    private bool isCapturingData = false;
    private bool hasExportedData = false;
    private float captureDuration = 40f;

    // Variables de estabilización al inicio
    private float warmupDuration = 2f;
    private bool warmupComplete = false;
    private float simulationStartTime;

    // Copias temporales de parámetros del Inspector para restauración post-warmup
    private float initialGo, initialBk, initialLeft, initialRight, initialSpinL, initialSpinR;
    private float initialD, initialAl;
    private bool initialValuesStored = false;
    private bool isGreeting = false;
    // Angulos de pose estable (reposo)
    private float[] restCoxa = { -33f, 0f, 33f, 33f, 0f, -33f };
    private float[] restFemur = { 0f, 0f, 0f, 0f, 0f, 0f };
    private float[] restTibia = { -90f, -90f, -90f, 90f, 90f, 90f };

    void Start()
    {
        // Desactivar gravedad si está configurado
        var rootBody = GetComponent<ArticulationBody>();
        if (disableGravity && rootBody != null)
        {
            rootBody.useGravity = false;
        }

        foreach (var body in GetComponentsInChildren<ArticulationBody>())
        {
            if (disableGravity)
            {
                body.useGravity = false;
            }

            body.matchAnchors = true;

            // Mostrar configuración de las articulaciones (útil para debug)
            if (!body.isRoot && body.jointType == ArticulationJointType.RevoluteJoint)
            {
                Debug.Log($"Joint {body.name}: Drive stiffness = {body.xDrive.stiffness}, damping = {body.xDrive.damping}");
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

        // Inicializar valor objetivo de al
        targetAl = al;

        // Warm-up: evitar captura inmediata de datos
        simulationStartTime = Time.time;
        warmupComplete = false;

        // Asegurarse de no comenzar a capturar datos aún
        isCapturingData = false;
        dataCaptureStartTime = Time.time;
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
        controls.Move.Saludar.started += OnSaludarPressed;

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
        controls.Move.Saludar.started -= OnSaludarPressed;
        controls.Disable();
    }

    void Update()
    {
        float elapsedSinceStart = Time.time - simulationStartTime;
        if (!warmupComplete)
        {
            if (!initialValuesStored)
            {
                // Guardar parámetros del Inspector
                if (controlMode == ControlMode.NeuralCircuit)
                {
                    initialGo = go;
                    initialBk = bk;
                    initialLeft = left;
                    initialRight = right;
                    initialSpinL = spinL;
                    initialSpinR = spinR;
                }
                else if (controlMode == ControlMode.InverseKinematics)
                {
                    initialD = d;
                    initialAl = al;
                }
                initialValuesStored = true;
            }

            // Asignar valores de estabilización
            if (controlMode == ControlMode.NeuralCircuit)
            {
                go = 0f;
                bk = 0f;
                left = 0f;
                right = 0f;
                spinL = 0f;
                spinR = 0f;
            }
            else if (controlMode == ControlMode.InverseKinematics)
            {
                d = 1f;
                al = 0f;
            }

            
            if (elapsedSinceStart >= warmupDuration)
            {
                warmupComplete = true;
                dataCaptureStartTime = Time.time;
                isCapturingData = true;

                // Restaurar valores del Inspector
                if (controlMode == ControlMode.NeuralCircuit)
                {
                    go = initialGo;
                    bk = initialBk;
                    left = initialLeft;
                    right = initialRight;
                    spinL = initialSpinL;
                    spinR = initialSpinR;
                }
                else if (controlMode == ControlMode.InverseKinematics)
                {
                    d = initialD;
                    al = initialAl;
                }

                Debug.Log("🏁 Warm-up terminado. Restaurados parámetros y comienza captura de datos.");
            }
        }
        if (isGreeting)
        {
            return; // No actualizar locomoción mientras saluda
        }
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
                if (useGamepadControl)
                {
                    ra = 0f;
                    c = 0f;
                    w = 1f;

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
                        targetAl = 35f;
                    }

                    rs = angleRad;
                }
                else
                {
                    // MODO MANUAL — deja que el usuario defina todo desde el Inspector
                    targetAl = al;
                    // NO tocar: w, ra, c, d, rs
                    // Son definidos manualmente
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
            //Debug.Log($"Input: {moveInput}, D: {d:F2}, Rs: {rs:F2} rad");
            List<float> jointAngles = new List<float>();
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
                jointAngles.Add(coxaDrive.target);

                var femurBody = femurs[i].GetComponent<ArticulationBody>();
                var femurDrive = femurBody.xDrive;
                femurDrive.target = angles.y * angleModifier;
                femurBody.xDrive = femurDrive;
                jointAngles.Add(femurDrive.target);

                var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                var tibiaDrive = tibiaBody.xDrive;
                tibiaDrive.target = angles.z * angleModifier;
                tibiaBody.xDrive = tibiaDrive;
                jointAngles.Add(tibiaDrive.target);
            }
            FindObjectOfType<PipeClient>()?.SendJointAngles(jointAngles);
            k += 60 * Mathf.PI / 100 * Time.deltaTime;
            if (k > 60 * Mathf.PI) k = 0;
        }
        else if (controlMode == ControlMode.NeuralCircuit)
        {
            //CONTROL POR MEDIO DEL PS4, HACE PARTE DE LA SEGUNDA VERSION 

            // Reseteo inputs
            //go = 0f;
            //bk = 0f;
            //left = 0f;
            //right = 0f;
            //spinL = 0f;
            //spinR = 0f; 

            // Leer input del analógico
            if (useGamepadControl)
            {
                // Leer input del analógico
                float joystickX = moveInput.x;
                float joystickY = moveInput.y;

                float threshold = 0.2f;

                if (joystickY > threshold)
                    go = joystickY;
                else if (joystickY < -threshold)
                    bk = -joystickY;

                if (joystickX > threshold)
                    right = joystickX;
                else if (joystickX < -threshold)
                    left = -joystickX;

                float l2 = controls.Move.GiroIzq.ReadValue<float>();
                float r2 = controls.Move.GiroDer.ReadValue<float>();

                if (l2 > 0.2f)
                    spinL = l2;
                if (r2 > 0.2f)
                    spinR = r2;

                float angle = Mathf.Atan2(moveInput.x, moveInput.y);
                if (angle < 0) angle += 2 * Mathf.PI;

                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                if (moveInput.y < 0)
                {
                    bk = -moveInput.y * 10f;
                    go = 0f;
                }
                else
                {
                    bk = 0f;
                    go = moveInput.y * 10f;
                }
                if (moveInput.x < 0)
                {
                    left = -moveInput.x * 10f;
                    right = 0f;
                }
                else
                {
                    left = 0f;
                    right = moveInput.x * 10f;
                }

                if (go < 3f) go = 0f;
                if (bk < 3f) bk = 0f;

                analogMagnitude = Mathf.Clamp01(moveInput.magnitude);
                float dynamicDt = analogMagnitude * 0.5f;

                if (!girandoDerecha && !girandoIzquierda)
                {
                    dt = 0.1f + dynamicDt;
                }
                if (girandoDerecha)
                {
                    spinR = 10f;
                    spinL = 0f;
                    dt = 0.5f;
                }
                else if (girandoIzquierda)
                {
                    spinL = 10f;
                    spinR = 0f;
                    dt = 0.5f;
                }
                else
                {
                    spinL = 0f;
                    spinR = 0f;
                }

                dt = Mathf.Clamp(dt + dtOffset, 0f, 0.9f);
            }
            else
            {
                dt = dtManual; // Usar el valor manual del Inspector
            }

            for (int j = 0; j < 50; j++)
            {
                float D = 4f;
                Stimuli.Update(neuralState, go, bk, spinL, spinR, left, right, dt);
                CPG.Update(neuralState.CPGs, dt);

                // 6 patas (0-5)
                Locomotion.Update(ref neuralState.Q1[0], ref neuralState.Q2[0], ref neuralState.Q3[0], ref neuralState.E[0], ref neuralState.Ei[0], ref neuralState.LP[0], ref neuralState.L2P[0], ref neuralState.L3P[0],
                    T[0] + neuralState.CPGs[5] * D * (neuralState.DIR3 - 0.1f * neuralState.DIR4), -neuralState.CPGs[5] * D * neuralState.DIR1 + RangoOPQ1_offset[0], 3f * neuralState.CPGs[8] * neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[4], ref neuralState.Q2[4], ref neuralState.Q3[4], ref neuralState.E[4], ref neuralState.Ei[4], ref neuralState.LP[4], ref neuralState.L2P[4], ref neuralState.L3P[4],
                    T[4] - neuralState.CPGs[5] * D * neuralState.DIR3, -neuralState.CPGs[5] * D * neuralState.DIR2 + RangoOPQ1_offset[4], 3f * neuralState.CPGs[8] * neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[2], ref neuralState.Q2[2], ref neuralState.Q3[2], ref neuralState.E[2], ref neuralState.Ei[2], ref neuralState.LP[2], ref neuralState.L2P[2], ref neuralState.L3P[2],
                    T[2] + neuralState.CPGs[5] * D * (neuralState.DIR3 + 0.1f * neuralState.DIR4), -neuralState.CPGs[5] * D * neuralState.DIR1 + RangoOPQ1_offset[2], 3f * neuralState.CPGs[8] * neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[3], ref neuralState.Q2[3], ref neuralState.Q3[3], ref neuralState.E[3], ref neuralState.Ei[3], ref neuralState.LP[3], ref neuralState.L2P[3], ref neuralState.L3P[3],
                    T[3] - neuralState.CPGs[6] * D * (neuralState.DIR3 - 0.1f * neuralState.DIR4), -neuralState.CPGs[6] * D * neuralState.DIR2 + RangoOPQ1_offset[3], 3f * neuralState.CPGs[9] * neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[1], ref neuralState.Q2[1], ref neuralState.Q3[1], ref neuralState.E[1], ref neuralState.Ei[1], ref neuralState.LP[1], ref neuralState.L2P[1], ref neuralState.L3P[1],
                    T[1] + neuralState.CPGs[6] * D * neuralState.DIR3, -neuralState.CPGs[6] * D * neuralState.DIR1 + RangoOPQ1_offset[1], 3f * neuralState.CPGs[9] * neuralState.MOV, dt);

                Locomotion.Update(ref neuralState.Q1[5], ref neuralState.Q2[5], ref neuralState.Q3[5], ref neuralState.E[5], ref neuralState.Ei[5], ref neuralState.LP[5], ref neuralState.L2P[5], ref neuralState.L3P[5],
                    T[5] - neuralState.CPGs[6] * D * (neuralState.DIR3 + 0.1f * neuralState.DIR4), -neuralState.CPGs[6] * D * neuralState.DIR2 + RangoOPQ1_offset[5], 3f * neuralState.CPGs[9] * neuralState.MOV, dt);

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

                dt = 0.3f; // resetear dt a 0.3 si no hay movimiento
                
                if (dtOffset != 0f)
                {
                    dtOffset = 0f;
                    Debug.Log("dtOffset reseteado a 0.3 por estar  quieto");
                }
            }
            if (dt < 0.05)
            {
                dt = 0f;
            }
        }

        ////////////////////////
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    ExportDataToCSV();
        //} 
        //////////////////////////////

        
    }

    // void FixedUpdate()
    // {
    //     // Call joint force logging here for proper physics timing
    //     LogJointForcesTorques();
    // }

    ///////////////////////////////////////////
    void LateUpdate()
    {
        if (!isCapturingData) return;

        float elapsed = Time.time - dataCaptureStartTime;
        if (elapsed <= captureDuration)
        {
            ArticulationBody rootBody = GetComponent<ArticulationBody>();
            if (rootBody == null) return;

            Vector3 currentPosition = transform.position;
            positionHistory.Add(currentPosition);

            Vector3 linearVel = rootBody.velocity;
            velocityHistory.Add(linearVel);

            angularVelocityHistory.Add(rootBody.angularVelocity);

            // Recolectar torques de las 18 articulaciones (6 patas x 3 articulaciones cada una)
            float[] jointTorques = new float[18];
            int torqueIndex = 0;

            if (positionHistory.Count > 1)
                totalDistance += Vector3.Distance(currentPosition, lastPosition);

            lastPosition = currentPosition;

            for (int leg = 0; leg < 6; leg++)
            {
                // Coxa (índices 0-5)
                var coxaBody = coxas[leg].GetComponent<ArticulationBody>();
                jointTorques[torqueIndex] = EstimateJointTorque(coxaBody);
                torqueIndex++;

                // Femur (índices 6-11)
                var femurBody = femurs[leg].GetComponent<ArticulationBody>();
                jointTorques[torqueIndex] = EstimateJointTorque(femurBody);
                torqueIndex++;

                // Tibia (índices 12-17)
                var tibiaBody = tibias[leg].GetComponent<ArticulationBody>();
                jointTorques[torqueIndex] = EstimateJointTorque(tibiaBody);
                torqueIndex++;
            }
            
            jointTorquesHistory.Add(jointTorques);

            if (positionHistory.Count > 1)
                totalDistance += Vector3.Distance(currentPosition, lastPosition);

            lastPosition = currentPosition;
        }
        else if (!hasExportedData)
        {
            ExportDataToCSV();
            isCapturingData = false;
            hasExportedData = true;
        }
    }

        ///////////////////////////////////////////////////////  
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
    private void OnGiroDerCanceled(InputAction.CallbackContext ctx) { girandoDerecha = false; n = 0; }
    private void OnGiroIzqStarted(InputAction.CallbackContext ctx) => girandoIzquierda = true;
    private void OnGiroIzqCanceled(InputAction.CallbackContext ctx) { girandoIzquierda = false; n = 0; }
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
        // Only log occasionally to avoid spam
        if (Time.fixedTime % 1.0f < Time.fixedDeltaTime)  // Log every second
        {
            for (int i = 0; i < 6; i++)
            {
                // Coxa joint
                var coxaBody = coxas[i].GetComponent<ArticulationBody>();
                if (coxaBody != null)
                {
                    var coxaJointForce = coxaBody.jointForce;
                    string coxaForceStr = $"Joint Forces (DOF Count: {coxaJointForce.dofCount}): ";
                    for (int dof = 0; dof < coxaJointForce.dofCount; dof++)
                    {
                        coxaForceStr += $"DOF{dof}: {coxaJointForce[dof]:F2} ";
                    }
                    
                    // Also log current drive state for comparison
                    var drive = coxaBody.xDrive;
                    coxaForceStr += $"| Target: {drive.target:F2}, Current: {coxaBody.jointPosition[0]:F2}, Vel: {coxaBody.jointVelocity[0]:F2}";

                    Debug.Log($"Leg {i} Coxa - {coxaForceStr}");
            }

                // Femur joint
                var femurBody = femurs[i].GetComponent<ArticulationBody>();
                if (femurBody != null)
                {
                    var femurJointForce = femurBody.jointForce;
                    string femurForceStr = $"Joint Forces (DOF Count: {femurJointForce.dofCount}): ";
                    for (int dof = 0; dof < femurJointForce.dofCount; dof++)
                    {
                        femurForceStr += $"DOF{dof}: {femurJointForce[dof]:F2} ";
                    }
                    
                    var drive = femurBody.xDrive;
                    femurForceStr += $"| Target: {drive.target:F2}, Current: {femurBody.jointPosition[0]:F2}, Vel: {femurBody.jointVelocity[0]:F2}";

                    Debug.Log($"Leg {i} Femur - {femurForceStr}");
                }

                // Tibia joint
                var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
                if (tibiaBody != null)
                {
                    var tibiaJointForce = tibiaBody.jointForce;
                    string tibiaForceStr = $"Joint Forces (DOF Count: {tibiaJointForce.dofCount}): ";
                    for (int dof = 0; dof < tibiaJointForce.dofCount; dof++)
                    {
                        tibiaForceStr += $"DOF{dof}: {tibiaJointForce[dof]:F2} ";
                    }
                    
                    var drive = tibiaBody.xDrive;
                    tibiaForceStr += $"| Target: {drive.target:F2}, Current: {tibiaBody.jointPosition[0]:F2}, Vel: {tibiaBody.jointVelocity[0]:F2}";

                    Debug.Log($"Leg {i} Tibia - {tibiaForceStr}");
                }
            }
        }
    }

    
    public float EstimateJointTorque(ArticulationBody joint)
    {
        if (joint == null || joint.dofCount == 0) return 0f;
        
        var drive = joint.xDrive;
        float positionError = drive.target - joint.jointPosition[0];
        float velocityError = drive.targetVelocity - joint.jointVelocity[0];
        float jointAcceleration = joint.jointAcceleration[0];
        
        // Estimate torque using PD control + inertial effects
        float estimatedTorque = drive.stiffness * positionError + 
                               drive.damping * velocityError + 
                               joint.mass * jointAcceleration; // Simplified inertial term
        
        return estimatedTorque;
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
    private void OnSaludarPressed(InputAction.CallbackContext ctx)
    {
        if (!isGreeting) // Evitar repetir si ya está saludando
        {
            StartCoroutine(StartGreetingRoutine());
        }
    }
    private IEnumerator StartGreetingRoutine()
{
    isGreeting = true;
    Debug.Log("Iniciando rutina de saludo...");

    // Paso 1: detener marcha
    if (controlMode == ControlMode.InverseKinematics)
    {
        d = 0f; al = 0f;
    }
    else if (controlMode == ControlMode.NeuralCircuit)
    {
        go = bk = left = right = spinL = spinR = 0f;
    }

    // Paso 2: mover TODAS las patas a la pose estable
    yield return StartCoroutine(MoveToRestPose(1.5f));

    // Paso 3: esperar en reposo
    yield return new WaitForSeconds(0.5f);

    // Paso 4: mover una pata a la pose de saludo
    int legIndex = 0; 
    var coxaBody = coxas[legIndex].GetComponent<ArticulationBody>();
    var femurBody = femurs[legIndex].GetComponent<ArticulationBody>();
    var tibiaBody = tibias[legIndex].GetComponent<ArticulationBody>();

    // fijar femur y tibia en la pose deseada
    var femurDrive = femurBody.xDrive;
    femurDrive.target = 0f;
    femurBody.xDrive = femurDrive;

    var tibiaDrive = tibiaBody.xDrive;
    tibiaDrive.target = 90f;
    tibiaBody.xDrive = tibiaDrive;

    for (int i = 0; i < 3; i++) // tres saludos
    {
        // Coxa adelante (-10)
        yield return StartCoroutine(MoveJointSmooth(coxaBody, -10f, 0.5f));
        SendAllJointAngles(); // 🔴 Enviar al pipe después del movimiento

        // Coxa atrás (-50)
        yield return StartCoroutine(MoveJointSmooth(coxaBody, -50f, 0.5f));
        SendAllJointAngles(); // 🔴 Enviar al pipe después del movimiento
    }

    // Paso 5: volver a reposo otra vez (por si quedó desajustada)
    yield return StartCoroutine(MoveToRestPose(1f));

    SendAllJointAngles(); // 🔴 Enviar pose final también

    Debug.Log("✅ Saludo completado.");
    isGreeting = false;
}

// Función auxiliar para recolectar y mandar todos los ángulos
private void SendAllJointAngles()
{
    List<float> jointAngles = new List<float>();

    for (int i = 0; i < 6; i++)
    {
        var coxaBody = coxas[i].GetComponent<ArticulationBody>();
        jointAngles.Add(coxaBody.xDrive.target);

        var femurBody = femurs[i].GetComponent<ArticulationBody>();
        jointAngles.Add(femurBody.xDrive.target);

        var tibiaBody = tibias[i].GetComponent<ArticulationBody>();
        jointAngles.Add(tibiaBody.xDrive.target);
    }

    Debug.Log("📡 Enviando ángulos: " + string.Join(",", jointAngles)); // 👈 verificar en consola Unity

    FindObjectOfType<PipeClient>()?.SendJointAngles(jointAngles);
}


    private IEnumerator MoveJointSmooth(ArticulationBody joint, float targetAngle, float duration)
    {
        var drive = joint.xDrive;
        float start = drive.target;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            drive.target = Mathf.Lerp(start, targetAngle, t);
            joint.xDrive = drive;
            yield return null;
        }

        // asegurar que termine exactamente en target
        drive.target = targetAngle;
        joint.xDrive = drive;
    }

    private IEnumerator MoveToRestPose(float duration = 1.5f)
    {
        float elapsed = 0f;

        // Guardar poses iniciales
        float[] startCoxa = new float[6];
        float[] startFemur = new float[6];
        float[] startTibia = new float[6];

        

        for (int i = 0; i < 6; i++)
        {
            startCoxa[i] = coxas[i].GetComponent<ArticulationBody>().xDrive.target;
            
            startFemur[i] = femurs[i].GetComponent<ArticulationBody>().xDrive.target;
            
            startTibia[i] = tibias[i].GetComponent<ArticulationBody>().xDrive.target;
            
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            for (int i = 0; i < 6; i++)
            {
                var coxaDrive = coxas[i].GetComponent<ArticulationBody>().xDrive;
                coxaDrive.target = Mathf.Lerp(startCoxa[i], restCoxa[i], t);
                coxas[i].GetComponent<ArticulationBody>().xDrive = coxaDrive;
                
                var femurDrive = femurs[i].GetComponent<ArticulationBody>().xDrive;
                femurDrive.target = Mathf.Lerp(startFemur[i], restFemur[i], t);
                femurs[i].GetComponent<ArticulationBody>().xDrive = femurDrive;
                
                var tibiaDrive = tibias[i].GetComponent<ArticulationBody>().xDrive;
                tibiaDrive.target = Mathf.Lerp(startTibia[i], restTibia[i], t);
                tibias[i].GetComponent<ArticulationBody>().xDrive = tibiaDrive;
                
            }
            


            yield return null;
        }
    }
    

    private bool HexapodIsMoving()
    {
        return moveInput.magnitude > 0.1f || girandoDerecha || girandoIzquierda;
    }


    /// /////////////////////////
    float CalculateLateralDeviation()
    {
        if (positionHistory.Count < 2) return 0f;

        Vector3 start = positionHistory[0];
        Vector3 end = positionHistory[^1];
        Vector3 direction = (end - start).normalized;

        float sum = 0f;
        foreach (var point in positionHistory)
        {
            Vector3 projected = Vector3.Project(point - start, direction) + start;
            float lateralDeviation = Vector3.Distance(projected, point);

            sum += lateralDeviation;
        }

        return sum / positionHistory.Count;
    }
    ////////////////////////////////


    ////////////////////////////////
    void ExportDataToCSV()
    {
        Debug.Log("Exporting data to CSV");
        string path = Application.dataPath + "/HexapodMetrics.csv";
        StringBuilder csvContent = new StringBuilder();

        // Encabezado con torques de las 18 articulaciones
        string header = "Time;PosX;PosY;PosZ;VelX;VelY;VelZ;AngVelX;AngVelY;AngVelZ;LateralDeviation;Distance";
        
        // Agregar encabezados para los torques de las 18 articulaciones
        for (int leg = 0; leg < 6; leg++)
        {
            header += $";Leg{leg}_Coxa_Torque;Leg{leg}_Femur_Torque;Leg{leg}_Tibia_Torque";
        }
        
        csvContent.AppendLine(header);

        int count = Mathf.Min(positionHistory.Count, velocityHistory.Count, angularVelocityHistory.Count, jointTorquesHistory.Count);

        for (int i = 0; i < count; i++)
        {
            float t = i * Time.fixedDeltaTime;
            Vector3 pos = positionHistory[i];
            Vector3 vel = velocityHistory[i];
            Vector3 angVel = angularVelocityHistory[i];
            float deviation = CalculateLateralDeviation();
            
            string line = $"{t:F2};{pos.x:F3};{pos.y:F3};{pos.z:F3};{vel.x:F3};{vel.y:F3};{vel.z:F3};{angVel.x:F3};{angVel.y:F3};{angVel.z:F3};{deviation:F3};{totalDistance:F3}";
            
            // Agregar datos de torques
            if (i < jointTorquesHistory.Count)
            {
                float[] torques = jointTorquesHistory[i];
                for (int j = 0; j < 18; j++)
                {
                    line += $";{torques[j]:F3}";
                }
            }
            
            csvContent.AppendLine(line);
        }

        File.WriteAllText(path, csvContent.ToString());
        Debug.Log("Datos exportados a: " + path);
    }
    /////////////////////////////////
    

}
