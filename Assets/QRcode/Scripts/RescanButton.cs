using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a Button inside the world-space Canvas.
/// When the XREAL ray points at it and the trigger is pressed,
/// it resets the QR scanner so it immediately tries again.
/// </summary>
[RequireComponent(typeof(Button))]
public class RescanButton : MonoBehaviour
{
    [Tooltip("Reference to QRCodeDecodeController in the scene.")]
    public QRCodeDecodeController qrController;

    void Start()
    {
        if (qrController == null)
            qrController = FindObjectOfType<QRCodeDecodeController>();

        if (qrController == null)
        {
            Debug.LogError("RescanButton: QRCodeDecodeController not found!");
            return;
        }

        GetComponent<Button>().onClick.AddListener(OnRescanClicked);
    }

    void OnRescanClicked()
    {
        Debug.Log("[RescanButton] Rescan triggered via XR ray");
        qrController.Reset();
    }
}
