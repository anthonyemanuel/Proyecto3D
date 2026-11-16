using System.Collections.Generic;
using UnityEngine;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Nombres canónicos de escenas y utilidades compartidas de configuración de usuario.
    /// No modifica scripts de mecánicas: únicamente lee/aplica propiedades públicas estándar.
    /// </summary>
    public static class UISettingsManager
    {
        public const string SceneBootLoading = "00_PantallaCargaInicial";
        public const string SceneMainMenu = "01_MenuInicio";
        public const string SceneGameLoading = "02_PantallaCargaJuego";
        public const string SceneGameplay = "SampleScene";

        private const string KeyVolume = "SR_MasterVolume";
        private const string KeySensitivity = "SR_MouseSensitivity";
        private const string KeyQuality = "SR_QualityLevel";
        private const string KeyFullscreen = "SR_Fullscreen";
        private const string KeyResolutionW = "SR_ResW";
        private const string KeyResolutionH = "SR_ResH";

        public const float DefaultVolume = 0.85f;
        public const float DefaultSensitivity = 180f;

        private static bool? cachedHasResolutions;

        public static float MasterVolume
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyVolume, DefaultVolume));
            set
            {
                float v = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyVolume, v);
                AudioListener.volume = v;
            }
        }

        public static float MouseSensitivity
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(KeySensitivity, DefaultSensitivity), 40f, 360f);
            set
            {
                float s = Mathf.Clamp(value, 40f, 360f);
                PlayerPrefs.SetFloat(KeySensitivity, s);
                ApplySensitivityToScenePlayer();
            }
        }

        public static int QualityLevel
        {
            get
            {
                int fallback = Mathf.Clamp(QualitySettings.names.Length - 1, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                int stored = PlayerPrefs.GetInt(KeyQuality, fallback);
                return Mathf.Clamp(stored, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            }
            set
            {
                int clamped = Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
                PlayerPrefs.SetInt(KeyQuality, clamped);
                QualitySettings.SetQualityLevel(clamped, true);
            }
        }

        public static bool IsFullscreen
        {
            get => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
                Screen.fullScreen = value;
            }
        }

        public static List<Vector2Int> GetUniqueResolutions()
        {
            var unique = new List<Vector2Int>();
            var seen = new HashSet<long>();
            Resolution[] all = Screen.resolutions;
            for (int i = 0; i < all.Length; i++)
            {
                long key = ((long)all[i].width << 32) | (uint)all[i].height;
                if (seen.Add(key))
                {
                    unique.Add(new Vector2Int(all[i].width, all[i].height));
                }
            }

            if (unique.Count == 0)
            {
                unique.Add(new Vector2Int(Screen.width > 0 ? Screen.width : 1920, Screen.height > 0 ? Screen.height : 1080));
            }

            cachedHasResolutions = unique.Count > 0;
            return unique;
        }

        public static int GetSavedOrCurrentResolutionIndex(List<Vector2Int> list)
        {
            if (list == null || list.Count == 0) return 0;

            int savedW = PlayerPrefs.GetInt(KeyResolutionW, Screen.currentResolution.width);
            int savedH = PlayerPrefs.GetInt(KeyResolutionH, Screen.currentResolution.height);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].x == savedW && list[i].y == savedH)
                    return i;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].x == Screen.width && list[i].y == Screen.height)
                    return i;
            }

            return list.Count - 1;
        }

        public static void SetResolution(int width, int height, bool fullscreen)
        {
            PlayerPrefs.SetInt(KeyResolutionW, width);
            PlayerPrefs.SetInt(KeyResolutionH, height);
            PlayerPrefs.SetInt(KeyFullscreen, fullscreen ? 1 : 0);
            Screen.SetResolution(width, height, fullscreen);
        }

        public static void ApplyAllSettings()
        {
            AudioListener.volume = MasterVolume;
            if (QualitySettings.names != null && QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(QualityLevel, true);
            }

            if (PlayerPrefs.HasKey(KeyFullscreen))
            {
                Screen.fullScreen = IsFullscreen;
            }

            if (PlayerPrefs.HasKey(KeyResolutionW) && PlayerPrefs.HasKey(KeyResolutionH) && (cachedHasResolutions ?? true))
            {
                int w = PlayerPrefs.GetInt(KeyResolutionW);
                int h = PlayerPrefs.GetInt(KeyResolutionH);
                if (w >= 640 && h >= 360)
                {
                    Screen.SetResolution(w, h, IsFullscreen);
                }
            }

            ApplySensitivityToScenePlayer();
        }

        public static void ApplySensitivityToScenePlayer()
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.sensibilidadMouse = MouseSensitivity;
            }
        }

        public static void ResetDefaults()
        {
            MasterVolume = DefaultVolume;
            MouseSensitivity = DefaultSensitivity;
            if (QualitySettings.names != null && QualitySettings.names.Length > 0)
            {
                QualityLevel = QualitySettings.names.Length - 1;
            }
            IsFullscreen = true;

            var list = GetUniqueResolutions();
            if (list.Count > 0)
            {
                Vector2Int maxRes = list[list.Count - 1];
                SetResolution(maxRes.x, maxRes.y, true);
            }

            PlayerPrefs.Save();
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        public static bool WasEscapeOrPausePressed()
        {
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                pressed = UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                       || UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!pressed && UnityEngine.InputSystem.Keyboard.current == null)
            {
                pressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
            }
#endif
            return pressed;
        }

        public static bool WasSkipPressed()
        {
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                pressed = UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame
                       || UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame;
            }
            if (!pressed && UnityEngine.InputSystem.Mouse.current != null)
            {
                pressed = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!pressed && UnityEngine.InputSystem.Keyboard.current == null)
            {
                pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
            }
#endif
            return pressed;
        }

        public static void QuitApplication()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
