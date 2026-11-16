using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Controlador reutilizable del panel de Opciones (usado tanto en Menú de Inicio como en el Menú del Juego).
    /// </summary>
    public class OptionsPanelController : MonoBehaviour
    {
        [Header("Controles")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeValueText;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityValueText;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button resetButton;

        private List<Vector2Int> resolutions = new List<Vector2Int>();
        private bool suppressCallbacks;

        private void Awake()
        {
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnDisable()
        {
            UISettingsManager.Save();
        }

        private void OnDestroy()
        {
            if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
            if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            if (resetButton != null) resetButton.onClick.RemoveListener(OnResetClicked);
        }

        public void RefreshUI()
        {
            suppressCallbacks = true;

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = UISettingsManager.MasterVolume;
            }
            UpdateVolumeLabel(UISettingsManager.MasterVolume);

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 40f;
                sensitivitySlider.maxValue = 360f;
                sensitivitySlider.value = UISettingsManager.MouseSensitivity;
            }
            UpdateSensitivityLabel(UISettingsManager.MouseSensitivity);

            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                var qOpts = new List<string>(QualitySettings.names);
                qualityDropdown.AddOptions(qOpts);
                qualityDropdown.value = UISettingsManager.QualityLevel;
                qualityDropdown.RefreshShownValue();
            }

            if (resolutionDropdown != null)
            {
                resolutions = UISettingsManager.GetUniqueResolutions();
                var resOpts = new List<string>(resolutions.Count);
                for (int i = 0; i < resolutions.Count; i++)
                {
                    resOpts.Add(resolutions[i].x + " × " + resolutions[i].y);
                }
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(resOpts);
                resolutionDropdown.value = UISettingsManager.GetSavedOrCurrentResolutionIndex(resolutions);
                resolutionDropdown.RefreshShownValue();
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = UISettingsManager.IsFullscreen;
            }

            suppressCallbacks = false;
        }

        private void OnVolumeChanged(float val)
        {
            if (suppressCallbacks) return;
            UISettingsManager.MasterVolume = val;
            UpdateVolumeLabel(val);
        }

        private void OnSensitivityChanged(float val)
        {
            if (suppressCallbacks) return;
            UISettingsManager.MouseSensitivity = val;
            UpdateSensitivityLabel(val);
        }

        private void OnQualityChanged(int index)
        {
            if (suppressCallbacks) return;
            UISettingsManager.QualityLevel = index;
        }

        private void OnResolutionChanged(int index)
        {
            if (suppressCallbacks || index < 0 || index >= resolutions.Count) return;
            Vector2Int res = resolutions[index];
            bool fs = fullscreenToggle != null ? fullscreenToggle.isOn : UISettingsManager.IsFullscreen;
            UISettingsManager.SetResolution(res.x, res.y, fs);
        }

        private void OnFullscreenChanged(bool isOn)
        {
            if (suppressCallbacks) return;
            UISettingsManager.IsFullscreen = isOn;
        }

        private void OnResetClicked()
        {
            UISettingsManager.ResetDefaults();
            RefreshUI();
        }

        private void UpdateVolumeLabel(float val)
        {
            if (volumeValueText != null)
            {
                volumeValueText.text = Mathf.RoundToInt(val * 100f) + "%";
            }
        }

        private void UpdateSensitivityLabel(float val)
        {
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = Mathf.RoundToInt(val).ToString();
            }
        }
    }
}
