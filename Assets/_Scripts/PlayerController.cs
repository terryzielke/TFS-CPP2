using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float speed = 50f;
    public float rotationSpeed = 10f;

    InputSystem_Actions input;
    Vector2 movementInput;
    Rigidbody rb;

    private void OnEnable()
    {
        input = new InputSystem_Actions();

        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMove;
        input.Player.Jump.started += OnJump;

        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();

        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMove;
        input.Player.Jump.started -= OnJump;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            movementInput = Vector2.zero;
            Debug.Log("Movement Input: 0");
            return;
        }
        movementInput = context.ReadValue<Vector2>();
        Debug.Log($"Movement Input: {movementInput}");
    }

    private void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Jump");

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 7f, rb.linearVelocity.z);
        }
    }
    /*
    // Update is called once per frame
    void Update()
    {
        if (!rb) return;

        Vector2 moveWithSpeed = movementInput * speed * Time.deltaTime;

        rb.linearVelocity = new Vector3(moveWithSpeed.x, rb.linearVelocity.y, moveWithSpeed.y);
    }
    /**/
    void Update()
    {
        if (!rb) return;

        // A and D rotate the player
        float turnInput = movementInput.x;

        transform.Rotate(
            0f,
            turnInput * rotationSpeed * Time.deltaTime * 10f,
            0f
        );

        // W and S move the player forward and backward
        float moveInput = movementInput.y;

        Vector3 moveDirection = transform.forward * moveInput;

        rb.linearVelocity = new Vector3(
            moveDirection.x * speed,
            rb.linearVelocity.y,
            moveDirection.z * speed
        );
    }
    /**/
}
