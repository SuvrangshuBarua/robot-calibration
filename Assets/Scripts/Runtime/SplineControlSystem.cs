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
    [SerializeField] private float tangentStrength = 0.33f; // Control smoothness
    
    [Header("Runtime Manipulation")]
    [SerializeField] private bool enableRuntimeManipulation = true;
    [SerializeField] private float handleSize = 0.3f;
    [SerializeField] private Color handleColor = Color.yellow;
    [SerializeField] private Color selectedHandleColor = Color.green;
    [SerializeField] private int handleLayer = 0; // Default layer
    [SerializeField] private bool debugMode = false;
    
    private SplineContainer splineContainer;
    private Spline spline;
    private Dictionary<int, Transform> knotToBoneMap = new Dictionary<int, Transform>();
    private List<GameObject> handleObjects = new List<GameObject>();
    private int selectedHandleIndex = -1;
    private Camera mainCamera;

    void Start()
    {
        splineContainer = GetComponent<SplineContainer>();
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! Make sure your camera is tagged as MainCamera");
        }
        
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
        
        // Always update bones from spline when manipulation is enabled
        if (autoUpdateSpline || enableRuntimeManipulation)
        {
            UpdateBonesFromSpline();
        }
    }

    /// <summary>
    /// Creates a Bezier spline connecting all bone roots
    /// </summary>
    public void InitializeSplineFromBones()
    {
        if (boneRoots.Count < 2)
        {
            Debug.LogWarning("Need at least 2 bones to create a spline");
            return;
        }

        // Create new spline
        spline = new Spline();
        knotToBoneMap.Clear();

        // Add knots for each bone root
        for (int i = 0; i < boneRoots.Count; i++)
        {
            Transform bone = boneRoots[i];
            if (bone == null) continue;

            // Convert world position to local space of this GameObject
            Vector3 localPos = transform.InverseTransformPoint(bone.position);
            localPos.z = 0f; // Force Z to zero
            
            // Create knot
            BezierKnot knot = new BezierKnot(localPos);
            
            // Calculate smooth tangents based on neighboring points
            if (i > 0 && i < boneRoots.Count - 1)
            {
                // Get previous and next positions
                Vector3 prevPos = transform.InverseTransformPoint(boneRoots[i - 1].position);
                Vector3 nextPos = transform.InverseTransformPoint(boneRoots[i + 1].position);
                
                // Calculate tangent direction (smoothly pointing from prev to next)
                Vector3 tangentDir = (nextPos - prevPos).normalized;
                float distance = Vector3.Distance(prevPos, nextPos) * tangentStrength;
                
                knot.TangentIn = -tangentDir * distance;
                knot.TangentOut = tangentDir * distance;
            }
            else if (i == 0 && boneRoots.Count > 1)
            {
                // First knot - tangent points toward next
                Vector3 nextPos = transform.InverseTransformPoint(boneRoots[i + 1].position);
                Vector3 tangentDir = (nextPos - localPos).normalized;
                float distance = Vector3.Distance(localPos, nextPos) * tangentStrength;
                
                knot.TangentOut = tangentDir * distance;
                knot.TangentIn = -tangentDir * distance * 0.5f;
            }
            else if (i == boneRoots.Count - 1 && i > 0)
            {
                // Last knot - tangent points from previous
                Vector3 prevPos = transform.InverseTransformPoint(boneRoots[i - 1].position);
                Vector3 tangentDir = (localPos - prevPos).normalized;
                float distance = Vector3.Distance(prevPos, localPos) * tangentStrength;
                
                knot.TangentIn = -tangentDir * distance;
                knot.TangentOut = tangentDir * distance * 0.5f;
            }
            
            spline.Add(knot, TangentMode.AutoSmooth); // Use manual for custom tangents
            
            // Map knot index to bone
            knotToBoneMap[i] = bone;
        }

        // Set the spline to the container
        splineContainer.Spline = spline;
        
        Debug.Log($"Spline created with {spline.Count} knots");
    }

    /// <summary>
    /// Creates visual handles for runtime manipulation
    /// </summary>
    private void CreateHandles()
    {
        // Clear existing handles
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
            Debug.Log($"Creating {spline.Count} handles");
        }

        // Create a handle for each knot
        for (int i = 0; i < spline.Count; i++)
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = $"SplineHandle_{i}";
            handle.transform.SetParent(transform);
            handle.transform.localScale = Vector3.one * handleSize;
            handle.layer = handleLayer;
            
            // Add a SplineHandle component to track index
            SplineHandle handleScript = handle.AddComponent<SplineHandle>();
            handleScript.Initialize(this, i);
            
            // Set color with unlit shader
            Renderer renderer = handle.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = handleColor;
                renderer.material = mat;
            }
            
            handleObjects.Add(handle);
            
            // Update position
            UpdateHandlePosition(i);
            
            if (debugMode)
            {
                Debug.Log($"Created handle {i} at position: {handle.transform.position}");
            }
        }
        
        Debug.Log($"Successfully created {handleObjects.Count} handle spheres");
    }

    /// <summary>
    /// Updates handle position based on spline knot
    /// </summary>
    private void UpdateHandlePosition(int index)
    {
        if (index >= handleObjects.Count || handleObjects[index] == null) return;
        if (spline == null || index >= spline.Count) return;

        BezierKnot knot = spline[index];
        Vector3 worldPos = transform.TransformPoint(knot.Position);
        handleObjects[index].transform.position = worldPos;
    }

    /// <summary>
    /// Handles mouse input for dragging handles
    /// </summary>
    private void HandleInput()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found!");
            return;
        }

        // Mouse down - select handle
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (debugMode)
                {
                    Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
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
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Project to plane parallel to camera at handle's distance
            Vector3 handlePos = handleObjects[selectedHandleIndex].transform.position;
            float distance = Vector3.Dot(handlePos - mainCamera.transform.position, mainCamera.transform.forward);
            Vector3 worldPos = ray.GetPoint(distance);
            
            if (debugMode)
            {
                Debug.Log($"Dragging handle {selectedHandleIndex} to {worldPos}");
            }
            
            SetKnotPosition(selectedHandleIndex, worldPos);
        }
    }

    /// <summary>
    /// Updates handle colors based on selection
    /// </summary>
    private void UpdateHandleColors()
    {
        for (int i = 0; i < handleObjects.Count; i++)
        {
            if (handleObjects[i] == null) continue;
            
            Renderer renderer = handleObjects[i].GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = (i == selectedHandleIndex) ? selectedHandleColor : handleColor;
            }
        }
    }

    /// <summary>
    /// Updates bone positions and rotations based on spline knot positions
    /// </summary>
    public void UpdateBonesFromSpline()
    {
        if (spline == null || spline.Count == 0) return;

        for (int i = 0; i < spline.Count; i++)
        {
            if (!knotToBoneMap.ContainsKey(i)) continue;
            
            Transform bone = knotToBoneMap[i];
            if (bone == null) continue;

            // Get knot in world space
            BezierKnot knot = spline[i];
            Vector3 worldPos = transform.TransformPoint(knot.Position);
            
            // Update bone position
            bone.position = worldPos;
            
            if (debugMode && i == selectedHandleIndex)
            {
                Debug.Log($"Bone {i} updated to position: {worldPos}");
            }

            // Calculate rotation based on tangent direction
            /*if (i < spline.Count - 1)
            {
                // Use forward tangent for rotation
                Vector3 tangent = transform.TransformDirection(knot.TangentOut);
                if (tangent != Vector3.zero)
                {
                    bone.rotation = Quaternion.LookRotation(tangent, transform.up);
                }
            }*/
        }
    }

    /// <summary>
    /// Updates spline knots based on current bone positions
    /// </summary>
    public void UpdateSplineFromBones()
    {
        if (spline == null) return;

        for (int i = 0; i < spline.Count; i++)
        {
            if (!knotToBoneMap.ContainsKey(i)) continue;
            
            Transform bone = knotToBoneMap[i];
            if (bone == null) continue;

            Vector3 localPos = transform.InverseTransformPoint(bone.position);
            localPos.z = 0f; // Force Z to zero
            BezierKnot knot = spline[i];
            knot.Position = localPos;
            spline.SetKnot(i, knot);
        }
        
        // Update handle positions
        if (enableRuntimeManipulation)
        {
            for (int i = 0; i < spline.Count; i++)
            {
                UpdateHandlePosition(i);
            }
        }
    }

    /// <summary>
    /// Manually set a knot position
    /// </summary>
    public void SetKnotPosition(int knotIndex, Vector3 worldPosition)
    {
        if (spline == null || knotIndex >= spline.Count) return;

        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        BezierKnot knot = spline[knotIndex];
        
        // Update position (keeping Z at zero)
        Vector3 positionDelta = localPos - (Vector3)knot.Position;
        localPos.z = 0f; // Force Z to zero
        knot.Position = localPos;
        
        // Move tangents with the knot to maintain curve shape
        knot.TangentIn += (float3)positionDelta;
        knot.TangentOut += (float3)positionDelta;
        
        spline.SetKnot(knotIndex, knot);
        
        // Recalculate tangents for smooth curve
        RecalculateTangentsForKnot(knotIndex);
        
        // Force update the spline container
        splineContainer.Spline = spline;
        
        UpdateHandlePosition(knotIndex);
        
        // Immediately update the specific bone
        if (knotToBoneMap.ContainsKey(knotIndex))
        {
            Transform bone = knotToBoneMap[knotIndex];
            if (bone != null)
            {
                bone.position = worldPosition;
                
                // Update rotation based on tangent
                if (knotIndex < spline.Count - 1)
                {
                    Vector3 tangent = transform.TransformDirection(knot.TangentOut);
                    if (tangent != Vector3.zero)
                    {
                        //bone.rotation = Quaternion.LookRotation(tangent, transform.up);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Recalculate tangents for a specific knot and its neighbors for smooth curves
    /// </summary>
    private void RecalculateTangentsForKnot(int knotIndex)
    {
        if (spline == null || knotIndex >= spline.Count) return;
        
        // Recalculate for current and neighboring knots
        for (int i = Mathf.Max(0, knotIndex - 1); i <= Mathf.Min(spline.Count - 1, knotIndex + 1); i++)
        {
            BezierKnot knot = spline[i];
            
            if (i > 0 && i < spline.Count - 1)
            {
                // Middle knot - smooth between neighbors
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
                // First knot
                Vector3 nextPos = spline[i + 1].Position;
                Vector3 tangentDir = ((float3)nextPos - knot.Position);
                tangentDir = tangentDir.normalized;
                float distance = Vector3.Distance(knot.Position, nextPos) * tangentStrength;
                
                knot.TangentOut = tangentDir * distance;
                knot.TangentIn = -(float3)tangentDir * distance * 0.5f;
            }
            else if (i == spline.Count - 1 && i > 0)
            {
                // Last knot
                Vector3 prevPos = spline[i - 1].Position;
                Vector3 tangentDir = (knot.Position - (float3)prevPos);
                tangentDir = tangentDir.normalized;
                float distance = Vector3.Distance(prevPos, knot.Position) * tangentStrength;
                
                knot.TangentIn = -tangentDir * distance;
                knot.TangentOut = (float3)tangentDir * distance * 0.5f;
            }
            
            spline.SetKnot(i, knot);
        }
    }

    /// <summary>
    /// Add a new bone to the spline
    /// </summary>
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

    /// <summary>
    /// Refresh tangent mode for smoother curves
    /// </summary>
    public void RecalculateTangents()
    {
        if (spline == null) return;
        
        for (int i = 0; i < spline.Count; i++)
        {
            RecalculateTangentsForKnot(i);
        }
        
        splineContainer.Spline = spline;
    }

    /// <summary>
    /// Toggle runtime manipulation on/off
    /// </summary>
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

    void OnDestroy()
    {
        // Clean up handles
        foreach (GameObject handle in handleObjects)
        {
            if (handle != null) Destroy(handle);
        }
    }

    // Gizmo visualization
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
/// <summary>
/// Helper component attached to handle objects
/// </summary>
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
