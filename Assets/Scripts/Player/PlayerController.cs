using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 5.5f;
    public float sprintSpeed = 9.5f;
    public float jumpHeight = 1.6f;
    public float gravity = -22f;

    [Header("Ground & Physics")]
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask = ~0;

    [Header("Head Bobbing")]
    public Transform cameraHolder;
    public float bobFrequency = 10f;
    public float bobAmount = 0.05f;

    [Header("State")]
    public bool canMove = true;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float defaultCameraPosY;
    private float bobTimer = 0f;
    private bool wasGroundedLastFrame = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraHolder != null)
        {
            defaultCameraPosY = cameraHolder.localPosition.y;
        }
    }

    private void Update()
    {
        if (!canMove)
        {
            if (cameraHolder != null)
            {
                Vector3 pos = cameraHolder.localPosition;
                pos.y = Mathf.Lerp(pos.y, defaultCameraPosY, Time.deltaTime * 8f);
                cameraHolder.localPosition = pos;
            }
            return;
        }

        // 1. Ground check
        isGrounded = controller.isGrounded;
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (isGrounded && !wasGroundedLastFrame && velocity.y <= -2f)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayLand();
        }
        wasGroundedLastFrame = isGrounded;

        // 2. Input movement using InputBridge
        float moveX = InputBridge.GetAxisRaw("Horizontal");
        float moveZ = InputBridge.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(moveX, 0f, moveZ).normalized;

        bool isSprinting = InputBridge.GetKey(KeyCode.LeftShift) || InputBridge.GetKey(KeyCode.RightShift);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = Vector3.zero;
        if (inputDir.magnitude >= 0.1f)
        {
            move = (transform.right * inputDir.x + transform.forward * inputDir.z) * currentSpeed;

            if (isGrounded && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayFootstep(isSprinting);
            }

            if (isGrounded && cameraHolder != null)
            {
                float freq = isSprinting ? bobFrequency * 1.35f : bobFrequency;
                bobTimer += Time.deltaTime * freq;
                float bobOffset = Mathf.Sin(bobTimer) * bobAmount * (isSprinting ? 1.4f : 1f);
                Vector3 camPos = cameraHolder.localPosition;
                camPos.y = defaultCameraPosY + bobOffset;
                cameraHolder.localPosition = camPos;
            }
        }
        else
        {
            if (cameraHolder != null)
            {
                Vector3 camPos = cameraHolder.localPosition;
                camPos.y = Mathf.Lerp(camPos.y, defaultCameraPosY, Time.deltaTime * 8f);
                cameraHolder.localPosition = camPos;
            }
        }

        // 3. Jump using InputBridge
        if (InputBridge.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayJump();
        }

        // 4. Apply Gravity
        velocity.y += gravity * Time.deltaTime;

        // 5. Move CharacterController
        Vector3 finalVelocity = (move + Vector3.up * velocity.y) * Time.deltaTime;
        controller.Move(finalVelocity);
    }

    public void SetPositionAndRotation(Vector3 pos, Quaternion rot)
    {
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = pos;
            transform.rotation = rot;
            controller.enabled = true;
        }
        else
        {
            transform.position = pos;
            transform.rotation = rot;
        }
        velocity = Vector3.zero;
    }
}
