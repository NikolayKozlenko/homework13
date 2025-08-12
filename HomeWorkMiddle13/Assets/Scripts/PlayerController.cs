using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Rigidbody), typeof(PhotonView))]
public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float syncSmoothing = 10f;
    [SerializeField] private float positionSnapThreshold = 5f;

    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundLayers = 1;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float groundStickForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;
    private float lastNetworkTime;
    private bool receivedValidData;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.maxAngularVelocity = 7f;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        CheckGrounded();
        HandleRotation();
    }

    [PunRPC]
    public void NetworkDestroyPlayer()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            HandleMovement();
        }
        else if (receivedValidData)
        {
            SyncRemotePlayer();
        }
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.CheckSphere(
            transform.position + Vector3.up * 0.1f,
            groundCheckRadius,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleMovement()
    {
        Vector3 input = new Vector3(
            Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical")
        ).normalized;

        Vector3 targetVelocity = input * moveSpeed;
        rb.velocity = Vector3.Lerp(
            rb.velocity,
            new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z),
            Time.fixedDeltaTime * 10f
        );

        if (isGrounded && rb.velocity.y < 0.1f)
        {
            rb.AddForce(Vector3.down * groundStickForce, ForceMode.Force);
        }
    }

    private void HandleRotation()
    {
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (flatVelocity.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(flatVelocity),
                Time.deltaTime * rotationSpeed
            );
        }
    }

    private void SyncRemotePlayer()
    {
        float distance = Vector3.Distance(rb.position, networkPosition);

        if (distance > positionSnapThreshold)
        {
            rb.position = networkPosition;
            rb.rotation = networkRotation;
            return;
        }

        rb.position = Vector3.Lerp(rb.position, networkPosition, Time.deltaTime * syncSmoothing);
        rb.rotation = Quaternion.Slerp(rb.rotation, networkRotation, Time.deltaTime * (syncSmoothing * 0.5f));
        rb.velocity = networkVelocity;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(isGrounded);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();
            isGrounded = (bool)stream.ReceiveNext();
            lastNetworkTime = Time.time;
            receivedValidData = true;

            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += networkVelocity * lag;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, groundCheckRadius);
    }
}