using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Proporciona retroalimentación visual y sonora fluida (independiente de Time.timeScale)
    /// al pasar el cursor, enfocar o pulsar botones del menú, evitando selecciones residuales.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Animación de Escala")]
        [SerializeField] private float hoverScale = 1.032f;
        [SerializeField] private float pressedScale = 0.975f;
        [SerializeField] private float transitionSpeed = 16f;

        [Header("Acento Visual")]
        [SerializeField] private Graphic targetBackground;
        [SerializeField] private Color normalBgColor = new Color(0.075f, 0.133f, 0.165f, 0.92f);
        [SerializeField] private Color hoverBgColor = new Color(0.118f, 0.345f, 0.349f, 0.98f);
        [SerializeField] private Graphic outlineGraphic;
        [SerializeField] private Color normalOutlineColor = new Color(0.173f, 0.769f, 0.710f, 0.30f);
        [SerializeField] private Color hoverOutlineColor = new Color(0.361f, 0.918f, 0.847f, 0.88f);
        [SerializeField] private Graphic accentStripe;
        [SerializeField] private Color normalStripeColor = new Color(0.173f, 0.769f, 0.710f, 0.38f);
        [SerializeField] private Color hoverStripeColor = new Color(0.361f, 0.918f, 0.847f, 1.0f);
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Color normalTextColor = new Color(0.86f, 0.93f, 0.94f, 1f);
        [SerializeField] private Color hoverTextColor = Color.white;

        [Header("Audio UI (funciona incluso en pausa)")]
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private float sfxVolume = 0.45f;

        private Button button;
        private AudioSource sfxSource;
        private Vector3 baseScale = Vector3.one;
        private bool isHovered;
        private bool isSelected;
        private bool isPressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }

            baseScale = transform.localScale;
            if (targetBackground == null) targetBackground = GetComponent<Graphic>();
            if (outlineGraphic == null)
            {
                Transform outTr = transform.Find("Outline");
                if (outTr != null) outlineGraphic = outTr.GetComponent<Graphic>();
            }
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
                if (sources[i].ignoreListenerPause && !sources[i].playOnAwake && sources[i].clip == null)
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
            ResetVisualState();
        }

        private void OnDisable()
        {
            ResetVisualState();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClickSound);
            }
        }

        public void ResetVisualState()
        {
            isHovered = false;
            isSelected = false;
            isPressed = false;
            transform.localScale = baseScale;
            ApplyVisualsImmediate();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime * transitionSpeed;
            bool interactable = button == null || button.interactable;
            bool activeHighlight = interactable && (isHovered || isSelected);
            float disabledFactor = interactable ? 1f : 0.42f;

            float targetMul = 1f;
            if (interactable)
            {
                if (isPressed) targetMul = pressedScale;
                else if (activeHighlight) targetMul = hoverScale;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetMul, dt);

            if (targetBackground != null)
            {
                Color c = activeHighlight ? hoverBgColor : normalBgColor;
                c.a *= interactable ? 1f : 0.65f;
                targetBackground.color = Color.Lerp(targetBackground.color, c, dt);
            }
            if (outlineGraphic != null)
            {
                Color c = activeHighlight ? hoverOutlineColor : normalOutlineColor;
                c.a *= disabledFactor;
                outlineGraphic.color = Color.Lerp(outlineGraphic.color, c, dt);
            }
            if (accentStripe != null)
            {
                Color c = activeHighlight ? hoverStripeColor : normalStripeColor;
                c.a *= disabledFactor;
                accentStripe.color = Color.Lerp(accentStripe.color, c, dt);
            }
            if (labelText != null)
            {
                Color c = activeHighlight ? hoverTextColor : normalTextColor;
                c.a *= disabledFactor;
                labelText.color = Color.Lerp(labelText.color, c, dt);
            }
        }

        private void ApplyVisualsImmediate()
        {
            if (targetBackground != null) targetBackground.color = normalBgColor;
            if (outlineGraphic != null) outlineGraphic.color = normalOutlineColor;
            if (accentStripe != null) accentStripe.color = normalStripeColor;
            if (labelText != null) labelText.color = normalTextColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            isHovered = true;
            PlayClip(hoverClip, 0.7f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
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
            if (eventData is PointerEventData) return;
            isSelected = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            isSelected = false;
            isPressed = false;
        }

        private void HandleClickSound()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            isSelected = false;
            isPressed = false;
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
