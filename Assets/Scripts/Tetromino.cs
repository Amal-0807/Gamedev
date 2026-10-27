using UnityEngine;

namespace BlockBlaster
{
    public sealed class Tetromino : MonoBehaviour
    {
        public bool Active => gameObject.activeSelf;
        GameManager game;
        BlockSpawner spawner;
        Rigidbody body;
        readonly BlockCube[] cubes = new BlockCube[4];
        int remaining;
        bool glowing;
        Color color;

        public void Initialize(GameManager manager, BlockSpawner owner)
        {
            game = manager;
            spawner = owner;
            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            for (int i = 0; i < cubes.Length; i++)
            {
                var go = Visuals.Cube("Cube " + i, transform, Vector3.zero,
                    new Vector3(GameRules.CubeSize, 0.04f, GameRules.CubeSize), Color.white, true);
                cubes[i] = go.AddComponent<BlockCube>();
                go.layer = 9;
                cubes[i].Initialize(this);
                // An inset bright face gives every cube a readable border at table distance.
                Visuals.Cube("Face", go.transform, new Vector3(0, 0.51f, 0), new Vector3(0.76f, 0.035f, 0.76f), Color.white);
            }
            Despawn();
        }

        public void Spawn(GameRules.Shape shape, GameRules.Cell[] layout, float x, bool powerBlock)
        {
            remaining = 4;
            glowing = powerBlock;
            color = Visuals.ShapeColors[(int)shape];
            transform.localPosition = new Vector3(x, GameRules.PlayHeight, GameRules.ArenaLength + 0.04f);
            body.position = transform.position;
            for (int i = 0; i < cubes.Length; i++)
                cubes[i].ResetCube(new Vector3(layout[i].X * GameRules.CellSize, 0, layout[i].Y * GameRules.CellSize), color, glowing);
            gameObject.SetActive(true);
        }

        void FixedUpdate()
        {
            if (!game.IsPlaying) return;
            var local = game.Arena.InverseTransformPoint(body.position);
            local.z -= GameRules.FallSpeed(game.Wave) * (game.SlowLeft > 0 ? 0.5f : 1) * Time.fixedDeltaTime;
            body.MovePosition(game.Arena.TransformPoint(local));
            // A partially chipped shape misses only when one of its remaining cubes crosses the line.
            foreach (var cube in cubes)
            {
                if (cube.gameObject.activeSelf && local.z + cube.transform.localPosition.z < -GameRules.CubeSize * 0.5f)
                { game.MissedBlock(); Despawn(); break; }
            }
        }

        public void CubeDestroyed(BlockCube cube)
        {
            if (!Active || !game.IsPlaying || !cube.gameObject.activeSelf) return;
            var position = game.Arena.InverseTransformPoint(cube.transform.position);
            cube.gameObject.SetActive(false);
            remaining--;
            game.AddCubeScore();
            game.Effects.Popup(game.Arena, position, "+" + GameRules.CubePoints, Color.white);
            game.Effects.Burst(game.Arena, position, color, remaining == 0 ? 16 : 7);
            game.Effects.Sound(remaining == 0 ? GameEffects.Cue.Explosion : GameEffects.Cue.Hit);
            if (remaining > 0) return;
            if (glowing) spawner.DropPower(position);
            Despawn();
        }

        public void Despawn() { gameObject.SetActive(false); }
    }
}
