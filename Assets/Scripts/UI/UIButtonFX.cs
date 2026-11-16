using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Proporciona retroalimentación visual y sonora fluida (independiente de Time.timeScale)
    /// al pasar el cursor, enfocar o pulsar botones del menú.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Animación de Escala")]
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressedScale = 0.97f;
        [SerializeField] private float transitionSpeed = 14f;

        [Header("Acento Visual Opcional")]
        [SerializeField] private Graphic targetBackground;
        [SerializeField] private Color normalBgColor = new Color(0.08f, 0.14f, 0.17f, 0.90f);
        [SerializeField] private Color hoverBgColor = new Color(0.13f, 0.27f, 0.31f, 0.98f);
        [SerializeField] private Graphic accentStripe;
        [SerializeField] private Color normalStripeColor = new Color(0.17f, 0.77f, 0.71f, 0.35f);
        [SerializeField] private Color hoverStripeColor = new Color(0.17f, 0.77f, 0.71f, 1.0f);
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Color normalTextColor = new Color(0.88f, 0.94f, 0.95f, 1f);
        [SerializeField] private Color hoverTextColor = Color.white;

        [Header("Audio UI (funciona incluso en pausa)")]
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private float sfxVolume = 0.45f;

        private Button button;
        private AudioSource sfxSource;
        private Vector3 baseScale = Vector3.one;
        private bool isHovered;
        private bool isPressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            baseScale = transform.localScale;
            if (targetBackground == null) targetBackground = GetComponent<Graphic>();
            if (labelText == null) labelText = GetComponentInChildren<TMP_Text>();

            sfxSource = FindOrCreateCanvasAudioSource();

            if (button != null)
            {
                button.onClick.AddListener(HandleClickSound);
            }
        }

        private AudioSource FindOrCreateCanvasAudioSource()
        {
            if (hoverClip == null && clickClip == null) return null;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            GameObject host = parentCanvas != null ? parentCanvas.rootCanvas.gameObject : gameObject;

            AudioSource[] sources = host.GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].ignoreListenerPause && sources[i].playOnAwake == false && sources[i].clip == null)
                {
                    return sources[i];
                }
            }

            AudioSource src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = true;
            return src;
        }

        private void OnEnable()
        {
            isHovered = false;
            isPressed = false;
            transform.localScale = baseScale;
            ApplyVisualsImmediate();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClickSound);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * transitionSpeed;
            float targetMul = 1f;
            if (button == null || button.interactable)
            {
                if (isPressed) targetMul = pressedScale;
                else if (isHovered) targetMul = hoverScale;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetMul, dt);

            bool activeHighlight = isHovered && (button == null || button.interactable);
            if (targetBackground != null)
            {
                targetBackground.color = Color.Lerp(targetBackground.color, activeHighlight ? hoverBgColor : normalBgColor, dt);
            }
            if (accentStripe != null)
            {
                accentStripe.color = Color.Lerp(accentStripe.color, activeHighlight ? hoverStripeColor : normalStripeColor, dt);
            }
            if (labelText != null)
            {
                labelText.color = Color.Lerp(labelText.color, activeHighlight ? hoverTextColor : normalTextColor, dt);
            }
        }

        private void ApplyVisualsImmediate()
        {
            if (targetBackground != null) targetBackground.color = normalBgColor;
            if (accentStripe != null) accentStripe.color = normalStripeColor;
            if (labelText != null) labelText.color = normalTextColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            isHovered = true;
            PlayClip(hoverClip, 0.7f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (button != null && !button.interactable) return;
            isHovered = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isHovered = false;
            isPressed = false;
        }

        private void HandleClickSound()
        {
            PlayClip(clickClip, 1f);
        }

        private void PlayClip(AudioClip clip, float scale)
        {
            if (clip == null) return;
            if (sfxSource == null) sfxSource = FindOrCreateCanvasAudioSource();
            if (sfxSource == null || !sfxSource.isActiveAndEnabled) return;

            sfxSource.ignoreListenerPause = true;
            sfxSource.PlayOneShot(clip, sfxVolume * scale);
        }
    }
}
