using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class PlayerInventory : MonoBehaviourPun
{
    [Header("UI Settings")]
    [SerializeField] private Vector3 worldUIOffset = new Vector3(0, 2.5f, 0);
    [SerializeField] private GameObject worldUIPrefab;

    [Header("Local UI References")]
    [SerializeField] public Text localHealthText;
    [SerializeField] public Text localCoinsText;
    [SerializeField] public Text localAmmoText;

    [Header("Inventory Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxAmmo = 100;
    [SerializeField] private int initialCoins = 0;

    private int coins;
    private int health;
    private int ammo;
    private WorldUIReferences worldUIController;

    public int CurrentHealth => health;
    public int CurrentAmmo => ammo;
    public int CurrentCoins => coins;
    public bool CanShoot => ammo > 0;

    private void Start()
    {
        health = maxHealth / 2; 
        ammo = 50; 
        coins = initialCoins;

        InitializeUI();
        UpdateUI();
    }

    private void InitializeUI()
    {
        if (photonView.IsMine)
        {
            Canvas hudCanvas = GameObject.Find("PlayerUI")?.GetComponent<Canvas>();
            if (hudCanvas != null)
            {
                localHealthText = hudCanvas.transform.Find("HealthText")?.GetComponent<Text>();
                localCoinsText = hudCanvas.transform.Find("CoinsText")?.GetComponent<Text>();
                localAmmoText = hudCanvas.transform.Find("AmmoText")?.GetComponent<Text>();
            }

            if (localHealthText == null || localCoinsText == null || localAmmoText == null)
            {
                Debug.LogError("HUD элементы не найдены! Создайте Canvas с тегами в сцене.");
            }
        }
        else
        {
            if (worldUIPrefab != null)
            {
                GameObject worldUI = Instantiate(worldUIPrefab, transform);
                worldUI.transform.localPosition = worldUIOffset;

                worldUIController = worldUI.GetComponent<WorldUIReferences>();
                if (worldUIController == null)
                {
                    Debug.LogError("WorldUIController component is missing on world UI prefab!", this);
                }

                Canvas canvas = worldUI.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.worldCamera = Camera.main;
                    canvas.sortingOrder = 100;
                }
            }
            else
            {
                Debug.LogError("World UI Prefab is not assigned!", this);
            }
        }
    }

    private void UpdateUI()
    {
        if (photonView.IsMine)
        {
            if (localHealthText != null) localHealthText.text = $"Health: {health}/{maxHealth}";
            if (localCoinsText != null) localCoinsText.text = $"Coins: {coins}";
            if (localAmmoText != null) localAmmoText.text = $"Ammo: {ammo}/{maxAmmo}";
        }
        else if (worldUIController != null)
        {
            worldUIController.UpdateUI(health, maxHealth, coins, ammo, maxAmmo);
            worldUIController.UpdateAmmo(ammo, maxAmmo);
            worldUIController.UpdateCoins(coins);
        }
    }

    public bool TryShoot()
    {
        if (ammo <= 0) return false;

        ammo--;
        UpdateUI();
        return true;
    }

    public void TakeDamage(int damage)
    {
        if (!photonView.IsMine) return;

        health = Mathf.Max(0, health - damage);
        UpdateUI();

        if (health <= 0)
        {
            photonView.RPC("Die", RpcTarget.All);
        }
    }

    public void ResetHealth()
    {
        if (!photonView.IsMine) return;

        health = maxHealth;
        UpdateUI();

        if (photonView.ObservedComponents.Contains(this))
        {
            photonView.RPC("OnHealthUpdated", RpcTarget.Others, health);
        }
    }

    public void ResetCoins()
    {
        if (!photonView.IsMine) return;

        coins = 0; 

        UpdateUI();

        if (photonView.ObservedComponents.Contains(this))
        {
            photonView.RPC("OnCoinsUpdated", RpcTarget.Others, coins);
        }
    }

    [PunRPC]
    private void OnHealthUpdated(int newHealth)
    {
        health = newHealth;
        UpdateUI();
    }

    [PunRPC]
    private void OnAmmoUpdated(int newAmmo)
    {
        ammo = newAmmo;
        UpdateUI();
    }

    [PunRPC]
    private void OnCoinsUpdated(int newCoins)
    {
        coins = newCoins;
        UpdateUI();
    }

    public void AddItem(ItemType itemType, int value)
    {
        if (!photonView.IsMine) return;

        switch (itemType)
        {
            case ItemType.Coin:
                coins += value;
                photonView.RPC("OnCoinsUpdated", RpcTarget.Others, coins); 
                break;
            case ItemType.Health:
                health = Mathf.Min(health + value, maxHealth);
                photonView.RPC("OnHealthUpdated", RpcTarget.Others, health);
                break;
            case ItemType.Ammo:
                ammo = Mathf.Min(ammo + value, maxAmmo);
                photonView.RPC("OnAmmoUpdated", RpcTarget.Others, ammo);
                break;
        }
        UpdateUI();
    }

    private void Update()
    {
        if (!photonView.IsMine && worldUIController != null)
        {
            Transform worldUITransform = worldUIController.transform;
            worldUITransform.rotation = Quaternion.LookRotation(
                worldUITransform.position - Camera.main.transform.position);
        }

        if (photonView.IsMine)
        {
            if (localHealthText != null)
                localHealthText.text = $"Health: {health}/{maxHealth}";
        }
        else if (worldUIController != null)
        {
            worldUIController.UpdateHealth(health, maxHealth);
        }

    }
    public void ResetAmmo()
    {
        if (!photonView.IsMine) return;

        ammo = maxAmmo;
        UpdateUI();

        if (photonView.ObservedComponents.Contains(this))
        {
            photonView.RPC("OnAmmoUpdated", RpcTarget.Others, ammo);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(coins);
            stream.SendNext(health);
            stream.SendNext(ammo);
        }
        else
        {
            coins = (int)stream.ReceiveNext();
            health = (int)stream.ReceiveNext();
            ammo = (int)stream.ReceiveNext();
            UpdateUI();
        }
    }

    private void OnDestroy()
    {
        if (worldUIController != null)
        {
            Destroy(worldUIController.gameObject);
        }
    }



}
