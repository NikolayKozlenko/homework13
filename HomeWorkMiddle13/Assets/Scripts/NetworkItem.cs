using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NetworkItem : MonoBehaviourPun, IPunObservable
{
    [Header("Settings")]
    [SerializeField] private ItemType itemType = ItemType.Coin; 
    [SerializeField] private int value = 1;

    public event Action OnItemCollected;

    private bool isCollected = false;
    private Renderer itemRenderer;
    private Collider itemCollider;

    private void Awake()
    {
        itemRenderer = GetComponent<Renderer>();
        itemCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || !PhotonNetwork.IsConnectedAndReady) return;

        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();
            if (playerView != null && playerView.IsMine)
            {
                photonView.RPC("CollectItemRPC", RpcTarget.AllBuffered, playerView.ViewID);
            }
        }
    }

    [PunRPC]
    private void CollectItemRPC(int collectorId)
    {
        isCollected = true;
        itemRenderer.enabled = false;
        itemCollider.enabled = false;

        PhotonView collector = PhotonView.Find(collectorId);
        if (collector != null)
        {
            collector.GetComponent<PlayerInventory>()?.AddItem(itemType, value);
        }

        OnItemCollected?.Invoke();
    }

    [PunRPC]
    public void RespawnItem(Vector3 newPosition)
    {
        isCollected = false;
        transform.position = newPosition;
        itemRenderer.enabled = true;
        itemCollider.enabled = true;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isCollected);
        }
        else
        {
            isCollected = (bool)stream.ReceiveNext();
            itemRenderer.enabled = !isCollected;
            itemCollider.enabled = !isCollected;
        }
    }
}
