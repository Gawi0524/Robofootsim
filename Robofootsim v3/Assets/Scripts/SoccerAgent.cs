using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class SoccerAgent : Agent
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private RobotController robotController; // Ссылка на наш физический контроллер моторов
    [SerializeField] private Rigidbody ballRb;                // Ссылка на Rigidbody теннисного мяча

    [Header("Параметры поля для нормализации")]
    private const float MaxFieldX = 0.54f; // От центра до бокового бортика
    private const float MaxFieldZ = 0.96f; // От центра до ворот

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialBallPosition;

    /// <summary>
    /// Вызывается один раз при старте. Запоминаем начальные позиции для сброса раунда.
    /// </summary>
    public override void Initialize()
    {
        if (robotController == null)
            robotController = GetComponent<RobotController>();

        initialPosition = robotController.transform.position;
        initialRotation = robotController.transform.rotation;

        if (ballRb != null)
            initialBallPosition = ballRb.transform.position;
    }

    /// <summary>
    /// Ступень 1: Сброс сцены в начале каждого нового раунда
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Возвращаем робота на базу и сбрасываем его физические скорости
        Rigidbody robotRb = robotController.GetComponent<Rigidbody>();
        if (robotRb != null)
        {
            robotRb.velocity = Vector3.zero;
            robotRb.angularVelocity = Vector3.zero;
        }
        robotController.transform.position = initialPosition;
        robotController.transform.rotation = initialRotation;

        // Сбрасываем мяч и даем ему легкий случайный начальный сдвиг, чтобы ИИ не заучивал одну траекторию
        if (ballRb != null)
        {
            ballRb.velocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.transform.position = initialBallPosition + new Vector3(Random.Range(-0.1f, 0.1f), 0f, Random.Range(-0.2f, 0.2f));
        }
    }

    /// <summary>
    /// Шаг 2: Сбор 8 нормализованных чисел контракта данных для нейросети
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 robotPos = robotController.transform.position;
        Vector3 robotForward = robotController.transform.forward;

        // 1, 2: Нормализованные координаты робота на поле [-1, 1]
        sensor.AddObservation(robotPos.x / MaxFieldX);
        sensor.AddObservation(robotPos.z / MaxFieldZ);

        // 3, 4: Направление носа робота (Синус и Косинус вектора Вперед)
        sensor.AddObservation(robotForward.x);
        sensor.AddObservation(robotForward.z);

        if (ballRb != null)
        {
            Vector3 ballPos = ballRb.transform.position;
            Vector3 ballVel = ballRb.velocity;

            // 5, 6: Нормализованные координаты мяча [-1, 1]
            sensor.AddObservation(ballPos.x / MaxFieldX);
            sensor.AddObservation(ballPos.z / MaxFieldZ);

            // 7, 8: Скорость мяча (сырая, в м/с)
            sensor.AddObservation(ballVel.x);
            sensor.AddObservation(ballVel.z);
        }
        else
        {
            // Заглушка, если мяч забыли прикрепить в инспекторе
            sensor.AddObservation(new float[4]);
        }
    }

    /// <summary>
    /// Применение команд от ИИ и расчет Кнутов и Пряников (Наград)
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        // Принимаем от нейросети два непрерывных числа в диапазоне [-1, 1]
        float leftMotorSignal = actions.ContinuousActions[0];
        float rightMotorSignal = actions.ContinuousActions[1];

        // Отправляем их в наш честный физический контроллер моторов
        robotController.SetNeuralInput(leftMotorSignal, rightMotorSignal);

        // --- РАСЧЕТ НАГРАД (ДРЕССИРОВКА) ---
        if (ballRb != null)
        {
            // Измеряем текущую дистанцию от центра робота до мяча
            float distanceToBall = Vector3.Distance(robotController.transform.position, ballRb.transform.position);

            // Мяч совсем близко (ИИ коснулся его или подобрался вплотную) — выдаем промежуточный пряник!
            if (distanceToBall < 0.08f)
            {
                AddReward(0.1f);
                // Для Ступени 1 ("просто доехать") этого достаточно, завершаем раунд победой
                EndEpisode();
            }

            // Кнут за вылет мяча за пределы игровой зоны (если бортик пробил)
            if (Mathf.Abs(ballRb.transform.position.x) > MaxFieldX + 0.1f || Mathf.Abs(ballRb.transform.position.z) > MaxFieldZ + 0.1f)
            {
                SetReward(-1f);
                EndEpisode();
            }
        }
    }

    /// <summary>
    /// Эмуляция ручного ввода. Позволяет тестировать Агента кнопками, имитируя ответы нейросети
    /// </summary>

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

        float forward = 0f;
        float turn = 0f;

        if (Input.GetKey(KeyCode.W)) forward += 1f;
        if (Input.GetKey(KeyCode.S)) forward -= 1f;
        if (Input.GetKey(KeyCode.A)) turn -= 1f; // Поворот влево
        if (Input.GetKey(KeyCode.D)) turn += 1f;  // Поворот вправо

        // ИСПРАВЛЕНО: Четко распределяем по индексам 0 и 1
        continuousActions[0] = Mathf.Clamp(forward + turn, -1f, 1f); // Левое колесо
        continuousActions[1] = Mathf.Clamp(forward - turn, -1f, 1f); // Правое колесо
    }
}