using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Rodas Visuais")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Sistema de Ré")]
    public bool reverseMode = false;

    [Header("Movimento Pesado")]
    public float acceleration = 8f;
    public float maxSpeed = 22f;
    public float turnSpeed = 45f;
    public float brakeDrag = 3f;
    public float steeringVisualAngle = 30f;

    [Header("Balanço Visual")]
    public Transform visualModel;
    public float maxLeanAngle = 2f;
    public float leanSmoothSpeed = 5f;

    public float maxBrakeDip = 3f;     // Inclinação frontal ao frear
    public float dipSmoothSpeed = 5f;

    private float currentLean;
    private float currentDip;

    [Header("Resistência Natural")]
    public float rollingResistance = 0.5f;
    public float airResistance = 0.02f;

    [Header("Velocidade Máxima Real")]
    public float maxSpeedKmh = 146f;
    private float maxSpeedMS;

    [Header("Marchas Automáticas")]
    public int totalGears = 6;
    public float[] gearSpeeds;  // limite de cada marcha
    private int currentGear = 1;

    private Rigidbody rb;

    private bool engineOn = false;
    private bool reverseGear = false;

    private float verticalInput;
    private float horizontalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 4000f;
        rb.drag = 1.5f;
        rb.angularDrag = 5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Centro de massa baixo = sensação pesada
        rb.centerOfMass = new Vector3(0, -1f, 0);

        maxSpeedMS = maxSpeedKmh / 3.6f;

        gearSpeeds = new float[]
        {
            0f,
            20f,
            40f,
            65f,
            90f,
            120f,
            146f
        };
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            engineOn = !engineOn;

        if (Input.GetKeyDown(KeyCode.R))
            reverseGear = !reverseGear;

        // ===== INPUT MANUAL CONTROLADO =====

        horizontalInput = Input.GetAxis("Horizontal");

        verticalInput = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            verticalInput = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            if (reverseGear)
                verticalInput = -1f; // Só dá ré se R estiver ativado
            else
                verticalInput = 0f;  // Só freia
        }

        UpdateAutomaticGears();
        AnimateWheels();
        ApplyVisualEffects();
    }

    void FixedUpdate()
    {
        if (!engineOn)
            return;

        if (Input.GetKey(KeyCode.S) && !reverseGear)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, 4f * Time.fixedDeltaTime);
        }

        ApplyMovement();
        ApplySteering();
        ApplyRealisticDrag();
        KillSidewaysVelocity();
    }

    void ApplyMovement()
    {
        if (verticalInput == 0f)
            return;

        Vector3 forward = transform.forward;

        float forwardSpeed = Vector3.Dot(rb.velocity, forward);

        if (Mathf.Abs(forwardSpeed) >= maxSpeedMS)
            return;

        // Quanto maior a marcha, menor o torque
        float gearMultiplier = 1f - (currentGear * 0.1f);

        rb.AddForce(forward * verticalInput * acceleration * gearMultiplier, ForceMode.Acceleration);
    }

    void UpdateAutomaticGears()
    {
        float speedKmh = Mathf.Abs(Vector3.Dot(rb.velocity, transform.forward)) * 3.6f;

        for (int i = 1; i < gearSpeeds.Length; i++)
        {
            if (speedKmh < gearSpeeds[i])
            {
                currentGear = i;
                break;
            }
        }
    }

    void KillSidewaysVelocity()
    {
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.velocity, transform.forward);
        rb.velocity = Vector3.Lerp(rb.velocity, forwardVelocity, 5f * Time.fixedDeltaTime);
    }

    void ApplySteering()
    {
        float speedFactor = Mathf.Clamp01(rb.velocity.magnitude / maxSpeed);

        // Quanto mais rápido, menos gira (sensação pesada)
        float adjustedTurnSpeed = turnSpeed * (1f - speedFactor * 0.7f);

        if (rb.velocity.magnitude > 1f)
        {
            float turn = horizontalInput * adjustedTurnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }

    void ApplyRealisticDrag()
    {
        float speed = rb.velocity.magnitude;

        if (speed < 0.1f)
            return;

        // Resistência de rolamento (constante)
        Vector3 rolling = -rb.velocity.normalized * rollingResistance;

        // Resistência do ar (cresce com velocidade²)
        Vector3 air = -rb.velocity.normalized * speed * speed * airResistance;

        rb.AddForce(rolling + air, ForceMode.Acceleration);
    }
    void ApplyVisualEffects()
    {
        float speed = rb.velocity.magnitude;

        float targetLean = 0f;
        float targetDip = 0f;

        // Só inclina se estiver realmente em movimento
        if (speed > 1f)
        {
            targetLean = -horizontalInput * maxLeanAngle;

            float brakeInput = 0f;

            if (Input.GetKey(KeyCode.S) && !reverseGear)
                brakeInput = 1f;

            targetDip = brakeInput * maxBrakeDip;
        }

        currentLean = Mathf.Lerp(currentLean, targetLean, leanSmoothSpeed * Time.deltaTime);
        currentDip = Mathf.Lerp(currentDip, targetDip, dipSmoothSpeed * Time.deltaTime);

        visualModel.localRotation = Quaternion.Euler(currentDip, 0f, currentLean);
    }

    void AnimateWheels()
    {
        float wheelSpin = rb.velocity.magnitude * 6f;

        RotateWheel(frontLeftMesh, wheelSpin);
        RotateWheel(frontRightMesh, wheelSpin);
        RotateWheel(rearLeftMesh, wheelSpin);
        RotateWheel(rearRightMesh, wheelSpin);

        float steerAngle = horizontalInput * steeringVisualAngle;

        frontLeftMesh.localRotation = Quaternion.Euler(
            frontLeftMesh.localEulerAngles.x,
            steerAngle,
            0f);

        frontRightMesh.localRotation = Quaternion.Euler(
            frontRightMesh.localEulerAngles.x,
            steerAngle,
            0f);
    }

    void RotateWheel(Transform wheel, float speed)
    {
        wheel.Rotate(Vector3.right, speed * Time.deltaTime);
    }
}
