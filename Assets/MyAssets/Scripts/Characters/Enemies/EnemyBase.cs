using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Configuración")]
    public float attackRange = 2f;
    public float damage = 20f;
    public float attackCooldown = 1.5f;

    [Header("Patrulla")]
    public List<GameObject> pointsOfGo;
    public float patrolWaitTime = 2f;
    public float patrolTolerance = 0.5f;

    protected Transform player;
    protected EntityStats playerStats;
    protected Animator anim;
    protected float lastAttackTime;
    protected NavMeshAgent agent;

    protected bool aggro = false;

    // --- Variables para la Patrulla ---
    private int currentPointIndex = -1;
    private bool isPatrolling = false;
    private bool isWaitingAtPoint = false;
    // ----------------------------------

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    protected virtual void Start()
    {
        if (PlayerManager.Instance != null)
        {
            player = PlayerManager.Instance.transform;
            playerStats = player.GetComponent<EntityStats>();
            if (playerStats == null)
            {
                Debug.LogWarning(gameObject.name + ": El jugador ('" + player.name + "') no tiene un componente EntityStats.");
            }
        }
        else
        {
            Debug.LogWarning(gameObject.name + ": PlayerManager.Instance no encontrado. Asegúrate de que PlayerManager esté en la escena y configurado.");
        }

        if (NPCSpawner.instance != null)
            NPCSpawner.instance.RegisterNPC(this);

        GameObject[] tempPoints = GameObject.FindGameObjectsWithTag("PointOfGo");
        if (tempPoints.Length > 0)
        {
            pointsOfGo = new List<GameObject>(tempPoints);
            // Aquí puedes descomentar si quieres ordenar los puntos por nombre
            // pointsOfGo.Sort((a, b) => string.Compare(a.name, b.name));
            Debug.Log("<color=green>" + gameObject.name + ": Encontrados y ordenados " + pointsOfGo.Count + " puntos de patrulla.</color>");
            StartPatrol();
        }
        else
        {
            Debug.LogWarning("<color=red>" + gameObject.name + ": ¡ERROR! No se encontraron objetos con el tag 'PointOfGo'. La patrulla no funcionará.</color>");
            isPatrolling = false;
        }
    }

    protected virtual void Update()
    {
        if (aggro && player != null && playerStats != null)
        {
            if (isPatrolling)
            {
                StopAllCoroutines();
                isPatrolling = false;
                isWaitingAtPoint = false;
                if (agent.isActiveAndEnabled) agent.isStopped = false;
            }

            float dist = Vector3.Distance(transform.position, player.position);

            if (dist > attackRange)
            {
                if (agent.isActiveAndEnabled)
                {
                    if (agent.isStopped) // Si está parado, reanudar
                    {
                        agent.isStopped = false;
                        Debug.Log(gameObject.name + ": Reanudando persecución del jugador.");
                    }
                    agent.SetDestination(player.position);
                }
            }
            else // Dentro del rango de ataque
            {
                if (agent.isActiveAndEnabled) agent.isStopped = true;
                Vector3 lookPos = player.position;
                lookPos.y = transform.position.y;
                transform.LookAt(lookPos);
                TryAttack();
            }
        }
        else // Si no hay aggro, gestiona la patrulla
        {
            if (pointsOfGo != null && pointsOfGo.Count > 0 && !isPatrolling && !isWaitingAtPoint)
            {
                StartPatrol();
            }

            if (isPatrolling && !isWaitingAtPoint)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance + patrolTolerance && !agent.pathPending)
                {
                    StartCoroutine(WaitAtPointCoroutine());
                }
            }
        }
    }

    protected virtual void TryAttack()
    {
        if (playerStats != null && Time.time - lastAttackTime >= attackCooldown)
        {
            playerStats.TakeDamage(damage, gameObject);
            lastAttackTime = Time.time;
        }
    }

    public virtual void ActivateAggro()
    {
        aggro = true;
        if (player != null && agent != null && agent.isActiveAndEnabled)
        {
            StopAllCoroutines();
            isPatrolling = false;
            isWaitingAtPoint = false;
            agent.isStopped = false; // Asegurarse de que el agente no esté parado al activar aggro
            agent.SetDestination(player.position);
        }
    }

    private void StartPatrol()
    {
        if (pointsOfGo == null || pointsOfGo.Count == 0) return;

        isPatrolling = true;
        isWaitingAtPoint = false;
        MoveToNextPatrolPoint();
    }

    private void MoveToNextPatrolPoint()
    {
        if (pointsOfGo.Count == 0)
        {
            isPatrolling = false;
            return;
        }

        currentPointIndex = GetNextPatrolPointIndex();

        if (currentPointIndex != -1 && pointsOfGo[currentPointIndex] != null)
        {
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = false; // Asegurarse de que el agente no esté parado al moverse a un nuevo punto de patrulla
                agent.SetDestination(pointsOfGo[currentPointIndex].transform.position);
            }
            else
            {
                isPatrolling = false;
            }
        }
        else
        {
            isPatrolling = false;
        }
    }

    private int GetNextPatrolPointIndex()
    {
        if (pointsOfGo.Count == 0) return -1;
        int nextIndex = (currentPointIndex + 1) % pointsOfGo.Count;
        return nextIndex;
    }

    private IEnumerator WaitAtPointCoroutine()
    {
        isWaitingAtPoint = true;
        if (agent.isActiveAndEnabled) agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);

        isWaitingAtPoint = false;
        MoveToNextPatrolPoint();
    }
}