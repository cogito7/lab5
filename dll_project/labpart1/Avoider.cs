using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace labpart1
{
    



    [RequireComponent(typeof(NavMeshAgent))]
    public class Avoider : MonoBehaviour
    {
        [Header("References")]
        public Transform avoidee;           // Object to avoid (drag player here in Inspector)

        [Header("Settings")]
        public float avoidRange = 10f;      // Distance at which Avoider reacts
        public float moveSpeed = 3.5f;      // NavMeshAgent speed
        public bool showGizmos = true;      // Show debug gizmos

        [Header("Poisson Sampling")]
        public float samplingRadius = 2f;   // Minimum distance between sample points
        public int maxSamplePoints = 30;    // Maximum points to generate
        public float sampleAreaRadius = 8f; // Radius around avoider to sample points

        private NavMeshAgent agent;
        private Vector3 lastAvoideePosition;
        private List<Vector3> validEscapePoints = new List<Vector3>();

        void Start()
        {
            // Check for NavMeshAgent
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError("Avoider: This GameObject needs a NavMeshAgent component!");
                return;
            }

            // Check if NavMesh is baked
            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            {
                Debug.LogWarning("Avoider: No NavMesh found! Please bake a NavMesh for this scene.");
            }

            // Configure agent
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0.1f;

            // Check for avoidee reference
            if (avoidee == null)
            {
                Debug.LogWarning("Avoider: Please assign an object to avoid in the Inspector!");
            }

            lastAvoideePosition = avoidee != null ? avoidee.position : Vector3.zero;
        }

        void Update()
        {
            if (avoidee == null) return;

            // Always look at the avoidee or Player (maintain eye contact)
            Vector3 directionToAvoidee = avoidee.position - transform.position;
            directionToAvoidee.y = 0; // Stay on horizontal plane
            if (directionToAvoidee.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(directionToAvoidee);
            }

            float distanceToAvoidee = Vector3.Distance(transform.position, avoidee.position);
            bool avoiderSeesAvoidee = IsPointVisibleFromAvoidee(transform.position);

            // If avoidee is in range, find escape point
            if (distanceToAvoidee < avoidRange && avoiderSeesAvoidee)
            {
                // Only recalculate if avoidee moved significantly or we don't have valid points
                if (Vector3.Distance(avoidee.position, lastAvoideePosition) > 1f || validEscapePoints.Count == 0)
                {
                    FindValidEscapePoints();
                    lastAvoideePosition = avoidee.position;

                    // Move to closest valid escape point
                    if (validEscapePoints.Count > 0)
                    {
                        Vector3 closestPoint = GetClosestEscapePoint();
                        agent.SetDestination(closestPoint);
                    }
                }
            }
        }

        void FindValidEscapePoints()
        {
            validEscapePoints.Clear();
            List<Vector3> poissonPoints = GeneratePoissonDiscPoints();

            foreach (Vector3 point in poissonPoints)
            {
                // Check if point is on NavMesh
                if (NavMesh.SamplePosition(point, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                {
                    // Check if point is NOT visible from avoidee (line of sight check)
                    if (!IsPointVisibleFromAvoidee(hit.position))
                    {
                        validEscapePoints.Add(hit.position);
                    }
                }
            }
        }

        List<Vector3> GeneratePoissonDiscPoints()
        {
            List<Vector3> points = new List<Vector3>();
            List<Vector3> activeList = new List<Vector3>();

            // Start with a random point around the avoider
            Vector3 firstPoint = transform.position + Random.insideUnitSphere * sampleAreaRadius;
            firstPoint.y = transform.position.y; // Keep on same Y level

            points.Add(firstPoint);
            activeList.Add(firstPoint);

            while (activeList.Count > 0 && points.Count < maxSamplePoints)
            {
                int randomIndex = Random.Range(0, activeList.Count);
                Vector3 currentPoint = activeList[randomIndex];
                bool foundValidPoint = false;

                // Try to generate new points around current point
                for (int i = 0; i < 30; i++) // Max attempts per point
                {
                    float angle = Random.Range(0f, 2f * Mathf.PI);
                    float radius = Random.Range(samplingRadius, 2f * samplingRadius);

                    Vector3 newPoint = currentPoint + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    );

                    // Check if point is within sample area and far enough from existing points
                    if (Vector3.Distance(newPoint, transform.position) <= sampleAreaRadius &&
                        IsPointValidDistance(newPoint, points))
                    {
                        points.Add(newPoint);
                        activeList.Add(newPoint);
                        foundValidPoint = true;
                        break;
                    }
                }

                if (!foundValidPoint)
                {
                    activeList.RemoveAt(randomIndex);
                }
            }

            return points;
        }

        bool IsPointValidDistance(Vector3 newPoint, List<Vector3> existingPoints)
        {
            foreach (Vector3 point in existingPoints)
            {
                if (Vector3.Distance(newPoint, point) < samplingRadius)
                {
                    return false;
                }
            }
            return true;
        }

        bool IsPointVisibleFromAvoidee(Vector3 point)
        {
            Vector3 directionToPoint = point - avoidee.position;
            Vector3 avoideeEyeLevel = avoidee.position + Vector3.up * 1.5f; // Eye level height
            Vector3 pointEyeLevel = point + Vector3.up * 0.5f; // Slightly above ground

            // Raycast from avoidee to point
            int raycast_mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            if (Physics.Raycast(avoideeEyeLevel, (pointEyeLevel - avoideeEyeLevel).normalized,
                out RaycastHit hit, directionToPoint.magnitude, raycast_mask))
            {
                // If ray hits something before reaching the point, point is hidden
                return Vector3.Distance(hit.point, pointEyeLevel) < 1f;
            }

            // If ray doesn't hit anything, point is visible
            return true;
        }

        Vector3 GetClosestEscapePoint()
        {
            Vector3 closestPoint = validEscapePoints[0];
            float closestDistance = Vector3.Distance(transform.position, closestPoint);

            foreach (Vector3 point in validEscapePoints)
            {
                float distance = Vector3.Distance(transform.position, point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = point;
                }
            }

            return closestPoint;
        }

        void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // Draw avoid range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, avoidRange);

            // Draw sampling area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sampleAreaRadius);

            // Draw valid escape points
            Gizmos.color = Color.green;
            foreach (Vector3 point in validEscapePoints)
            {
                Gizmos.DrawWireSphere(point, 1f);
                Gizmos.DrawLine(transform.position, point);
            }

            // Draw path to destination
            if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(transform.position, agent.destination);
                Gizmos.DrawWireSphere(agent.destination, 1f);
            }

            // Draw line of sight to avoidee
            if (avoidee != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position + Vector3.up, avoidee.position + Vector3.up);
            }
        }
    }
}
