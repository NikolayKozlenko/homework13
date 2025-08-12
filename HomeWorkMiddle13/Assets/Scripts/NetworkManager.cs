using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks

{
    [Header("Player Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] public Transform spawnPointMaster;
    [SerializeField] public Transform spawnPointClient;

    [Header("Item Spawn Settings")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private int maxItems = 10;
    [SerializeField] private Vector2 spawnArea = new Vector2(20f, 20f);
    [SerializeField] private float itemRespawnTime = 10f;
    [SerializeField] private float itemSpawnHeight = 1.5f;
    [SerializeField] private bool alignToGround = true;
    [SerializeField] private LayerMask groundMask = -1;

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Start()
    {
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server. Joining room...");
        PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions { MaxPlayers = 4 }, null);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room. Spawning player...");
        SpawnPlayer();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master client detected. Spawning items...");
            StartCoroutine(SpawnInitialItemsWithDelay());
        }
    }

    public Vector3 GetStartSpawnPosition(bool isMasterClient)
    {
        return isMasterClient ? spawnPointMaster.position : spawnPointClient.position;
    }

    private void SpawnPlayer()
    {
        Transform spawnPoint = PhotonNetwork.IsMasterClient ?
            spawnPointMaster : spawnPointClient;

        if (spawnPoint == null)
        {
            Debug.LogWarning("Spawn point not assigned! Using default position.");
            spawnPoint = new GameObject("TempSpawn").transform;
            spawnPoint.position = Vector3.zero;
        }

        PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log($"Player spawned at: {spawnPoint.position}");
    }

    private IEnumerator SpawnInitialItemsWithDelay()
    {
        yield return new WaitForSeconds(1f);
        SpawnInitialItems();
    }

    private void SpawnInitialItems()
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogWarning("Cannot spawn items: no item prefabs assigned!");
            return;
        }

        for (int i = 0; i < maxItems; i++)
        {
            SpawnSingleItem();
        }
    }

    public Vector3 GetRespawnPosition()
    {
        Transform spawnPoint = PhotonNetwork.IsMasterClient ?
            spawnPointMaster : spawnPointClient;

        Vector3 randomOffset = new Vector3(
            Random.Range(-3f, 3f),
            0,
            Random.Range(-3f, 3f)
        );

        return spawnPoint.position + randomOffset;
    }

    private void SpawnSingleItem()
    {
        Vector3 spawnPosition = new Vector3(
            Random.Range(-spawnArea.x, spawnArea.x),
            itemSpawnHeight,
            Random.Range(-spawnArea.y, spawnArea.y)
        );

        if (alignToGround)
        {
            if (Physics.Raycast(spawnPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundMask))
            {
                spawnPosition.y = hit.point.y + itemSpawnHeight;
            }
        }

        GameObject randomPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        GameObject item = PhotonNetwork.InstantiateRoomObject(
            randomPrefab.name,
            spawnPosition,
            Quaternion.identity
        );

        spawnedItems.Add(item);

        NetworkItem itemComponent = item.GetComponent<NetworkItem>();
        if (itemComponent != null)
        {
            itemComponent.OnItemCollected += () => StartCoroutine(RespawnItemAfterDelay(item));
        }
    }

    private IEnumerator RespawnItemAfterDelay(GameObject item)
    {
        yield return new WaitForSeconds(itemRespawnTime);

        if (PhotonNetwork.IsMasterClient && item != null)
        {
            Vector3 newPosition = new Vector3(
                Random.Range(-spawnArea.x, spawnArea.x),
                itemSpawnHeight,
                Random.Range(-spawnArea.y, spawnArea.y)
            );

            if (alignToGround)
            {
                if (Physics.Raycast(newPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundMask))
                {
                    newPosition.y = hit.point.y + itemSpawnHeight;
                }
            }

            item.GetComponent<PhotonView>().RPC("RespawnItem", RpcTarget.All, newPosition);
        }
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            spawnedItems.RemoveAll(item => item == null);
        }
    }

    private void OnDestroy()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                NetworkItem itemComponent = item.GetComponent<NetworkItem>();
                if (itemComponent != null)
                {
                    itemComponent.OnItemCollected -= () => StartCoroutine(RespawnItemAfterDelay(item));
                }
            }
        }
    }
}
