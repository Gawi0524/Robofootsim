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

    [SerializeField] private float wheelRadius = 0.021f;          // 21 мм
    [SerializeField] private float wheelBase = 0.05f;             // 50 мм

    [Header("Motion limits")]
    [SerializeField] private float maxWheelRpm = 295f;            // об/мин
    [SerializeField] private float maxLinearSpeed = 0.65f;        // м/с
    [SerializeField] private float maxBodyAngularVelocity = 31f;  // рад/с

    [Header("Motor")]
    [SerializeField] private float maxTorquePerWheel = 0.15f;     // Н*м

    [Tooltip("Сила разгона к целевой скорости. Больше значение — резче разгон.")]
    [SerializeField] private float accelerationKp = 0.035f;

    [Tooltip("Сила торможения к целевой скорости. Больше значение — резче торможение.")]
    [SerializeField] private float brakingKp = 0.055f;

    [Tooltip("Торможение, когда стик почти отпущен.")]
    [SerializeField] private float idleBrakeKp = 0.06f;

    [Header("Joystick input")]
    [SerializeField] private string leftStickVerticalAxis = "LeftStickVertical";
    [SerializeField] private string rightStickVerticalAxis = "RightStickVertical";

    [SerializeField] private float joystickDeadZone = 0.12f;

    [Tooltip("Кривая чувствительности стика. 1 — линейно, 2 — мягче около центра.")]
    [SerializeField] private float inputCurvePower = 1.4f;

    [Header("Input smoothing")]
    [Tooltip("Скорость изменения команды при разгоне.")]
    [SerializeField] private float inputAcceleration = 4.5f;

    [Tooltip("Скорость изменения команды при торможении или отпускании стика.")]
    [SerializeField] private float inputDeceleration = 7.5f;

    [Header("Wheel axis")]
    [Tooltip("Локальная ось вращения колеса. Обычно Vector3.right, Vector3.up или Vector3.forward.")]
    [SerializeField] private Vector3 localWheelTorqueAxis = Vector3.right;

    [Header("Invert")]
    [SerializeField] private bool invertLeftWheel = false;
    [SerializeField] private bool invertRightWheel = false;

    [Header("Stability")]
    [SerializeField] private bool lowerCenterOfMass = true;
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.015f, 0f);
    [SerializeField] private float bodyDrag = 0.2f;
    [SerializeField] private float bodyAngularDrag = 4f;
    [SerializeField] private float wheelDrag = 0.03f;
    [SerializeField] private float wheelAngularDrag = 0.04f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private float maxWheelAngularVelocity;

    private float leftInputSmoothed;
    private float rightInputSmoothed;

    private void Awake()
    {
        if (bodyRb == null)
            bodyRb = GetComponent<Rigidbody>();

        maxWheelAngularVelocity = maxWheelRpm * 2f * Mathf.PI / 60f;

        SetupBody();
        SetupWheel(leftWheelRb);
        SetupWheel(rightWheelRb);

        if (localWheelTorqueAxis.sqrMagnitude < 0.0001f)
            localWheelTorqueAxis = Vector3.right;

        localWheelTorqueAxis.Normalize();
    }

    private void FixedUpdate()
    {
        float leftRawInput = Input.GetAxis(leftStickVerticalAxis);
        float rightRawInput = Input.GetAxis(rightStickVerticalAxis);

        float leftTargetInput = ProcessJoystickInput(leftRawInput);
        float rightTargetInput = ProcessJoystickInput(rightRawInput);

        if (invertLeftWheel)
            leftTargetInput *= -1f;

        if (invertRightWheel)
            rightTargetInput *= -1f;

        leftInputSmoothed = SmoothInput(leftInputSmoothed, leftTargetInput);
        rightInputSmoothed = SmoothInput(rightInputSmoothed, rightTargetInput);


        ApplyWheelControl(leftWheelRb, leftInputSmoothed);
        ApplyWheelControl(rightWheelRb, rightInputSmoothed);

        LimitBodyLinearSpeed();

        if (showDebugInfo)
            PrintDebugInfo();
    }

    private void SetupBody()
    {
        if (bodyRb == null)
            return;

        bodyRb.mass = bodyMass;
        bodyRb.drag = bodyDrag;
        bodyRb.angularDrag = bodyAngularDrag;
        bodyRb.maxAngularVelocity = maxBodyAngularVelocity;

        bodyRb.interpolation = RigidbodyInterpolation.Interpolate;
        bodyRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (lowerCenterOfMass)
            bodyRb.centerOfMass = centerOfMassOffset;
    }

    private void SetupWheel(Rigidbody wheelRb)
    {
        if (wheelRb == null)
            return;

        wheelRb.mass = wheelMass;
        wheelRb.drag = wheelDrag;
        wheelRb.angularDrag = wheelAngularDrag;
        wheelRb.maxAngularVelocity = maxWheelAngularVelocity * 1.2f;

        wheelRb.interpolation = RigidbodyInterpolation.Interpolate;
        wheelRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private float ProcessJoystickInput(float rawInput)
    {
        rawInput = Mathf.Clamp(rawInput, -1f, 1f);

        if (Mathf.Abs(rawInput) < joystickDeadZone)
            return 0f;

        float sign = Mathf.Sign(rawInput);
        float magnitude = Mathf.Abs(rawInput);

        magnitude = Mathf.InverseLerp(joystickDeadZone, 1f, magnitude);

        magnitude = Mathf.Pow(magnitude, inputCurvePower);

        return sign * magnitude;
    }

    private float SmoothInput(float current, float target)
    {
        float rate;

        bool movingAwayFromZero = Mathf.Abs(target) > Mathf.Abs(current);
        bool changingDirection = Mathf.Sign(current) != Mathf.Sign(target) && Mathf.Abs(current) > 0.01f && Mathf.Abs(target) > 0.01f;

        if (changingDirection)
            rate = inputDeceleration;
        else if (movingAwayFromZero)
            rate = inputAcceleration;
        else
            rate = inputDeceleration;

        return Mathf.MoveTowards(current, target, rate * Time.fixedDeltaTime);
    }

    private void ApplyWheelControl(Rigidbody wheelRb, float input)
    {
        if (wheelRb == null)
            return;

        Vector3 worldAxis = wheelRb.transform.TransformDirection(localWheelTorqueAxis).normalized;

        float currentAngularVelocity = Vector3.Dot(wheelRb.angularVelocity, worldAxis);
        float targetAngularVelocity = input * maxWheelAngularVelocity;

        float speedError = targetAngularVelocity - currentAngularVelocity;

        float kp;

        if (Mathf.Abs(input) < 0.01f)
        {
            kp = idleBrakeKp;
        }
        else
        {
            bool wheelMovingSameDirectionAsTarget =
                Mathf.Sign(currentAngularVelocity) == Mathf.Sign(targetAngularVelocity);

            bool needToSlowDown =
                Mathf.Abs(currentAngularVelocity) > Mathf.Abs(targetAngularVelocity);

            if (!wheelMovingSameDirectionAsTarget || needToSlowDown)
                kp = brakingKp;
            else
                kp = accelerationKp;
        }

        float torque = speedError * kp;
        torque = Mathf.Clamp(torque, -maxTorquePerWheel, maxTorquePerWheel);

        wheelRb.AddTorque(worldAxis * torque, ForceMode.Force);
    }

    private void LimitBodyLinearSpeed()
    {
        if (bodyRb == null)
            return;

        Vector3 velocity = bodyRb.velocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (horizontalVelocity.magnitude > maxLinearSpeed)
        {
            Vector3 limitedHorizontalVelocity = horizontalVelocity.normalized * maxLinearSpeed;

            bodyRb.velocity = new Vector3(
                limitedHorizontalVelocity.x,
                velocity.y,
                limitedHorizontalVelocity.z
            );


        }
    }

    public float GetLeftWheelRpm()
    {
        return GetWheelRpm(leftWheelRb);
    }

    public float GetRightWheelRpm()
    {
        return GetWheelRpm(rightWheelRb);
    }

    private float GetWheelRpm(Rigidbody wheelRb)
    {
        if (wheelRb == null)
            return 0f;

        Vector3 worldAxis = wheelRb.transform.TransformDirection(localWheelTorqueAxis).normalized;
        float angularVelocity = Vector3.Dot(wheelRb.angularVelocity, worldAxis);

        return angularVelocity * 60f / (2f * Mathf.PI);
    }

    public float GetEstimatedLinearSpeed()
    {
        if (leftWheelRb == null || rightWheelRb == null)
            return 0f;

        float leftOmega = GetWheelAngularVelocity(leftWheelRb);
        float rightOmega = GetWheelAngularVelocity(rightWheelRb);

        return wheelRadius * (leftOmega + rightOmega) * 0.5f;
    }

    public float GetEstimatedAngularSpeed()
    {
        if (leftWheelRb == null || rightWheelRb == null)
            return 0f;

        float leftOmega = GetWheelAngularVelocity(leftWheelRb);
        float rightOmega = GetWheelAngularVelocity(rightWheelRb);

        return wheelRadius * (rightOmega - leftOmega) / wheelBase;
    }

    private float GetWheelAngularVelocity(Rigidbody wheelRb)
    {
        if (wheelRb == null)
            return 0f;

        Vector3 worldAxis = wheelRb.transform.TransformDirection(localWheelTorqueAxis).normalized;
        return Vector3.Dot(wheelRb.angularVelocity, worldAxis);
    }

    private void PrintDebugInfo()
    {
        Debug.Log(
            "Left input: " + leftInputSmoothed.ToString("F2") +
            " | Right input: " + rightInputSmoothed.ToString("F2") +
            " | Left RPM: " + GetLeftWheelRpm().ToString("F0") +
            " | Right RPM: " + GetRightWheelRpm().ToString("F0") +
            " | Estimated speed: " + GetEstimatedLinearSpeed().ToString("F2") + " m/s"
        );
    }
}