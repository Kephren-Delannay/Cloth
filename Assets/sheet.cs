using UnityEngine;

public class sheet : MonoBehaviour
{

    public GameObject paper;
    public Transform target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        paper.GetComponent<MeshFilter>().sharedMesh.vertices[10] = target.position;
    }
}
