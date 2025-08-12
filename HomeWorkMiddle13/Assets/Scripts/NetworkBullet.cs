using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class NetworkBullet : MonoBehaviourPun, IPunObservable
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float speed = 25f;
    [SerializeField] private int damage = 10;

    private Rigidbody rb;
    private float spawnTime;
    private int ownerId;
    private bool isInitialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spawnTime = Time.time;

        if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
        {
            ownerId = (int)photonView.InstantiationData[0];
        }
    }

    public void Initialize(int ownerViewId, Vector3 direction)
    {
        ownerId = ownerViewId;
        rb.velocity = direction * speed;
        isInitialized = true;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (Time.time - spawnTime > lifetime)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine || !isInitialized) return;

        PhotonView otherView = other.GetComponent<PhotonView>();

        if (otherView != null && otherView.ViewID == ownerId) return;

        if (other.CompareTag("Player") && otherView != null)
        {
            Vector3 hitDirection = (other.transform.position - transform.position).normalized;
            otherView.RPC("TakeDamageRPC", RpcTarget.All, damage, ownerId, hitDirection);
        }

        PhotonNetwork.Destroy(gameObject);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(rb.velocity);
            stream.SendNext(ownerId);
        }
        else
        {
            rb.velocity = (Vector3)stream.ReceiveNext();
            ownerId = (int)stream.ReceiveNext();
        }
    }
}
