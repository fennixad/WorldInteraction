using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner instance;

    public GameObject npcPrefab;
    public float spawnDistance = 20f;
    public int maxNPCs = 5;
    public float spawnInterval = 5f;

    private float spawnTimer = 0f;
    private static int currentGlobalNPCs = 0;

    private List<EnemyBase> allEnemies = new();

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;

        if (PlayerManager.Instance == null) return;

        float distance = Vector3.Distance(PlayerManager.Instance.transform.position, transform.position);

        if (spawnTimer >= spawnInterval && distance < spawnDistance && currentGlobalNPCs < maxNPCs)
        {
            SpawnNPC();
            spawnTimer = 0f;
        }
    }

    void SpawnNPC()
    {
        Vector3 spawnPos = transform.position + Random.insideUnitSphere * 2f;
        spawnPos.y = transform.position.y;

        GameObject newNPC = Instantiate(npcPrefab, spawnPos, Quaternion.identity);
        RegisterNPC(newNPC.GetComponent<EnemyBase>());
        currentGlobalNPCs++;
    }

    public void RegisterNPC(EnemyBase npc)
    {
        if (npc != null && !allEnemies.Contains(npc))
        {
            allEnemies.Add(npc);
        }
    }

    public void NotifyAllAggro()
    {
        foreach (var npc in allEnemies)
        {
            if (npc != null)
                npc.ActivateAggro();
        }
    }

    public void NPCDiedGlobal()
    {
        currentGlobalNPCs = Mathf.Max(0, currentGlobalNPCs - 1);
    }
}
