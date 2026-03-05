using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BusController : MonoBehaviour
{
    [Header("Rodas Visuais")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Modelo Visual")]
    public Transform visualModel;

    [Header("Motor")]
    public float acceleration = 5f;
    public float brakeForce = 10f;
    public float maxSpeedKmh = 100f;

    [Header("Direção Real")]
    public float wheelBase = 6f;           // Distância eixo dianteiro/traseiro
    public float maxSteerAngle = 30f;
    public float steeringSpeed = 60f;

    [Header("Visual Lean")]
    public float maxLeanAngle = 2f;
    public float leanSmooth = 3f;

    private Rigidbody rb;

    private float maxSpeedMS;
    private float verticalInput;
    private float horizontalInput;

    private float currentSteerAngle;
    private float currentLean;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 6000f;
        rb.drag = 0.5f;
        rb.angularDrag = 2f;
        rb.centerOfMass = new Vector3(0, -1.2f, 0);
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        maxSpeedMS = maxSpeedKmh / 3.6f;
    }

    void Update()
    {
        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");

        AnimateWheels();
        ApplyVisualLean();
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyRealSteering();
    }

    // ===========================
    // MOVIMENTO
    // ===========================
    void ApplyMovement()
	{
		Vector3 forward = transform.forward;
		float forwardSpeed = Vector3.Dot(rb.velocity, forward);

		if (verticalInput > 0 && forwardSpeed < maxSpeedMS)
		{
			rb.AddForce(forward * acceleration, ForceMode.Acceleration);
		}
		else if (verticalInput < 0)
		{
			if (forwardSpeed > 1f)
			{
				rb.AddForce(-forward * brakeForce, ForceMode.Acceleration);
			}
			else
			{
				rb.AddForce(-forward * acceleration, ForceMode.Acceleration);
			}
		}
	}

    // ===========================
    // CURVA COM RAIO REAL
    // ===========================
    void ApplyRealSteering()
    {
        float speed = rb.velocity.magnitude;

        if (speed < 0.5f) return;

        float targetSteer = horizontalInput * maxSteerAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteer, steeringSpeed * Time.fixedDeltaTime);

        float turnRadius = wheelBase / Mathf.Tan(currentSteerAngle * Mathf.Deg2Rad);

        float angularVelocity = speed / turnRadius;

        if (float.IsNaN(angularVelocity) || float.IsInfinity(angularVelocity))
            return;

        float rotationAmount = angularVelocity * Mathf.Rad2Deg * Time.fixedDeltaTime;

        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotationAmount, 0f));
    }

    // ===========================
    // VISUAL
    // ===========================
    void ApplyVisualLean()
    {
        float targetLean = -horizontalInput * maxLeanAngle;
        currentLean = Mathf.Lerp(currentLean, targetLean, leanSmooth * Time.deltaTime);

        visualModel.localRotation = Quaternion.Euler(0f, 0f, currentLean);
    }

    void AnimateWheels()
    {
        float wheelSpin = rb.velocity.magnitude * 8f;

        RotateWheel(rearLeftMesh, wheelSpin);
        RotateWheel(rearRightMesh, wheelSpin);
        RotateWheel(frontLeftMesh, wheelSpin);
        RotateWheel(frontRightMesh, wheelSpin);

        frontLeftMesh.localRotation = Quaternion.Euler(
            frontLeftMesh.localEulerAngles.x,
            currentSteerAngle,
            0f);

        frontRightMesh.localRotation = Quaternion.Euler(
            frontRightMesh.localEulerAngles.x,
            currentSteerAngle,
            0f);
    }

    void RotateWheel(Transform wheel, float speed)
    {
        wheel.Rotate(Vector3.right, speed * Time.deltaTime);
    }
}