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
    [SerializeField, Range(0f, 1f)] private float forwardThreshold = 0.2f; // stick y needed to count as moving while turning
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [Header("Animation Settings")]
    [SerializeField] private float animationDampTime = 0.1f; // smooths blend tree changes
    [Header("Character Model")]
    [SerializeField] private GameObject characterModel; // leave empty to keep the model already in the prefab
    [SerializeField] private RuntimeAnimatorController animatorController; // used on the spawned model
    [SerializeField] private Vector3 modelOffset = new Vector3(0f, -1.08f, 0f); // drops feet to the bottom of the capsule

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

        if (characterModel != null) SpawnCharacterModel();

        CalculateJumpVars();
    }

    void SpawnCharacterModel()
    {
        // Reuse the old model's controller if none was assigned, then remove the old model
        if (animator != null)
        {
            if (animatorController == null) animatorController = animator.runtimeAnimatorController;
            Destroy(animator.gameObject);
        }

        GameObject model = Instantiate(characterModel, transform);
        model.name = "Model";
        model.transform.SetLocalPositionAndRotation(modelOffset, Quaternion.identity);

        animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = animatorController;
        animator.applyRootMotion = false; // CharacterController drives movement

        // Our clips are Humanoid, so they only play on a model imported with a Humanoid avatar
        if (animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogWarning(characterModel.name + " has no Humanoid avatar, so animations won't play. Select the model, open the Rig tab, set Animation Type to Humanoid and click Apply.");
        }
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

    void OnMove(Vector2 move) => movementInput = ToTankInput(move);

    // On a stick, turning means pushing sideways, which drains the forward value (a hard turn is only ~0.3 forward).
    // Keyboard W+A keeps full forward while turning, so match that: x only turns, and any forward/back push
    // moves at the stick's full push strength. Pushing purely sideways still turns in place.
    Vector2 ToTankInput(Vector2 input)
    {
        // Keyboard diagonals come in as (1, 1), so cap the length to keep both devices in the same range
        input = Vector2.ClampMagnitude(input, 1f);
        float strength = input.magnitude;

        float forward = Mathf.Abs(input.y) >= forwardThreshold ? Mathf.Sign(input.y) * strength : 0f;
        // Stretch turn so a diagonal turns at full rate, like holding A or D
        float turn = Mathf.Clamp(input.x * 1.4142f, -1f, 1f);

        Vector2 output = new Vector2(turn, forward);
        return output;
    }

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
