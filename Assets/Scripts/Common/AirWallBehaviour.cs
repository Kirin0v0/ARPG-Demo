using System;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Common
{
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public class AirWallBehaviour : MonoBehaviour
    {
        private static readonly int ShaderPropertyPlayerPosition = Shader.PropertyToID("_PlayerPosition");
        private static readonly int ShaderPropertyMainTexture = Shader.PropertyToID("_MainTexture");
        private static readonly int ShaderPropertyScanTexture = Shader.PropertyToID("_ScanTexture");
        private static readonly int ShaderPropertyScrollSpeed = Shader.PropertyToID("_ScrollSpeed");
        private static readonly int ShaderPropertyMaxDistance = Shader.PropertyToID("_MaxDistance");
        private static readonly int ShaderPropertyEmissionIntensity = Shader.PropertyToID("_EmissionIntensity");

        [SerializeField] private Material materialPrefab;

        [SerializeField, InlineButton("SetMainTextureTilling")]
        private Vector2 mainTextureTilling = Vector2.one;

        [SerializeField, InlineButton("SetScanTextureTilling")]
        private Vector2 scanTextureTilling = Vector2.one;

        [SerializeField, InlineButton("SetScrollSpeed")]
        private float scrollSpeed = 0.2f;

        [SerializeField, InlineButton("SetMaxDistance")]
        private float maxDistance = 5;

        [SerializeField, InlineButton("SetEmissionIntensity")]
        private float emissionIntensity = 1.2f;

        [Inject] private GameManager _gameManager;

        private Material _material;
        private Material _originalMaterial;
        private Renderer _renderer;

        private void Awake()
        {
            _material = Instantiate(materialPrefab);
            _renderer = GetComponent<Renderer>();
            _originalMaterial = _renderer.material;
            _renderer.material = _material;
            SetMainTextureTilling();
            SetScanTextureTilling();
        }

        private void Update()
        {
            if (_gameManager && _gameManager.Player)
            {
                _material.SetVector(ShaderPropertyPlayerPosition, _gameManager.Player.Parameters.position);
            }
        }

        private void OnDestroy()
        {
            if (_material)
            {
                GameObject.Destroy(_material);
                _material = null;
            }

            _renderer.material = _originalMaterial;
        }

        private void SetMainTextureTilling()
        {
            if (!_material)
            {
                return;
            }

            _material.SetTextureScale(ShaderPropertyMainTexture, mainTextureTilling);
        }

        private void SetScanTextureTilling()
        {
            if (!_material)
            {
                return;
            }

            _material.SetTextureScale(ShaderPropertyScanTexture, scanTextureTilling);
        }

        private void SetScrollSpeed()
        {
            if (!_material)
            {
                return;
            }

            _material.SetFloat(ShaderPropertyScrollSpeed, scrollSpeed);
        }

        private void SetMaxDistance()
        {
            if (!_material)
            {
                return;
            }

            _material.SetFloat(ShaderPropertyMaxDistance, maxDistance);
        }

        private void SetEmissionIntensity()
        {
            if (!_material)
            {
                return;
            }

            _material.SetFloat(ShaderPropertyEmissionIntensity, emissionIntensity);
        }
    }
}