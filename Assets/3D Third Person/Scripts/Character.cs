﻿using UnityEngine;

[SelectionBase]
[RequireComponent(typeof(CharacterController))]
public class Character : MonoBehaviour {

    public State state;

    [Header("Movement")]

    [Range(0.01f, 10f)]
    public float speed = 5f;

    [Range(1f, 10f)]
    public float jumpForce = 5f;

    public LayerMask mask;
    public float rollSpeed = 1f;

    [Header("Locks")]
    public CharacterLockState lockState;

    [Header("Debug")]
    public bool grounded = false;
    public bool walking = false;
    public bool running = false;
    public float yForce = 0f, xForce = 0f;
    public Vector3 velocity;

    public CharacterStateInfo stateInfo;

    //Información del entorno
    public float groundHeight;

    //Triggers
    bool jump;

    Vector3 moveVector;
    Vector3 inputVector;

    CharacterController characterController;
    Animator animator;
    LedgeGrabber ledgeGrabber;

    void Awake() {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update() {

        //Guardar la posicion inicial para calcular la velocidad al final
        Vector3 initPos = transform.position;

        switch (state) {
            //SUELO O AIRE
            case State.OnGround:
            case State.OnAir: {

                    //Activar el CharacterController de Unity
                    characterController.enabled = true;

                    CheckGroundHeight();
                    ApplyGravity();
                    CheckForJump();
                    CalculateMoveVector();

                    //Mover el CharacterController y detectar si está en el piso (es necesario moverlo primero por cosas de Unity)
                    characterController.Move(moveVector + inputVector * speed * Time.deltaTime);
                    grounded = characterController.isGrounded;
                    if (grounded) { state = State.OnGround; stateInfo.grounded = true; } else { state = State.OnAir; stateInfo.grounded = false; }

                    //Si el personaje está en el aire y tratando de moverse
                    /*if (state == State.OnAir && inputVector != Vector3.zero && yForce <= 0f) {
                        ledgeGrabber.GetClosestLedgePoint();
                        if (ledgeGrabber.inLedgeRange) {
                            transform.position = ledgeGrabber.closestLedgePoint - transform.rotation * ledgeGrabber.grabPoint;
                            state = State.OnLedge;
                        }
                    }*/
                    break;
                }

            //ESCALANDO UN BORDE
            case State.ClimbingLedge: {

                    characterController.enabled = false;

                    break;
                }

            //RODANDO
            case State.Rolling: {
                    rollTimer += Time.deltaTime;

                    CheckGroundHeight();
                    ApplyGravity();
                    CalculateMoveVector();

                    moveVector += transform.forward * rollSpeed * Time.deltaTime;
                    inputVector = Vector3.zero;

                    //Mover el CharacterController y detectar si está en el piso (es necesario moverlo primero por cosas de Unity)
                    characterController.Move(moveVector + inputVector * speed * Time.deltaTime);
                    grounded = characterController.isGrounded;
                    if (grounded) { state = State.Rolling; } else { state = State.OnAir; }

                    //Revisar si ya se acabó la animacion
                    if (rollTimer >= animator.GetCurrentAnimatorStateInfo(0).length) {
                        state = State.OnGround;
                    }
                    break;
                }

            //DEAD
            case State.Dead: {
                    
                    break;
            }
        }

        UpdateAnimator();

        //Resetear todo lo que se tiene que resetear
        if (inputVector == Vector3.zero) { stateInfo.forwardMove = Mathf.MoveTowards(stateInfo.forwardMove, 0f, 2f * Time.deltaTime); }
        moveVector = Vector3.zero;
        inputVector = Vector3.zero;

        //Calcular la velocidad
        velocity = transform.position - initPos;
    }

    #region Acciones Internas

    void ApplyGravity() {
        //Debug.Log("grounded=" + grounded + ", yForce=" + yForce);
        if (!grounded || yForce > 0) {
            yForce = yForce + (Physics2D.gravity.y * Time.deltaTime);
        } else {
            yForce = -0.1f;
            yForce = -1;
        }
    }

    void CheckGroundHeight() {

        //Detectar la altura del piso
        RaycastHit hit = RaycastUtil.RaycastPastItself(gameObject, transform.position + characterController.center, Vector3.down, characterController.height / 2f + 4f, mask);
        if (hit.collider != null) {
            groundHeight = hit.point.y;
        } else {
            groundHeight = float.MinValue;
        }
    }

    void CalculateMoveVector() {
        moveVector = moveVector + new Vector3(0f, yForce * Time.deltaTime, 0f);
    }

    void CheckForJump() {
        if (jump) {
            yForce = jumpForce;
            grounded = false;
            jump = false;
        }
    }

    void UpdateAnimator() {
        stateInfo.UpdateToAnimator(animator);
    }

    void SetAllAnimatorBoolsToFalse() {
        for (int x = 0; x < animator.parameterCount; x++) {
            if (animator.parameters[x].type == AnimatorControllerParameterType.Bool) {
                animator.SetBool(animator.parameters[x].nameHash, false);
            }
        }
    }

    #endregion

    #region Acciones Básicas

    public void Move(Vector3 direction, float speed) {
        inputVector = direction.normalized * speed;
        Vector3 i = transform.InverseTransformDirection(inputVector);
        stateInfo.forwardMove = Mathf.MoveTowards(stateInfo.forwardMove, i.z, 2f * Time.deltaTime);
        //stateInfo.forwardMove = i.z;
        stateInfo.rightMove = i.x;
    }

    public void Jump() {
        if (state == State.OnGround) {
            jump = true;
            state = State.OnAir;
        }
    }

    float climbTimer = 0f;
    public void Climb() {
        if (state == State.OnLedge) {
            state = State.ClimbingLedge;
            animator.SetTrigger("climbLedge");
        }
    }

    float rollTimer = 0f;
    public void Roll() {
        if (state == State.OnGround) {
            state = State.Rolling;
            rollTimer = 0f;
        }
    }

    public void RotateTowards(Vector3 direction, float speed) {
        if (state == State.OnGround || state == State.OnAir) {
            ForceRotateTowards(direction, speed);
        }
    }

    public void ForceRotateTowards(Vector3 direction, float speed) {
        Vector3 procesedDirection = new Vector3(direction.x, 0f, direction.z);
        //Debug.Log("Procesed Dir = " + procesedDirection + ", Raw dir = " + direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(procesedDirection), speed);
    }

    public void SetFeet(Vector3 pos) {
        Vector3 realCenter = transform.position + characterController.center;
        Vector3 feetPosition = new Vector3(realCenter.x, characterController.bounds.min.y, realCenter.z);
        transform.position = pos + (transform.position - feetPosition);
    }

    Vector3 ledgeMovingDirection;
    public void LedgeMove(Vector3 direction) {
        ledgeMovingDirection = direction;
    }

    #endregion

    public enum State { OnGround, OnAir, OnLedge, Rolling, ClimbingLedge, Dead }
}

[System.Serializable]
public class CharacterStateInfo {
    public float forwardMove;
    public float rightMove;

    public bool grounded;
    public float groundHeight = float.MinValue;

    public bool dead;

    public CharacterStateInfo() {

    }

    public void UpdateToAnimator(Animator anim) {
        anim.SetFloat("forwardMove", forwardMove);
        anim.SetFloat("rightMove", rightMove);
        anim.SetBool("grounded", grounded);
        anim.SetBool("dead", dead);
    }

    public void Reset() {
        forwardMove = 0f;
        rightMove = 0f;
    }
}

[System.Serializable]
public class CharacterLockState {
    public bool canMove = true;
    public bool canTurn = true;
    public bool canJump = true;
    public bool canRoll = true;

    public CharacterLockState() {

    }
}