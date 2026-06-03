using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankRobotController2022 : MonoBehaviour
{
    [Header("Ссылки на колеса")]
    [Tooltip("Rigidbody левого колеса")]
    public Rigidbody leftWheelRb;
    [Tooltip("Rigidbody правого колеса")]
    public Rigidbody rightWheelRb;

    [Header("Названия осей джойстика в Input Manager")]
    public string leftStickAxisName = "LeftStickY";
    public string rightStickAxisName = "RightStickY";

    [Header("Физические параметры")]
    [Tooltip("Максимальный крутящий момент мотора FAULHABER с редуктором (Н*м)")]
    public float maxMotorTorque = 0.15f;
    [Tooltip("Целевая максимальная скорость робота (м/с) для расчета лимитов")]
    public float targetMaxSpeed = 0.65f;
    [Tooltip("Сила торможения редуктора при отпущенном стике (0 - накат, 1 - резкий стоп)")]
    [Range(0f, 1f)] public float gearBrakePower = 0.2f;

    private float maxWheelAngularVelocity;

    void Start()
    {
        // Вычисляем максимальную угловую скорость колеса (рад/с) исходя из желаемой скорости (0.65 м/с)
        // Формула: w = v / r. Радиус колеса = 0.021м (42мм / 2)
        maxWheelAngularVelocity = targetMaxSpeed / 0.021f;

        // В Unity 2022 настраиваем физический предел вращения Rigidbody колес
        if (leftWheelRb != null) leftWheelRb.maxAngularVelocity = maxWheelAngularVelocity;
        if (rightWheelRb != null) rightWheelRb.maxAngularVelocity = maxWheelAngularVelocity;
    }

    void FixedUpdate()
    {
        if (leftWheelRb == null || rightWheelRb == null) return;

        // Считываем плавные аналоговые значения со стиков геймпада (от -1.0 до 1.0)
        float leftInput = Input.GetAxis(leftStickAxisName);
        float rightInput = Input.GetAxis(rightStickAxisName);

        // Управляем каждым колесом индивидуально через крутящий момент
        ApplyPhysicsTorque(leftWheelRb, leftInput);
        ApplyPhysicsTorque(rightWheelRb, rightInput);
    }

    private void ApplyPhysicsTorque(Rigidbody wheelRb, float inputSignal)
    {
        // Если стик отклонен (есть сигнал на движение дальше мертвой зоны джойстика)
        if (Mathf.Abs(inputSignal) > 0.02f)
        {
            // Получаем текущую скорость вращения колеса
            float currentAngularVel = wheelRb.angularVelocity.magnitude;

            // Плавный коэффициент падения крутящего момента при приближении к максимальной скорости.
            // Имитирует реальную противо-ЭДС электродвигателя: чем быстрее крутится мотор, тем меньше у него момента.
            float torqueModifier = Mathf.Clamp01(1f - (currentAngularVel / maxWheelAngularVelocity));

            // Проверяем: если игрок жмет стик в противоположную сторону от вращения колеса (тормозит/реверсирует),
            // то отдаем полный крутящий момент без ограничений для мгновенного торможения.
            float localRotationDir = wheelRb.transform.InverseTransformDirection(wheelRb.angularVelocity).x;

            if (Mathf.Sign(inputSignal) != Mathf.Sign(localRotationDir))
            {
                torqueModifier = 1f;
            }

            // Прикладываем крутящий момент с учетом плавности стика и ограничителя скорости
            float finalTorque = inputSignal * maxMotorTorque * torqueModifier;
            wheelRb.AddRelativeTorque(Vector3.right * finalTorque, ForceMode.Force);
        }
        else
        {
            // Если стик отпущен, имитируем сопротивление шестеренок реального редуктора (активное торможение)
            wheelRb.angularVelocity = Vector3.Lerp(wheelRb.angularVelocity, Vector3.zero, gearBrakePower);
        }
    }
}
