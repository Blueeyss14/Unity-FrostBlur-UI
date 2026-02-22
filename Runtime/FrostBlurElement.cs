using UnityEngine;
using UnityEngine.UI;

namespace FrostBlurUI
{
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/Frost Blur/Frost Blur Element")]
    [ExecuteAlways]
    public class FrostBlurElement : MonoBehaviour
    {
        static readonly int s_RectSizeID = Shader.PropertyToID("_RectSize");

        Graphic       _graphic;
        RectTransform _rect;
        Material      _matInstance;
        Vector2       _lastSize;

        void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _rect    = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            _graphic = GetComponent<Graphic>();
            _rect    = GetComponent<RectTransform>();

            if (_graphic.material != null && !_graphic.material.name.Contains("(Instance)"))
            {
                _matInstance      = new Material(_graphic.material);
                _graphic.material = _matInstance;
            }
            else
            {
                _matInstance = _graphic.material;
            }
            SyncSize();
        }

        void OnDisable()
        {
            if (_matInstance != null)
            {
                DestroyImmediate(_matInstance);
                _matInstance = null;
            }
        }

        void Update()
        {
            if (_rect.rect.size != _lastSize) SyncSize();
        }

        void OnValidate()                      => SyncSize();
        void OnRectTransformDimensionsChange() => SyncSize();

        void SyncSize()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            if (_graphic == null) _graphic = GetComponent<Graphic>();
            if (_graphic == null || _rect == null) return;

            if (_matInstance == null && _graphic.material != null)
            {
                _matInstance      = new Material(_graphic.material);
                _graphic.material = _matInstance;
            }

            if (_matInstance == null) return;

            _lastSize = _rect.rect.size;
            _matInstance.SetVector(s_RectSizeID, new Vector4(_lastSize.x, _lastSize.y, 0, 0));
        }
    }
}