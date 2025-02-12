
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    [Header("Patrulla")]
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 3f;

    [Header("Ajuste de Rotación")]
    public float rotationOffset = -90f;  


    [Header("Persecución")]
    public float chaseSpeed = 2.5f;
    public float detectionRadius = 3f;
    public float fieldOfView = 90f;
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    [Header("Referencia al Jugador")]
    public Transform player;
    private Vector3 initialPosition;
    private Transform currentPatrolTarget;
    private SpriteRenderer spriteRenderer;
    private State currentState = State.Patrol;

    private Vector2 lastDirection = Vector2.up; // Guarda la última dirección del movimiento

    private enum State { Patrol, Chase, Return }

    void Start()
    {
        initialPosition = transform.position;
        currentPatrolTarget = pointB;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null) return;

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                if (CanSeePlayer())
                {
                    currentState = State.Chase;
                    spriteRenderer.color = Color.red;
                }
                break;
            case State.Chase:
                Chase();
                if (!CanSeePlayer())
                {
                    currentState = State.Return;
                    spriteRenderer.color = Color.white;
                }
                if (Vector2.Distance(transform.position, player.position) < 0.5f)
                {
                    Debug.Log("Jugador atrapado");
                    SceneManager.LoadScene("Ending");
                }
                break;
            case State.Return:
                ReturnToInitial();
                if (Vector2.Distance(transform.position, initialPosition) < 0.1f)
                {
                    currentState = State.Patrol;
                    currentPatrolTarget = GetClosestPatrolPoint();
                }
                break;
        }
    }

    void Patrol()
    {
        MoveTowards(currentPatrolTarget.position, patrolSpeed);
        if (Vector2.Distance(transform.position, currentPatrolTarget.position) < 0.1f)
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
    }

    void Chase()
    {
        MoveTowards(player.position, chaseSpeed);
    }

    void ReturnToInitial()
    {
        MoveTowards(initialPosition, patrolSpeed);
    }

    void MoveTowards(Vector3 target, float speed)
    {
        Vector2 moveDir = (target - transform.position).normalized;

        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
    }

    bool CanSeePlayer()
    {
        // Calcula la dirección hacia el jugador
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Usa -transform.right para que la dirección de "frente" sea la misma que la del cono de detección
        float adjustedAngle = Vector2.Angle(-transform.right, directionToPlayer);

        // Si el jugador está fuera del ángulo de visión, no se detecta
        if (adjustedAngle > fieldOfView / 2)
            return false;

        // Comprueba la distancia al jugador
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > detectionRadius)
            return false;

        // Realiza un raycast para ver si hay un obstáculo en el camino
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, detectionRadius, obstacleMask | playerMask);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    private Transform GetClosestPatrolPoint() 
    {
        return Vector2.Distance(transform.position, pointA.position) < Vector2.Distance(transform.position, pointB.position) ? pointA : pointB;
    }

    private void OnDrawGizmos()
    {
        // Dibuja la línea entre los puntos de patrulla si están asignados
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }

        // Dibuja el radio de detección (esfera roja)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Usamos -transform.right para girar el cono 180 grados
        Vector3 forward = -transform.right;

        // Calcula los límites del campo de visión usando el ángulo fieldOfView
        Vector3 leftBoundary = Quaternion.Euler(0, 0, fieldOfView / 2) * forward * detectionRadius;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -fieldOfView / 2) * forward * detectionRadius;

        // Establece un color amarillo transparente para el cono de detección
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 origin = transform.position;

        // Dibuja las líneas que forman el triángulo
        Gizmos.DrawLine(origin, origin + leftBoundary);
        Gizmos.DrawLine(origin, origin + rightBoundary);
        Gizmos.DrawLine(origin + leftBoundary, origin + rightBoundary);
    }
}
