using UnityEngine;

public class sheet : MonoBehaviour
{
    [Header("Mesh")]
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] vertices;
    private Vector3[] velocities;
    
    [Header("Physics")]
    [SerializeField] private float stiffness = 50f;
    [SerializeField] private float damping = 0.9f;
    [SerializeField] private float mass = 0.4f;
    
    [Header("Corners")]
    public Transform[] cornerTargets;
    private int[] cornerIndices;
    
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        
        originalVertices = mesh.vertices;
        vertices = new Vector3[originalVertices.Length];
        velocities = new Vector3[originalVertices.Length];
        
        System.Array.Copy(originalVertices, vertices, originalVertices.Length);
        
        cornerIndices = FindCornerVertices();
        CreateCornerTargets();
    }
    
    void FixedUpdate()
    {
        // Fixer les coins aux targets
        for (int i = 0; i < cornerIndices.Length; i++)
        {
            int idx = cornerIndices[i];
            vertices[idx] = transform.InverseTransformPoint(cornerTargets[i].position);
            velocities[idx] = Vector3.zero;
        }
        
        // Simulation masse-ressort pour les autres vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            if (IsCorner(i)) continue;
            
            Vector3 force = Vector3.zero;
            
            // Force de ressort vers les voisins
            int[] neighbors = GetNeighbors(i);
            foreach (int neighborIdx in neighbors)
            {
                Vector3 delta = vertices[neighborIdx] - vertices[i];
                float restLength = Vector3.Distance(originalVertices[i], originalVertices[neighborIdx]);
                float currentLength = delta.magnitude;
                
                if (currentLength > 0.001f)
                {
                    Vector3 springForce = delta.normalized * (currentLength - restLength) * stiffness;
                    force += springForce;
                }
            }
            
            // Intégration
            Vector3 acceleration = force / mass;
            velocities[i] += acceleration * Time.fixedDeltaTime;
            velocities[i] *= damping;
            vertices[i] += velocities[i] * Time.fixedDeltaTime;
        }
        
        // Appliquer au mesh
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
    
    int[] GetNeighbors(int vertexIndex)
    {
        // Pour un mesh en grille, retourner les 4 voisins (haut, bas, gauche, droite)
        // Cette fonction dépend de la topologie de ton mesh
        // Exemple simplifié pour une grille carrée :
        
        int gridWidth = Mathf.RoundToInt(Mathf.Sqrt(vertices.Length));
        int x = vertexIndex % gridWidth;
        int y = vertexIndex / gridWidth;
        
        System.Collections.Generic.List<int> neighbors = new System.Collections.Generic.List<int>();
        
        if (x > 0) neighbors.Add(vertexIndex - 1); // gauche
        if (x < gridWidth - 1) neighbors.Add(vertexIndex + 1); // droite
        if (y > 0) neighbors.Add(vertexIndex - gridWidth); // bas
        if (y < gridWidth - 1) neighbors.Add(vertexIndex + gridWidth); // haut
        
        return neighbors.ToArray();
    }
    
    bool IsCorner(int index)
    {
        foreach (int corner in cornerIndices)
            if (corner == index) return true;
        return false;
    }
    
    void CreateCornerTargets()
    {
        if (cornerTargets == null || cornerTargets.Length == 0)
        {
            cornerTargets = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject go = new GameObject($"Corner_{i}");
                go.transform.parent = transform;
                go.transform.localPosition = originalVertices[cornerIndices[i]];
                cornerTargets[i] = go.transform;
            }
        }
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
}