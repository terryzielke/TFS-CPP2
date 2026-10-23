using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    //Component references
    private CharacterController cc;
    private Animator animator;

    // Animator parameter IDs
    private static readonly int ForwardHash = Animator.StringToHash("Forward");
    private static readonly int StrafeHash = Animator.StringToHash("Strafe");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f; // degrees per second
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [Header("Animation Settings")]
    [SerializeField] private float animationDampTime = 0.1f; // smooths blend tree changes

    // Calculated values
    private float gravity;
    private float initalJumpVelocity;


    // Values filled by inputs
    private Vector2 movementInput;
    private float strafeInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool jumpPressed = false;



    private void OnEnable()
    {
        InputManager.Instance.OnMoveEvent += OnMove;
        InputManager.Instance.OnJumpEvent += OnJump;
        InputManager.Instance.OnStrafeEvent += OnStrafe;
    }

    private void OnDisable()
    {
        if (InputManager.IsInitialized)
        {
            InputManager.Instance.OnMoveEvent -= OnMove;
            InputManager.Instance.OnJumpEvent -= OnJump;
            InputManager.Instance.OnStrafeEvent -= OnStrafe;
        }
    }

    private void Start()
    {
        cc = GetComponent<CharacterController>();

        if (cc == null)
        {
            throw new MissingComponentException("CharacterController component is missing from the GameObject. Please add a CharacterController component to this GameObject.");
        }

        // Animator lives on the character model child; the player still works without one
        animator = GetComponentInChildren<Animator>();

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

    void OnStrafe(float strafe) => strafeInput = strafe;

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

    private void Update()
    {
        UpdateAnimator();
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat(ForwardHash, movementInput.y, animationDampTime, Time.deltaTime);
        animator.SetFloat(StrafeHash, strafeInput, animationDampTime, Time.deltaTime);
        animator.SetFloat(SpeedHash, Mathf.Clamp01(new Vector2(strafeInput, movementInput.y).magnitude));
        animator.SetBool(IsGroundedHash, cc.isGrounded);
    }

    void UpdateCharacterVelocity()
    {
        // A and D rotate player
        transform.Rotate(0, movementInput.x * rotationSpeed * Time.fixedDeltaTime, 0);

        // W and S move player forward and backward along the direction they're facing
        // Q and E strafe player left and right
        Vector3 moveDirection = transform.forward * movementInput.y + transform.right * strafeInput;
        Vector3 horizontalVelocity = Vector3.ClampMagnitude(moveDirection, 1f) * movementSpeed;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

        if (cc.isGrounded)
        {
            // Reset y velocity to -skinWidth to keep the character grounded
            velocity.y = -cc.skinWidth;
            // Jump if jump is pressed
            if (jumpPressed)
            {
                velocity.y = initalJumpVelocity;
                if (animator != null) animator.SetTrigger(JumpHash);
            }

            // if player y position is greater than 102 throw exception
            if (transform.position.y > 102f)
            {
                throw new MissingComponentException("Player y position is " + transform.position.y + ", higher than the default terrain. They have jumped onto an object.");
            }
        }
        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }


        // if player y position is less than 90 throw exception
        if (transform.position.y < 90f)
        {
            throw new MissingComponentException("Player y position is " + transform.position.y + ", lower than the default terrain. They have fallen off the terrain.");
        }
    }
}
