using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
public class SplineControlSystem : MonoBehaviour
{
    [Header("Bone Setup")]
    [SerializeField] private List<Transform> boneRoots = new List<Transform>();
    
    [Header("Spline Settings")]
    [SerializeField] private bool autoUpdateSpline = true;
    [SerializeField] private TangentMode tangentMode = TangentMode.AutoSmooth;
    [SerializeField] private float tangentStrength = 0.33f;
    
    [Header("Runtime Manipulation")]
    [SerializeField] private bool enableRuntimeManipulation = true;
    [SerializeField] private float handleSize = 0.5f;
    [SerializeField] private Sprite handleSprite; // Assign in inspector
    [SerializeField] private Color handleColor = new Color(1f, 1f, 0f, 0.7f);
    [SerializeField] private Color selectedHandleColor = new Color(0f, 1f, 0f, 0.7f);
    [SerializeField] private int sortingOrder = 100; // Higher values render on top
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private bool debugMode = false;
    
    private SplineContainer splineContainer;
    private Spline spline;
    private Dictionary<int, Transform> knotToBoneMap = new Dictionary<int, Transform>();
    private List<GameObject> handleObjects = new List<GameObject>();
    private int selectedHandleIndex = -1;
    private Camera mainCamera;
    private Plane dragPlane; // For 2D dragging
    
    // Store original positions for reset functionality
    private Dictionary<int, Vector3> originalKnotPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, BezierKnot> originalKnotData = new Dictionary<int, BezierKnot>();

    void Start()
    {
        splineContainer = GetComponent<SplineContainer>();
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! Make sure your camera is tagged as MainCamera");
        }
        
        // Create drag plane for 2D manipulation
        dragPlane = new Plane(Vector3.forward, Vector3.zero);
        
        InitializeSplineFromBones();
        
        if (enableRuntimeManipulation)
        {
            if (debugMode)
            {
                Debug.Log("Runtime manipulation enabled, creating handles...");
            }
            CreateHandles();
        }
        else
        {
            Debug.LogWarning("Runtime manipulation is disabled. Enable it in the inspector to see handles.");
        }
    }

    void Update()
    {
        if (enableRuntimeManipulation)
        {
            HandleInput();
        }
        
        if (autoUpdateSpline || enableRuntimeManipulation)
        {
            UpdateBonesFromSpline();
        }
    }

    public void InitializeSplineFromBones()
    {
        if (boneRoots.Count < 2)
        {
            Debug.LogWarning("Need at least 2 bones to create a spline");
            return;
        }

        spline = new Spline();
        knotToBoneMap.Clear();
        originalKnotPositions.Clear();
        originalKnotData.Clear();

        for (int i = 0; i < boneRoots.Count; i++)
        {
            Transform bone = boneRoots[i];
            if (bone == null) continue;

            Vector3 localPos = transform.InverseTransformPoint(bone.position);
            localPos.z = 0f;
            
            BezierKnot knot = new BezierKnot(localPos);
            
            if (i > 0 && i < boneRoots.Count - 1)
            {
                Vector3 prevPos = transform.InverseTransformPoint(boneRoots[i - 1].position);
                Vector3 nextPos = transform.InverseTransformPoint(boneRoots[i + 1].position);
                
                Vector3 tangentDir = (nextPos - prevPos).normalized;
                float distance = Vector3.Distance(prevPos, nextPos) * tangentStrength;
                
                knot.TangentIn = -tangentDir * distance;
                knot.TangentOut = tangentDir * distance;
            }
            else if (i == 0 && boneRoots.Count > 1)
            {
                Vector3 nextPos = transform.InverseTransformPoint(boneRoots[i + 1].position);
                Vector3 tangentDir = (nextPos - localPos).normalized;
                float distance = Vector3.Distance(localPos, nextPos) * tangentStrength;
                
                knot.TangentOut = tangentDir * distance;
                knot.TangentIn = -tangentDir * distance * 0.5f;
            }
            else if (i == boneRoots.Count - 1 && i > 0)
            {
                Vector3 prevPos = transform.InverseTransformPoint(boneRoots[i - 1].position);
                Vector3 tangentDir = (localPos - prevPos).normalized;
                float distance = Vector3.Distance(prevPos, localPos) * tangentStrength;
                
                knot.TangentIn = -tangentDir * distance;
                knot.TangentOut = tangentDir * distance * 0.5f;
            }
            
            spline.Add(knot, TangentMode.AutoSmooth);
            knotToBoneMap[i] = bone;
            
            // Store original position and knot data
            originalKnotPositions[i] = bone.position;
            originalKnotData[i] = knot;
        }

        splineContainer.Spline = spline;
        
        Debug.Log($"Spline created with {spline.Count} knots");
    }

    private void CreateHandles()
    {
        foreach (GameObject handle in handleObjects)
        {
            if (handle != null) Destroy(handle);
        }
        handleObjects.Clear();

        if (spline == null)
        {
            Debug.LogWarning("Spline is null, cannot create handles");
            return;
        }

        if (debugMode)
        {
            Debug.Log($"Creating {spline.Count} sprite handles");
        }

        for (int i = 0; i < spline.Count; i++)
        {
            GameObject handle = new GameObject($"SplineHandle_{i}");
            handle.transform.SetParent(transform);
            
            // Add SpriteRenderer
            SpriteRenderer spriteRenderer = handle.AddComponent<SpriteRenderer>();
            
            // Use provided sprite or create a default circle sprite
            if (handleSprite != null)
            {
                spriteRenderer.sprite = handleSprite;
            }
            else
            {
                // Create a simple circle sprite if none provided
                spriteRenderer.sprite = CreateCircleSprite(64);
            }
            
            spriteRenderer.color = handleColor;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
            
            // Set size
            handle.transform.localScale = Vector3.one * handleSize;
            
            // Add collider for mouse interaction (2D)
            CircleCollider2D collider = handle.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f; // Matches sprite size
            
            // Add SplineHandle component
            SplineHandle handleScript = handle.AddComponent<SplineHandle>();
            handleScript.Initialize(this, i);
            
            handleObjects.Add(handle);
            UpdateHandlePosition(i);
            
            if (debugMode)
            {
                Debug.Log($"Created sprite handle {i} at position: {handle.transform.position}");
            }
        }
        
        Debug.Log($"Successfully created {handleObjects.Count} sprite handles");
    }

    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];
        
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // Create smooth anti-aliased edge
                    float alpha = 1f - Mathf.Clamp01((distance - radius + 2) / 2f);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }

    private void UpdateHandlePosition(int index)
    {
        if (index >= handleObjects.Count || handleObjects[index] == null) return;
        if (spline == null || index >= spline.Count) return;

        BezierKnot knot = spline[index];
        Vector3 worldPos = transform.TransformPoint(knot.Position);
        worldPos.z = 0f; // Keep sprites at z=0
        handleObjects[index].transform.position = worldPos;
    }

    private void HandleInput()
    {
        if (mainCamera == null) return;

        // Mouse down - select handle (using 2D raycasting)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                if (debugMode)
                {
                    Debug.Log($"2D Raycast hit: {hit.collider.gameObject.name}");
                }
                
                SplineHandle handleScript = hit.collider.GetComponent<SplineHandle>();
                if (handleScript != null && handleScript.Controller == this)
                {
                    selectedHandleIndex = handleScript.KnotIndex;
                    UpdateHandleColors();
                    
                    if (debugMode)
                    {
                        Debug.Log($"Selected handle: {selectedHandleIndex}");
                    }
                }
            }
        }

        // Mouse up - deselect
        if (Input.GetMouseButtonUp(0))
        {
            selectedHandleIndex = -1;
            UpdateHandleColors();
        }

        // Mouse drag - move selected handle
        if (Input.GetMouseButton(0) && selectedHandleIndex >= 0)
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f; // Keep in 2D plane
            
            if (debugMode)
            {
                Debug.Log($"Dragging handle {selectedHandleIndex} to {mouseWorldPos}");
            }
            
            SetKnotPosition(selectedHandleIndex, mouseWorldPos);
        }
    }

    private void UpdateHandleColors()
    {
        for (int i = 0; i < handleObjects.Count; i++)
        {
            if (handleObjects[i] == null) continue;
            
            SpriteRenderer spriteRenderer = handleObjects[i].GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (i == selectedHandleIndex) ? selectedHandleColor : handleColor;
            }
        }
    }

    public void UpdateBonesFromSpline()
    {
        if (spline == null || spline.Count == 0) return;

        for (int i = 0; i < spline.Count; i++)
        {
            if (!knotToBoneMap.ContainsKey(i)) continue;
            
            Transform bone = knotToBoneMap[i];
            if (bone == null) continue;

            BezierKnot knot = spline[i];
            Vector3 worldPos = transform.TransformPoint(knot.Position);
            
            bone.position = worldPos;
            
            if (debugMode && i == selectedHandleIndex)
            {
                Debug.Log($"Bone {i} updated to position: {worldPos}");
            }
        }
    }

    public void UpdateSplineFromBones()
    {
        if (spline == null) return;

        for (int i = 0; i < spline.Count; i++)
        {
            if (!knotToBoneMap.ContainsKey(i)) continue;
            
            Transform bone = knotToBoneMap[i];
            if (bone == null) continue;

            Vector3 localPos = transform.InverseTransformPoint(bone.position);
            localPos.z = 0f;
            BezierKnot knot = spline[i];
            knot.Position = localPos;
            spline.SetKnot(i, knot);
        }
        
        if (enableRuntimeManipulation)
        {
            for (int i = 0; i < spline.Count; i++)
            {
                UpdateHandlePosition(i);
            }
        }
    }

    public void SetKnotPosition(int knotIndex, Vector3 worldPosition)
    {
        if (spline == null || knotIndex >= spline.Count) return;

        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        BezierKnot knot = spline[knotIndex];
        
        Vector3 positionDelta = localPos - (Vector3)knot.Position;
        localPos.z = 0f;
        knot.Position = localPos;
        
        knot.TangentIn += (float3)positionDelta;
        knot.TangentOut += (float3)positionDelta;
        
        spline.SetKnot(knotIndex, knot);
        
        RecalculateTangentsForKnot(knotIndex);
        
        splineContainer.Spline = spline;
        
        UpdateHandlePosition(knotIndex);
        
        if (knotToBoneMap.ContainsKey(knotIndex))
        {
            Transform bone = knotToBoneMap[knotIndex];
            if (bone != null)
            {
                bone.position = worldPosition;
            }
        }
    }
    
    private void RecalculateTangentsForKnot(int knotIndex)
    {
        if (spline == null || knotIndex >= spline.Count) return;
        
        for (int i = Mathf.Max(0, knotIndex - 1); i <= Mathf.Min(spline.Count - 1, knotIndex + 1); i++)
        {
            BezierKnot knot = spline[i];
            
            if (i > 0 && i < spline.Count - 1)
            {
                Vector3 prevPos = spline[i - 1].Position;
                Vector3 nextPos = spline[i + 1].Position;
                Vector3 tangentDir = (nextPos - prevPos).normalized;
                
                float distPrev = Vector3.Distance(prevPos, knot.Position);
                float distNext = Vector3.Distance(knot.Position, nextPos);
                
                knot.TangentIn = (-(float3)tangentDir * distPrev * tangentStrength);
                knot.TangentOut = (float3)tangentDir * distNext * tangentStrength;
            }
            else if (i == 0 && spline.Count > 1)
            {
                Vector3 nextPos = spline[i + 1].Position;
                Vector3 tangentDir = ((float3)nextPos - knot.Position);
                tangentDir.Normalize();
                float distance = Vector3.Distance(knot.Position, nextPos) * tangentStrength;
                
                knot.TangentOut = tangentDir * distance;
                knot.TangentIn = -(float3)tangentDir * distance * 0.5f;
            }
            else if (i == spline.Count - 1 && i > 0)
            {
                Vector3 prevPos = spline[i - 1].Position;
                Vector3 tangentDir = (knot.Position - (float3)prevPos);
                tangentDir.Normalize();
                float distance = Vector3.Distance(prevPos, knot.Position) * tangentStrength;
                
                knot.TangentIn = -tangentDir * distance;
                knot.TangentOut = (float3)tangentDir * distance * 0.5f;
            }
            
            spline.SetKnot(i, knot);
        }
    }

    public void AddBone(Transform bone)
    {
        if (bone == null) return;
        
        boneRoots.Add(bone);
        
        Vector3 localPos = transform.InverseTransformPoint(bone.position);
        BezierKnot knot = new BezierKnot(localPos);
        spline.Add(knot, tangentMode);
        
        knotToBoneMap[spline.Count - 1] = bone;
        
        if (enableRuntimeManipulation)
        {
            CreateHandles();
        }
    }

    public void RecalculateTangents()
    {
        if (spline == null) return;
        
        for (int i = 0; i < spline.Count; i++)
        {
            RecalculateTangentsForKnot(i);
        }
        
        splineContainer.Spline = spline;
    }

    public void SetRuntimeManipulation(bool enabled)
    {
        enableRuntimeManipulation = enabled;
        
        if (enabled)
        {
            CreateHandles();
        }
        else
        {
            foreach (GameObject handle in handleObjects)
            {
                if (handle != null) Destroy(handle);
            }
            handleObjects.Clear();
        }
    }

    /// <summary>
    /// Reset all knots to their original positions
    /// </summary>
    public void ResetToOriginalPositions()
    {
        if (spline == null || originalKnotData.Count == 0)
        {
            Debug.LogWarning("No original positions stored to reset to!");
            return;
        }

        if (debugMode)
        {
            Debug.Log("Resetting all knots to original positions...");
        }

        // Reset each knot to its original state
        for (int i = 0; i < spline.Count; i++)
        {
            if (originalKnotData.ContainsKey(i))
            {
                BezierKnot originalKnot = originalKnotData[i];
                spline.SetKnot(i, originalKnot);
                
                // Update the corresponding bone
                if (knotToBoneMap.ContainsKey(i) && originalKnotPositions.ContainsKey(i))
                {
                    Transform bone = knotToBoneMap[i];
                    if (bone != null)
                    {
                        bone.position = originalKnotPositions[i];
                    }
                }
                
                // Update handle position
                if (enableRuntimeManipulation)
                {
                    UpdateHandlePosition(i);
                }
            }
        }

        // Force update the spline container
        splineContainer.Spline = spline;
        
        if (debugMode)
        {
            Debug.Log("Reset complete!");
        }
    }

    /// <summary>
    /// Reset a specific knot to its original position
    /// </summary>
    public void ResetKnotToOriginal(int knotIndex)
    {
        if (spline == null || knotIndex >= spline.Count)
        {
            Debug.LogWarning($"Invalid knot index: {knotIndex}");
            return;
        }

        if (!originalKnotData.ContainsKey(knotIndex))
        {
            Debug.LogWarning($"No original data stored for knot {knotIndex}");
            return;
        }

        if (debugMode)
        {
            Debug.Log($"Resetting knot {knotIndex} to original position...");
        }

        // Reset the knot
        BezierKnot originalKnot = originalKnotData[knotIndex];
        spline.SetKnot(knotIndex, originalKnot);

        // Update the corresponding bone
        if (knotToBoneMap.ContainsKey(knotIndex) && originalKnotPositions.ContainsKey(knotIndex))
        {
            Transform bone = knotToBoneMap[knotIndex];
            if (bone != null)
            {
                bone.position = originalKnotPositions[knotIndex];
            }
        }

        // Update handle position
        if (enableRuntimeManipulation)
        {
            UpdateHandlePosition(knotIndex);
        }

        // Force update the spline container
        splineContainer.Spline = spline;
    }

    /// <summary>
    /// Save current positions as new original positions
    /// </summary>
    public void SaveCurrentAsOriginal()
    {
        if (spline == null)
        {
            Debug.LogWarning("No spline to save!");
            return;
        }

        originalKnotPositions.Clear();
        originalKnotData.Clear();

        for (int i = 0; i < spline.Count; i++)
        {
            originalKnotData[i] = spline[i];
            
            if (knotToBoneMap.ContainsKey(i))
            {
                Transform bone = knotToBoneMap[i];
                if (bone != null)
                {
                    originalKnotPositions[i] = bone.position;
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"Saved current state as original for {originalKnotData.Count} knots");
        }
    }

    void OnDestroy()
    {
        foreach (GameObject handle in handleObjects)
        {
            if (handle != null) Destroy(handle);
        }
    }

    void OnDrawGizmos()
    {
        if (boneRoots == null || boneRoots.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (Transform bone in boneRoots)
        {
            if (bone != null)
            {
                Gizmos.DrawWireSphere(bone.position, 0.1f);
            }
        }
    }
}

public class SplineHandle : MonoBehaviour
{
    public SplineControlSystem Controller { get; private set; }
    public int KnotIndex { get; private set; }

    public void Initialize(SplineControlSystem controller, int index)
    {
        Controller = controller;
        KnotIndex = index;
    }
}