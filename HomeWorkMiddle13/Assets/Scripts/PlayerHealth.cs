using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerHealth : MonoBehaviourPun
{
    [Header("Settings")]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager networkManager;
    private PlayerInventory inventory;
    private bool isDead;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        networkManager = FindObjectOfType<NetworkManager>();
    }

    [PunRPC]
    public void TakeDamageRPC(int damageAmount, int attackerViewId, Vector3 hitDirection, PhotonMessageInfo info)
    {
        if (!photonView.IsMine || isDead) return;

        inventory.TakeDamage(damageAmount);

        if (inventory.CurrentHealth <= 0)
        {
            photonView.RPC("Die", RpcTarget.All);
        }
    }

    [PunRPC]
    public void Die()
    {
        if (isDead) return;

        isDead = true;
        SetPlayerActive(false);

        if (photonView.IsMine)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
    }

    private void Respawn()
    {
        photonView.RPC("RespawnPlayer", RpcTarget.AllViaServer);
    }
        
    private void SetPlayerActive(bool isActive)
    {
        GetComponent<Renderer>().enabled = isActive;
        GetComponent<Collider>().enabled = isActive;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !isActive;
            if (isActive) rb.velocity = Vector3.zero;
        }

        GetComponent<PlayerController>().enabled = isActive;
    }

    [PunRPC]
    private void RespawnPlayer()
    {
        isDead = false;
        SetPlayerActive(true);

        if (!photonView.IsMine) return;

        NetworkManager manager = FindObjectOfType<NetworkManager>();
        if (manager != null)
        {
            Vector3 spawnPosition = PhotonNetwork.IsMasterClient
                ? manager.spawnPointMaster.position
                : manager.spawnPointClient.position;

            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            );

            transform.position = spawnPosition + randomOffset;
            Debug.Log($"Player respawned at start position: {transform.position}");
        }
        else
        {
            Debug.LogError("NetworkManager not found!");
            transform.position = Vector3.zero;
        }

        GetComponent<PlayerInventory>().ResetHealth();
        Debug.Log("Player respawned with full health");

        PlayerInventory inventory = GetComponent<PlayerInventory>();
        inventory.ResetHealth();
        inventory.ResetAmmo();
        inventory.ResetCoins(); 
    }
}