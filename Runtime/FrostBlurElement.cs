using UnityEngine;
using UnityEngine.UI;

namespace FrostBlurUI
{
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UI/Frost Blur/Frost Blur Element")]
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
            if (_graphic.material != null)
            {
                _matInstance      = new Material(_graphic.material);
                _graphic.material = _matInstance;
            }
            SyncSize();
        }

        void OnDisable()
        {
            if (_matInstance != null)
            {
                Destroy(_matInstance);
                _matInstance = null;
            }
        }

        void Update()
        {
            if (_rect.rect.size != _lastSize) SyncSize();
        }

#if UNITY_EDITOR
        void OnValidate()                      => SyncSize();
        void OnRectTransformDimensionsChange() => SyncSize();
#endif

        void SyncSize()
        {
            if (_matInstance == null || _rect == null) return;
            _lastSize = _rect.rect.size;
            _matInstance.SetVector(s_RectSizeID, new Vector4(_lastSize.x, _lastSize.y, 0, 0));
        }
    }
}
