using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Controla el Menú de Inicio (Build Index 1): navegación limpia entre paneles modales,
    /// selector de niveles, confirmación de salida y transición hacia la Pantalla de Carga del Juego.
    /// </summary>
    public class MenuInicioController : MonoBehaviour
    {
        [Header("Grupos de Paneles")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private GameObject panelPrincipal;
        [SerializeField] private GameObject panelSelectorNiveles;
        [SerializeField] private GameObject panelComoJugar;
        [SerializeField] private GameObject panelOpciones;
        [SerializeField] private GameObject panelCreditos;
        [SerializeField] private GameObject panelConfirmarSalida;

        [Header("Botones Principales")]
        [SerializeField] private Button btnJugar;
        [SerializeField] private Button btnSeleccionarNivel;
        [SerializeField] private Button btnComoJugar;
        [SerializeField] private Button btnOpciones;
        [SerializeField] private Button btnCreditos;
        [SerializeField] private Button btnSalir;

        [Header("Botones Volver / Cerrar Modales")]
        [SerializeField] private Button btnCerrarSelectorNiveles;
        [SerializeField] private Button btnCerrarComoJugar;
        [SerializeField] private Button btnCerrarOpciones;
        [SerializeField] private Button btnCerrarCreditos;
        [SerializeField] private Button btnConfirmarSalirSi;
        [SerializeField] private Button btnConfirmarSalirNo;

        [Header("Audio de Menú")]
        [SerializeField] private AudioSource menuMusicSource;
        [SerializeField] private float targetMusicVolume = 0.35f;
        [SerializeField] private float fadeDuration = 0.45f;

        private bool isTransitioning;

        private void Awake()
        {
            if (btnJugar != null) btnJugar.onClick.AddListener(OnPlayClicked);
            if (btnSeleccionarNivel != null) btnSeleccionarNivel.onClick.AddListener(() => ShowModal(panelSelectorNiveles));
            if (btnComoJugar != null) btnComoJugar.onClick.AddListener(() => ShowModal(panelComoJugar));
            if (btnOpciones != null) btnOpciones.onClick.AddListener(() => ShowModal(panelOpciones));
            if (btnCreditos != null) btnCreditos.onClick.AddListener(() => ShowModal(panelCreditos));
            if (btnSalir != null) btnSalir.onClick.AddListener(() => ShowModal(panelConfirmarSalida));

            if (btnCerrarSelectorNiveles != null) btnCerrarSelectorNiveles.onClick.AddListener(ShowMainPanelOnly);
            if (btnCerrarComoJugar != null) btnCerrarComoJugar.onClick.AddListener(ShowMainPanelOnly);
            if (btnCerrarOpciones != null) btnCerrarOpciones.onClick.AddListener(ShowMainPanelOnly);
            if (btnCerrarCreditos != null) btnCerrarCreditos.onClick.AddListener(ShowMainPanelOnly);
            if (btnConfirmarSalirNo != null) btnConfirmarSalirNo.onClick.AddListener(ShowMainPanelOnly);
            if (btnConfirmarSalirSi != null) btnConfirmarSalirSi.onClick.AddListener(OnConfirmQuitClicked);

            if (panelSelectorNiveles != null)
            {
                LevelSelectorPanelController selector = panelSelectorNiveles.GetComponent<LevelSelectorPanelController>();
                if (selector != null)
                {
                    selector.SetOnLevelLaunchingCallback(OnBeforeLevelSceneChange);
                }
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MonsterAI.UsarGameOverExterno = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UISettingsManager.ApplyAllSettings();
            ShowMainPanelOnly();
            StartCoroutine(FadeInRoutine());
        }

        private void Update()
        {
            if (isTransitioning) return;

            if (UISettingsManager.WasEscapeOrPausePressed())
            {
                if (IsAnyModalOpen())
                {
                    ShowMainPanelOnly();
                }
                else
                {
                    ShowModal(panelConfirmarSalida);
                }
            }
        }

        private void OnDestroy()
        {
            if (btnJugar != null) btnJugar.onClick.RemoveAllListeners();
            if (btnSeleccionarNivel != null) btnSeleccionarNivel.onClick.RemoveAllListeners();
            if (btnComoJugar != null) btnComoJugar.onClick.RemoveAllListeners();
            if (btnOpciones != null) btnOpciones.onClick.RemoveAllListeners();
            if (btnCreditos != null) btnCreditos.onClick.RemoveAllListeners();
            if (btnSalir != null) btnSalir.onClick.RemoveAllListeners();
            if (btnCerrarSelectorNiveles != null) btnCerrarSelectorNiveles.onClick.RemoveAllListeners();
            if (btnCerrarComoJugar != null) btnCerrarComoJugar.onClick.RemoveAllListeners();
            if (btnCerrarOpciones != null) btnCerrarOpciones.onClick.RemoveAllListeners();
            if (btnCerrarCreditos != null) btnCerrarCreditos.onClick.RemoveAllListeners();
            if (btnConfirmarSalirNo != null) btnConfirmarSalirNo.onClick.RemoveAllListeners();
            if (btnConfirmarSalirSi != null) btnConfirmarSalirSi.onClick.RemoveAllListeners();
        }

        private bool IsAnyModalOpen()
        {
            return (panelSelectorNiveles != null && panelSelectorNiveles.activeSelf)
                || (panelComoJugar != null && panelComoJugar.activeSelf)
                || (panelOpciones != null && panelOpciones.activeSelf)
                || (panelCreditos != null && panelCreditos.activeSelf)
                || (panelConfirmarSalida != null && panelConfirmarSalida.activeSelf);
        }

        public void ShowMainPanelOnly()
        {
            ClearSelection();
            if (panelPrincipal != null) panelPrincipal.SetActive(true);
            if (panelSelectorNiveles != null) panelSelectorNiveles.SetActive(false);
            if (panelComoJugar != null) panelComoJugar.SetActive(false);
            if (panelOpciones != null) panelOpciones.SetActive(false);
            if (panelCreditos != null) panelCreditos.SetActive(false);
            if (panelConfirmarSalida != null) panelConfirmarSalida.SetActive(false);
        }

        private void ShowModal(GameObject modal)
        {
            if (isTransitioning) return;
            ShowMainPanelOnly();
            if (modal != null) modal.SetActive(true);
            ClearSelection();
        }

        private void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (panelPrincipal != null)
            {
                UIButtonFX[] buttons = panelPrincipal.GetComponentsInChildren<UIButtonFX>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].ResetVisualState();
                }
            }
        }

        private void OnBeforeLevelSceneChange()
        {
            isTransitioning = true;
            UISettingsManager.Save();
        }

        private void OnPlayClicked()
        {
            if (isTransitioning) return;
            StartCoroutine(LoadGameLoadingScreenRoutine(0));
        }

        private void OnConfirmQuitClicked()
        {
            if (isTransitioning) return;
            UISettingsManager.QuitApplication();
        }

        private IEnumerator FadeInRoutine()
        {
            if (menuMusicSource != null)
            {
                menuMusicSource.volume = 0f;
                if (!menuMusicSource.isPlaying) menuMusicSource.Play();
            }

            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.alpha = 1f;
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / fadeDuration);
                    fadeOverlay.alpha = 1f - k;
                    if (menuMusicSource != null) menuMusicSource.volume = targetMusicVolume * k;
                    yield return null;
                }
                fadeOverlay.alpha = 0f;
                fadeOverlay.gameObject.SetActive(false);
            }
            else if (menuMusicSource != null)
            {
                menuMusicSource.volume = targetMusicVolume;
            }
        }

        private IEnumerator LoadGameLoadingScreenRoutine(int levelIndex)
        {
            isTransitioning = true;
            UISettingsManager.Save();

            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.alpha = 0f;
                float startVol = menuMusicSource != null ? menuMusicSource.volume : 0f;
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / fadeDuration);
                    fadeOverlay.alpha = k;
                    if (menuMusicSource != null) menuMusicSource.volume = Mathf.Lerp(startVol, 0f, k);
                    yield return null;
                }
                fadeOverlay.alpha = 1f;
            }

            UILevelFlow.LoadLevel(levelIndex);
        }
    }
}
