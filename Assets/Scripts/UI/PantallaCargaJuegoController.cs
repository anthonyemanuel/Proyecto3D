using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Pantalla de carga dedicada para entrar al nivel seleccionado (SampleScene o futuros niveles)
    /// con carga asíncrona, información dinámica del nivel y consejos temáticos.
    /// </summary>
    public class PantallaCargaJuegoController : MonoBehaviour
    {
        [Header("Referencias UI")]
        [SerializeField] private CanvasGroup rootFadeGroup;
        [SerializeField] private Image progressBarFill;
        [SerializeField] private RectTransform spinnerRing;
        [SerializeField] private TMP_Text levelHeaderLabel;
        [SerializeField] private TMP_Text percentLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text tipTitleLabel;
        [SerializeField] private TMP_Text tipBodyLabel;

        [Header("Tiempos")]
        [SerializeField] private float minDisplayDuration = 2.6f;
        [SerializeField] private float tipInterval = 2.2f;
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float spinnerSpeed = -135f;

        private struct SurvivalTip
        {
            public string Title;
            public string Body;

            public SurvivalTip(string title, string body)
            {
                Title = title;
                Body = body;
            }
        }

        private static readonly SurvivalTip[] Tips =
        {
            new SurvivalTip("TORMENTA Y AGUA", "Pisar el agua genera ondas y ruido: si entras en ella, el monstruo escuchará tu posición y te perseguirá."),
            new SurvivalTip("SISTEMA DE 3 VIDAS", "Si el monstruo te atrapa en el agua perderás una vida y reaparecerás en la plataforma segura. ¡Cuida tus 3 vidas!"),
            new SurvivalTip("PLATAFORMAS SEGURAS", "Permanece sobre las plataformas y veredas de ladrillo para quedar fuera del alcance del monstruo."),
            new SurvivalTip("FAROS Y CLASIFICACIÓN", "Sigue las auras luminosas: Papel (Azul) → Tacho Azul · Plástico (Dorado) → Tacho Blanco · Orgánico (Verde) → Tacho Marrón."),
            new SurvivalTip("MENÚ EN PARTIDA", "Pulsa [ESC] o [P] en cualquier momento para pausar, cambiar de nivel, ajustar la sensibilidad o revisar controles.")
        };

        private float displayedProgress;
        private int currentTipIndex;
        private float tipTimer;
        private string sceneToLoad;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MonsterAI.UsarGameOverExterno = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            sceneToLoad = UILevelFlow.TargetSceneName;
            if (!UILevelFlow.CanLoadScene(sceneToLoad))
            {
                sceneToLoad = UISettingsManager.SceneGameplay;
            }

            if (levelHeaderLabel != null)
            {
                string title = string.IsNullOrEmpty(UILevelFlow.TargetLevelTitle) ? "BARRIO INUNDADO" : UILevelFlow.TargetLevelTitle.ToUpperInvariant();
                levelHeaderLabel.text = "NIVEL " + UILevelFlow.TargetLevelNumber.ToString("00") + "  ·  " + title;
            }

            currentTipIndex = Random.Range(0, Tips.Length);
            ApplyTip(currentTipIndex);
            StartCoroutine(LoadGameplayRoutine());
        }

        private void Update()
        {
            if (spinnerRing != null)
            {
                spinnerRing.Rotate(0f, 0f, spinnerSpeed * Time.unscaledDeltaTime);
            }

            tipTimer += Time.unscaledDeltaTime;
            if (tipTimer >= tipInterval)
            {
                tipTimer = 0f;
                currentTipIndex = (currentTipIndex + 1) % Tips.Length;
                ApplyTip(currentTipIndex);
            }
        }

        private IEnumerator LoadGameplayRoutine()
        {
            if (rootFadeGroup != null)
            {
                rootFadeGroup.alpha = 0f;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
            if (op != null)
            {
                op.allowSceneActivation = false;
            }

            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                if (rootFadeGroup != null && rootFadeGroup.alpha < 1f)
                {
                    rootFadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                }

                float realProgress = op != null ? Mathf.Clamp01(op.progress / 0.9f) : 1f;
                float timeProgress = Mathf.Clamp01(elapsed / minDisplayDuration);
                float target = Mathf.Min(realProgress, timeProgress);

                displayedProgress = Mathf.MoveTowards(displayedProgress, target, Time.unscaledDeltaTime * 0.95f);
                UpdateProgress(displayedProgress);

                if (realProgress >= 0.99f && elapsed >= minDisplayDuration && displayedProgress >= 0.99f)
                {
                    break;
                }

                yield return null;
            }

            UpdateProgress(1f);

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

            if (op != null)
            {
                op.allowSceneActivation = true;
            }
            else
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }

        private void UpdateProgress(float p)
        {
            if (progressBarFill != null) progressBarFill.fillAmount = Mathf.Clamp01(p);
            if (percentLabel != null) percentLabel.text = Mathf.RoundToInt(p * 100f) + "%";
            if (statusLabel != null)
            {
                if (p < 0.35f) statusLabel.text = "GENERANDO ZONA INUNDADA...";
                else if (p < 0.75f) statusLabel.text = "ACTIVANDO RUTA DE PATRULLA Y RESIDUOS...";
                else if (p < 0.99f) statusLabel.text = "SINCRONIZANDO CONTENEDORES DE RECICLAJE...";
                else statusLabel.text = "LISTO. ¡MANTENTE SOBRE LOS LADRILLOS!";
            }
        }

        private void ApplyTip(int idx)
        {
            if (Tips.Length == 0) return;
            SurvivalTip tip = Tips[Mathf.Clamp(idx, 0, Tips.Length - 1)];
            if (tipTitleLabel != null) tipTitleLabel.text = tip.Title;
            if (tipBodyLabel != null) tipBodyLabel.text = tip.Body;
        }
    }
}
