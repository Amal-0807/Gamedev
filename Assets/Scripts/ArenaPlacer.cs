using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;

namespace BlockBlaster
{
    public sealed class ArenaPlacer : MonoBehaviour
    {
        public bool IsAR { get; private set; }
        public bool HasTracking => !IsAR || (ARSession.state == ARSessionState.SessionTracking &&
            (!placed || (anchor != null && arenaObject != null && anchor.trackingState == TrackingState.Tracking)));
        public Camera ViewCamera => IsAR && arCamera != null ? arCamera : practiceCamera;
        public string ScanMessage { get; private set; } = "Point at a table and move slowly";
        GameManager game;
        Camera practiceCamera, arCamera;
        GameObject originObject, sessionObject, arenaObject, reticle;
        ARPlaneManager planes;
        ARRaycastManager raycasts;
        ARAnchorManager anchors;
        ARAnchor anchor;
        readonly List<ARRaycastHit> hits = new();
        bool placing, placed;
        int generation;
        static readonly Vector3 PracticeView = new Vector3(0, 0.60f, -0.20f);

        public void Initialize(GameManager manager)
        {
            game = manager;
            var cameraObject = new GameObject("Practice camera", typeof(Camera), typeof(AudioListener));
            practiceCamera = cameraObject.GetComponent<Camera>();
            practiceCamera.tag = "MainCamera";
            practiceCamera.clearFlags = CameraClearFlags.SolidColor;
            practiceCamera.backgroundColor = Visuals.Ink;
            practiceCamera.nearClipPlane = 0.01f;
            practiceCamera.farClipPlane = 20;
            practiceCamera.transform.position = PracticeView;
            // Low chase view from behind the ship: the board recedes toward a starry horizon.
            practiceCamera.transform.LookAt(new Vector3(0, 0, 0.54f));
            ResizePracticeView();
            MakeStars(cameraObject.transform);
        }

        void ResizePracticeView()
        {
            // Hold a 54-degree horizontal view so the ship's row fits edge to edge on narrow portrait phones.
            practiceCamera.fieldOfView = Mathf.Clamp(2 * Mathf.Atan(Mathf.Tan(27 * Mathf.Deg2Rad) / practiceCamera.aspect) * Mathf.Rad2Deg, 60, 90);
        }

        public void BeginPractice()
        {
            IsAR = false;
            CreateArena(null, Vector3.zero, Quaternion.identity);
        }

        public void BeginAR()
        {
            IsAR = true;
            StartCoroutine(StartAR());
        }

        IEnumerator StartAR()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            Fail("Use Practice in the editor. AR needs a supported Android phone.");
            yield break;
#else
            ScanMessage = "Allow camera access to scan the table";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                bool answered = false;
                var callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted += _ => answered = true;
                callbacks.PermissionDenied += _ => answered = true;
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera, callbacks);
                float deadline = Time.realtimeSinceStartup + 60;
                while (!answered && Time.realtimeSinceStartup < deadline) yield return null;
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                { Fail("Camera permission is needed for AR. You can still play Practice."); yield break; }
            }
            ScanMessage = "Checking AR support...";
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager == null) { Fail("AR is not configured. Use Practice or check the Android build settings."); yield break; }
            if (!manager.isInitializationComplete) yield return manager.InitializeLoader();
            if (manager.activeLoader == null) { Fail("This phone cannot start AR. Choose Practice."); yield break; }
            manager.StartSubsystems();

            sessionObject = new GameObject("AR Session");
            sessionObject.SetActive(false);
            var session = sessionObject.AddComponent<ARSession>();
            session.attemptUpdate = true;
            sessionObject.AddComponent<ARInputManager>();

            originObject = new GameObject("XR Origin");
            originObject.SetActive(false);
            var origin = originObject.AddComponent<XROrigin>();
            var offset = new GameObject("Camera offset");
            offset.transform.SetParent(originObject.transform, false);
            var cameraObject = new GameObject("AR Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(offset.transform, false);
            arCamera = cameraObject.GetComponent<Camera>();
            arCamera.tag = "MainCamera";
            arCamera.nearClipPlane = 0.01f;
            arCamera.farClipPlane = 20;
            arCamera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<ARCameraManager>();
            cameraObject.AddComponent<ARCameraBackground>();
#pragma warning disable CS0618
            cameraObject.AddComponent<ARPoseDriver>();
#pragma warning restore CS0618
            origin.Camera = arCamera;
            origin.CameraFloorOffsetObject = offset;
            origin.CameraYOffset = 0;
            planes = originObject.AddComponent<ARPlaneManager>();
            planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            raycasts = originObject.AddComponent<ARRaycastManager>();
            anchors = originObject.AddComponent<ARAnchorManager>();
            practiceCamera.gameObject.SetActive(false);
            sessionObject.SetActive(true);
            originObject.SetActive(true);
            ScanMessage = "Point at a table and move slowly";
            yield return ARSession.CheckAvailability();
            if (ARSession.state == ARSessionState.NeedsInstall) yield return ARSession.Install();
            if (ARSession.state == ARSessionState.Unsupported || ARSession.state == ARSessionState.NeedsInstall)
                Fail("Google Play Services for AR is unavailable. Choose Practice.");
#endif
        }

        void Update()
        {
            if (!IsAR)
            {
                ResizePracticeView();
                // Camera kick when the ship is hit. AR leaves the real camera alone.
                practiceCamera.transform.position = PracticeView + (Vector3)UnityEngine.Random.insideUnitCircle * (0.012f * game.HitKick);
            }
            if (game.State == GameState.Place) ResizeArena();
            if (!IsAR || planes == null) return;
            if ((game.State == GameState.Playing || game.State == GameState.Countdown) && !HasTracking)
                game.Pause("Move phone slowly to find the table");
            bool scanning = game.State == GameState.Scan;
            foreach (var plane in planes.trackables)
            {
                if (!plane.TryGetComponent<PlaneVisual>(out var visual)) visual = plane.gameObject.AddComponent<PlaneVisual>();
                visual.SetVisible(scanning && plane.trackingState == TrackingState.Tracking && plane.subsumedBy == null);
            }
            if (!scanning || placing) { if (reticle != null) reticle.SetActive(false); return; }
            bool found = raycasts.Raycast(new Vector2(Screen.width * 0.5f, Screen.height * 0.45f), hits, TrackableType.PlaneWithinPolygon);
            if (found)
            {
                if (reticle == null) reticle = MakeReticle();
                reticle.SetActive(true);
                reticle.transform.SetPositionAndRotation(hits[0].pose.position + Vector3.up * 0.004f, hits[0].pose.rotation);
                ScanMessage = "Table found. Tap a glowing surface to place.";
            }
            else { if (reticle != null) reticle.SetActive(false); ScanMessage = "Point at a table and move slowly"; }

            if (!TryPlacementTap(out var point)) return;
            if (raycasts.Raycast(point, hits, TrackableType.PlaneWithinPolygon)) PlaceAnchor(hits[0].pose);
        }

        bool TryPlacementTap(out Vector2 point)
        {
            point = default;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began || EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return false;
                point = touch.position;
                return true;
            }
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            { point = Input.mousePosition; return true; }
            return false;
        }

        async void PlaceAnchor(Pose pose)
        {
            placing = true;
            int requestGeneration = generation;
            ScanMessage = "Anchoring arena...";
            try
            {
                var result = await anchors.TryAddAnchorAsync(pose);
                var created = result.value;
                if (!result.status.IsSuccess() || created == null)
                { if (requestGeneration == generation) ScanMessage = "Could not anchor. Move slowly and tap again."; return; }
                if (requestGeneration != generation || game.State != GameState.Scan)
                { Destroy(created.gameObject); return; }
                anchor = created;
                // The front edge faces the player at placement; then remains fixed on the table.
                var forward = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                var rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                CreateArena(anchor.transform, pose.position, rotation);
                placed = true;
                HidePlanes();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                if (requestGeneration == generation) ScanMessage = "Anchor failed. Tap the table again.";
            }
            finally { if (requestGeneration == generation) placing = false; }
        }

        void CreateArena(Transform parent, Vector3 position, Quaternion rotation)
        {
            arenaObject = new GameObject("Arena · 60 × 80 cm");
            arenaObject.transform.SetParent(parent, false);
            arenaObject.transform.SetPositionAndRotation(position, rotation);
            Visuals.BuildArena(arenaObject.transform);
            game.SetArena(arenaObject.transform);
        }

        void ResizeArena()
        {
            if (arenaObject == null) return;
            float factor = 1;
            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                float previous = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                if (previous > 1) factor = Vector2.Distance(a.position, b.position) / previous;
            }
            else if (Application.isEditor) factor += Input.mouseScrollDelta.y * 0.05f;
            float scale = Mathf.Clamp(arenaObject.transform.localScale.x * factor, 0.5f, 1.4f);
            arenaObject.transform.localScale = Vector3.one * scale;
        }

        public void HidePlanes()
        {
            if (planes != null)
                foreach (var plane in planes.trackables)
                    if (plane.TryGetComponent<PlaneVisual>(out var visual)) visual.SetVisible(false);
            if (reticle != null) reticle.SetActive(false);
        }

        public void ResetPlacement()
        {
            generation++;
            StopAllCoroutines();
            placing = placed = false;
            if (arenaObject != null) Destroy(arenaObject);
            if (anchor != null) Destroy(anchor.gameObject);
            if (reticle != null) Destroy(reticle);
            if (originObject != null) { originObject.SetActive(false); Destroy(originObject); }
            if (sessionObject != null) { sessionObject.SetActive(false); Destroy(sessionObject); }
            arenaObject = originObject = sessionObject = reticle = null;
            anchor = null; planes = null; anchors = null; raycasts = null; arCamera = null;
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (IsAR && manager != null && manager.isInitializationComplete)
            { manager.StopSubsystems(); manager.DeinitializeLoader(); }
            IsAR = false;
            practiceCamera.gameObject.SetActive(true);
        }

        void Fail(string message) { game.Menu(); game.Tell(message, 8); }

        // Stars streaming past below the practice board; parented to the practice camera so AR never shows them.
        static void MakeStars(Transform parent)
        {
            var go = new GameObject("Starfield");
            go.transform.SetParent(parent, true);
            var stars = go.AddComponent<ParticleSystem>();
            stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = stars.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 14;
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.03f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1, 1, 1, 0.25f), new Color(0.6f, 0.9f, 1, 0.9f));
            main.maxParticles = 500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = stars.emission;
            emission.rateOverTime = 30;
            var shape = stars.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0, -0.7f, 3.5f);
            shape.scale = new Vector3(7, 1.2f, 9);
            var drift = stars.velocityOverLifetime;
            drift.enabled = true;
            drift.space = ParticleSystemSimulationSpace.World;
            drift.x = 0; drift.y = 0; drift.z = -0.35f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Visuals.Material(Color.white);
            stars.Play();
        }

        GameObject MakeReticle()
        {
            var go = new GameObject("Placement ring");
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 32;
            line.widthMultiplier = 0.003f;
            line.sharedMaterial = Visuals.Material(Visuals.Cyan);
            for (int i = 0; i < 32; i++)
            { float angle = i * Mathf.PI * 2 / 32; line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.05f); }
            return go;
        }
    }

}
