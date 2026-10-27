using System.Collections.Generic;
using UnityEngine;

namespace BlockBlaster
{
    public sealed class BlockSpawner : MonoBehaviour
    {
        GameManager game;
        readonly List<Tetromino> blocks = new();
        readonly List<Bullet> bullets = new();
        readonly List<PowerUp> pickups = new();
        float spawnLeft;

        public void Initialize(GameManager manager)
        {
            game = manager;
            for (int i = 0; i < GameRules.MaxBlocks; i++)
            {
                var go = new GameObject("Pooled tetromino " + i);
                go.transform.SetParent(game.Arena, false);
                var block = go.AddComponent<Tetromino>();
                block.Initialize(game, this);
                blocks.Add(block);
            }
            for (int i = 0; i < 64; i++)
            {
                var go = new GameObject("Pooled bullet " + i);
                go.transform.SetParent(game.Arena, false);
                var bullet = go.AddComponent<Bullet>();
                bullet.Initialize(game);
                bullets.Add(bullet);
            }
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject("Pooled power-up " + i);
                go.transform.SetParent(game.Arena, false);
                var pickup = go.AddComponent<PowerUp>();
                pickup.Initialize(game);
                pickups.Add(pickup);
            }
            spawnLeft = 0.8f;
        }

        void Update()
        {
            if (!game.IsPlaying) return;
            spawnLeft -= Time.deltaTime;
            if (spawnLeft > 0) return;
            spawnLeft = GameRules.SpawnInterval(game.Wave);
            foreach (var block in blocks)
            {
                if (block.Active) continue;
                var shape = (GameRules.Shape)Random.Range(0, 7);
                var cells = GameRules.Layout(shape, Random.Range(0, 4));
                int column = Random.Range(0, GameRules.Columns - GameRules.Width(cells) + 1);
                float x = (column - (GameRules.Columns - 1) * 0.5f) * GameRules.CellSize;
                block.Spawn(shape, cells, x, Random.Range(0, 8) == 0);
                break;
            }
        }

        public void Fire(Vector3 localMuzzle, float angle)
        {
            foreach (var bullet in bullets)
            { if (!bullet.gameObject.activeSelf) { bullet.Launch(localMuzzle, angle); return; } }
        }

        public void DropPower(Vector3 localPosition)
        {
            foreach (var pickup in pickups)
            { if (!pickup.gameObject.activeSelf) { pickup.Drop(localPosition, (GameRules.Power)Random.Range(0, 4)); return; } }
        }

        public void Clear()
        {
            foreach (var block in blocks) if (block != null) block.Despawn();
            foreach (var bullet in bullets) if (bullet != null) bullet.Retire();
            foreach (var pickup in pickups) if (pickup != null) pickup.Retire();
            spawnLeft = 0.8f;
        }
    }
}
