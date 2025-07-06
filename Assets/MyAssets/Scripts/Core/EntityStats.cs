
using System;
using UnityEngine;

public class EntityStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth = 100f;
    private float currentHealth;
    public bool isPlayer;

    public event Action<GameObject> OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        isPlayer = CompareTag("Player");
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} recibió {amount} de daño de {attacker.name}. Vida restante: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Die(attacker);
        }
    }

    private void Die(GameObject killer)
    {
        Debug.Log($"{gameObject.name} murió a manos de {killer.name}");

        OnDeath?.Invoke(killer);

        if (!isPlayer)
        {
            Destroy(gameObject, 2f);
        }
        else
        {
            // lógica para muerte de jugador
        }
    }

    public float GetHealth() => currentHealth;
}