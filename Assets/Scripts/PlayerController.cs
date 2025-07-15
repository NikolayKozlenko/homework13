using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private int health = 100;

    private Rigidbody rb;
    private Vector3 networkPosition;
    private Quaternion networkRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Только локальный игрок управляет этим объектом
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
        {
            rb.isKinematic = true; // Отключаем физику для чужих игроков
        }
    }

    private void Update()
    {
        if (photonView.IsMine) // Управляем только своим персонажем
        {
            // Получаем ввод с клавиатуры/геймпада
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // Рассчитываем направление движения
            Vector3 moveDirection = new Vector3(moveX, 0, moveZ).normalized;

            // Применяем движение
            rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, moveDirection.z * moveSpeed);

        }
        else // Синхронизация позиции других игроков
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 10);
        }
    }

    // Синхронизация данных между клиентами
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // Отправляем наши данные
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(health);
        }
        else // Получаем данные других игроков
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            health = (int)stream.ReceiveNext();
        }
    }

    // Урон игроку (синхронизировано)
    [PunRPC]
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
            // Респавн через NetworkManager
            FindObjectOfType<NetworkManager>().SpawnPlayer();
        }
    }
}
