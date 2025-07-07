using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Configuraci�n")]
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

        // Asegurarse de que el NavMeshAgent est� habilitado y no est� parado al inicio
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
    }

    protected virtual void Start()
    {
        // Buscar el PlayerManager y obtener el jugador
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
            Debug.LogWarning(gameObject.name + ": PlayerManager.Instance no encontrado. Aseg�rate de que PlayerManager est� en la escena y configurado.");
        }

        // Registrarse en el NPCSpawner si existe
        if (NPCSpawner.instance != null)
            NPCSpawner.instance.RegisterNPC(this);

        // Encontrar todos los puntos de patrulla con el tag "PointOfGo"
        GameObject[] tempPoints = GameObject.FindGameObjectsWithTag("PointOfGo");
        if (tempPoints.Length > 0)
        {
            pointsOfGo = new List<GameObject>(tempPoints);
            // Opcional: Ordenar los puntos por nombre para una patrulla predecible (ej. Point_0, Point_1, etc.)
            pointsOfGo.Sort((a, b) => string.Compare(a.name, b.name));

            Debug.Log("<color=green>" + gameObject.name + ": Encontrados y ordenados " + pointsOfGo.Count + " puntos de patrulla.</color>");

            // Iniciar la patrulla. Se mover� al primer punto (�ndice 0).
            StartPatrol();
        }
        else
        {
            Debug.LogWarning("<color=red>" + gameObject.name + ": �ERROR! No se encontraron objetos con el tag 'PointOfGo'. La patrulla no funcionar�.</color>");
            isPatrolling = false; // Asegurar que no intenta patrullar
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
                Debug.Log("<color=orange>" + gameObject.name + ": Aggro activado, deteniendo patrulla y persiguiendo.</color>");
            }

            float dist = Vector3.Distance(transform.position, player.position);

            if (dist > attackRange)
            {
                if (agent.isActiveAndEnabled && !agent.isStopped) 
                {
                    agent.SetDestination(player.position);
                }
            }
            else 
            {
                if (agent.isActiveAndEnabled) agent.isStopped = true; 
                Vector3 lookPos = player.position;
                lookPos.y = transform.position.y;
                transform.LookAt(lookPos);
                TryAttack();

            }
        }

        else
        {
            if (pointsOfGo != null && pointsOfGo.Count > 0 && !isPatrolling && !isWaitingAtPoint)
            {
                StartPatrol();
            }

            if (isPatrolling && !isWaitingAtPoint)
            {
                // Verificar si el agente ha llegado a su destino actual
                // Usamos agent.remainingDistance y agent.pathStatus para mayor fiabilidad
                if (agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance + patrolTolerance && !agent.pathPending)
                {
                    Debug.Log(gameObject.name + ": <color=blue>Lleg� cerca del punto " + pointsOfGo[currentPointIndex].name + ". RemainingDistance: " + agent.remainingDistance + ". Iniciando espera.</color>");
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
            Debug.Log(gameObject.name + ": <color=red>ATAC� al jugador. Da�o: " + damage + "</color>");
        }
    }

    public virtual void ActivateAggro()
    {
        aggro = true;
        if (player != null && agent != null && agent.isActiveAndEnabled)
        {
            StopAllCoroutines(); // Asegurarse de detener cualquier patrulla activa
            isPatrolling = false;
            isWaitingAtPoint = false;
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        Debug.Log("<color=red>" + gameObject.name + ": �Aggro activado!</color>");
    }

    // --- M�todos de Patrulla ---
    private void StartPatrol()
    {
        if (pointsOfGo == null || pointsOfGo.Count == 0) return;

        isPatrolling = true;
        isWaitingAtPoint = false;
        Debug.Log(gameObject.name + ": <color=green>Iniciando patrulla.</color>");

        // Mueve al primer punto (o el siguiente si ya estaba en uno)
        MoveToNextPatrolPoint();
    }

    private void MoveToNextPatrolPoint()
    {
        if (pointsOfGo.Count == 0)
        {
            Debug.LogWarning(gameObject.name + ": No hay puntos de patrulla definidos. Deteniendo patrulla.");
            isPatrolling = false;
            return;
        }

        // Actualiza el �ndice ANTES de establecer el destino
        currentPointIndex = GetNextPatrolPointIndex();

        if (currentPointIndex != -1 && pointsOfGo[currentPointIndex] != null)
        {
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = false;
                agent.SetDestination(pointsOfGo[currentPointIndex].transform.position);
                Debug.Log(gameObject.name + ": <color=cyan>Movi�ndose al punto de patrulla: " + pointsOfGo[currentPointIndex].name + " (�ndice: " + currentPointIndex + ").</color>");
            }
            else
            {
                Debug.LogWarning(gameObject.name + ": NavMeshAgent no est� activo/habilitado. No puede moverse.");
                isPatrolling = false;
            }
        }
        else
        {
            Debug.LogWarning(gameObject.name + ": <color=orange>El punto de patrulla en el �ndice " + currentPointIndex + " es nulo o la lista est� vac�a despu�s de la actualizaci�n. Deteniendo patrulla.</color>");
            isPatrolling = false; // Si hay un problema con un punto, detener la patrulla
        }
    }

    // L�gica para obtener el siguiente �ndice de forma secuencial
    private int GetNextPatrolPointIndex()
    {
        if (pointsOfGo.Count == 0) return -1;

        // Si es la primera vez que se llama, inicia en el punto 0.
        // Si no, avanza al siguiente punto.
        int nextIndex = (currentPointIndex + 1) % pointsOfGo.Count;

        // La l�nea de abajo era la que te di antes, es equivalente a la de arriba y est� bien.
        // int nextIndex = currentPointIndex + 1;
        // if (nextIndex >= pointsOfGo.Count) { nextIndex = 0; }

        return nextIndex;
    }

    private IEnumerator WaitAtPointCoroutine()
    {
        isWaitingAtPoint = true;
        if (agent.isActiveAndEnabled) agent.isStopped = true; // Detener el agente
        Debug.Log(gameObject.name + ": <color=blue>Lleg� al punto " + pointsOfGo[currentPointIndex].name + ". Esperando " + patrolWaitTime + " segundos.</color>");

        yield return new WaitForSeconds(patrolWaitTime);

        isWaitingAtPoint = false;
        Debug.Log(gameObject.name + ": <color=blue>Termin� la espera en el punto " + pointsOfGo[currentPointIndex].name + ". Movi�ndose al siguiente.</color>");
        MoveToNextPatrolPoint(); // Moverse al siguiente punto despu�s de la espera
    }

    // Para depuraci�n visual en el editor
    void OnDrawGizmosSelected()
    {
        if (pointsOfGo != null && pointsOfGo.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < pointsOfGo.Count; i++)
            {
                if (pointsOfGo[i] != null)
                {
                    Gizmos.DrawSphere(pointsOfGo[i].transform.position, 0.5f); // Dibuja una esfera en cada punto
                    // Dibuja l�neas entre los puntos para visualizar la ruta
                    if (i < pointsOfGo.Count - 1 && pointsOfGo[i + 1] != null)
                    {
                        Gizmos.DrawLine(pointsOfGo[i].transform.position, pointsOfGo[i + 1].transform.position);
                    }
                }
            }
            // Conecta el �ltimo punto con el primero para un ciclo completo
            if (pointsOfGo.Count > 1 && pointsOfGo[0] != null && pointsOfGo[pointsOfGo.Count - 1] != null)
            {
                Gizmos.DrawLine(pointsOfGo[pointsOfGo.Count - 1].transform.position, pointsOfGo[0].transform.position);
            }
        }

        // Dibuja la ruta actual del NavMeshAgent (amarillo)
        if (agent != null && agent.isActiveAndEnabled && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] pathCorners = agent.path.corners;
            for (int i = 0; i < pathCorners.Length - 1; i++)
            {
                Gizmos.DrawLine(pathCorners[i], pathCorners[i + 1]);
            }
        }
    }
}