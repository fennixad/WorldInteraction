using UnityEngine;

public class ChildCreature : EnemyBase
{
    public GameObject motherPrefab;

    private void Start()
    {
        base.Start();

        var stats = GetComponent<EntityStats>();
        if (stats != null)
        {
            stats.OnDeath += OnDeathByPlayer;
        }
    }

    private void OnDeathByPlayer(GameObject killer)
    {
        SummonMother();
    }

    private void SummonMother()
    {
        GameObject mother = Instantiate(motherPrefab, transform.position, Quaternion.identity);
        var ai = mother.GetComponent<StandardCreature>();
        if (ai != null)
        {
            ai.ActivateAggro();
        }
    }
}