using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;

    private Transform player;
    private EntityStats playerStats;
    private NavMeshAgent agent;
    private float lastAttackTime;

    private Animator anim;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        player = PlayerManager.Instance.PlayerTransform;
        playerStats = player.GetComponent<EntityStats>();
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= detectionRange)
        {
            agent.SetDestination(player.position);

            if (dist <= attackRange)
            {
                TryAttack();
            }

            if (anim != null)
            {
                anim.SetBool("Moving", dist > attackRange);
                anim.SetBool("Attacking", dist <= attackRange);
            }
        }
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            playerStats.TakeDamage(attackDamage, gameObject);
            lastAttackTime = Time.time;

            if (anim != null)
                anim.SetTrigger("AttackTrigger");
        }
    }
}
