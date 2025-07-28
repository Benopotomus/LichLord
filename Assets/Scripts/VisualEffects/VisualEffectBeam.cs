using Codice.CM.Common;
using DWD.Pooling;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    public class VisualEffectBeam : DWDObjectPoolObject
    {
        //[SerializeField] VisualEffectBase _visualEffectBase;

        [SerializeField] ParticleSystem[] _startSystems;
        [SerializeField] ParticleSystem[] _endSystems;

        [Header ("Beam")]
        [SerializeField] 
        LineRenderer[] _lineRenderers;
        [SerializeField]
        private List<ParticleSystem> _beamSystems = new List<ParticleSystem>();
        [SerializeField]
        private List<UnityEngine.ParticleSystem.MinMaxCurve> _defaultDensity = new List<UnityEngine.ParticleSystem.MinMaxCurve>();

        private bool _isOn;
        public void ToggleBeam(bool isOn)
        {
            if (_isOn == isOn)
                return;

            _isOn = isOn;

            foreach (LineRenderer line in _lineRenderers)
            {
                line.enabled = _isOn;
            }

            foreach (var beamSystem in _beamSystems)
            {
                if (_isOn)
                    beamSystem.Play();
                else
                    beamSystem.Stop();
            }

            foreach (var beamSystem in _startSystems)
            {
                if (_isOn)
                    beamSystem.Play();
                else
                    beamSystem.Stop();
            }

            foreach (var beamSystem in _endSystems)
            {
                if (_isOn)
                    beamSystem.Play();
                else
                    beamSystem.Stop();
            }
        }

        public void UpdateBeamPosition(Vector3 startPosition, Vector3 targetPosition)
        {
            foreach (LineRenderer _line in _lineRenderers)
            {
                _line.useWorldSpace = true; // Needed.
                _line.SetPosition(1, startPosition);
                _line.SetPosition(0, targetPosition);
            }

            foreach (var beamSystem in _beamSystems)
            {
                beamSystem.transform.position = startPosition;
            }

            foreach (var beamSystem in _startSystems)
            {
                beamSystem.transform.position = startPosition;
            }

            foreach (var beamSystem in _endSystems)
            {
                beamSystem.transform.position = targetPosition;
            }

            UpdateBeamParticleDirection(startPosition, targetPosition);
            UpdateParticleDensity(startPosition, targetPosition);
        }

        private void UpdateBeamParticleDirection(Vector3 startPosition, Vector3 targetPosition)
        {
            foreach (ParticleSystem _ps in _beamSystems)
            {
                // Make particle look toward target.

                Quaternion _lookRotation = Quaternion.LookRotation(targetPosition - startPosition).normalized;
                _ps.gameObject.transform.rotation = _lookRotation;

                // Make shape lenght equal to distance between particle's start and end point.
                var sh = _ps.shape;
                sh.rotation = new Vector3(0, 90, 0); // We do this to allign the beam with the forward direction.
                float beamLenght = Vector3.Distance(targetPosition, startPosition) / 2; // Divide by two since it increases on negative and positive axis
                sh.radius = beamLenght;
                // Increase offset on the Z shape position to set the pivot at start point.
                sh.position = new Vector3(0, 0, beamLenght);
            }
        }

        private void UpdateParticleDensity(Vector3 startPosition, Vector3 targetPosition)
        {
            float distance = Vector3.Distance(startPosition, targetPosition);
            distance -= 5f;
            if (distance > 0)
            {
                float distanceMultiplier = 1 + (distance / 5);
                for (int i = 0; i < _beamSystems.Count; i++)
                {
                    var emission = _beamSystems[i].emission;
                    emission.rateOverTime = _defaultDensity[i].constant * distanceMultiplier;
                }
            }
            else
            {
                for (int i = 0; i < _beamSystems.Count; i++)
                {
                    var emission = _beamSystems[i].emission;
                    emission.rateOverTime = _defaultDensity[i].constant;
                }
            }
        }

        public void StartRecycle(float delay)
        {
            ToggleBeam(false);

            if (delay == 0)
                RecycleVisualEffect();
            else
                StartCoroutine(RecycleAfterDelay(delay));
        }

        protected IEnumerator RecycleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            RecycleVisualEffect();
        }

        public void RecycleVisualEffect()
        {
            foreach (LineRenderer _line in _lineRenderers)
            {
                _line.useWorldSpace = true;
                _line.SetPosition(1, DWDObjectPool.POOL_LOCATION);
                _line.SetPosition(0, DWDObjectPool.POOL_LOCATION);
                _line.enabled = false;
            }

            DWDObjectPool.Instance.Recycle(this);
        }
    }
}
