using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{

    [Header("Movement")]
    public float moveSpeed;

    public float groundDrag;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [HideInInspector] public float walkSpeed;
    [HideInInspector] public float sprintSpeed;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;
    Animator anim;

    [Header("Rotation")]
    [SerializeField] private float sensX;
    [SerializeField] private float sensY;

    float yRotation;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        anim = GetComponent<Animator>();

        readyToJump = true;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MyInput();
        SpeedControl();
        IdlePlayer();

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;

        yRotation += mouseX;

        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if (Input.GetKey(jumpKey) && readyToJump )
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // on ground
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        anim.SetBool("move", true);
    }

    private void IdlePlayer()
    {
        if(Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.D))
        {
            anim.SetBool("move", false);
            Debug.Log("tes");
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if needed
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }


    /* [SerializeField] private KeyCode _leftWard, _rightWard, _upWard, _downWard, _leftRotate, _rightRotate;
     [SerializeField] private float _speed;
     private float HorizontalInput;
     private float VerticalInput;
     Animator _anim;
     Transform _player;
     Vector3 Movement;
     Vector3 Rotation;

     private void Start()
     {
         _anim = GetComponent<Animator>();
         _player = GetComponent<Transform>();
     }

     private void Update()
     {
         Move();
         Rotate();
     }

     private void Move()
     {

         if (Input.GetKey(_leftWard) || Input.GetKey(_rightWard) || Input.GetKey(_downWard) || Input.GetKey(_upWard))
         {
             HorizontalInput = Input.GetAxis("Horizontal");
             VerticalInput = Input.GetAxis("Vertical");
             Movement = new Vector3(HorizontalInput, 0, VerticalInput);
             Movement.Normalize();
             _player.transform.Translate(Movement * Time.deltaTime * _speed);
             _anim.SetBool("move", true);
         }
         else if(Input.GetKeyUp(_leftWard) || Input.GetKeyUp(_rightWard) || Input.GetKeyUp(_upWard) || Input.GetKeyUp(_downWard))
         {
             _anim.SetBool("move", false);
         }
     }

     private void Rotate()
     {
         if (Input.GetKey(_leftRotate) || Input.GetKey(_rightRotate))
         {
             if (Input.GetKey(_leftRotate))
             {
                 Rotation = new Vector3(0, -90, 0);
             }
             else if(Input.GetKey(_rightRotate))
             {
                 Rotation = new Vector3(0, 90, 0);
             }

             _player.transform.Rotate(Rotation * 100f * Time.deltaTime);
         }
     }




 */



    //////////////////////////////////////////////////////////////////////////////////////
    /*[SerializeField] private float speed;
    Transform Character;
    Animator Anim;
    private float HorizontalInput;
    private float VerticalInput;
    Vector3 Movement;
    Vector3 Rotation;

    void Start()
    {
        Character = GetComponent<Transform>();
        Anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Rotate();
    }

    private void Move()
    {
        HorizontalInput = Input.GetAxis("Horizontal");
        VerticalInput = Input.GetAxis("Vertical");
        Movement = new Vector3(HorizontalInput, 0, VerticalInput);
        Movement.Normalize();
        Character.transform.Translate(Movement * speed * Time.deltaTime);
    }

    private void Rotate()
    {
        Rotation = new Vector3(0, HorizontalInput, 0);
        Rotation.Normalize();
        Character.transform.Rotate(Rotation * 250f * Time.deltaTime);
    }*/
}
