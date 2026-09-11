using UnityEngine;

namespace Mahjong.Visual
{
    /// <summary>
    /// CameraController: Mengontrol kamera 3D dengan sudut pandang isometrik VIP Casino.
    /// Mendukung adaptasi dinamis untuk layar HP (Portrait / Landscape) pada Android & iOS,
    /// serta efek smooth zoom saat dealing ubin dan pengumuman kemenangan (Round End).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Target & Posisi Sudut Pandang")]
        public Transform targetFocus; // Titik fokus kamera (tengah meja Mahjong)
        public Vector3 defaultOffset = new Vector3(0, 0.46f, -0.40f);
        public Vector3 defaultRotation = new Vector3(45f, 0, 0);

        [Header("Pengaturan Mobile Responsif")]
        public float landscapeFOV = 34f;
        public float portraitFOV = 48f;
        public float smoothSpeed = 6.0f;

        private Camera cam;
        private Vector3 targetPos;
        private Quaternion targetRot;
        private float targetFOV;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            targetPos = transform.position;
            targetRot = transform.rotation;
            targetFOV = landscapeFOV;
            ApplyDefaultView();
        }

        private void Start()
        {
            AdjustFOVForScreenAspect();
        }

        private void LateUpdate()
        {
            // Periksa jika orientasi layar perangkat berubah (misal: rotasi HP)
            AdjustFOVForScreenAspect();

            // Smooth Interpolation untuk pergerakan kamera
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
        }

        [ContextMenu("Apply Default View")]
        public void ApplyDefaultView()
        {
            Vector3 focusPoint = (targetFocus != null) ? targetFocus.position : Vector3.zero;
            targetPos = focusPoint + defaultOffset;
            targetRot = Quaternion.Euler(defaultRotation);
        }

        /// <summary>
        /// Efek zoom kamera mendekat ke ubin tangan saat fase deklarasi Menang / Riichi.
        /// </summary>
        public void FocusOnWin(Vector3 winnerHandPosition)
        {
            targetPos = winnerHandPosition + new Vector3(0, 0.45f, -0.35f);
            targetRot = Quaternion.Euler(55f, 0, 0);
            targetFOV = 32f;
        }

        /// <summary>
        /// Mengembalikan kamera ke posisi standar meja penuh.
        /// </summary>
        public void ResetToTableView()
        {
            ApplyDefaultView();
            AdjustFOVForScreenAspect();
        }

        private void AdjustFOVForScreenAspect()
        {
            float aspect = (float)Screen.width / Screen.height;
            if (aspect < 1.0f) // Layar Tegak (Portrait)
            {
                targetFOV = portraitFOV;
            }
            else // Layar Mendatar (Landscape)
            {
                targetFOV = landscapeFOV;
            }
        }
    }
}
