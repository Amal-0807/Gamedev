using UnityEngine;
using UnityEngine.EventSystems;

namespace BlockBlaster
{
    public sealed class ShipController : MonoBehaviour
    {
        GameManager game;
        Rigidbody body;
        Renderer hull;
        Transform craft;
        GameObject shield;
        float targetX, nextShot;
        bool pointerHeld;
        int fingerId = -1;

        public void Initialize(GameManager manager)
        {
            game = manager;
            gameObject.layer = 10;
            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.070f, 0.055f, 0.060f);
            // Kenney "Space Kit" starfighter (CC0); its UVs index the palette in Visuals.ShipMaterial.
            craft = Instantiate(Resources.Load<GameObject>("Starfighter"), transform).transform;
            craft.name = "Hull";
            craft.localPosition = new Vector3(0, -0.018f, 0);
            craft.localScale = Vector3.one * 0.045f;
            hull = craft.GetComponentInChildren<Renderer>();
            hull.sharedMaterial = Visuals.ShipMaterial();
            Exhaust(-0.6f);
            Exhaust(0.6f);
            shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "Shield bubble";
            shield.transform.SetParent(transform, false);
            shield.transform.localScale = new Vector3(0.13f, 0.08f, 0.13f);
            Destroy(shield.GetComponent<Collider>());
            shield.GetComponent<Renderer>().sharedMaterial = Visuals.Material(new Color(0.20f, 0.94f, 0.91f, 0.22f));
            // Draw the bubble after the hull so the ship stays visible through it.
            shield.GetComponent<Renderer>().sharedMaterial.renderQueue = 3100;
            shield.SetActive(false);
            ResetShip();
        }

        public void ResetShip()
        {
            ReleasePointer();
            targetX = 0;
            craft.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3(0, GameRules.PlayHeight, GameRules.ShipZ);
            body.position = transform.position;
            nextShot = 0;
            hull.enabled = true;
            shield.SetActive(false);
        }

        void Update()
        {
            shield.SetActive(game.Shield);
            hull.enabled = game.InvincibleLeft <= 0 || Mathf.FloorToInt(Time.unscaledTime * 12) % 2 == 0;
            if (!game.IsPlaying) { ReleasePointer(); return; }
            ReadPointer();
            if (Application.isEditor)
            {
                float horizontal = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
                targetX += horizontal * 0.65f * Time.deltaTime;
            }
            targetX = Mathf.Clamp(targetX, -0.255f, 0.255f);
            // Bank into the turn; the hull levels out once the ship reaches the finger.
            float lean = Mathf.Clamp((transform.localPosition.x - targetX) * 300, -28, 28);
            craft.localRotation = Quaternion.Slerp(craft.localRotation, Quaternion.Euler(0, 0, lean), 12 * Time.deltaTime);
            // Auto-fire: the player only steers.
            if (Time.time < nextShot) return;
            nextShot = Time.time + GameRules.FireInterval;
            Vector3 muzzle = transform.localPosition + new Vector3(0, 0, 0.043f);
            game.Spawner.Fire(muzzle, 0);
            if (game.TripleLeft > 0) { game.Spawner.Fire(muzzle, -12); game.Spawner.Fire(muzzle, 12); }
            game.Effects.Sound(GameEffects.Cue.Shot);
        }

        void ReadPointer()
        {
            Vector2 point = default;
            bool held = false;
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (fingerId < 0 && touch.phase == TouchPhase.Began && touch.position.y < Screen.height * 0.5f
                        && !EventSystem.current.IsPointerOverGameObject(touch.fingerId)) fingerId = touch.fingerId;
                    if (touch.fingerId != fingerId) continue;
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) { fingerId = -1; continue; }
                    point = touch.position; held = true;
                }
            }
            else
            {
                fingerId = -1;
                if (Input.GetMouseButtonDown(0))
                    pointerHeld = Input.mousePosition.y < Screen.height * 0.5f && !EventSystem.current.IsPointerOverGameObject();
                if (!Input.GetMouseButton(0)) pointerHeld = false;
                held = pointerHeld;
                point = Input.mousePosition;
            }
            if (!held) return;
            // Raycast against the arena's own plane: controls work from any viewing angle.
            var plane = new Plane(game.Arena.up, game.Arena.TransformPoint(new Vector3(0, GameRules.PlayHeight, 0)));
            Ray ray = game.Placer.ViewCamera.ScreenPointToRay(point);
            if (plane.Raycast(ray, out float distance)) targetX = game.Arena.InverseTransformPoint(ray.GetPoint(distance)).x;
        }

        void FixedUpdate()
        {
            if (!game.IsPlaying) return;
            var local = game.Arena.InverseTransformPoint(body.position);
            local.x = Mathf.MoveTowards(local.x, targetX, 1.2f * Time.fixedDeltaTime);
            body.MovePosition(game.Arena.TransformPoint(local));
        }

        // Engine flame: white-hot at the nozzle, cooling to cyan as it trails off.
        void Exhaust(float x)
        {
            var go = new GameObject("Exhaust");
            go.transform.SetParent(craft, false);
            go.transform.localPosition = new Vector3(x, 0.3f, -1.05f);
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var flame = go.AddComponent<ParticleSystem>();
            flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = flame.main;
            main.loop = true;
            main.startLifetime = 0.22f;
            main.startSpeed = 5;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = flame.emission;
            emission.rateOverTime = 60;
            var shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 4;
            shape.radius = 0.08f;
            var fade = flame.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Visuals.Cyan, 0.4f) },
                new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var shrink = flame.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.2f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = Visuals.Material(Color.white);
            flame.Play();
        }

        public void ReleasePointer() { pointerHeld = false; fingerId = -1; }

        void OnTriggerEnter(Collider other)
        {
            if (!game.IsPlaying) return;
            var cube = other.GetComponent<BlockCube>();
            if (cube != null && cube.Owner.Active)
            {
                var block = cube.Owner;
                block.Despawn();
                game.ShipHit();
                return;
            }
            var pickup = other.GetComponent<PowerUp>();
            if (pickup != null) pickup.Collect();
        }
    }
}
