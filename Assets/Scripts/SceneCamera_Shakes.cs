using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    public partial class SceneCamera : SceneService
    {
        // ────────────────────────────────────────────────────────────────
        // Camera Shake Fields
        // ────────────────────────────────────────────────────────────────
        [Header("Camera Shake Profiles")]
        [SerializeField] private NoiseSettings _fireShakeSettings;
        [SerializeField] private NoiseSettings _takeDamageShakeSettings;
        [SerializeField] private NoiseSettings _aoeShakeSettings;
        // Add more profiles here as needed

        [Header("Default Shake Values (fallback when params are -1)")]
        [SerializeField] private float _defaultShakeDuration = 0.35f;
        [SerializeField] private float _defaultShakeAmplitude = 1.8f;
        [SerializeField] private float _defaultShakeFrequency = 12f;

        // Tracks all currently running shakes
        private List<ActiveShake> _activeShakes = new List<ActiveShake>();

        private class ActiveShake
        {
            public float currentAmplitude;
            public Coroutine routine;
        }

        [Serializable]
        public struct CameraShakeParams
        {
            public NoiseSettings profile;
            public float duration;
            public float peakAmplitude;
            public float peakFrequency;
            public float fadeInTime;
            public float fadeOutTime;
            public float sustainTime;

            public CameraShakeParams(
                NoiseSettings profile,
                float duration = 0.5f,
                float peakAmplitude = 2f,
                float peakFrequency = 10f,
                float fadeIn = 0.12f,
                float fadeOut = 0.25f,
                float sustain = 0f)
            {
                this.profile = profile;
                this.duration = duration;
                this.peakAmplitude = peakAmplitude;
                this.peakFrequency = peakFrequency;
                this.fadeInTime = fadeIn;
                this.fadeOutTime = fadeOut;
                this.sustainTime = sustain;
            }

            public static CameraShakeParams Fire(NoiseSettings profile)
                => new(profile, 0.25f, 1f, 1f, 0.05f, 0.1f, 0f);

            public static CameraShakeParams Damage(NoiseSettings profile)
                => new(profile, 0.3f, 3.5f, 14f, 0.08f, 0.1f, 0f);

            public static CameraShakeParams AOE(NoiseSettings profile)
                => new(profile, 1.1f, 4.2f, 9f, 0.15f, 0.50f, 0.45f);
        }

        // ────────────────────────────────────────────────────────────────
        // Public shake methods
        // ────────────────────────────────────────────────────────────────

        public void Shake(
            ECameraShakeType type,
            float overrideAmplitude = -1f,
            float overrideDuration = -1f,
            float overrideFrequency = -1f,
            float overrideFadeIn = -1f,
            float overrideFadeOut = -1f,
            float overrideSustain = -1f)
        {
            Debug.Log("Shake");

            var (profile, baseParams) = GetBaseShake(type);
            if (profile == null)
            {
                Debug.LogWarning($"[Camera Shake] Profile for {type} is not assigned!");
                return;
            }

            var p = new CameraShakeParams(
                profile,
                overrideDuration >= 0 ? overrideDuration : baseParams.duration,
                overrideAmplitude >= 0 ? overrideAmplitude : baseParams.peakAmplitude,
                overrideFrequency >= 0 ? overrideFrequency : baseParams.peakFrequency,
                overrideFadeIn >= 0 ? overrideFadeIn : baseParams.fadeInTime,
                overrideFadeOut >= 0 ? overrideFadeOut : baseParams.fadeOutTime,
                overrideSustain >= 0 ? overrideSustain : baseParams.sustainTime
            );

            StartNewShake(p);
        }

        public void ShakeCamera(CameraShakeParams shakeParams)
        {
            if (shakeParams.profile == null)
            {
                Debug.LogWarning("[Camera Shake] No NoiseSettings profile provided!");
                return;
            }

            StartNewShake(shakeParams);
        }

        // Quick overload with defaults (still requires profile)
        public void ShakeCamera(
            NoiseSettings profile,
            float duration = -1f,
            float amplitude = -1f,
            float frequency = -1f,
            float fadeIn = 0.12f,
            float fadeOut = 0.25f,
            float sustain = 0f)
        {
            var p = new CameraShakeParams(
                profile,
                duration < 0 ? _defaultShakeDuration : duration,
                amplitude < 0 ? _defaultShakeAmplitude : amplitude,
                frequency < 0 ? _defaultShakeFrequency : frequency,
                fadeIn,
                fadeOut,
                sustain
            );
            ShakeCamera(p);
        }

        // ────────────────────────────────────────────────────────────────
        // Stacking / Additive logic
        // ────────────────────────────────────────────────────────────────

        private void StartNewShake(CameraShakeParams p)
        {
            var shake = new ActiveShake { currentAmplitude = 0f };

            shake.routine = StartCoroutine(ShakeWithFadeCoroutine(p, shake));

            _activeShakes.Add(shake);
        }

        private IEnumerator ShakeWithFadeCoroutine(CameraShakeParams p, ActiveShake thisShake)
        {
            float elapsed = 0f;

            // Calculate ideal phase times, but cap total to p.duration
            float idealTotal = p.fadeInTime + p.duration + p.fadeOutTime;
            float actualDuration = Mathf.Min(p.duration, idealTotal);  // respect override duration

            // Adjust phases proportionally if duration is shorter than ideal
            float scale = (idealTotal > 0) ? actualDuration / idealTotal : 1f;
            float fadeInEnd = p.fadeInTime * scale;
            float sustainEnd = fadeInEnd + (p.sustainTime * scale);
            float fadeOutEnd = actualDuration;  // fade-out gets whatever time is left

            //Debug.Log($"[Shake] {p.duration}s total | fadeIn: {fadeInEnd:F2} | sustainEnd: {sustainEnd:F2} | actual end: {actualDuration:F2}");

            // Phase 1: Fade IN
            while (elapsed < fadeInEnd)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInEnd;
                thisShake.currentAmplitude = Mathf.Lerp(0f, p.peakAmplitude, t);
                UpdateCombinedAmplitude();
                yield return null;
            }

            // Phase 2: Sustain at peak
            while (elapsed < sustainEnd)
            {
                elapsed += Time.deltaTime;
                thisShake.currentAmplitude = p.peakAmplitude;
                UpdateCombinedAmplitude();
                yield return null;
            }

            // Phase 3: Fade OUT (uses remaining time)
            float fadeOutStart = elapsed;
            float fadeOutDuration = actualDuration - fadeOutStart;
            while (elapsed < actualDuration)
            {
                elapsed += Time.deltaTime;
                float t = (elapsed - fadeOutStart) / fadeOutDuration;
                t = Mathf.Clamp01(t);
                thisShake.currentAmplitude = Mathf.Lerp(p.peakAmplitude, 0f, t);
                UpdateCombinedAmplitude();
                yield return null;
            }

            // Cleanup
            thisShake.currentAmplitude = 0f;
            UpdateCombinedAmplitude();
            _activeShakes.Remove(thisShake);
        }

        private void UpdateCombinedAmplitude()
        {
            float total = 0f;
            foreach (var shake in _activeShakes)
            {
                total += shake.currentAmplitude;
            }

            // Optional: cap to prevent extreme shaking
            // total = Mathf.Min(total, 12f);

            SetAmplitudeOnBoth(total);
        }

        private void SetAmplitudeOnBoth(float amp)
        {
            SetAmplitude(thirdPersonCam, amp);
            SetAmplitude(firstPersonCam, amp);
        }

        private void SetAmplitude(CinemachineVirtualCamera vcam, float amp)
        {
            if (vcam == null) return;
            var noise = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise == null) return;
            noise.m_AmplitudeGain = amp;
        }

        private void ResetShake(CinemachineVirtualCamera vcam)
        {
            if (vcam == null) return;
            var noise = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise == null) return;
            noise.m_AmplitudeGain = 0f;
        }

        // Utility: stop everything (e.g. player death, cutscene)
        public void StopAllShakes()
        {
            foreach (var shake in _activeShakes)
            {
                if (shake.routine != null)
                {
                    StopCoroutine(shake.routine);
                }
            }
            _activeShakes.Clear();
            SetAmplitudeOnBoth(0f);
        }

        // ────────────────────────────────────────────────────────────────
        // Internal helpers (unchanged)
        // ────────────────────────────────────────────────────────────────
        private (NoiseSettings profile, CameraShakeParams baseParams) GetBaseShake(ECameraShakeType type)
        {
            return type switch
            {
                ECameraShakeType.Fire =>
                    (_fireShakeSettings, CameraShakeParams.Fire(_fireShakeSettings)),
                ECameraShakeType.Damage =>
                    (_takeDamageShakeSettings, CameraShakeParams.Damage(_takeDamageShakeSettings)),
                ECameraShakeType.AOE =>
                    (_aoeShakeSettings, CameraShakeParams.AOE(_aoeShakeSettings)),
                _ => throw new ArgumentException($"Unsupported ShakeType: {type}")
            };
        }

        // ────────────────────────────────────────────────────────────────
        // Your original non-shake code goes here (OnInitialize, OnTick, raycast, etc.)
        // ────────────────────────────────────────────────────────────────
        // ...
    }

    // ────────────────────────────────────────────────────────────────
    // Shake Type & Parameters
    // ────────────────────────────────────────────────────────────────
    public enum ECameraShakeType
    {
        Fire,
        Damage,
        AOE,
        // Explosion,
        // Footstep,
        // etc.
    }
}