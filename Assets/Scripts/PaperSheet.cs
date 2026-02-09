using UnityEngine;
using System.Collections.Generic;

public class PaperSheet : MonoBehaviour
{
    [Header("Mesh")]
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] vertices;
    private Vector3[] velocities;
    private int[] triangles;
    
    [Header("Physics Parameters")]
    [SerializeField] private float structuralStiffness = 80f;  // Résistance à l'étirement
    [SerializeField] private float shearStiffness = 20f;       // Résistance au cisaillement (diagonales)
    [SerializeField] private float bendingStiffness = 10f;     // Résistance au pliage
    [SerializeField] private float damping = 0.95f;
    [SerializeField] private Vector3 gravity = new Vector3(0, -2f, 0);
    
    [Header("Constraints")]
    [SerializeField] private int solverIterations = 3;  // Plus = plus stable mais plus lent
    [SerializeField] private float maxStretch = 1.2f;   // Limite d'étirement (1.0 = pas d'étirement)
    
    [Header("Corners")]
    private Transform[] controlPoints;
    private int[] controlPointsIndices;
    
    // Structure pour optimiser la recherche de voisins
    private class VertexConnection
    {
        public List<int> structuralNeighbors = new List<int>();  // Voisins directs (grille)
        public List<int> shearNeighbors = new List<int>();       // Voisins diagonaux
        public List<int> bendingNeighbors = new List<int>();     // Voisins à 2 de distance
        public List<float> structuralRestLengths = new List<float>();
        public List<float> shearRestLengths = new List<float>();
        public List<float> bendingRestLengths = new List<float>();
    }
    
    private VertexConnection[] connections;
    
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = Instantiate(meshFilter.sharedMesh); // Créer une copie pour ne pas modifier l'original
        meshFilter.mesh = mesh;
        
        originalVertices = mesh.vertices;
        triangles = mesh.triangles;
        vertices = new Vector3[originalVertices.Length];
        velocities = new Vector3[originalVertices.Length];
        
        System.Array.Copy(originalVertices, vertices, originalVertices.Length);
        
        controlPointsIndices = FindControlPointsIndices();
        CreateControlTargets();
        BuildConnections();
    }
    
    void BuildConnections()
    {
        connections = new VertexConnection[vertices.Length];
        
        for (int i = 0; i < vertices.Length; i++)
        {
            connections[i] = new VertexConnection();
        }
        
        // Construire les connexions à partir des triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];
            
            // Connexions structurelles (edges des triangles)
            AddConnection(v0, v1, true);
            AddConnection(v1, v2, true);
            AddConnection(v2, v0, true);
        }
        
        // Détecter les connexions de cisaillement et de pliage
        BuildShearAndBendingConnections();
    }
    
    void AddConnection(int v1, int v2, bool structural)
    {
        if (structural)
        {
            if (!connections[v1].structuralNeighbors.Contains(v2))
            {
                connections[v1].structuralNeighbors.Add(v2);
                connections[v1].structuralRestLengths.Add(
                    Vector3.Distance(originalVertices[v1], originalVertices[v2])
                );
            }
            
            if (!connections[v2].structuralNeighbors.Contains(v1))
            {
                connections[v2].structuralNeighbors.Add(v1);
                connections[v2].structuralRestLengths.Add(
                    Vector3.Distance(originalVertices[v2], originalVertices[v1])
                );
            }
        }
    }
    
    void BuildShearAndBendingConnections()
    {
        // Pour chaque vertex, trouver les voisins à distance 2 (bending) et diagonaux (shear)
        for (int i = 0; i < vertices.Length; i++)
        {
            HashSet<int> neighbors1 = new HashSet<int>(connections[i].structuralNeighbors);
            HashSet<int> neighbors2 = new HashSet<int>();
            
            // Voisins à distance 2 pour le bending
            foreach (int n1 in neighbors1)
            {
                foreach (int n2 in connections[n1].structuralNeighbors)
                {
                    if (n2 != i && !neighbors1.Contains(n2))
                    {
                        neighbors2.Add(n2);
                    }
                }
            }
            
            // Détecter les diagonales (shear) vs bending
            foreach (int n in neighbors2)
            {
                float dist = Vector3.Distance(originalVertices[i], originalVertices[n]);
                
                // Si la distance est proche de sqrt(2) * edge length, c'est une diagonale
                bool isShear = false;
                foreach (float restLength in connections[i].structuralRestLengths)
                {
                    if (Mathf.Abs(dist - restLength * 1.414f) < 0.1f)
                    {
                        isShear = true;
                        break;
                    }
                }
                
                if (isShear)
                {
                    connections[i].shearNeighbors.Add(n);
                    connections[i].shearRestLengths.Add(dist);
                }
                else
                {
                    connections[i].bendingNeighbors.Add(n);
                    connections[i].bendingRestLengths.Add(dist);
                }
            }
        }
    }
    
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        
        // Appliquer les forces
        ApplyForces(dt);
        
        // Contraintes de distance (Position Based Dynamics)
        for (int iter = 0; iter < solverIterations; iter++)
        {
            SolveConstraints();
        }
        
        // Fixer les coins
        FixControlPoints();
        
        // Appliquer au mesh
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    
    void ApplyForces(float dt)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            if (IsControlPoint(i)) continue;
            
            // Gravité
            velocities[i] += gravity * dt;
            
            
            // Damping
            velocities[i] *= damping;
            
            // Intégration de Verlet
            vertices[i] += velocities[i] * dt;
        }
    }
    
    void SolveConstraints()
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            if (IsControlPoint(i)) continue;
            
            Vector3 correction = Vector3.zero;
            int correctionCount = 0;
            
            // Contraintes structurelles
            for (int j = 0; j < connections[i].structuralNeighbors.Count; j++)
            {
                int neighborIdx = connections[i].structuralNeighbors[j];
                float restLength = connections[i].structuralRestLengths[j];
                
                Vector3 delta = vertices[neighborIdx] - vertices[i];
                float currentLength = delta.magnitude;
                
                if (currentLength > 0.0001f)
                {
                    float error = (currentLength - restLength) / currentLength;
                    
                    // Limiter l'étirement
                    if (currentLength > restLength * maxStretch)
                    {
                        error = (currentLength - restLength * maxStretch) / currentLength;
                    }
                    
                    correction += delta * error * structuralStiffness * 0.5f;
                    correctionCount++;
                }
            }
            
            // Contraintes de cisaillement
            for (int j = 0; j < connections[i].shearNeighbors.Count; j++)
            {
                int neighborIdx = connections[i].shearNeighbors[j];
                float restLength = connections[i].shearRestLengths[j];
                
                Vector3 delta = vertices[neighborIdx] - vertices[i];
                float currentLength = delta.magnitude;
                
                if (currentLength > 0.0001f)
                {
                    float error = (currentLength - restLength) / currentLength;
                    correction += delta * error * shearStiffness * 0.5f;
                    correctionCount++;
                }
            }
            
            // Contraintes de pliage
            for (int j = 0; j < connections[i].bendingNeighbors.Count; j++)
            {
                int neighborIdx = connections[i].bendingNeighbors[j];
                float restLength = connections[i].bendingRestLengths[j];
                
                Vector3 delta = vertices[neighborIdx] - vertices[i];
                float currentLength = delta.magnitude;
                
                if (currentLength > 0.0001f)
                {
                    float error = (currentLength - restLength) / currentLength;
                    correction += delta * error * bendingStiffness * 0.5f;
                    correctionCount++;
                }
            }
            
            if (correctionCount > 0)
            {
                correction /= correctionCount;
                vertices[i] += correction / solverIterations;
            }
        }
    }
    
    void FixControlPoints()
    {
        for (int i = 0; i < controlPointsIndices.Length; i++)
        {
            int idx = controlPointsIndices[i];
            Vector3 targetPos = transform.InverseTransformPoint(controlPoints[i].position);
            
            // Calculer la vélocité implicite pour un mouvement smooth
            velocities[idx] = (targetPos - vertices[idx]) / Time.fixedDeltaTime;
            vertices[idx] = targetPos;
        }
    }
    
    bool IsControlPoint(int index)
    {
        foreach (int controlPoint in controlPointsIndices)
            if (controlPoint == index) return true;
        return false;
    }
    
    void CreateControlTargets()
    {
        if (controlPoints == null || controlPoints.Length == 0)
        {
            controlPoints = new Transform[5];
            for (int i = 0; i < 5; i++)
            {
                GameObject go = new GameObject($"ControlTarget_{i}");
                go.transform.parent = transform;
                go.transform.localPosition = originalVertices[controlPointsIndices[i]];
                controlPoints[i] = go.transform;
            }
        }
    }
    
    int FindCenterVertex()
    {
        // Calculer le centre du mesh
        Vector3 center = Vector3.zero;
        foreach (Vector3 v in originalVertices)
        {
            center += v;
        }
        center /= originalVertices.Length;
        
        // Trouver le vertex le plus proche du centre
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float distance = Vector3.Distance(originalVertices[i], center);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    int[] FindCornerVertices()
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        
        foreach (Vector3 v in originalVertices)
        {
            if (v.x < minX) minX = v.x;
            if (v.x > maxX) maxX = v.x;
            if (v.z < minZ) minZ = v.z;
            if (v.z > maxZ) maxZ = v.z;
        }
        
        int[] corners = new int[4];
        float threshold = 0.01f;
        
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];
            
            if (Mathf.Abs(v.x - minX) < threshold && Mathf.Abs(v.z - minZ) < threshold)
                corners[0] = i;
            else if (Mathf.Abs(v.x - maxX) < threshold && Mathf.Abs(v.z - minZ) < threshold)
                corners[1] = i;
            else if (Mathf.Abs(v.x - minX) < threshold && Mathf.Abs(v.z - maxZ) < threshold)
                corners[2] = i;
            else if (Mathf.Abs(v.x - maxX) < threshold && Mathf.Abs(v.z - maxZ) < threshold)
                corners[3] = i;
        }
        
        return corners;
    }

    int[] FindControlPointsIndices()
    {
        int[] indices = new int[5];
        
        int[] corners = FindCornerVertices();
        int centerIndex = FindCenterVertex();

        for (int i = 0; i < 4; i++)
        {
            indices[i] = corners[i];
        }
        
        indices[4] = centerIndex;
        
        return indices;
    }
    
    // Gizmos pour débugger
    void OnDrawGizmos()
    {
        if (vertices == null || !Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        for (int i = 0; i < controlPointsIndices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(vertices[controlPointsIndices[i]]);
            Gizmos.DrawWireSphere(worldPos, 0.2f);
        }
    }
}
