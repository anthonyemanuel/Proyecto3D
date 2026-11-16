using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Menú del Juego (Pausa, Opciones, Controles, Reinicio y Retorno al Menú de Inicio) dentro de SampleScene.
    /// No modifica ningún script de mecánicas existente: deshabilita temporalmente los scripts de control
    /// del jugador y pausa Time.timeScale/AudioListener únicamente mientras el menú está abierto.
    /// </summary>
    public class MenuPausaJuegoController : MonoBehaviour
    {
        [Header("Contenedores de Interfaz")]
        [SerializeField] private GameObject hudCornerHint;
        [SerializeField] private GameObject pauseBackdropRoot;
        [SerializeField] private CanvasGroup pauseFadeGroup;
        [SerializeField] private GameObject panelPausaPrincipal;
        [SerializeField] private GameObject panelControles;
        [SerializeField] private GameObject panelOpciones;
        [SerializeField] private GameObject panelConfirmarAccion;

        [Header("Botones del Menú de Pausa")]
        [SerializeField] private Button btnContinuar;
        [SerializeField] private Button btnReiniciar;
        [SerializeField] private Button btnControles;
        [SerializeField] private Button btnOpciones;
        [SerializeField] private Button btnMenuInicio;
        [SerializeField] private Button btnSalir;

        [Header("Botones de Subpaneles")]
        [SerializeField] private Button btnCerrarControles;
        [SerializeField] private Button btnCerrarOpciones;
        [SerializeField] private TMPro.TMP_Text confirmTitleText;
        [SerializeField] private TMPro.TMP_Text confirmMessageText;
        [SerializeField] private Button btnConfirmarSi;
        [SerializeField] private Button btnConfirmarNo;

        private enum PendingAction { None, Restart, MainMenu, Quit }

        private PlayerController playerController;
        private PlayerPickup playerPickup;
        private bool prevControllerEnabled = true;
        private bool prevPickupEnabled = true;
        private bool isPaused;
        private bool isTransitioning;
        private PendingAction pendingAction = PendingAction.None;

        private void Awake()
        {
            if (btnContinuar != null) btnContinuar.onClick.AddListener(ResumeGame);
            if (btnReiniciar != null) btnReiniciar.onClick.AddListener(() => AskConfirmation(
                PendingAction.Restart,
                "REINICIAR PARTIDA",
                "¿Deseas reiniciar el nivel actual desde el comienzo? Se perderá el progreso de la ronda en curso."));
            if (btnControles != null) btnControles.onClick.AddListener(() => ShowPauseSubpanel(panelControles));
            if (btnOpciones != null) btnOpciones.onClick.AddListener(() => ShowPauseSubpanel(panelOpciones));
            if (btnMenuInicio != null) btnMenuInicio.onClick.AddListener(() => AskConfirmation(
                PendingAction.MainMenu,
                "VOLVER AL MENÚ PRINCIPAL",
                "¿Deseas salir de la partida actual y regresar al Menú de Inicio?"));
            if (btnSalir != null) btnSalir.onClick.AddListener(() => AskConfirmation(
                PendingAction.Quit,
                "SALIR DEL JUEGO",
                "¿Estás seguro de que deseas cerrar Sneaky Recycling?"));

            if (btnCerrarControles != null) btnCerrarControles.onClick.AddListener(ShowMainPausePanel);
            if (btnCerrarOpciones != null) btnCerrarOpciones.onClick.AddListener(ShowMainPausePanel);
            if (btnConfirmarNo != null) btnConfirmarNo.onClick.AddListener(ShowMainPausePanel);
            if (btnConfirmarSi != null) btnConfirmarSi.onClick.AddListener(ExecuteConfirmedAction);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            UISettingsManager.ApplyAllSettings();

            playerController = FindFirstObjectByType<PlayerController>();
            playerPickup = FindFirstObjectByType<PlayerPickup>();
            UISettingsManager.ApplySensitivityToScenePlayer();

            if (pauseBackdropRoot != null) pauseBackdropRoot.SetActive(false);
            if (hudCornerHint != null) hudCornerHint.SetActive(true);
            isPaused = false;
        }

        private void Update()
        {
            if (isTransitioning) return;

            if (UISettingsManager.WasEscapeOrPausePressed())
            {
                if (!isPaused)
                {
                    PauseGame();
                }
                else if (IsSubpanelOpen())
                {
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
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (btnContinuar != null) btnContinuar.onClick.RemoveAllListeners();
            if (btnReiniciar != null) btnReiniciar.onClick.RemoveAllListeners();
            if (btnControles != null) btnControles.onClick.RemoveAllListeners();
            if (btnOpciones != null) btnOpciones.onClick.RemoveAllListeners();
            if (btnMenuInicio != null) btnMenuInicio.onClick.RemoveAllListeners();
            if (btnSalir != null) btnSalir.onClick.RemoveAllListeners();
            if (btnCerrarControles != null) btnCerrarControles.onClick.RemoveAllListeners();
            if (btnCerrarOpciones != null) btnCerrarOpciones.onClick.RemoveAllListeners();
            if (btnConfirmarNo != null) btnConfirmarNo.onClick.RemoveAllListeners();
            if (btnConfirmarSi != null) btnConfirmarSi.onClick.RemoveAllListeners();
        }

        public void PauseGame()
        {
            if (isPaused || isTransitioning) return;
            isPaused = true;

            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
            if (playerPickup == null) playerPickup = FindFirstObjectByType<PlayerPickup>();

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
            ShowMainPausePanel();

            StopAllCoroutines();
            if (pauseFadeGroup != null) pauseFadeGroup.alpha = 1f;
        }

        public void ResumeGame()
        {
            if (!isPaused || isTransitioning) return;
            isPaused = false;
            pendingAction = PendingAction.None;

            UISettingsManager.Save();

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

        private bool IsSubpanelOpen()
        {
            return (panelControles != null && panelControles.activeSelf)
                || (panelOpciones != null && panelOpciones.activeSelf)
                || (panelConfirmarAccion != null && panelConfirmarAccion.activeSelf);
        }

        private void ShowMainPausePanel()
        {
            pendingAction = PendingAction.None;
            if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(true);
            if (panelControles != null) panelControles.SetActive(false);
            if (panelOpciones != null) panelOpciones.SetActive(false);
            if (panelConfirmarAccion != null) panelConfirmarAccion.SetActive(false);
        }

        private void ShowPauseSubpanel(GameObject subpanel)
        {
            if (panelPausaPrincipal != null) panelPausaPrincipal.SetActive(false);
            if (panelControles != null) panelControles.SetActive(subpanel == panelControles);
            if (panelOpciones != null) panelOpciones.SetActive(subpanel == panelOpciones);
            if (panelConfirmarAccion != null) panelConfirmarAccion.SetActive(subpanel == panelConfirmarAccion);
        }

        private void AskConfirmation(PendingAction action, string title, string message)
        {
            pendingAction = action;
            if (confirmTitleText != null) confirmTitleText.text = title;
            if (confirmMessageText != null) confirmMessageText.text = message;
            ShowPauseSubpanel(panelConfirmarAccion);
        }

        private void ExecuteConfirmedAction()
        {
            if (isTransitioning) return;
            PendingAction chosen = pendingAction;
            pendingAction = PendingAction.None;

            switch (chosen)
            {
                case PendingAction.Restart:
                    isTransitioning = true;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    UISettingsManager.Save();
                    SceneManager.LoadScene(UISettingsManager.SceneGameLoading);
                    break;

                case PendingAction.MainMenu:
                    isTransitioning = true;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    UISettingsManager.Save();
                    SceneManager.LoadScene(UISettingsManager.SceneMainMenu);
                    break;

                case PendingAction.Quit:
                    isTransitioning = true;
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    UISettingsManager.QuitApplication();
                    break;
            }
        }
    }
}
