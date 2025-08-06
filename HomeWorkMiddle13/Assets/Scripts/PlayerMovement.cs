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
    [SerializeField] private ForceMode forceMode = ForceMode.VelocityChange;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundStickForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // Отключаем ненужные компоненты для чужих игроков
        if (!photonView.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>()?.gameObject);
            Destroy(GetComponentInChildren<AudioListener>()?.gameObject);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        GroundCheck();
        HandleRotation();
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            HandleMovement();
        }
        else
        {
            // Плавная интерполяция для удаленных игроков
            rb.position = Vector3.Lerp(rb.position, networkPosition, Time.deltaTime * 10f);
            rb.rotation = Quaternion.Lerp(rb.rotation, networkRotation, Time.deltaTime * 10f);
            rb.velocity = networkVelocity;
        }
    }

    private void GroundCheck()
    {
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 targetVelocity = direction * moveSpeed;

        // Плавное изменение скорости
        rb.velocity = Vector3.Lerp(
            rb.velocity,
            new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z),
            Time.fixedDeltaTime * 10f
        );

        // Прижимаем к земле для стабильности
        if (isGrounded && rb.velocity.y < 0.1f)
        {
            rb.AddForce(Vector3.down * groundStickForce, ForceMode.Force);
        }
    }

    private void HandleRotation()
    {
        if (new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                new Vector3(rb.velocity.x, 0, rb.velocity.z)
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Отправляем свои данные
            stream.SendNext(rb.position);
            stream.SendNext(rb.rotation);
            stream.SendNext(rb.velocity);
        }
        else
        {
            // Получаем данные других игроков
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkVelocity = (Vector3)stream.ReceiveNext();

            // Коррекция задержки (опционально)
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            networkPosition += networkVelocity * lag;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (groundCheckDistance + 0.1f));
    }
}