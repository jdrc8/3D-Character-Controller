﻿using UnityEngine;
using UnityEngine.AI;

[SelectionBase]
public class Character : MonoBehaviour {

    public float height, radius;

    [Header("Movement")]

    [Range(0.01f, 10)]
    public float speed = 5f;
    public float rotationSpeed = 400f;

    [Range(0.01f, 1f)]
    public float jumpForce = 0.1f;

    public LayerMask mask;
    public float rollSpeed = 1f;

    [Header("Debug")]
    public Vector3 velocity;
    public Vector3 relativeVelocity;
    public Vector3 lookDirection;

    public CharacterState state;

    //Información del entorno
    public float groundHeight;

    //Triggers
    bool jump;

    //Events
    public Action OnJump;
    public Action OnHighFall;

    Vector3 inputVector;

    [HideInInspector]
    Animator animator;

    #region Monobehaviours

    void Awake() {
        animator = GetComponent<Animator>();
        lookDirection = transform.forward;
    }

    void Update() {

        CheckGrounded();

        // Apply gravity if necessary
        if (!state.grounded && Vector3.Dot(velocity, Physics.gravity.normalized) < Physics.gravity.magnitude) {
            velocity = velocity + Physics.gravity * 0.06f * Time.deltaTime;
        }

        Move(velocity + inputVector * speed * Time.deltaTime);
        inputVector = Vector3.zero;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(transform.forward, -Physics.gravity.normalized), 200f * Time.deltaTime);

    }

    #endregion

    #region Private methods

    public void Move(Vector3 delta) {

        RaycastHit hit; bool didHit = Physics.CapsuleCast(
            transform.position + transform.up * radius, transform.position + transform.up * height - transform.up * radius,
            radius, delta.normalized, out hit, delta.magnitude, mask);

        if (didHit) {

            transform.position += delta.normalized * (hit.distance - 0.02f);

            debugDirection = Vector3.Cross(Vector3.Cross(hit.normal, delta.normalized), hit.normal);

            didHit = Physics.CapsuleCast(
            transform.position + transform.up * radius, transform.position + transform.up * height - transform.up * radius,
            radius, debugDirection, out hit, Vector3.Dot(delta.normalized * (delta.magnitude - hit.distance), debugDirection), mask);

            if (!didHit) {
                transform.position += debugDirection * Vector3.Dot(delta.normalized * (delta.magnitude - hit.distance), debugDirection);
            }

        } else {
            transform.position += delta;
        }
    }

    public void CheckGrounded() {
        bool check = Physics.CheckSphere(transform.position + transform.up * radius, radius + 0.04f, mask);
        if (check && Vector3.Dot(velocity, Physics.gravity.normalized) >= 0f) {
            state.grounded = true;
            velocity = Vector3.zero;
        } else {
            state.grounded = false;
        }
    }

    #endregion

    #region Public methods

    public void Walk(Vector3 delta) {
        inputVector = Vector3.ClampMagnitude(delta, 1f);
    }

    public void Jump() {
        velocity = velocity - Physics.gravity.normalized * jumpForce;
    }

    public void RotateTowards(Vector3 direction, float speed) {
        lookDirection = direction;
    }

    public void ForceRotateTowards(Vector3 direction, float speed) {
        Vector3 procesedDirection = new Vector3(direction.x, 0f, direction.z);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(procesedDirection), speed);
    }

    #endregion

    public Vector3 debugDirection;

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + transform.up * radius, radius);
        Gizmos.DrawWireSphere(transform.position + transform.up * height - transform.up * radius, radius);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, debugDirection);
    }

}

[System.Serializable]
public class CharacterState {
    public float forwardMove;
    public float rightMove;
    public float moveSpeed;

    public bool moving;
    public bool grounded;
    public bool previouslyGrounded;

    public CharacterState() {

    }

    public void UpdateToAnimator(Animator anim) {
        //anim.SetFloat("forwardMove", Mathf.MoveTowards(anim.GetFloat("forwardMove"), forwardMove, 1f * Time.deltaTime));
        //anim.SetFloat("rightMove", Mathf.MoveTowards(anim.GetFloat("rightMove"), rightMove, 1f * Time.deltaTime));

        anim.SetFloat("forwardMove", forwardMove);
        anim.SetFloat("rightMove", rightMove);
        anim.SetFloat("moveSpeed", moveSpeed);

        anim.SetBool("grounded", grounded);
        anim.SetBool("moving", moving);

    }

    public void Reset() {
        forwardMove = 0f;
        moveSpeed = 0f;
        rightMove = 0f;
    }
}

public delegate void Action();
public delegate void FloatAction(float n);
