using UnityEngine;
using TMPro;

public class RobotTelemetryUI : MonoBehaviour
{
    [Header("Робот")]
    public Rigidbody body;
    public Transform blade;

    [Header("UI")]
    public TextMeshProUGUI text;

    [Header("Геометрия корпуса (м)")]
    public float bodyWidth = 0.0542f;
    public float bodyLength = 0.065f;

    [Header("Геометрия отвала (м)")]
    public float bladeWidth = 0.078f;
    public float bladeLength = 0.0346f;

    void Start()
    {
        // 👉 уменьшаем текст глобально
        if (text != null)
        {
            text.fontSize = 14f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
        }
    }

    void Update()
    {
        if (body == null || text == null || blade == null)
            return;

        // ===== ГЛОБАЛЬНЫЕ НАПРАВЛЕНИЯ В МИРЕ =====
        Vector3 right = body.transform.right;
        Vector3 forward = body.transform.forward;

        Vector3 center = body.position;

        float hw = bodyWidth * 0.5f;
        float hl = bodyLength * 0.5f;

        // УГЛЫ КОРПУСА (WORLD SPACE)
        Vector3 b1 = center + right * hw + forward * hl;
        Vector3 b2 = center - right * hw + forward * hl;
        Vector3 b3 = center - right * hw - forward * hl;
        Vector3 b4 = center + right * hw - forward * hl;

        // УГЛЫ ОТВАЛА (WORLD SPACE)
        Vector3 bc = blade.position;

        float bw = bladeWidth * 0.5f;
        float bl = bladeLength * 0.5f;

        Vector3 f1 = bc + right * bw + forward * bl;
        Vector3 f2 = bc - right * bw + forward * bl;
        Vector3 f3 = bc - right * bw - forward * bl;
        Vector3 f4 = bc + right * bw - forward * bl;

        // ДИНАМИКА РОБОТА
        Vector3 v = body.velocity;
        float speed = new Vector3(v.x, 0, v.z).magnitude;

        float angularSpeed = body.angularVelocity.y;

        //  угол в мировых координатах (0 = глобальная ось Z)
        float yawWorld = Mathf.Atan2(body.transform.forward.x, body.transform.forward.z);

        // ===== ВЫВОД В UI =====
        text.text =
            $"=== РОБОТ ===\n\n" +

            $" КОРПУС (4 угла):\n" +
            $"1 {b1}\n" +
            $"2 {b2}\n" +
            $"3 {b3}\n" +
            $"4 {b4}\n\n" +

            $" ОТВАЛ:\n" +
            $"1 {f1}\n" +
            $"2 {f2}\n" +
            $"3 {f3}\n" +
            $"4 {f4}\n\n" +

            $" УГОЛ: {yawWorld:F3} рад\n" +
            $" СКОРОСТЬ: {speed:F3} м/с\n" +
            $" УГЛ. СКОРОСТЬ: {angularSpeed:F3} рад/с";
    }
}