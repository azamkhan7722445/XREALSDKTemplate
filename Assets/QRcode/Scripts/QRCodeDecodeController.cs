using UnityEngine;
using System;
using System.Collections.Generic;
using ZXing;
using ZXing.Common;
using UnityEngine.Events;

public class QRCodeDecodeController : MonoBehaviour
{
    [Serializable]
    public class UnityEventString : UnityEvent<string> { };
    public UnityEventString onQRScanFinished;

    public DeviceCameraController e_DeviceController;

    private BarcodeReader barReader;
    private bool decoding = false;
    private string dataText = null;

    private int blockWidth = 350;
    private bool cameraReady = false;
    private int frameWait = 0;
    private int framerate = 0;
    private int m_ScanAttempts = 0;
    private int m_ScanFails = 0;

    void Start()
    {
        barReader = new BarcodeReader
        {
            AutoRotate = true,
            TryInverted = true,
            Options = new DecodingOptions
            {
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                TryHarder = true
            }
        };

        if (!e_DeviceController)
        {
            e_DeviceController = GameObject.FindObjectOfType<DeviceCameraController>();
            if (!e_DeviceController)
                Debug.LogError("❌ DeviceCameraController missing!");
        }
    }

    void Update()
    {
        // Skip frames for performance
        if (framerate++ % 5 != 0) return;
        if (!e_DeviceController || !e_DeviceController.isPlaying || decoding) return;

        // Allow camera to warm up
        if (frameWait < 15) { frameWait++; return; }

        Texture2D tex = e_DeviceController.GetReadableTexture();
        if (tex == null) return;

        if (!cameraReady)
        {
            Debug.Log("✅ Camera Ready for QR");
            cameraReady = true;
        }

        int W = tex.width;
        int H = tex.height;
        if (W < 100 || H < 100) return;

        blockWidth = Mathf.Min(W, H);

        Color32[] fullPixels = tex.GetPixels32();
        if (fullPixels == null || fullPixels.Length == 0) return;

        // Crop center block
        int posx = (W - blockWidth) / 2;
        int posy = (H - blockWidth) / 2;
        Color32[] targetColorARR = new Color32[blockWidth * blockWidth];

        int index = 0;
        for (int y = 0; y < blockWidth; y++)
        {
            for (int x = 0; x < blockWidth; x++)
            {
                int srcIndex = (posy + y) * W + (posx + x);
                targetColorARR[index++] = fullPixels[srcIndex];
            }
        }

        // DEBUG: log scan attempt with center pixel color every 10 attempts
        m_ScanAttempts++;
        if (m_ScanAttempts % 10 == 1)
        {
            Color32 center = targetColorARR[targetColorARR.Length / 2];
            Debug.Log($"QR scan #{m_ScanAttempts} | blockWidth:{blockWidth} | centerPixel R:{center.r} G:{center.g} B:{center.b} | fails so far:{m_ScanFails}");
        }

        // Decode async — result dispatched back to main thread
        decoding = true;
        Loom.RunAsync(() =>
        {
            try
            {
                var result = barReader.Decode(targetColorARR, blockWidth, blockWidth);
                if (result != null)
                {
                    string text = result.Text;
                    Loom.QueueOnMainThread(() =>
                    {
                        Debug.Log("✅ QR DETECTED: " + text);
                        onQRScanFinished.Invoke(text);
                        decoding = false;
                    });
                }
                else
                {
                    m_ScanFails++;
                    Loom.QueueOnMainThread(() => decoding = false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Decode Error: {e.Message}");
                Loom.QueueOnMainThread(() => decoding = false);
            }
        });
    }

    // ─────────────────────────────
    public void Reset()
    {
        decoding = false;
        dataText = null;
    }

    public void StartWork()
    {
        if (e_DeviceController != null)
            e_DeviceController.StartWork();
        Reset();
    }

    public void StopWork()
    {
        if (e_DeviceController != null)
            e_DeviceController.StopWork();
    }

    // ─────────────────────────────
    public static string DecodeByStaticPic(Texture2D tex)
    {
        var reader = new BarcodeReader { AutoRotate = true, TryInverted = true };
        var result = reader.Decode(tex.GetPixels32(), tex.width, tex.height);
        return result != null ? result.Text : "decode failed!";
    }
}
