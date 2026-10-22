using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    //Component references
    private CharacterController cc;


    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f; // degrees per second
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float timeToJumpApex = 0.4f;

    // Calculated values
    private float gravity;
    private float initalJumpVelocity;


    // Values filled by inputs
    private Vector2 movementInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool jumpPressed = false;



    private void OnEnable()
    {
        InputManager.Instance.OnMoveEvent += OnMove;
        InputManager.Instance.OnJumpEvent += OnJump;
    }

    private void OnDisable()
    {
        if (InputManager.IsInitialized)
        {
            InputManager.Instance.OnMoveEvent -= OnMove;
            InputManager.Instance.OnJumpEvent -= OnJump;
        }
    }

    private void Start()
    {
        cc = GetComponent<CharacterController>();

        if (cc == null)
        {
            throw new MissingComponentException("CharacterController component is missing from the GameObject. Please add a CharacterController component to this GameObject.");
        }

        CalculateJumpVars();
    }

    private void OnValidate()
    {
        CalculateJumpVars();
    }

    private void CalculateJumpVars()
    {
        if (timeToJumpApex <= 0 || float.IsNaN(timeToJumpApex) || float.IsInfinity(timeToJumpApex))
        {
            Debug.LogWarning("timeToJumpApex must be greater than 0 and a valid number. Resetting to default value of 0.4f.");
            timeToJumpApex = 0.4f;
        }

        if (jumpHeight <= 0 || float.IsNaN(jumpHeight) || float.IsInfinity(jumpHeight))
        {
            Debug.LogWarning("Jump height must be greater than 0 and a valid number. Resetting to default value of 2f.");
            jumpHeight = 2f;
        }

        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        initalJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    void OnMove(Vector2 move) => movementInput = move;

    void OnJump(bool isPressed) => jumpPressed = isPressed;

    // Update is called once per frame
    void FixedUpdate()
    {
        //update our characters velocity based on input
        //check if grounded and reset y velocity to be -skinWidth to keep the character grounded
        //if jump is pressed, set y velocity to the initial jump velocity
        //otherwise, apply gravity to the y velocity

        UpdateCharacterVelocity();

        //move the character controller based on the velocity
        cc.Move(velocity * Time.fixedDeltaTime);
    }

    void UpdateCharacterVelocity()
    {
        // A and D rotate player
        transform.Rotate(0, movementInput.x * rotationSpeed * Time.fixedDeltaTime, 0);

        // W and S move player forward and backward along the direction they're facing
        Vector3 forwardVelocity = transform.forward * movementInput.y * movementSpeed;
        velocity.x = forwardVelocity.x;
        velocity.z = forwardVelocity.z;

        if (cc.isGrounded)
        {
            // Reset y velocity to -skinWidth to keep the character grounded
            velocity.y = -cc.skinWidth;
            // Jump if jump is pressed
            if (jumpPressed)
            {
                velocity.y = initalJumpVelocity;
            }
        }
        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }
    }
}
