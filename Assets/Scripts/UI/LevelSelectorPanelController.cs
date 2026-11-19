using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Controlador reutilizable para la ventana de Selector / Cambio de Niveles
    /// (usado tanto en el Menú de Inicio como dentro del Menú del Juego / Victoria / Game Over).
    /// </summary>
    public class LevelSelectorPanelController : MonoBehaviour
    {
        [Header("Plantilla y Contenedor")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private GameObject levelCardTemplate;
        [SerializeField] private TMP_Text helperNoteText;

        private readonly List<GameObject> spawnedCards = new List<GameObject>();
        private Action onLevelLaunching;

        public void SetOnLevelLaunchingCallback(Action callback)
        {
            onLevelLaunching = callback;
        }

        private void OnEnable()
        {
            RefreshCards();
        }

        public void RefreshCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null) Destroy(spawnedCards[i]);
            }
            spawnedCards.Clear();

            if (levelCardTemplate == null || listContainer == null) return;
            levelCardTemplate.SetActive(false);

            LevelCatalog catalog = UILevelFlow.GetCatalog();
            if (catalog == null || catalog.Count == 0)
            {
                if (helperNoteText != null)
                {
                    helperNoteText.text = "No se encontró LevelCatalog en Resources. Cargando escenario principal por defecto.";
                }
                return;
            }

            string currentScene = SceneManager.GetActiveScene().name;
            int playableCount = 0;

            for (int i = 0; i < catalog.Count; i++)
            {
                LevelCatalog.LevelEntry entry = catalog.GetLevel(i);
                if (entry == null) continue;

                GameObject card = Instantiate(levelCardTemplate, listContainer);
                card.name = "LevelCard_" + entry.levelNumber;
                card.SetActive(true);
                spawnedCards.Add(card);

                bool sceneAvailable = UILevelFlow.CanLoadScene(entry.sceneName);
                bool unlocked = UILevelFlow.IsLevelUnlocked(i);
                bool isCurrentLevel = string.Equals(currentScene, entry.sceneName, StringComparison.OrdinalIgnoreCase);
                if (sceneAvailable) playableCount++;

                TMP_Text numText = card.transform.Find("Badge_Nivel/Txt")?.GetComponent<TMP_Text>();
                TMP_Text titleText = card.transform.Find("Title")?.GetComponent<TMP_Text>();
                TMP_Text diffText = card.transform.Find("Badge_Diff/Txt")?.GetComponent<TMP_Text>();
                TMP_Text subText = card.transform.Find("Subtitle")?.GetComponent<TMP_Text>();
                TMP_Text statusText = card.transform.Find("ActionBadge/Label")?.GetComponent<TMP_Text>();
                Image badgeImg = card.transform.Find("Badge_Nivel")?.GetComponent<Image>();
                Image diffImg = card.transform.Find("Badge_Diff")?.GetComponent<Image>();
                Image actionBg = card.transform.Find("ActionBadge")?.GetComponent<Image>();
                Button btn = card.GetComponent<Button>();
                UIButtonFX fx = card.GetComponent<UIButtonFX>();

                if (numText != null) numText.text = "NIVEL " + entry.levelNumber.ToString("00");
                if (titleText != null) titleText.text = entry.displayName;
                if (diffText != null) diffText.text = string.IsNullOrEmpty(entry.difficulty) ? "NORMAL" : entry.difficulty.ToUpperInvariant();

                int bestScore = UILevelFlow.GetBestScore(i);
                if (subText != null)
                {
                    if (!sceneAvailable)
                    {
                        subText.text = entry.subtitle + "  <color=#8FAAB2><i>(Pendiente en Build Settings)</i></color>";
                    }
                    else if (bestScore >= 0)
                    {
                        subText.text = entry.subtitle + "  ·  <color=#5CEAD8>Mejor puntaje: " + bestScore + "</color>";
                    }
                    else
                    {
                        subText.text = entry.subtitle;
                    }
                }

                bool canClick = sceneAvailable && unlocked;

                if (statusText != null)
                {
                    if (!sceneAvailable) statusText.text = "PRÓXIMAMENTE";
                    else if (!unlocked) statusText.text = "BLOQUEADO";
                    else if (isCurrentLevel) statusText.text = "REINICIAR";
                    else statusText.text = "JUGAR";
                    statusText.color = canClick
                        ? new Color(0.925f, 0.961f, 0.965f, 1f)
                        : new Color(0.58f, 0.68f, 0.72f, 0.65f);
                }

                if (numText != null)
                {
                    numText.color = canClick
                        ? new Color(0.361f, 0.918f, 0.847f, 1f)
                        : new Color(0.55f, 0.65f, 0.69f, 0.7f);
                }

                if (badgeImg != null)
                {
                    badgeImg.color = canClick
                        ? new Color(0.10f, 0.25f, 0.28f, 0.95f)
                        : new Color(0.11f, 0.14f, 0.16f, 0.70f);
                }

                if (diffImg != null)
                {
                    diffImg.color = new Color(0.14f, 0.16f, 0.22f, 0.9f);
                }

                if (actionBg != null)
                {
                    actionBg.color = canClick
                        ? new Color(0.12f, 0.36f, 0.36f, 0.95f)
                        : new Color(0.10f, 0.13f, 0.15f, 0.75f);
                }

                if (btn != null)
                {
                    btn.interactable = canClick;
                    btn.onClick.RemoveAllListeners();
                    if (canClick)
                    {
                        int capturedIndex = i;
                        btn.onClick.AddListener(() =>
                        {
                            onLevelLaunching?.Invoke();
                            UILevelFlow.LoadLevel(capturedIndex);
                        });
                    }
                }

                if (fx != null)
                {
                    fx.enabled = canClick;
                }
            }

            if (helperNoteText != null)
            {
                helperNoteText.text = playableCount < catalog.Count
                    ? "Los niveles marcados como PRÓXIMAMENTE se habilitarán automáticamente al añadir sus escenas en Build Settings."
                    : "Selecciona cualquier nivel desbloqueado para iniciar la incursión.";
            }
        }
    }
}
