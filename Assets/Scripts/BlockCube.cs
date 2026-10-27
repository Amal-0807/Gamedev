using UnityEngine;

namespace BlockBlaster
{
    public sealed class BlockCube : MonoBehaviour
    {
        public Tetromino Owner { get; private set; }
        Renderer shell, face;
        public void Initialize(Tetromino owner) { Owner = owner; shell = GetComponent<Renderer>(); }

        public void ResetCube(Vector3 position, Color color, bool glowing)
        {
            transform.localPosition = position;
            shell.sharedMaterial = Visuals.Material(Color.Lerp(color, Visuals.Ink, 0.45f), 1);
            if (face == null) face = transform.GetChild(0).GetComponent<Renderer>();
            face.sharedMaterial = Visuals.Material(glowing ? Visuals.Gold : color);
            gameObject.SetActive(true);
        }

        public void Hit() { if (Owner.Active && gameObject.activeSelf) Owner.CubeDestroyed(this); }
    }
}
