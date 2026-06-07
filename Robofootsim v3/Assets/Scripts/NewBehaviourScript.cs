using UnityEngine;

public class RobotJoystickTorqueController : MonoBehaviour
{
    [Header("Rigidbodies")]
    [SerializeField] private Rigidbody bodyRb;
    [SerializeField] private Rigidbody leftWheelRb;
    [SerializeField] private Rigidbody rightWheelRb;

    [Header("Robot physical parameters")]
    [SerializeField] private float bodyMass = 0.225f;
    [SerializeField] private float wheelMass = 0.02f;

    [SerializeField] private float wheelRadius = 0.021f;
    [SerializeField] private float wheelBase = 0.05f;

    [Header("Motion limits")]
    [SerializeField] private float maxWheelRpm = 295f;
    [SerializeField] private float maxLinearSpeed = 0.65f;
    [SerializeField] private float maxBodyAngularVelocity = 31f;

    [Header("Motor")]
    [SerializeField] private float maxTorquePerWheel = 0.15f;

    [Header("Wheel axis")]
    [SerializeField] private Vector3 localWheelTorqueAxis = Vector3.right;

    [Header("Invert (на случай зеркала модели)")]
    [SerializeField] private bool invertLeft = false;
    [SerializeField] private bool invertRight = false;

    private float maxWheelAngularVelocity;

    private void Awake()
    {
        if (bodyRb == null)
            bodyRb = GetComponent<Rigidbody>();

        maxWheelAngularVelocity = maxWheelRpm * 2f * Mathf.PI / 60f;

        SetupBody();
        SetupWheel(leftWheelRb);
        SetupWheel(rightWheelRb);
    }

    private void FixedUpdate()
    {
        // ======================
        // 1. WASD INPUT
        // ======================
        float forward = 0f;
        float turn = 0f;

        if (Input.GetKey(KeyCode.W)) forward += 1f;
        if (Input.GetKey(KeyCode.S)) forward -= 1f;

        if (Input.GetKey(KeyCode.D)) turn += 1f;
        if (Input.GetKey(KeyCode.A)) turn -= 1f;

        // ======================
        // 2. DIFF DRIVE MODEL
        // ======================
        float left = forward + turn;
        float right = forward - turn;

        left = Mathf.Clamp(left, -1f, 1f);
        right = Mathf.Clamp(right, -1f, 1f);

        if (invertLeft) left *= -1f;
        if (invertRight) right *= -1f;

        // ======================
        // 3. APPLY TORQUE
        // ======================
        ApplyWheel(leftWheelRb, left);
        ApplyWheel(rightWheelRb, right);

        // ======================
        // 4. LIMIT SPEED
        // ======================
        LimitLinearSpeed();
    }

    private void ApplyWheel(Rigidbody wheel, float input)
    {
        if (wheel == null) return;

        Vector3 axis = wheel.transform.right;

        float currentOmega = Vector3.Dot(wheel.angularVelocity, axis);
        float targetOmega = input * maxWheelAngularVelocity;

        float error = targetOmega - currentOmega;

        float torque = error * maxTorquePerWheel;
        torque = Mathf.Clamp(torque, -maxTorquePerWheel, maxTorquePerWheel);

        wheel.AddTorque(axis * torque, ForceMode.Force);
    }

    private void LimitLinearSpeed()
    {
        if (bodyRb == null) return;

        Vector3 v = bodyRb.velocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);

        if (flat.magnitude > maxLinearSpeed)
        {
            Vector3 limited = flat.normalized * maxLinearSpeed;
            bodyRb.velocity = new Vector3(limited.x, v.y, limited.z);
        }
    }

    private void SetupBody()
    {
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