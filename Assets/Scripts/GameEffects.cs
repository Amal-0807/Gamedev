using System.Collections.Generic;
using UnityEngine;

namespace BlockBlaster
{
    public sealed class GameEffects : MonoBehaviour
    {
        public enum Cue { Shot, Hit, Explosion, Hurt, Pickup }
        public bool Muted { get; private set; }
        AudioSource audioSource;
        readonly Dictionary<Cue, AudioClip> clips = new();
        readonly List<ParticleSystem> bursts = new();
        readonly List<TextMesh> popups = new();

        public void Initialize()
        {
            Muted = PlayerPrefs.GetInt("BlockBlaster.Muted", 0) == 1;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
            clips[Cue.Shot] = Tone("Shot", 900, 220, 0.06f);
            clips[Cue.Hit] = Tone("Hit", 360, 110, 0.08f);
            clips[Cue.Explosion] = Tone("Explosion", 160, 35, 0.20f);
            clips[Cue.Hurt] = Tone("Ship hit", 140, 55, 0.30f);
            clips[Cue.Pickup] = Tone("Power-up", 440, 1100, 0.24f);
            for (int i = 0; i < 12; i++)
            {
                var go = new GameObject("Pooled hit burst " + i);
                go.transform.SetParent(transform, false);
                var particles = go.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = false;
                main.playOnAwake = false;
                main.duration = 0.5f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.003f, 0.008f);
                main.maxParticles = 32;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                var emission = particles.emission;
                emission.enabled = false;
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.008f;
                var renderer = particles.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = Visuals.Material(Color.white);
                bursts.Add(particles);
            }
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < 10; i++)
            {
                var go = new GameObject("Pooled score popup " + i, typeof(TextMesh));
                go.transform.SetParent(transform, false);
                var text = go.GetComponent<TextMesh>();
                text.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                text.anchor = TextAnchor.MiddleCenter;
                text.fontSize = 48;
                text.fontStyle = FontStyle.Bold;
                text.characterSize = 0.004f;
                go.SetActive(false);
                popups.Add(text);
            }
        }

        void Update()
        {
            // A popup's remaining life is its alpha: it drifts up the board while fading out.
            foreach (var popup in popups)
            {
                if (!popup.gameObject.activeSelf) continue;
                var color = popup.color;
                color.a -= Time.deltaTime / 0.7f;
                if (color.a <= 0) { popup.gameObject.SetActive(false); continue; }
                popup.color = color;
                popup.transform.localPosition += new Vector3(0, 0, 0.09f * Time.deltaTime);
            }
        }

        public void Popup(Transform arena, Vector3 position, string text, Color color)
        {
            foreach (var popup in popups)
            {
                if (popup.gameObject.activeSelf) continue;
                popup.transform.SetParent(arena, false);
                popup.transform.localPosition = position + new Vector3(0, 0.03f, 0);
                popup.transform.localRotation = Quaternion.Euler(90, 0, 0);
                popup.text = text;
                popup.color = color;
                popup.gameObject.SetActive(true);
                return;
            }
        }

        public void Sound(Cue cue)
        { if (!Muted) audioSource.PlayOneShot(clips[cue], cue == Cue.Shot ? 0.16f : 0.35f); }

        public void ToggleSound()
        {
            Muted = !Muted;
            PlayerPrefs.SetInt("BlockBlaster.Muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
            if (Muted) audioSource.Stop();
        }

        public void Burst(Transform arena, Vector3 position, Color color, int count)
        {
            foreach (var particles in bursts)
            {
                if (particles.IsAlive()) continue;
                particles.transform.SetParent(arena, false);
                particles.transform.localPosition = position;
                particles.transform.localRotation = Quaternion.identity;
                particles.transform.localScale = Vector3.one;
                var main = particles.main;
                main.startColor = color;
                particles.Play();
                particles.Emit(count);
                return;
            }
        }

        public void Clear()
        {
            audioSource.Stop();
            foreach (var particles in bursts)
            {
                if (particles == null) continue;
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.transform.SetParent(transform, false);
            }
            foreach (var popup in popups)
            {
                if (popup == null) continue;
                popup.gameObject.SetActive(false);
                popup.transform.SetParent(transform, false);
            }
        }

        static AudioClip Tone(string name, float from, float to, float duration)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * rate)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Mathf.Lerp(from, to, t) * Mathf.PI * 2 / rate;
                samples[i] = Mathf.Sin(phase) * Mathf.Pow(1 - t, 2) * Mathf.Min(1, t * 40);
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy() { foreach (var clip in clips.Values) if (clip != null) Destroy(clip); }
    }
}
