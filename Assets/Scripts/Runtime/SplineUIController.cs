using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Optional UI controller for manipulating spline at runtime
/// </summary>
public class SplineUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SplineControlSystem splineController;
    
    [Header("UI Elements (Optional)")]
    [SerializeField] private Slider handleSizeSlider;
    [SerializeField] private Toggle autoUpdateToggle;
    [SerializeField] private Button recalculateTangentsButton;
    [SerializeField] private TMP_Text infoText;
    
    [Header("Keyboard Controls")]
    [SerializeField] private KeyCode toggleManipulationKey = KeyCode.M;
    [SerializeField] private KeyCode recalculateTangentsKey = KeyCode.R;
    [SerializeField] private KeyCode resetKey = KeyCode.Backspace;

    private bool manipulationEnabled = true;

    void Start()
    {
        SetupUI();
        UpdateInfoText();
    }

    void Update()
    {
        HandleKeyboardInput();
    }

    private void SetupUI()
    {
        if (handleSizeSlider != null)
        {
            handleSizeSlider.onValueChanged.AddListener(OnHandleSizeChanged);
        }

        if (autoUpdateToggle != null)
        {
            autoUpdateToggle.onValueChanged.AddListener(OnAutoUpdateChanged);
        }

        if (recalculateTangentsButton != null)
        {
            recalculateTangentsButton.onClick.AddListener(OnRecalculateTangents);
        }
    }

    private void HandleKeyboardInput()
    {
        // Toggle manipulation
        if (Input.GetKeyDown(toggleManipulationKey))
        {
            manipulationEnabled = !manipulationEnabled;
            splineController.SetRuntimeManipulation(manipulationEnabled);
            UpdateInfoText();
        }

        // Recalculate tangents
        if (Input.GetKeyDown(recalculateTangentsKey))
        {
            splineController.RecalculateTangents();
            UpdateInfoText("Tangents recalculated");
        }

        // Reset spline from bones
        if (Input.GetKeyDown(resetKey))
        {
            splineController.UpdateSplineFromBones();
            UpdateInfoText("Spline reset to bone positions");
        }
    }

    private void OnHandleSizeChanged(float value)
    {
        // Handle size change would require exposing this in BoneSplineController
        UpdateInfoText($"Handle size: {value:F2}");
    }

    private void OnAutoUpdateChanged(bool value)
    {
        // Auto update toggle would require exposing this in BoneSplineController
        UpdateInfoText($"Auto update: {(value ? "ON" : "OFF")}");
    }

    private void OnRecalculateTangents()
    {
        if (splineController != null)
        {
            splineController.RecalculateTangents();
            UpdateInfoText("Tangents recalculated");
        }
    }

    private void UpdateInfoText(string message = null)
    {
        if (infoText == null) return;

        if (message != null)
        {
            infoText.text = message;
            return;
        }

        infoText.text = $"Controls:\n" +
                       $"[{toggleManipulationKey}] Toggle Manipulation: {(manipulationEnabled ? "ON" : "OFF")}\n" +
                       $"[{recalculateTangentsKey}] Recalculate Tangents\n" +
                       $"[{resetKey}] Reset Spline\n" +
                       $"Left Click + Drag to move handles";
    }

    void OnGUI()
    {
        // Simple on-screen display if no UI Text is assigned
        if (infoText != null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Spline Manipulation: {(manipulationEnabled ? "ON" : "OFF")}");
        GUILayout.Label($"Press [{toggleManipulationKey}] to toggle");
        GUILayout.Label($"Press [{recalculateTangentsKey}] to smooth");
        GUILayout.Label($"Press [{resetKey}] to reset");
        GUILayout.Label("Click & drag yellow spheres");
        GUILayout.EndArea();
    }
}