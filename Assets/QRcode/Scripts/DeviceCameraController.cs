using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.XR.XREAL;

public class DeviceCameraController : MonoBehaviour
{
    public Text t1,t2,t3,t4,t5,t6,t7,t8,t9,t10;
    public DeviceCamera dWebCam => webcam;
    private DeviceCamera webcam;

    public RawImage previewImage;
    public AspectRatioFitter previewAspectFitter;
    public Material yuvMaterial; // must be a YUV→RGB shader material

    public bool isPlaying => m_IsCapturing;

    private XREALRGBCameraTexture m_RGBCameraTexture;
    private bool m_IsCapturing = false;

    private Texture2D m_RGBTexture;
    private RenderTexture m_RT;
    private int m_FrameCount = 0;
    private bool m_YUVFormatLogged = false;

    void Start()
    {
        if (previewImage == null)
        {
            Debug.LogError("❌ Preview Image missing!");
            return;
        }

        Debug.Log("✅ DeviceCameraController Start");

        // DEBUG: material assignment check
        if (yuvMaterial == null)
        {
            Debug.LogError("❌ yuvMaterial is NULL — assign YUV2RGB_Material in Inspector!");
            if (t3 != null) t3.text = "Mat: NULL ❌";
        }
        else
        {
            Debug.Log($"✅ yuvMaterial='{yuvMaterial.name}' shader='{yuvMaterial.shader.name}'");
            if (t3 != null) t3.text = $"Shader: {yuvMaterial.shader.name}";
        }

        webcam = new DeviceCamera(this);
        StartWork();
    }

    public void StartWork()
    {
        Debug.Log("▶️ StartWork called");

        if (m_RGBCameraTexture == null)
        {
            m_RGBCameraTexture = XREALRGBCameraTexture.CreateSingleton();
            m_RGBCameraTexture.OnRGBCameraUpdate += OnCameraFrameUpdate;
            Debug.Log("📷 XREAL Camera Initialized");
            if (t1 != null) t1.text = "Camera Initialized";
        }

        if (!m_IsCapturing)
        {
            m_RGBCameraTexture.StartCapture();
            m_IsCapturing = true;
            Debug.Log("🟢 Camera Capture Started");
            if (t2 != null) t2.text = "Camera Capture Started";
        }
    }

    public void StopWork()
    {
        Debug.Log("⏹ StopWork called");

        if (m_RGBCameraTexture != null && m_IsCapturing)
        {
            m_RGBCameraTexture.StopCapture();
            m_IsCapturing = false;
            Debug.Log("🔴 Camera Capture Stopped");
            if (t3 != null) t3.text = "Camera Capture Stopped";
        }

        if (previewImage != null)
            previewImage.texture = null;
    }

        private void OnCameraFrameUpdate()
    {
        if (m_RGBCameraTexture == null)
        {
            Debug.LogError("❌ CameraTexture NULL");
            if (t4 != null) t4.text = "CameraTexture NULL";
            return;
        }

        var yuv = m_RGBCameraTexture.GetYUVFormatTextures();
        if (yuv == null || yuv.Length < 3)
        {
            Debug.LogWarning("⚠️ YUV textures missing");
            if (t5 != null) t5.text = "YUV texture NULL";
            return;
        }

        // DEBUG: log YUV texture formats once — should say Alpha8
        if (!m_YUVFormatLogged && yuv[0] != null)
        {
            m_YUVFormatLogged = true;
            Debug.Log($"YUV formats → Y:{yuv[0].format} {yuv[0].width}x{yuv[0].height} | U:{yuv[1]?.format} {yuv[1]?.width}x{yuv[1]?.height} | V:{yuv[2]?.format}");
            if (t4 != null) t4.text = $"Y:{yuv[0].format} U:{yuv[1]?.format} V:{yuv[2]?.format}";
            if (yuvMaterial == null)
                Debug.LogError("❌ yuvMaterial NULL — Blit will fail silently!");
        }

        m_FrameCount++;
        if (t7 != null) t7.text = $"Frames: {m_FrameCount}";

        Vector2Int res = m_RGBCameraTexture.GetResolution();
        if (t6 != null) t6.text = $"Res: {res.x}x{res.y}";

        // YUV_420_888: 3 separate planes — matches official XREAL SDK example
        // yuv[0]=Y passed as Blit source (→ _MainTex), yuv[1]=U, yuv[2]=V set via SetTexture
        if (yuvMaterial != null)
        {
            yuvMaterial.SetTexture("_UTex", yuv[1]);
            yuvMaterial.SetTexture("_VTex", yuv[2]);
        }

        // Convert YUV → RGB (cached RenderTexture — no per-frame alloc)
        if (m_RT == null || m_RT.width != res.x || m_RT.height != res.y)
        {
            if (m_RT != null) m_RT.Release();
            m_RT = new RenderTexture(res.x, res.y, 0, RenderTextureFormat.ARGB32);
        }
        // Pass Y plane as source so shader receives it as _MainTex
        Graphics.Blit(yuv[0], m_RT, yuvMaterial);

        if (m_RGBTexture == null || m_RGBTexture.width != res.x || m_RGBTexture.height != res.y)
        {
            m_RGBTexture = new Texture2D(res.x, res.y, TextureFormat.RGB24, false);
        }

        RenderTexture.active = m_RT;
        m_RGBTexture.ReadPixels(new Rect(0, 0, res.x, res.y), 0, 0);
        m_RGBTexture.Apply();
        RenderTexture.active = null;

        // DEBUG: sample center pixel every 90 frames — tells us if conversion output is correct
        // Green (0,1,0) = shader reading wrong channel. Gray = YUV data not arriving. Correct = natural colors.
        if (m_FrameCount % 90 == 0)
        {
            Color32 c = m_RGBTexture.GetPixel(res.x / 2, res.y / 2);
            Debug.Log($"Center pixel RGB → R:{c.r} G:{c.g} B:{c.b}  (green screen if G>>R,B)");
            if (t5 != null) t5.text = $"Pixel R:{c.r} G:{c.g} B:{c.b}";
        }

        // Preview
        if (previewImage != null)
        {
            previewImage.material = null;
            previewImage.texture = m_RGBTexture;
        }

        // Aspect Ratio
        if (previewAspectFitter != null && res.x > 0 && res.y > 0)
        {
            previewAspectFitter.aspectRatio = (float)res.x / res.y;
        }
    }

    public Texture2D GetReadableTexture()
    {
        if (m_RGBTexture == null)
        {
            Debug.LogError("❌ RGB Texture NULL (QR scan fail hoga)");
            if (t7 != null) t7.text = "RGB Texture NULL";
        }
        return m_RGBTexture;
    }

    public void tapFocus() { }
    public void swithCamera() { }
    public void toggleTorch() { Debug.Log("⚠️ Torch not supported on XREAL"); }

    void OnApplicationPause(bool pause)
    {
        Debug.Log("⏸ App Pause: " + pause);
        if (pause) StopWork();
        else StartWork();
    }

    void OnDestroy()
    {
        Debug.Log("💀 OnDestroy called");
        if (m_RGBCameraTexture != null)
            m_RGBCameraTexture.OnRGBCameraUpdate -= OnCameraFrameUpdate;

        StopWork();

        if (m_RT != null) { m_RT.Release(); m_RT = null; }
    }

    // =====================================================================
    public class DeviceCamera
    {
        private DeviceCameraController ctrl;
        public DeviceCamera(DeviceCameraController c) { ctrl = c; }

        public bool isPlaying() => ctrl.m_IsCapturing;

        public int Width()
        {
            var t = ctrl.GetReadableTexture();
            int w = t != null ? t.width : 0;
            Debug.Log("📏 Width: " + w);
            if (ctrl.t8 != null) ctrl.t8.text = "Width: " + w;
            return w;
        }

        public int Height()
        {
            var t = ctrl.GetReadableTexture();
            int h = t != null ? t.height : 0;
            Debug.Log("📏 Height: " + h);
            if (ctrl.t9 != null) ctrl.t9.text = "Height: " + h;
            return h;
        }

        public Color32[] GetPixels32()
        {
            var t = ctrl.GetReadableTexture();
            if (t == null)
            {
                Debug.LogWarning("⏳ Frame not ready yet");
                if (ctrl.t10 != null) ctrl.t10.text = "GetPixels32 FAILED";
                return null;
            }
            return t.GetPixels32();
        }

        public Texture preview => ctrl.GetReadableTexture();
        public void Play() => ctrl.StartWork();
        public void Stop() => ctrl.StopWork();
    }
}
