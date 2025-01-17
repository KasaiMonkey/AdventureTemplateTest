//This Script Allows the player to move around the scene
//It uses Unity's new Input System
//And also MasayaScripts Namespace
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MasayaScripts
{
    public class PlayerMovement : MonoBehaviour
    {
        public static PlayerMovement current; //creates a public static variable of this class so we can easily reference it

        InputMaster inputMaster; //The inputs we will be using
        Rigidbody rb; //The rigidbody attached to the player
        [SerializeField] bool canMove; //This boolean will be used to stop player movement
        float horizontal; //Used for left/right movement
        float vertical; //Used for forward/back movement
        int currentDirection = 1; //Used for checking what direction the user is moving; 1 = right, -1 = left
        bool isGrounded; //Boolean will change based on if the used is touching the ground or not

        [Header("Character Stats")]
        [SerializeField] private float speed = 1; //How fast the player can move
        [SerializeField] private float rotationSpeed = 5; //How fast the player will rotate when facing a different direction
        [SerializeField] private float jumpHeight = 1; //How high the player can jump

        [Header("References")]
        [SerializeField] private LayerMask groundLayer; //Used for checking what the player can jump off
        [SerializeField] private Transform visuals; //All the visuals of the player should be in this group as it will be rotated when facing a certain direction
        [SerializeField] private Animator anim; //The animator attached to the character
        [SerializeField] private PhysicMaterial noFriction; //Physic material used so the player won't get stuck against walls
        [SerializeField] private PhysicMaterial maxFriction; //Physic material used so the player won't slide down a ramp
        bool physicsCooldown;
        private CapsuleCollider col;

        private void Start()
        {
            if (StartLevelLocation.setLocation == true)
            {
                transform.position = StartLevelLocation.position;
                StartLevelLocation.setLocation = false;
            }

            current = this; //Gets reference to itself so we can easily access this script anywhere
            col = GetComponent<CapsuleCollider>(); //Grabs reference to the capsulecollider attached to the player

            currentDirection = 1; //Sets the current direction as right
            rb = GetComponent<Rigidbody>(); //Gets reference to the rigidbody attached to the player

            inputMaster = new InputMaster(); //Creates the inputs we will be using for the player
            inputMaster.PlayerMovement.Jump.performed += Jump; //Create action for the jump button
            inputMaster.Enable(); //enabled to inputs

            StartCoroutine(CharacterDirection()); //Starts a coroutine checking for the players direction
        }


        private void Update()
        {
            //Checks if the player can move
            if (canMove)
            {
                horizontal = inputMaster.PlayerMovement.Horizontal.ReadValue<float>(); //Gets the value for left/right input
                vertical = inputMaster.PlayerMovement.Vertical.ReadValue<float>(); //Gets the value for forward/back input
            }
            else
            {
                //if the player can move, then we set the left/right and forward/back values to 0
                horizontal = 0;
                vertical = 0;
            }

            //This if statement is used for setting animation by using the preivous input values
            if (horizontal != 0 || vertical != 0)
            {
                //if the player has press a movement input
                anim.SetBool("isMoving", true); //tells the animator that isMoving is true
            }
            else
            {
                //if the player isn't pressing anything or can't move
                anim.SetBool("isMoving", false); //tells the animator that isMoving is false
            }

            //Sets the moving direction so we can rotate the player to face the correct way
            if (horizontal != 0 && currentDirection != horizontal)
            {
                currentDirection = (int)horizontal;
            }

            //If we can't move then we can just stop reading the rest of the UpdateFunction
            if (!canMove)
            {
                return;
            }

            //Calls the CheckGround function which checks for if the player is touching the ground
            CheckGround();
        }

        /// <summary>
        /// FixedUpdate gets called every physics frame
        /// We put the movement in here so that it can caluclate if the player is colliding with walls more accurately
        /// </summary>
        private void FixedUpdate()
        {
            Vector3 horizontalDirection = transform.right * horizontal; //Gets how much the player is moving left/right
            Vector3 verticalDirection = transform.forward * vertical; //Gets how much the player is moving forward/back
            Vector3 moveDirection = (verticalDirection + horizontalDirection).normalized * speed * Time.deltaTime * 1; //Combines the directions together
                                                                                                                       //rb.MovePosition(moveDirection + rb.position); //Moves the player to the new position by adding the movedirection to the players current position
            rb.MovePosition(transform.position + moveDirection);
        }

        /// <summary>
        /// This coroutine will always be checking which direction the player should be facing
        /// It gets called in the start function - StartCoroutine(CharacterDirection());
        /// </summary>
        /// <returns></returns>
        IEnumerator CharacterDirection()
        {
            //set the default rot value to 1, as the character should be facing right by default
            float rotValue = 1; //1 = facing right, -1 = facing left

            while (true)
            {
                //Check which direction the player is moving
                //Then it will rotate the visuals transform to face the correct direction
                if (currentDirection == 1 && rotValue != 1)
                {
                    rotValue += Time.deltaTime * rotationSpeed * 1000;
                    if (rotValue >= 0)
                    {
                        rotValue = 0;
                    }
                    visuals.localEulerAngles = new Vector3(0, rotValue, 0);
                }
                else if (currentDirection == -1 && rotValue != -1)
                {
                    rotValue -= Time.deltaTime * rotationSpeed * 1000;
                    if (rotValue <= -180)
                    {
                        rotValue = -180;
                    }
                    visuals.localEulerAngles = new Vector3(0, rotValue, 0);
                }
                yield return null;
            }
        }

        /// <summary>
        /// This function can be called from anywhere
        /// calling this function will either stop/allow the player from being able to move
        /// To call it use the below line of code. Change value to either true or false, true = player can move, false = player can't move
        /// PlayerMovement.current.SetMovement(value)
        /// </summary>
        /// <param name="value"></param>
        public void SetMovement(bool value)
        {
            canMove = value;
        }

        /// <summary>
        /// This function checks for if the player is touching the ground
        /// It gets called from the UpdateFunction
        /// </summary>
        private void CheckGround()
        {
            //First we shoot a raycast downwards from slightly above the players feet
            if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, 0.15f, groundLayer))
            {
                //If the raycast hits something which has the ground layer
                //then we can set the isGrounded boolean to true and change the physics material so the player won't slip
                isGrounded = true;
                ChangePhysicsMaterial(maxFriction);
            }
            else
            {
                //If the raycast doesn't hit anything
                //then we can set the isGrounded boolean to false and change the physics material so the player won't sget stuck against the wall while falling/jumping
                isGrounded = false;
                ChangePhysicsMaterial(noFriction);
            }
        }

        /// <summary>
        /// This function gets called when the jump input is pressed
        /// it is set within the start function - inputMaster.PlayerMovement.Jump.performed += Jump
        /// The obj variable can be used for checking what device is being used
        /// </summary>
        /// <param name="obj"></param>
        private void Jump(InputAction.CallbackContext obj)
        {
            if (isGrounded && canMove)
            {
                isGrounded = false;
                ChangePhysicsMaterial(noFriction);
                if (!physicsCooldown)
                {
                    physicsCooldown = true;
                    Invoke("ResetPhysicsCooldown", 0.1f);
                }
                rb.AddForce(Vector3.up * jumpHeight * 100);
            }
        }

        //This cooldown is used so that the player can jump while pressing against an object
        void ResetPhysicsCooldown()
        {
            physicsCooldown = false;
        }

        void ChangePhysicsMaterial(PhysicMaterial mat)
        {
            if (!physicsCooldown)
            {
                col.material = mat;
            }
        }
    }
}
