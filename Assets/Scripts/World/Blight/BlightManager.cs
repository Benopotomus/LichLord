using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LichLord.World
{
    public class BlightManager : MonoBehaviour
    {
        public static BlightManager Instance;

        //max number of sources that can be set in the array
        private const int _SHADER_MAX = 64;
        //the global array prop
        private const string _SHADER_ARRAY = "_BlightArray";
        //the current count of relevant lights the shader needs to handle
        private const string _SHADER_COUNT = "_BlightCount";

        private int ShaderArrayID { get { return Shader.PropertyToID(_SHADER_ARRAY); } }
        private int ShaderCountID { get { return Shader.PropertyToID(_SHADER_COUNT); } }

        private Transform _referenceTransform;
        private Transform ReferenceTransform
        {
            get
            {
                if(_referenceTransform == null)
                    _referenceTransform = Camera.main?.transform;
                return _referenceTransform;
            }
        }

        private List<BlightSourceComponent> _blightSources = new List<BlightSourceComponent>();
        private List<BlightSourceComponent> _relevantSources = new List<BlightSourceComponent>();

        private Vector4[] _output = new Vector4[64];

        private bool _queueUpdate = false;
        private bool _hasUpdatedThisFrame = false;

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
            {
                if (Instance != this)
                    Destroy(this);
            }
        }

        public void RegisterSource(BlightSourceComponent source)
        {
            if(_blightSources.Contains(source) == false)
            {
                _blightSources.Add(source);
                source.OnBlightSourceChanged += HandleBlightSourceChanged;
                QueueUpdate();
            }
        }

        public void UnregisterSource(BlightSourceComponent source)
        {
            if(_blightSources.Contains(source))
            {
                _blightSources.Remove(source);
                source.OnBlightSourceChanged -= HandleBlightSourceChanged;
                QueueUpdate();
            }
        }

        private void HandleBlightSourceChanged(BlightSourceComponent source)
        {
            QueueUpdate();
        }

        private void QueueUpdate()
        {
            _queueUpdate = true;
        }

        private void Update()
        {
            if(_queueUpdate == true && _hasUpdatedThisFrame == false)
            {
                _hasUpdatedThisFrame = true;
                _queueUpdate = false;
                ProcessSourcesForShader();
            }
        }

        private void LateUpdate()
        {
            _hasUpdatedThisFrame = false;
        }

        private void ProcessSourcesForShader()
        {
            if (ReferenceTransform == null)
                return;

            _relevantSources.Clear();
            if (_blightSources.Count < _SHADER_MAX)
            {
                _relevantSources.AddRange(_blightSources);
            }
            else
            {              
                Vector3 refPosition = ReferenceTransform.position;
                var sortedSources = _blightSources
                    .OrderBy(source => (source.transform.position - refPosition).sqrMagnitude)
                    .Take(_SHADER_MAX);
                _relevantSources.AddRange(sortedSources);
            }

            OutputSourcesForShader();
        }

        private void OutputSourcesForShader()
        {
            int count = _relevantSources.Count;
            Shader.SetGlobalInt(ShaderCountID, count);

            for (int a = 0; a < count; a++)
            {
                _output[a] = _relevantSources[a].ShaderData;
            }

            Shader.SetGlobalVectorArray(ShaderArrayID, _output);
        }

        private void OnDestroy()
        {
            Shader.SetGlobalInt(ShaderCountID, 0);
        }
    }
}