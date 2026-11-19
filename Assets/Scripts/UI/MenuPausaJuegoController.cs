using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Menú del Juego en partida: Pausa (ESC/P), Selector/Cambio de Nivel,
    /// Ventana de Nivel Completado (Siguiente Nivel) y Ventana de Game Over (conectada a MonsterAI).
    /// No modifica mecánicas del jugador: únicamente gestiona pausa/cursor y transiciones de escena.
    /// </summary>
    public class MenuPausaJuegoController : MonoBehaviour
    {
        public static MenuPausaJuegoController Instance { get; private set; }

        [Header("Contenedores de Interfaz")]
        [SerializeField] private GameObject hudCornerHint;
        [SerializeField] private GameObject pauseBackdropRoot;
        [SerializeField] private CanvasGroup pauseFadeGroup;
        [SerializeField] private GameObject panelPausaPrincipal;
        [SerializeField] private GameObject panelCambiarNivel;
        [SerializeField] private GameObject panelControles;
        [SerializeField] private GameObject panelOpciones;
        [SerializeField] private GameObject panelConfirmarAccion;
        [SerializeField] private GameObject panelNivelCompletado;
        [SerializeField] private GameObject panelGameOver;

        [Header("Botones del Menú de Pausa")]
        [SerializeField] private Button btnContinuar;
        [SerializeField] private Button btnReiniciar;
        [SerializeField] private Button btnCambiarNivel;
        [SerializeField] private Button btnControles;
        [SerializeField] private Button btnOpciones;
        [SerializeField] private Button btnMenuInicio;
        [SerializeField] private Button btnSalir;

        [Header("Botones de Subpaneles")]
        [SerializeField] private Button btnCerrarCambiarNivel;
        [SerializeField] private Button btnCerrarControles;
        [SerializeField] private Button btnCerrarOpciones;
        [SerializeField] private TMP_Text confirmTitleText;
        [SerializeField] private TMP_Text confirmMessageText;
        [SerializeField] private Button btnConfirmarSi;
        [SerializeField] private Button btnConfirmarNo;

        [Header("Ventana Nivel Completado")]
        [SerializeField] private TMP_Text winLevelLabel;
        [SerializeField] private TMP_Text winStatsLabel;
        [SerializeField] private TMP_Text winNextNoticeLabel;
        [SerializeField] private Button btnSiguienteNivel;
        [SerializeField] private Button btnRepetirNivelWin;
        [SerializeField] private Button btnCambiarNivelWin;
        [SerializeField] private Button btnMenuInicioWin;

        [Header("Ventana Game Over")]
        [SerializeField] private TMP_Text gameOverLevelLabel;
        [SerializeField] private TMP_Text gameOverStatsLabel;
        [SerializeField] private Button btnReintentarGameOver;
        [SerializeField] private Button btnCambiarNivelGameOver;
        [SerializeField] private Button btnMenuInicioGameOver;
        [SerializeField] private Button btnSalirGameOver;

        private enum PendingAction { None, Restart, MainMenu, Quit }
        private enum LevelModalOrigin { Pause, Win, GameOver }

        private PlayerController playerController;
        private PlayerPickup playerPickup;
        private MonsterAI monsterAI;
        private bool prevControllerEnabled = true;
        private bool prevPickupEnabled = true;
        private bool isPaused;
        private bool isEndStateOpen;
        private bool isTransitioning;
        private PendingAction pendingAction = PendingAction.None;
        private LevelModalOrigin levelModalOrigin = LevelModalOrigin.Pause;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (btnContinuar != null) btnContinuar.onClick.AddListener(ResumeGame);
            if (btnReiniciar != null) btnReiniciar.onClick.AddListener(() => AskConfirmation(
                PendingAction.Restart,
                "REINICIAR PARTIDA",
                "¿Deseas reiniciar el nivel actual desde el comienzo? Se perderá el progreso de la ronda en curso."));
            if (btnCambiarNivel != null) btnCambiarNivel.onClick.AddListener(() => OpenLevelSwitcher(LevelModalOrigin.Pause));
            if (btnControles != null) btnControles.onClick.AddListener(() => ShowModalSubpanel(panelControles));
            if (btnOpciones != null) btnOpciones.onClick.AddListener(() => ShowModalSubpanel(panelOpciones));
            if (btnMenuInicio != null) btnMenuInicio.onClick.AddListener(() => AskConfirmation(
                PendingAction.MainMenu,
                "VOLVER AL MENÚ PRINCIPAL",
                "¿Deseas salir de la partida actual y regresar al Menú de Inicio?"));
            if (btnSalir != null) btnSalir.onClick.AddListener(() => AskConfirmation(
                PendingAction.Quit,
                "SALIR DEL JUEGO",
                "¿Estás seguro de que deseas cerrar Sneaky Recycling?"));

            if (btnCerrarCambiarNivel != null) btnCerrarCambiarNivel.onClick.AddListener(ReturnFromLevelSwitcher);
            if (btnCerrarControles != null) btnCerrarControles.onClick.AddListener(ShowMainPausePanel);
            if (btnCerrarOpciones != null) btnCerrarOpciones.onClick.AddListener(ShowMainPausePanel);
            if (btnConfirmarNo != null) btnConfirmarNo.onClick.AddListener(ShowMainPausePanel);
            if (btnConfirmarSi != null) btnConfirmarSi.onClick.AddListener(ExecuteConfirmedAction);

            // Ventana Nivel Completado
            if (btnSiguienteNivel != null) btnSiguienteNivel.onClick.AddListener(OnNextLevelClicked);
            if (btnRepetirNivelWin != null) btnRepetirNivelWin.onClick.AddListener(ReloadCurrentLevelNow);
            if (btnCambiarNivelWin != null) btnCambiarNivelWin.onClick.AddListener(() => OpenLevelSwitcher(LevelModalOrigin.Win));
            if (btnMenuInicioWin != null) btnMenuInicioWin.onClick.AddListener(ReturnToMainMenuNow);

            // Ventana Game Over
            if (btnReintentarGameOver != null) btnReintentarGameOver.onClick.AddListener(ReloadCurrentLevelNow);
            if (btnCambiarNivelGameOver != null) btnCambiarNivelGameOver.onClick.AddListener(() => OpenLevelSwitcher(LevelModalOrigin.GameOver));
            if (btnMenuInicioGameOver != null) btnMenuInicioGameOver.onClick.AddListener(ReturnToMainMenuNow);
            if (btnSalirGameOver != null) btnSalirGameOver.onClick.AddListener(() =>
            {
                isTransitioning = true;
                Time.timeScale = 1f;
                AudioListener.pause = false;
                UISettingsManager.QuitApplication();
            });

            if (panelCambiarNivel != null)
            {
                LevelSelectorPanelController selector = panelCambiarNivel.GetComponent<LevelSelectorPanelController>();
                if (selector != null)
                {
                    selector.SetOnLevelLaunchingCallback(() => isTransitioning = true);
                }
            }
        }

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MonsterAI.UsarGameOverExterno = true;

            UISettingsManager.ApplyAllSettings();

            playerController = FindFirstObjectByType<PlayerController>();
            playerPickup = FindFirstObjectByType<PlayerPickup>();
            monsterAI = FindFirstObjectByType<MonsterAI>();
            UISettingsManager.ApplySensitivityToScenePlayer();

            if (pauseBackdropRoot != null) pauseBackdropRoot.SetActive(false);
            if (hudCornerHint != null) hudCornerHint.SetActive(true);
            isPaused = false;
            isEndStateOpen = false;
            ClearSelection();
        }

        private void Update()
        {
            if (isTransitioning) return;

            if (!isEndStateOpen && monsterAI != null && monsterAI.JuegoTerminado)
            {
                ShowGameOverWindow();
                return;
            }

            if (isEndStateOpen)
            {
                if (panelGameOver != null && panelGameOver.activeSelf && WasRestartKeyPressed())
                {
                    ReloadCurrentLevelNow();
                }
                else if (panelCambiarNivel != null && panelCambiarNivel.activeSelf && UISettingsManager.WasEscapeOrPausePressed())
                {
                    ReturnFromLevelSwitcher();
                }
                return;
            }

            if (UISettingsManager.WasEscapeOrPausePressed())
            {
                if (!isPaused)
                {
                    PauseGame();
                }
                else if (IsPauseSubpanelOpen())
                {
                    if (panelCambiarNivel != null && panelCambiarNivel.activeSelf)
                        ReturnFromLevelSwitcher();
                    else
                        ShowMainPausePanel();
                }
                else
                {
                    ResumeGame();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            MonsterAI.UsarGameOverExterno = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (btnContinuar != null) btnContinuar.onClick.RemoveAllListeners();
            if (btnReiniciar != null) btnReiniciar.onClick.RemoveAllListeners();
            if (btnCambiarNivel != null) btnCambiarNivel.onClick.RemoveAllListeners();
            if (btnControles != null) btnControles.onClick.RemoveAllListeners();
            if (btnOpciones != null) btnOpciones.onClick.RemoveAllListeners();
            if (btnMenuInicio != null) btnMenuInicio.onClick.RemoveAllListeners();
            if (btnSalir != null) btnSalir.onClick.RemoveAllListeners();
            if (btnCerrarCambiarNivel != null) btnCerrarCambiarNivel.onClick.RemoveAllListeners();
            if (btnCerrarControles != null) btnCerrarControles.onClick.RemoveAllListeners();
            if (btnCerrarOpciones != null) btnCerrarOpciones.onClick.RemoveAllListeners();
            if (btnConfirmarNo != null) btnConfirmarNo.onClick.RemoveAllListeners();
            if (btnConfirmarSi != null) btnConfirmarSi.onClick.RemoveAllListeners();
            if (btnSiguienteNivel != null) btnSiguienteNivel.onClick.RemoveAllListeners();
            if (btnRepetirNivelWin != null) btnRepetirNivelWin.onClick.RemoveAllListeners();
            if (btnCambiarNivelWin != null) btnCambiarNivelWin.onClick.RemoveAllListeners();
            if (btnMenuInicioWin != null) btnMenuInicioWin.onClick.RemoveAllListeners();
            if (btnReintentarGameOver != null) btnReintentarGameOver.onClick.RemoveAllListeners();
            if (btnCambiarNivelGameOver != null) btnCambiarNivelGameOver.onClick.RemoveAllListeners();
            if (btnMenuInicioGameOver != null) btnMenuInicioGameOver.onClick.RemoveAllListeners();
            if (btnSalirGameOver != null) btnSalirGameOver.onClick.RemoveAllListeners();
        }

        // ====== API Pública para Cambio / Fin de Nivel ======

        public static void MostrarNivelCompletado(int puntaje = -1)
        {
            if (Instance != null)
            {
                Instance.ShowLevelCompletedWindow(puntaje);
            }
        }

        public static void MostrarGameOver(int puntaje = -1)
        {
            if (Instance != null)
            {
                Instance.ShowGameOverWindow(puntaje);
            }
        }

        public static bool CargarSiguienteNivel()
        {
            return UILevelFlow.TryLoadNextLevel();
        }

        public void ShowLevelCompletedWindow(int puntaje = -1)
        {
            if (isTransitioning) return;

            int activeIdx = UILevelFlow.GetActiveLevelIndex();
            UILevelFlow.MarkLevelCompleted(activeIdx, puntaje);

            FreezePlayerAndShowOverlay();
            isEndStateOpen = true;
            isPaused = false;

            HideAllPanels();
            if (panelNivelCompletado != null) panelNivelCompletado.SetActive(true);

            LevelCatalog.LevelEntry entry = UILevelFlow.GetActiveLevelEntry();
            string levelName = entry != null ? ("NIVEL " + entry.levelNumber.ToString("00") + "  ·  " + entry.displayName.ToUpperInvariant()) : SceneManager.GetActiveScene().name.ToUpperInvariant();
            if (winLevelLabel != null) winLevelLabel.text = levelName;

            int lives = monsterAI != null ? monsterAI.VidasActuales : 3;
            int maxLives = monsterAI != null ? monsterAI.vidasMaximas : 3;
            if (winStatsLabel != null)
            {
                winStatsLabel.text = puntaje >= 0
                    ? "Vidas restantes: <color=#5CEAD8><b>" + lives + " / " + maxLives + "</b></color>    ·    Puntaje: <color=#5CEAD8><b>" + puntaje + "</b></color>"
                    : "Vidas restantes: <color=#5CEAD8><b>" + lives + " / " + maxLives + "</b></color>    ·    Estado: <color=#5CEAD8><b>Zona Reciclada</b></color>";
            }

            bool hasNextPlayable = UILevelFlow.HasNextPlayableLevel(activeIdx);
            if (btnSiguienteNivel != null)
            {
                btnSiguienteNivel.interactable = hasNextPlayable;
                TMP_Text lbl = btnSiguienteNivel.GetComponentInChildren<TMP_Text>();
                if (lbl != null)
                {
                    lbl.text = hasNextPlayable ? "SIGUIENTE NIVEL" : "SIGUIENTE NIVEL (PRÓXIMAMENTE)";
                }
            }

            if (winNextNoticeLabel != null)
            {
                winNextNoticeLabel.text = hasNextPlayable
                    ? "El siguiente sector está desbloqueado y listo para comenzar."
                    : "Aún no hay una escena de Nivel 2 agregada en Build Settings. Puedes repetir el nivel o volver al menú.";
            }

            ClearSelection();
        }

        public void ShowGameOverWindow(int puntaje = -1)
        {
            if (isTransitioning) return;

            FreezePlayerAndShowOverlay();
            isEndStateOpen = true;
            isPaused = false;

            HideAllPanels();
            if (panelGameOver != null) panelGameOver.SetActive(true);

            LevelCatalog.LevelEntry entry = UILevelFlow.GetActiveLevelEntry();
            string levelName = entry != null ? ("NIVEL " + entry.levelNumber.ToString("00") + "  ·  " + entry.displayName.ToUpperInvariant()) : SceneManager.GetActiveScene().name.ToUpperInvariant();
            if (gameOverLevelLabel != null) gameOverLevelLabel.text = levelName;

            if (gameOverStatsLabel != null)
            {
                TrashItem[] remainingTrash = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
                int leftCount = remainingTrash != null ? remainingTrash.Length : 0;
                gameOverStatsLabel.text = puntaje >= 0
                    ? "Vidas: <color=#FF6B6B><b>0 / 3</b></color>    ·    Puntaje: <color=#FF9999><b>" + puntaje + "</b></color>"
                    : "Vidas: <color=#FF6B6B><b>0 / 3</b></color>    ·    Residuos pendientes: <color=#FF9999><b>" + leftCount + "</b></color>";
            }

            ClearSelection();
        }

        // ====== Pausa Estándar ======

        public void PauseGame()
        {
            if (isPaused || isEndStateOpen || isTransitioning) return;
            isPaused = true;

            FreezePlayerAndShowOverlay();
            ShowMainPausePanel();
        }

        public void ResumeGame()
        {
            if (!isPaused || isEndStateOpen || isTransitioning) return;
            isPaused = false;
            pendingAction = PendingAction.None;

            UISettingsManager.Save();
            ClearSelection();

            if (pauseBackdropRoot != null) pauseBackdropRoot.SetActive(false);
            if (hudCornerHint != null) hudCornerHint.SetActive(true);

            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (playerController != null)
            {
                playerController.sensibilidadMouse = UISettingsManager.MouseSensitivity;
                playerController.enabled = prevControllerEnabled;
            }
            if (playerPickup != null)
            {
                playerPickup.enabled = prevPickupEnabled;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void FreezePlayerAndShowOverlay()
        {
            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
            if (playerPickup == null) playerPickup = FindFirstObjectByType<PlayerPickup>();
            if (monsterAI == null) monsterAI = FindFirstObjectByType<MonsterAI>();

            if (playerController != null)
            {
                prevControllerEnabled = playerController.enabled;
                playerController.enabled = false;
            }
            if (playerPickup != null)
            {
                prevPickupEnabled = playerPickup.enabled;
                playerPickup.enabled = false;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (hudCornerHint != null) hudCornerHint.SetActive(false);
            if (pauseBackdropRoot != null) pauseBackdropRoot.SetActive(true);
            if (pauseFadeGroup != null) pauseFadeGroup.alpha = 1f;
        }

        private bool IsPauseSubpanelOpen()
        {
            return (panelCambiarNivel != null && panelCambiarNivel.activeSelf)
                || (panelControles != null && panelControles.activeSelf)
                || (panelOpciones != null && panelOpciones.activeSelf)
                || (panelConfirmarAccion != null && panelConfirmarAccion.activeSelf);
        }

        private void HideAllPanels()
        {
            if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(false);
            if (panelCambiarNivel != null) panelCambiarNivel.SetActive(false);
            if (panelControles != null) panelControles.SetActive(false);
            if (panelOpciones != null) panelOpciones.SetActive(false);
            if (panelConfirmarAccion != null) panelConfirmarAccion.SetActive(false);
            if (panelNivelCompletado != null) panelNivelCompletado.SetActive(false);
            if (panelGameOver != null) panelGameOver.SetActive(false);
        }

        private void ShowMainPausePanel()
        {
            pendingAction = PendingAction.None;
            HideAllPanels();
            if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(true);
            ClearSelection();
        }

        private void ShowModalSubpanel(GameObject subpanel)
        {
            HideAllPanels();
            if (subpanel != null) subpanel.SetActive(true);
            ClearSelection();
        }

        private void OpenLevelSwitcher(LevelModalOrigin origin)
        {
            levelModalOrigin = origin;
            ShowModalSubpanel(panelCambiarNivel);
        }

        private void ReturnFromLevelSwitcher()
        {
            switch (levelModalOrigin)
            {
                case LevelModalOrigin.Win:
                    ShowModalSubpanel(panelNivelCompletado);
                    break;
                case LevelModalOrigin.GameOver:
                    ShowModalSubpanel(panelGameOver);
                    break;
                default:
                    ShowMainPausePanel();
                    break;
            }
        }

        private void AskConfirmation(PendingAction action, string title, string message)
        {
            pendingAction = action;
            if (confirmTitleText != null) confirmTitleText.text = title;
            if (confirmMessageText != null) confirmMessageText.text = message;
            ShowModalSubpanel(panelConfirmarAccion);
        }

        private void ExecuteConfirmedAction()
        {
            if (isTransitioning) return;
            PendingAction chosen = pendingAction;
            pendingAction = PendingAction.None;

            switch (chosen)
            {
                case PendingAction.Restart:
                    ReloadCurrentLevelNow();
                    break;
                case PendingAction.MainMenu:
                    ReturnToMainMenuNow();
                    break;
                case PendingAction.Quit:
                    isTransitioning = true;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    UISettingsManager.QuitApplication();
                    break;
            }
        }

        private void OnNextLevelClicked()
        {
            if (isTransitioning) return;
            if (UILevelFlow.TryLoadNextLevel())
            {
                isTransitioning = true;
            }
            else if (winNextNoticeLabel != null)
            {
                winNextNoticeLabel.text = "No hay una escena siguiente disponible en Build Settings todavía.";
            }
        }

        private void ReloadCurrentLevelNow()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            UILevelFlow.ReloadCurrentLevel();
        }

        private void ReturnToMainMenuNow()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MonsterAI.UsarGameOverExterno = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UISettingsManager.Save();
            SceneManager.LoadScene(UISettingsManager.SceneMainMenu);
        }

        private void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static bool WasRestartKeyPressed()
        {
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                pressed = UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!pressed && UnityEngine.InputSystem.Keyboard.current == null)
            {
                pressed = Input.GetKeyDown(KeyCode.R);
            }
#endif
            return pressed;
        }
    }
}
