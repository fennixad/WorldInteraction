using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EntityStats))]
public class Nido : MonoBehaviour, IDamageable
{
    public GameObject standardPrefab;
    public GameObject childPrefab;
    public float respawnTime = 10f;
    public int initialSpawn = 5;
    public float spawnRadius = 4f;

    private bool destroyed = false;
    private EntityStats stats;

    private void Awake()
    {
        stats = GetComponent<EntityStats>();
    }

    private void Start()
    {
        for (int i = 0; i < initialSpawn; i++)
            SpawnCreature();

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (!destroyed)
        {
            yield return new WaitForSeconds(respawnTime);
            SpawnCreature();
        }
    }

    void SpawnCreature()
    {
        GameObject prefab = Random.Range(0, 4) == 0 ? childPrefab : standardPrefab;
        Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
        pos.y = transform.position.y;

        Instantiate(prefab, pos, Quaternion.identity);
        // El NPC se autorregistrará solo
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        stats.TakeDamage(amount, attacker);
        Debug.Log("¡Nido atacado! Avisando a todos los enemigos.");

        NPCSpawner.instance?.NotifyAllAggro();

        if (stats.GetHealth() <= 0)
        {
            destroyed = true;
            Destroy(gameObject);
        }
    }
}