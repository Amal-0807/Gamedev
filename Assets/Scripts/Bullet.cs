using UnityEngine;

namespace BlockBlaster
{
    public sealed class Bullet : MonoBehaviour
    {
        GameManager game;
        Rigidbody body;
        TrailRenderer trail;
        bool consumed;
        float life;

        public void Initialize(GameManager manager)
        {
            game = manager;
            gameObject.layer = 8;
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.009f, 0.018f, 0.024f);
            Visuals.Cube("Bolt", transform, Vector3.zero, collider.size, Visuals.Gold);
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.10f;
            trail.startWidth = 0.008f;
            trail.endWidth = 0;
            trail.sharedMaterial = Visuals.Material(Visuals.Gold);
            trail.startColor = Color.white;
            trail.endColor = new Color(1, 1, 1, 0);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Retire();
        }

        public void Launch(Vector3 position, float angle)
        {
            consumed = false;
            life = 1.5f;
            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(0, angle, 0);
            gameObject.SetActive(true);
            body.position = transform.position;
            body.rotation = transform.rotation;
            trail.Clear();
            body.angularVelocity = Vector3.zero;
            body.linearVelocity = transform.forward * (1.30f * game.Arena.lossyScale.x);
        }

        void Update()
        {
            if (!game.IsPlaying) return;
            life -= Time.deltaTime;
            var local = game.Arena.InverseTransformPoint(body.position);
            if (life <= 0 || local.z > 1.08f || Mathf.Abs(local.x) > 0.34f) Retire();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (consumed || !game.IsPlaying) return;
            var cube = collision.collider.GetComponent<BlockCube>();
            if (cube == null) return;
            consumed = true; // A multi-contact physics step can damage only one cube.
            cube.Hit();
            Retire();
        }

        public void Retire() { consumed = true; body.linearVelocity = Vector3.zero; gameObject.SetActive(false); }
    }
}
