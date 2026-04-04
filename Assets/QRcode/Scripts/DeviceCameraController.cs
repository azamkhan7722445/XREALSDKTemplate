using UnityEngine;
using UnityEngine.UI;
using Unity.XR.XREAL;

public class DeviceCameraController : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────
    public RawImage previewImage;
    public AspectRatioFitter previewAspectFitter;
    public Material yuvMaterial;

    // ─── QRCodeDecodeController ke liye ──────────────────────────────────
    public CameraWrapper dWebCam { get; private set; }
    public bool isPlaying { get; private set; } = false;

    // ─── Private ──────────────────────────────────────────────────────────
    private XREALRGBCameraTexture m_RGBCameraTexture;
    private RenderTexture m_RenderTexture;   // GPU → CPU bridge
    private Texture2D m_ReadableTexture;     // CPU readable — QR ke liye

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (previewImage == null)
        {
            Debug.LogError("❌ Preview Image NULL — Inspector mein assign karo!");
            return;
        }
        dWebCam = new CameraWrapper(this);
        StartWork();
    }

    // ─────────────────────────────────────────────────────────────────────
    public void StartWork()
    {
        if (m_RGBCameraTexture == null)
        {
            m_RGBCameraTexture = XREALRGBCameraTexture.CreateSingleton();
            m_RGBCameraTexture.OnRGBCameraUpdate += OnCameraFrameUpdate;
        }
        m_RGBCameraTexture.StartCapture();
        isPlaying = true;
        Debug.Log("✅ XREAL Eye Camera: Started");
    }

    // ─────────────────────────────────────────────────────────────────────
    public void StopWork()
    {
        if (m_RGBCameraTexture != null && isPlaying)
        {
            m_RGBCameraTexture.StopCapture();
            isPlaying = false;
        }
        if (previewImage != null)
            previewImage.texture = null;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void OnCameraFrameUpdate()
    {
        if (m_RGBCameraTexture == null) return;

        var yuvTextures = m_RGBCameraTexture.GetYUVFormatTextures();
        if (yuvTextures == null || yuvTextures[0] == null) return;

        Texture2D yTex = yuvTextures[0];

        // ── Preview ──────────────────────────────────────────────────────
        if (previewImage != null)
        {
            if (yuvMaterial != null)
            {
                previewImage.material = yuvMaterial;
                previewImage.texture  = yuvTextures[0];
                yuvMaterial.SetTexture("_UTex", yuvTextures[1]);
                yuvMaterial.SetTexture("_VTex", yuvTextures[2]);
            }
            else
            {
                previewImage.texture = yuvTextures[0];
            }
        }

        // ── Aspect ratio ─────────────────────────────────────────────────
        if (previewAspectFitter != null)
        {
            Vector2Int res = m_RGBCameraTexture.GetResolution();
            if (res.x > 0 && res.y > 0)
                previewAspectFitter.aspectRatio = (float)res.x / res.y;
        }

        // ── GPU texture → CPU readable (QR scanning ke liye) ─────────────
        // Step 1: RenderTexture banao agar pehli baar hai
        if (m_RenderTexture == null ||
            m_RenderTexture.width  != yTex.width ||
            m_RenderTexture.height != yTex.height)
        {
            if (m_RenderTexture != null) m_RenderTexture.Release();
            m_RenderTexture = new RenderTexture(yTex.width, yTex.height, 0,
                                                RenderTextureFormat.ARGB32);
        }

        // Step 2: CPU readable Texture2D banao
        if (m_ReadableTexture == null ||
            m_ReadableTexture.width  != yTex.width ||
            m_ReadableTexture.height != yTex.height)
        {
            m_ReadableTexture = new Texture2D(yTex.width, yTex.height,
                                              TextureFormat.RGBA32, false);
        }

        // Step 3: GPU texture ko RenderTexture mein blit karo
        Graphics.Blit(yTex, m_RenderTexture);

        // Step 4: RenderTexture se pixels CPU mein copy karo
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = m_RenderTexture;
        m_ReadableTexture.ReadPixels(
            new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height), 0, 0);
        m_ReadableTexture.Apply();
        RenderTexture.active = prev;
    }

    // ─────────────────────────────────────────────────────────────────────
    public Texture2D GetCurrentTexture() => m_ReadableTexture;

    public void tapFocus()
    {
        Debug.Log("tapFocus called");
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) StopWork();
        else       StartWork();
    }

    void OnDestroy()
    {
        if (m_RGBCameraTexture != null)
            m_RGBCameraTexture.OnRGBCameraUpdate -= OnCameraFrameUpdate;
        StopWork();
        if (m_RenderTexture != null) m_RenderTexture.Release();
    }

    // =====================================================================
    // CameraWrapper
    // =====================================================================
    public class CameraWrapper
    {
        private DeviceCameraController m_Controller;
        public CameraWrapper(DeviceCameraController c) { m_Controller = c; }

        public int Width()
        {
            var t = m_Controller.GetCurrentTexture();
            return t != null ? t.width : 0;
        }

        public int Height()
        {
            var t = m_Controller.GetCurrentTexture();
            return t != null ? t.height : 0;
        }

        public Color[] GetPixels(int x, int y, int w, int h)
        {
            var t = m_Controller.GetCurrentTexture();
            if (t == null) return new Color[w * h];

            // Bounds check — crash se bachao
            x = Mathf.Clamp(x, 0, t.width  - 1);
            y = Mathf.Clamp(y, 0, t.height - 1);
            w = Mathf.Clamp(w, 1, t.width  - x);
            h = Mathf.Clamp(h, 1, t.height - y);

            return t.GetPixels(x, y, w, h);
        }
    }
}