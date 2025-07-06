using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMesh), typeof(Animator))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Configuración")]
    public float attackRange = 2f;
    public float damage = 20f;
    public float attackCooldown = 1.5f;

    protected Transform player;
    protected EntityStats playerStats;
    protected Animator anim;
    protected float lastAttackTime;
    protected NavMeshAgent agent;

    protected bool aggro = false;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        if (PlayerManager.Instance != null)
        {
            player = PlayerManager.Instance.transform;
            playerStats = player.GetComponent<EntityStats>();
        }
        if (NPCSpawner.instance != null)
            NPCSpawner.instance.RegisterNPC(this);
    }

    protected virtual void Update()
    {
        if (!aggro || player == null || playerStats == null)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
            TryAttack();
        }
    }

    protected virtual void TryAttack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            playerStats.TakeDamage(damage, gameObject);
            lastAttackTime = Time.time;
            // if (anim != null) anim.SetTrigger("AttackTrigger");
        }
    }

    public virtual void ActivateAggro()
    {
        aggro = true;
    }
}