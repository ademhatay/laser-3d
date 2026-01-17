using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -50f;
    [SerializeField] private float fallMultiplier = 6f;
    [SerializeField] private float maxFallSpeed = -50f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookUp = 85f;
    [SerializeField] private float maxLookDown = 60f;
    [SerializeField] private Transform playerCamera;
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    [SerializeField] private LayerMask groundMask;
    
    [Header("Spawn Settings")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";
    
    [Header("Player Body")]
    [SerializeField] private GameObject playerBody;
    [SerializeField] private bool hideBodyFromCamera = true;
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private bool canJump = true;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1f, 0);
        }
        
        // Find and move to spawn point
        MoveToSpawnPoint();
        
        // Create camera if not assigned
        if (playerCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;
                playerCamera.SetParent(transform);
                playerCamera.localPosition = new Vector3(0, 1.6f, 0);
                playerCamera.localRotation = Quaternion.identity;
            }
        }
        
        // Create ground check if not assigned
        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, 0, 0);
            groundCheck = gc.transform;
        }
        
        // Hide player body from camera
        if (hideBodyFromCamera && playerBody != null)
        {
            // Set player body to a different layer that camera doesn't render
            playerBody.SetActive(false);
        }
        
        // Find player body if not assigned
        if (playerBody == null)
        {
            Transform body = transform.Find("PlayerBody");
            if (body != null)
            {
                playerBody = body.gameObject;
                playerBody.SetActive(false);
            }
        }
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Set ground mask to include default layer
        if (groundMask == 0)
        {
            groundMask = LayerMask.GetMask("Default");
        }
    }
    
    private void MoveToSpawnPoint()
    {
        GameObject spawnPoint = GameObject.FindGameObjectWithTag(spawnPointTag);
        if (spawnPoint != null)
        {
            // Disable controller temporarily to allow teleport
            if (controller != null && controller.enabled)
            {
                controller.enabled = false;
                transform.position = spawnPoint.transform.position;
                transform.rotation = spawnPoint.transform.rotation;
                controller.enabled = true;
            }
            else
            {
                transform.position = spawnPoint.transform.position;
                transform.rotation = spawnPoint.transform.rotation;
            }
        }
    }
    
    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleMouseLook();
        HandleCursorToggle();
    }
    
    private void HandleGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            canJump = true;
        }
    }
    
    private void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Calculate move direction
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        
        // Apply speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);
        
        // Jump - only if grounded and can jump (no double jump)
        if (Input.GetButtonDown("Jump") && isGrounded && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            canJump = false;
        }
        
        // Apply gravity - aggressive fall
        if (velocity.y < 0)
        {
            // Düşerken çok daha hızlı düş
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
            // Maksimum düşüş hızını sınırla
            velocity.y = Mathf.Max(velocity.y, maxFallSpeed);
        }
        else
        {
            // Yükselirken normal yer çekimi
            velocity.y += gravity * Time.deltaTime;
        }
        
        controller.Move(velocity * Time.deltaTime);
    }
    
    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Vertical rotation (camera only) with deadzone for looking down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookUp, maxLookDown);
        
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        
        // Horizontal rotation (player body)
        transform.Rotate(Vector3.up * mouseX);
    }
    
    private void HandleCursorToggle()
    {
        // Press Escape to toggle cursor lock
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    public void Respawn()
    {
        MoveToSpawnPoint();
        velocity = Vector3.zero;
    }
}