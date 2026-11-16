using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Controla la Pantalla de Carga Inicial (Build Index 0) al iniciar el juego
    /// y realiza la transición fluida hacia el Menú de Inicio.
    /// </summary>
    public class PantallaCargaInicialController : MonoBehaviour
    {
        [Header("Referencias UI")]
        [SerializeField] private CanvasGroup rootFadeGroup;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private RectTransform spinnerRing;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text percentLabel;
        [SerializeField] private TMP_Text skipHintLabel;

        [Header("Tiempos")]
        [SerializeField] private float minDisplayDuration = 2.6f;
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float spinnerSpeed = -120f;

        private static readonly string[] StatusMessages =
        {
            "DESPERTANDO LA BRUMA DEL BARRIO...",
            "CALIBRANDO SENSORES DE AGUA...",
            "LOCALIZANDO PUNTOS DE RECICLAJE...",
            "PREPARANDO MENÚ PRINCIPAL..."
        };

        private float displayedProgress;
        private bool canSkip;
        private bool finishing;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UISettingsManager.ApplyAllSettings();

            if (skipHintLabel != null)
            {
                skipHintLabel.alpha = 0f;
            }

            StartCoroutine(BootSequenceRoutine());
        }

        private void Update()
        {
            if (spinnerRing != null)
            {
                spinnerRing.Rotate(0f, 0f, spinnerSpeed * Time.unscaledDeltaTime);
            }

            if (canSkip && !finishing && UISettingsManager.WasSkipPressed())
            {
                finishing = true;
            }
        }

        private IEnumerator BootSequenceRoutine()
        {
            if (rootFadeGroup != null)
            {
                rootFadeGroup.alpha = 0f;
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(UISettingsManager.SceneMainMenu);
            if (asyncLoad != null)
            {
                asyncLoad.allowSceneActivation = false;
            }

            float elapsed = 0f;
            while (!finishing)
            {
                elapsed += Time.unscaledDeltaTime;
                if (rootFadeGroup != null && rootFadeGroup.alpha < 1f)
                {
                    rootFadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                }

                float realProgress = asyncLoad != null ? Mathf.Clamp01(asyncLoad.progress / 0.9f) : 1f;
                float timeProgress = Mathf.Clamp01(elapsed / minDisplayDuration);
                float targetProgress = Mathf.Min(realProgress, timeProgress);

                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 0.9f);
                UpdateProgressUI(displayedProgress);

                if (elapsed >= 1.1f && !canSkip && realProgress >= 0.99f)
                {
                    canSkip = true;
                    if (skipHintLabel != null) skipHintLabel.alpha = 0.75f;
                }

                if (realProgress >= 0.99f && elapsed >= minDisplayDuration && displayedProgress >= 0.99f)
                {
                    finishing = true;
                }

                yield return null;
            }

            UpdateProgressUI(1f);

            if (rootFadeGroup != null)
            {
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    rootFadeGroup.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
                    yield return null;
                }
                rootFadeGroup.alpha = 0f;
            }

            if (asyncLoad != null)
            {
                asyncLoad.allowSceneActivation = true;
            }
            else
            {
                SceneManager.LoadScene(UISettingsManager.SceneMainMenu);
            }
        }

        private void UpdateProgressUI(float p)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01(p);
            }

            if (percentLabel != null)
            {
                percentLabel.text = Mathf.RoundToInt(p * 100f) + "%";
            }

            if (statusLabel != null && StatusMessages.Length > 0)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt(p * StatusMessages.Length), 0, StatusMessages.Length - 1);
                statusLabel.text = StatusMessages[idx];
            }
        }
    }
}
