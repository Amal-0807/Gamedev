using UnityEngine;

namespace BlockBlaster
{
    public sealed class PowerUp : MonoBehaviour
    {
        GameManager game;
        Rigidbody body;
        Renderer face, icon;
        Transform visual;
        GameRules.Power kind;
        float life;
        bool collected;
        static readonly UiArt.Icon[] Icons = { UiArt.Icon.Shield, UiArt.Icon.Life, UiArt.Icon.Triple, UiArt.Icon.Slow };
        static readonly Color[] Colors = { Visuals.Cyan, Visuals.Coral, Visuals.Gold, new Color(0.68f, 0.51f, 1f) };

        public void Initialize(GameManager manager)
        {
            game = manager;
            gameObject.layer = 11;
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.026f;
            visual = Visuals.Cube("Pickup", transform, Vector3.zero, new Vector3(0.03f, 0.03f, 0.03f), Color.white).transform;
            face = visual.GetComponent<Renderer>();
            var symbol = GameObject.CreatePrimitive(PrimitiveType.Quad);
            symbol.name = "Symbol";
            Destroy(symbol.GetComponent<Collider>());
            symbol.transform.SetParent(transform, false);
            symbol.transform.localPosition = new Vector3(0, 0.022f, 0);
            symbol.transform.localRotation = Quaternion.Euler(90, 0, 0);
            symbol.transform.localScale = Vector3.one * 0.034f;
            icon = symbol.GetComponent<Renderer>();
            Retire();
        }

        public void Drop(Vector3 localPosition, GameRules.Power power)
        {
            kind = power;
            collected = false;
            life = 10;
            localPosition.y = GameRules.PlayHeight;
            transform.localPosition = localPosition;
            face.sharedMaterial = Visuals.Material(Colors[(int)power]);
            icon.sharedMaterial = Visuals.IconMaterial(Icons[(int)power]);
            gameObject.SetActive(true);
            body.position = transform.position;
            body.angularVelocity = Vector3.zero;
            // Real ballistic launch and scaled gravity, independent of the arena's world pose.
            body.linearVelocity = game.Arena.TransformVector(new Vector3(Random.Range(-0.025f, 0.025f), 0.19f, -0.16f));
        }

        void FixedUpdate()
        {
            if (!game.IsPlaying) return;
            var local = game.Arena.InverseTransformPoint(body.position);
            if (local.y > GameRules.PlayHeight + 0.002f || Vector3.Dot(body.linearVelocity, game.Arena.up) > 0)
                body.AddForce(game.Arena.TransformVector(new Vector3(0, -0.75f, 0)), ForceMode.Acceleration);
            else
            {
                local.y = GameRules.PlayHeight;
                body.position = game.Arena.TransformPoint(local);
                var velocity = game.Arena.InverseTransformVector(body.linearVelocity);
                velocity.y = 0;
                body.linearVelocity = game.Arena.TransformVector(velocity);
            }
            if (local.z < -0.06f || Mathf.Abs(local.x) > 0.32f) Retire();
        }

        void Update()
        {
            if (!game.IsPlaying) return;
            visual.Rotate(0, 100 * Time.deltaTime, 0, Space.Self);
            life -= Time.deltaTime;
            if (life <= 0) Retire();
        }

        public void Collect()
        {
            if (collected || !game.IsPlaying || !gameObject.activeSelf) return;
            collected = true;
            game.Collect(kind);
            game.Effects.Burst(game.Arena, transform.localPosition, Colors[(int)kind], 14);
            Retire();
        }

        public void Retire() { body.linearVelocity = Vector3.zero; gameObject.SetActive(false); }
    }
}
