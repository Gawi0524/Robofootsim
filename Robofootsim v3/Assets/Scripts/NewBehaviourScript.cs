using UnityEngine;

public class RobotController : MonoBehaviour
{
    // ==========================
    // ТИП УПРАВЛЕНИЯ
    // ==========================
    public enum ControlScheme
    {
        WASD,
        Arrows
    }

    [Header("Управление")]
    [SerializeField] private ControlScheme controlScheme = ControlScheme.WASD;

    // ==========================
    // RIGIDBODIES
    // ==========================
    [Header("Rigidbodies")]
    [SerializeField] private Rigidbody bodyRb;
    [SerializeField] private Rigidbody leftWheelRb;
    [SerializeField] private Rigidbody rightWheelRb;

    // ==========================
    // ФИЗИЧЕСКИЕ ПАРАМЕТРЫ
    // ==========================
    [Header("Robot physical parameters")]
    [SerializeField] private float bodyMass = 0.225f;
    [SerializeField] private float wheelMass = 0.02f;

    [SerializeField] private float wheelRadius = 0.021f; // 21 мм
    [SerializeField] private float wheelBase = 0.05f;

    // ==========================
    // ОГРАНИЧЕНИЯ
    // ==========================
    [Header("Motion limits")]
    [SerializeField] private float maxWheelRpm = 295f;
    [SerializeField] private float maxLinearSpeed = 0.65f;
    [SerializeField] private float maxBodyAngularVelocity = 31f;

    // ==========================
    // ДВИГАТЕЛЬ
    // ==========================
    [Header("Motor")]
    [SerializeField] private float maxTorquePerWheel = 0.15f;

    // ==========================
    // ОСЬ КОЛЕСА
    // ==========================
    [Header("Wheel axis")]
    [SerializeField] private Vector3 localWheelTorqueAxis = Vector3.right;

    // ==========================
    // ИНВЕРСИЯ
    // ==========================
    [Header("Invert")]
    [SerializeField] private bool invertLeft = false;
    [SerializeField] private bool invertRight = false;

    private float maxWheelAngularVelocity;

    private void Awake()
    {
        if (bodyRb == null)
            bodyRb = GetComponent<Rigidbody>();

        maxWheelAngularVelocity =
            maxWheelRpm * 2f * Mathf.PI / 60f;

        SetupBody();
        SetupWheel(leftWheelRb);
        SetupWheel(rightWheelRb);
    }

    private void FixedUpdate()
    {
        float forward = 0f;
        float turn = 0f;

        // ==========================
        // СЧИТЫВАНИЕ УПРАВЛЕНИЯ
        // ==========================
        if (controlScheme == ControlScheme.WASD)
        {
            if (Input.GetKey(KeyCode.W))
                forward += 1f;

            if (Input.GetKey(KeyCode.S))
                forward -= 1f;

            if (Input.GetKey(KeyCode.D))
                turn += 1f;

            if (Input.GetKey(KeyCode.A))
                turn -= 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.UpArrow))
                forward += 1f;

            if (Input.GetKey(KeyCode.DownArrow))
                forward -= 1f;

            if (Input.GetKey(KeyCode.RightArrow))
                turn += 1f;

            if (Input.GetKey(KeyCode.LeftArrow))
                turn -= 1f;
        }

        // ==========================
        // ДИФФЕРЕНЦИАЛЬНЫЙ ПРИВОД
        // ==========================
        float left = forward + turn;
        float right = forward - turn;

        left = Mathf.Clamp(left, -1f, 1f);
        right = Mathf.Clamp(right, -1f, 1f);

        if (invertLeft)
            left *= -1f;

        if (invertRight)
            right *= -1f;

        // ==========================
        // ПРИМЕНЕНИЕ МОМЕНТА
        // ==========================
        ApplyWheel(leftWheelRb, left);
        ApplyWheel(rightWheelRb, right);

        // ==========================
        // ОГРАНИЧЕНИЕ СКОРОСТИ
        // ==========================
        LimitLinearSpeed();
    }

    private void ApplyWheel(Rigidbody wheel, float input)
    {
        if (wheel == null)
            return;

        Vector3 axis =
            wheel.transform.TransformDirection(localWheelTorqueAxis)
            .normalized;

        float currentOmega =
            Vector3.Dot(wheel.angularVelocity, axis);

        float targetOmega =
            input * maxWheelAngularVelocity;

        float error =
            targetOmega - currentOmega;

        float torque =
            error * maxTorquePerWheel;

        torque = Mathf.Clamp(
            torque,
            -maxTorquePerWheel,
            maxTorquePerWheel
        );

        wheel.AddTorque(
            axis * torque,
            ForceMode.Force
        );
    }

    private void LimitLinearSpeed()
    {
        if (bodyRb == null)
            return;

        Vector3 velocity = bodyRb.velocity;

        Vector3 horizontal =
            new Vector3(
                velocity.x,
                0f,
                velocity.z
            );

        if (horizontal.magnitude > maxLinearSpeed)
        {
            Vector3 limited =
                horizontal.normalized * maxLinearSpeed;

            bodyRb.velocity =
                new Vector3(
                    limited.x,
                    velocity.y,
                    limited.z
                );
        }
    }

    private void SetupBody()
    {
        if (bodyRb == null)
            return;

        bodyRb.mass = bodyMass;
        bodyRb.maxAngularVelocity =
            maxBodyAngularVelocity;

        bodyRb.interpolation =
            RigidbodyInterpolation.Interpolate;

        bodyRb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
    }

    private void SetupWheel(Rigidbody wheel)
    {
        if (wheel == null)
            return;

        wheel.mass = wheelMass;

        wheel.maxAngularVelocity =
            maxWheelAngularVelocity;

        wheel.interpolation =
            RigidbodyInterpolation.Interpolate;

        wheel.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
    }
}