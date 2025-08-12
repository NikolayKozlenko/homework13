using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldUIReferences : MonoBehaviour
{
    [SerializeField] private Text healthText;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text ammoText;

    public void UpdateUI(int health, int maxHealth, int coins, int ammo, int maxAmmo)
    {
        if (healthText != null)
            healthText.text = $"Health: {health}/{maxHealth}";
        if (coinsText != null)
            coinsText.text = $"Coins: {coins}";
        if (ammoText != null)
            ammoText.text = $"Ammo: {ammo}/{maxAmmo}";
    }
    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null)
            healthText.text = $"Health: {currentHealth}/{maxHealth}";
    }

    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null)
            ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
    }

    public void UpdateCoins(int currentCoins)
    {
        if (coinsText != null)
            coinsText.text = $"Coins: {currentCoins}";
    }
}
