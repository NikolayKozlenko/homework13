using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PlayerShooting : MonoBehaviourPun
{
    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private float bulletSpeed = 25f;

    private float nextFireTime;
    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        if (!photonView.IsMine || inventory == null) return;

        if (Input.GetMouseButtonDown(0)) 
        {
            TryShoot();
        }
    }

    private void TryShoot()
    {
        if (Time.time < nextFireTime || !inventory.CanShoot) return;

        nextFireTime = Time.time + fireRate;
        Vector3 shootDirection = GetShootDirection();
        photonView.RPC("FireBulletRPC", RpcTarget.AllViaServer, shootDirection);
    }

    private Vector3 GetShootDirection()
    {
        Vector3 direction = transform.forward;
        direction.y = 0; 
        return direction.normalized;
    }

    [PunRPC]
    private void FireBulletRPC(Vector3 shootDirection)
    {
        if (photonView.IsMine && !inventory.TryShoot()) return;

        GameObject bullet = PhotonNetwork.Instantiate(
            bulletPrefab.name,
            firePoint.position,
            Quaternion.LookRotation(shootDirection)
        );

        bullet.GetComponent<NetworkBullet>().Initialize(photonView.ViewID, shootDirection * bulletSpeed);
    }
}
