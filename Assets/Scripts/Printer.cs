using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Printer : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private DecalProjector mDecalProjector;
    [SerializeField] private string mArrayPropertyName = "_TextureIndices";
    
    [Header("Texture Indices")]
    [SerializeField] private Vector3 mIndicesToPrint = Vector3.zero;
    
    // Cache des références
    private Material _printingMaterial;
    private static int _propertyID; // Cache du property ID
    
    private void Awake()
    {
        if (mDecalProjector == null)
        {
            Debug.LogError($"[Printer] DecalProjector not assigned on {gameObject.name}");
            enabled = false;
            return;
        }
        
        _printingMaterial = new Material(mDecalProjector.material);
        mDecalProjector.material = _printingMaterial;
        
        if (_propertyID == 0)
            _propertyID = Shader.PropertyToID(mArrayPropertyName);
    }
    
    private void Start()
    {
        Print();
    }
    
    /// <summary>
    /// Applique les indices de texture au material
    /// </summary>
    public void Print()
    {
        if (_printingMaterial == null)
        {
            Debug.LogWarning($"[Printer] Material not initialized on {gameObject.name}");
            return;
        }
        
        _printingMaterial.SetVector(_propertyID, mIndicesToPrint);
    }
    
    /// <summary>
    /// Change les indices et applique immédiatement
    /// </summary>
    public void SetAndPrint(int index0, int index1, int index2)
    {
        mIndicesToPrint = new Vector3(index0, index1, index2);
        Print();
    }
    
    /// <summary>
    /// Change les indices et applique immédiatement
    /// </summary>
    public void SetAndPrint(Vector3 indices)
    {
        mIndicesToPrint = indices;
        Print();
    }
    
    /// <summary>
    /// Réinitialise à zéro
    /// </summary>
    [ContextMenu("Reset to Zero")]
    public void ResetIndices()
    {
        SetAndPrint(Vector3.zero);
    }
    
    private void OnDestroy()
    {
        // Nettoyer le material instancié
        if (_printingMaterial != null)
        {
            Destroy(_printingMaterial);
        }
    }

    private void OnApplicationQuit()
    {
        ResetIndices();
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Appliquer automatiquement en mode édition
        if (Application.isPlaying && _printingMaterial != null)
        {
            Print();
        }
    }
#endif
}