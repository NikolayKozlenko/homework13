using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private string gameVersion = "1.0";

    private void Start()
    {
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    // Подключение к серверу
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Server");
        PhotonNetwork.JoinRandomRoom(); // Подключаемся к случайной комнате
    }

    // Если нет доступных комнат — создаём новую
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 4 });
    }

    // При успешном подключении к комнате
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        SpawnPlayer();
    }

    // Спавн игрока
    private void SpawnPlayer()
    {
        PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
    }
}
