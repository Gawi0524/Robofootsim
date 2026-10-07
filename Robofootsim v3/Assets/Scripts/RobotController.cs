using UnityEngine;

public class RobotController : MonoBehaviour
{
    public enum ControlMode
    {
        Manual,      // Ручной режим (WASD/Стрелки) для тебя
        NeuralNetwork // Режим для нейросети (ML-Agents / Python)
    }

    public enum ControlScheme { WASD, Arrows }

    [Header("Режим работы")]
    [SerializeField] private ControlMode currentMode = ControlMode.Manual;
    [SerializeField] private ControlScheme controlScheme = ControlScheme.WASD;

    [Header("Rigidbodies")]
    [SerializeField] private Rigidbody bodyRb;
    [SerializeField] private Rigidbody leftWheelRb;
    [SerializeField] private Rigidbody rightWheelRb;

    [Header("Robot physical parameters")]
    [SerializeField] private float bodyMass = 0.225f;
    [SerializeField] private float wheelMass = 0.02f;
    [SerializeField] private float maxBodyAngularVelocity = 31f;

    [Header("Motor (Datasheet)")]
    [SerializeField] private float maxWheelRpm = 295f;
    [SerializeField] private float maxTorquePerWheel = 0.15f;
    [Range(0.1f, 20f)][SerializeField] private float pGain = 5f; // Коэффициент P-регулятора для жесткости моторов

    [Header("Wheel axis & Invert")]
    [SerializeField] private Vector3 localWheelTorqueAxis = Vector3.right;
    [SerializeField] private bool invertLeft = false;
    [SerializeField] private bool invertRight = false;

    private float maxWheelAngularVelocity;

    // Входные сигналы для моторов (значения от -1.0f до 1.0f)
    // Сюда будут писать ИЛИ клавиатура, ИЛИ нейросеть
    private float leftMotorInput = 0f;
    private float rightMotorInput = 0f;

    private void Awake()
    {
        if (bodyRb == null) bodyRb = GetComponent<Rigidbody>();

        // Перевод RPM в Радианы в секунду (как в даташите)
        maxWheelAngularVelocity = maxWheelRpm * 2f * Mathf.PI / 60f;

        SetupBody();
        SetupWheel(leftWheelRb);
        SetupWheel(rightWheelRb);
    }

    private void Update()
    {
        // Считываем клавиатуру ТОЛЬКО если включен ручной режим
        if (currentMode == ControlMode.Manual)
        {
            ReadKeyboardInput();
        }
    }

    private void FixedUpdate()
    {
        // Применяем физику на основе текущих leftMotorInput и rightMotorInput
        ApplyWheelPhysics(leftWheelRb, leftMotorInput, invertLeft);
        ApplyWheelPhysics(rightWheelRb, rightMotorInput, invertRight);
    }

    /// <summary>
    /// Этот метод вызывается снаружи (из класса Agent в ML-Agents), когда ИИ принимает решение
    /// </summary>
    public void SetNeuralInput(float leftSignals, float rightSignals)
    {
        if (currentMode != ControlMode.NeuralNetwork) return;

        leftMotorInput = Mathf.Clamp(leftSignals, -1f, 1f);
        rightMotorInput = Mathf.Clamp(rightSignals, -1f, 1f);
    }

    private void ReadKeyboardInput()
    {
        float forward = 0f;
        float turn = 0f;

        if (controlScheme == ControlScheme.WASD)
        {
            if (Input.GetKey(KeyCode.W)) forward += 1f;
            if (Input.GetKey(KeyCode.S)) forward -= 1f;
            if (Input.GetKey(KeyCode.D)) turn += 1f;
            if (Input.GetKey(KeyCode.A)) turn -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow)) forward += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) forward -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) turn += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;
        }

        // Дифференциальная кинематика коробочки
        leftMotorInput = Mathf.Clamp(forward + turn, -1f, 1f);
        rightMotorInput = Mathf.Clamp(forward - turn, -1f, 1f);
    }

    private void ApplyWheelPhysics(Rigidbody wheel, float input, bool isInverted)
    {
        if (wheel == null) return;

        if (isInverted) input *= -1f;

        // Мировое направление оси вращения колеса
        Vector3 axis = wheel.transform.TransformDirection(localWheelTorqueAxis).normalized;

        // Проекция текущей угловой скорости на ось вращения
        float currentOmega = Vector3.Dot(wheel.angularVelocity, axis);

        // Расчет целевой угловой скорости (Рад/с)
        float targetOmega = input * maxWheelAngularVelocity;

        // P-Регулятор (Пропорциональный контур скорости)
        float error = targetOmega - currentOmega;
        float torque = error * pGain * maxTorquePerWheel;

        // Ограничение момента жестко по даташиту мотора
        torque = Mathf.Clamp(torque, -maxTorquePerWheel, maxTorquePerWheel);

        wheel.AddTorque(axis * torque, ForceMode.Force);
    }

    private void SetupBody()
    {
        if (bodyRb == null) return;
        bodyRb.mass = bodyMass;
        bodyRb.maxAngularVelocity = maxBodyAngularVelocity;
        bodyRb.interpolation = RigidbodyInterpolation.Interpolate;
        bodyRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void SetupWheel(Rigidbody wheel)
    {
        if (wheel == null) return;
        wheel.mass = wheelMass;
        wheel.maxAngularVelocity = maxWheelAngularVelocity;
        wheel.interpolation = RigidbodyInterpolation.Interpolate;
        wheel.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}
