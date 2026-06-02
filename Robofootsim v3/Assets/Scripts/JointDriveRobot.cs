using UnityEngine;

public class JointDriveRobot : MonoBehaviour
{
    public HingeJoint leftWheel;
    public HingeJoint rightWheel;

    public float maxSpeed = 300f;

    void FixedUpdate()
    {
        float v = Input.GetAxis("Vertical");
        float t = Input.GetAxis("Horizontal");

        float left = v + t;
        float right = v - t;

        SetMotor(leftWheel, left);
        SetMotor(rightWheel, right);
    }

    void SetMotor(HingeJoint joint, float input)
    {
        JointMotor motor = joint.motor;
        motor.targetVelocity = input * maxSpeed;
        motor.force = 100;
        joint.motor = motor;
        joint.useMotor = true;
    }
}