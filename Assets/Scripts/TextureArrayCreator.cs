using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TextureArrayCreator : EditorWindow
{
    private List<Texture2D> textures = new List<Texture2D>();
    private string assetPath = "Assets/CreatedTextureArray.asset";
    private Vector2 scrollPosition;

    [MenuItem("Tools/Create Texture Array")]
    public static void ShowWindow()
    {
        GetWindow<TextureArrayCreator>("Texture Array Creator");
    }

    void OnGUI()
    {
        // Drag and drop area for textures
        EditorGUILayout.LabelField("Drag Textures Here (multiple drops allowed):", EditorStyles.boldLabel);
        
        Rect dropArea = EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(100));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Drop textures here", EditorStyles.centeredGreyMiniLabel);
        GUILayout.Label("You can drop multiple times to add more", EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();

        Event evt = Event.current;

        if (dropArea.Contains(evt.mousePosition))
        {
            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                
                // Ajouter les textures sans effacer les précédentes
                foreach (Object obj in DragAndDrop.objectReferences)
                {
                    if (obj is Texture2D texture2D)
                    {
                        // Éviter les doublons
                        if (!textures.Contains(texture2D))
                        {
                            textures.Add(texture2D);
                        }
                    }
                }
                
                evt.Use();
            }
        }

        if (textures != null && textures.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Selected {textures.Count} texture(s):", EditorStyles.boldLabel);
            
            // Scrollview pour afficher la liste des textures
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            for (int i = 0; i < textures.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}. {textures[i].name} ({textures[i].width}x{textures[i].height})");
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    textures.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            assetPath = EditorGUILayout.TextField("Save Path:", assetPath);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Texture Array Asset", GUILayout.Height(30)))
            {
                CreateTextureArray();
            }
            
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                textures.Clear();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    void CreateTextureArray()
    {
        if (textures == null || textures.Count == 0) return;

        // Vérifier que toutes les textures sont lisibles
        foreach (var tex in textures)
        {
            if (tex == null)
            {
                Debug.LogError("One of the textures is null. Please remove it from the list.");
                return;
            }
            
            if (!tex.isReadable)
            {
                Debug.LogError($"Texture '{tex.name}' is not readable. Please enable 'Read/Write Enabled' in import settings.");
                return;
            }
        }

        // Vérifier que toutes les textures ont les mêmes dimensions
        int width = textures[0].width;
        int height = textures[0].height;
        TextureFormat format = textures[0].format;

        foreach (var tex in textures)
        {
            if (tex.width != width || tex.height != height)
            {
                Debug.LogError($"All textures must have the same dimensions. '{tex.name}' is {tex.width}x{tex.height} but expected {width}x{height}.");
                return;
            }
        }

        int sliceCount = textures.Count;

        // Créer le Texture2DArray
        Texture2DArray textureArray = new Texture2DArray(width, height, sliceCount, format, true);

        // Copier les pixels de chaque texture dans le tableau
        for (int i = 0; i < sliceCount; i++)
        {
            Graphics.CopyTexture(textures[i], 0, textureArray, i);
        }

        textureArray.Apply();

        // Sauvegarder l'asset
        AssetDatabase.CreateAsset(textureArray, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully created texture array at '{assetPath}' with {sliceCount} texture(s).");
        
        // Sélectionner l'asset créé
        Selection.activeObject = textureArray;
        EditorGUIUtility.PingObject(textureArray);
    }
}