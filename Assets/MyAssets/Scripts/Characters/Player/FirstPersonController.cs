using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Combat")]
    public float attackRange = 2.5f;
    public float attackDamage = 25f;
    public LayerMask attackMask;
    public float attackCooldown = 0.5f;
    private float lastAttackTime;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        Vector3 move = transform.right * Input.GetAxis("Horizontal") +
                      transform.forward * Input.GetAxis("Vertical");
        controller.Move(move * walkSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        float verticalRot = cameraTransform.localEulerAngles.x - mouseY;
        verticalRot = Mathf.Clamp(verticalRot > 180 ? verticalRot - 360 : verticalRot, -90f, 90f);
        cameraTransform.localEulerAngles = Vector3.right * verticalRot;
    }
}