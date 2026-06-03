using UnityEngine;

public class DiffDriveTorque : MonoBehaviour
{
    [Header("Wheels")]
    public Rigidbody leftWheel;
    public Rigidbody rightWheel;

    [Header("Tuning")]
    public float stallTorque = 0.021f;   // Nm
    public float maxRPM = 2500f;
    public float wheelRadius = 0.015f;

    [Header("Input smoothing")]
    public float response = 10f;

    private float leftInput;
    private float rightInput;

    void Update()
    {
        float v = Input.GetAxis("Vertical");   // forward/back
        float t = Input.GetAxis("Horizontal"); // turn

        float targetLeft = v + t;
        float targetRight = v - t;

        leftInput = Mathf.Lerp(leftInput, targetLeft, Time.deltaTime * response);
        rightInput = Mathf.Lerp(rightInput, targetRight, Time.deltaTime * response);
    }

    void FixedUpdate()
    {
        ApplyMotor(leftWheel, leftInput);
        ApplyMotor(rightWheel, rightInput);
    }

    void ApplyMotor(Rigidbody wheel, float input)
    {
        // ограничение входа
        input = Mathf.Clamp(input, -1f, 1f);

        // текущая угловая скорость
        float rpm = wheel.angularVelocity.magnitude * 9.55f;

        // если колесо уже близко к max RPM — уменьшаем момент
        float speedFactor = 1f - Mathf.InverseLerp(maxRPM, maxRPM * 1.2f, rpm);

        // итоговый torque
        float torque = input * stallTorque * speedFactor;

        // направление оси колеса (X как ты сказал)
        Vector3 axis = wheel.transform.right;

        wheel.AddTorque(axis * torque, ForceMode.Force);
    }
}