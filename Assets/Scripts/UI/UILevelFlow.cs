using UnityEngine;
using UnityEngine.SceneManagement;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Coordina el cambio dinámico de niveles, el desbloqueo progresivo y los datos mostrados en la pantalla de carga.
    /// </summary>
    public static class UILevelFlow
    {
        private const string KeyUnlockedMax = "SR_UnlockedMaxLevel";
        private const string KeyBestScorePrefix = "SR_BestScore_";

        private static LevelCatalog cachedCatalog;

        public static string TargetSceneName { get; private set; } = UISettingsManager.SceneGameplay;
        public static int TargetLevelIndex { get; private set; } = 0;
        public static int TargetLevelNumber { get; private set; } = 1;
        public static string TargetLevelTitle { get; private set; } = "Barrio Inundado";
        public static string TargetLevelSubtitle { get; private set; } = "Noche de tormenta · Clasifica los residuos evitando el agua";

        public static LevelCatalog GetCatalog()
        {
            if (cachedCatalog == null)
            {
                cachedCatalog = Resources.Load<LevelCatalog>("LevelCatalog");
            }
            return cachedCatalog;
        }

        public static bool CanLoadScene(string sceneName)
        {
            return !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public static int GetActiveLevelIndex()
        {
            LevelCatalog cat = GetCatalog();
            string activeName = SceneManager.GetActiveScene().name;
            if (cat != null)
            {
                int found = cat.FindIndexByScene(activeName);
                if (found >= 0) return found;
            }
            return TargetLevelIndex >= 0 ? TargetLevelIndex : 0;
        }

        public static LevelCatalog.LevelEntry GetActiveLevelEntry()
        {
            LevelCatalog cat = GetCatalog();
            if (cat == null) return null;
            return cat.GetLevel(GetActiveLevelIndex());
        }

        public static bool IsLevelUnlocked(int index)
        {
            LevelCatalog cat = GetCatalog();
            if (cat == null) return index == 0;
            if (cat.unlockAllForTesting) return true;

            LevelCatalog.LevelEntry entry = cat.GetLevel(index);
            if (entry == null) return false;
            if (entry.unlockedByDefault || index == 0) return true;

            int unlockedMax = PlayerPrefs.GetInt(KeyUnlockedMax, 0);
            return index <= unlockedMax;
        }

        public static int GetBestScore(int index)
        {
            return PlayerPrefs.GetInt(KeyBestScorePrefix + index, -1);
        }

        public static void MarkLevelCompleted(int index, int score = -1)
        {
            int nextIndex = index + 1;
            int prevMax = PlayerPrefs.GetInt(KeyUnlockedMax, 0);
            if (nextIndex > prevMax)
            {
                PlayerPrefs.SetInt(KeyUnlockedMax, nextIndex);
            }

            if (score >= 0)
            {
                int prevBest = GetBestScore(index);
                if (score > prevBest)
                {
                    PlayerPrefs.SetInt(KeyBestScorePrefix + index, score);
                }
            }

            PlayerPrefs.Save();
        }

        public static bool HasNextLevelEntry(int currentIndex)
        {
            LevelCatalog cat = GetCatalog();
            return cat != null && currentIndex + 1 < cat.Count;
        }

        public static bool HasNextPlayableLevel(int currentIndex)
        {
            LevelCatalog cat = GetCatalog();
            if (cat == null) return false;
            LevelCatalog.LevelEntry next = cat.GetLevel(currentIndex + 1);
            return next != null && CanLoadScene(next.sceneName);
        }

        public static void LoadLevel(int index)
        {
            LevelCatalog cat = GetCatalog();
            LevelCatalog.LevelEntry entry = cat != null ? cat.GetLevel(index) : null;

            if (entry != null)
            {
                TargetLevelIndex = index;
                TargetSceneName = entry.sceneName;
                TargetLevelNumber = entry.levelNumber;
                TargetLevelTitle = entry.displayName;
                TargetLevelSubtitle = entry.subtitle;
            }
            else
            {
                TargetLevelIndex = 0;
                TargetSceneName = UISettingsManager.SceneGameplay;
                TargetLevelNumber = 1;
                TargetLevelTitle = "Barrio Inundado";
                TargetLevelSubtitle = "Noche de tormenta · Clasifica los residuos evitando el agua";
            }

            LoadViaLoadingScreen();
        }

        public static void LoadLevelByScene(string sceneName, string customTitle = null)
        {
            LevelCatalog cat = GetCatalog();
            int idx = cat != null ? cat.FindIndexByScene(sceneName) : -1;
            if (idx >= 0)
            {
                LoadLevel(idx);
                return;
            }

            TargetLevelIndex = 0;
            TargetSceneName = string.IsNullOrEmpty(sceneName) ? UISettingsManager.SceneGameplay : sceneName;
            TargetLevelNumber = 1;
            TargetLevelTitle = string.IsNullOrEmpty(customTitle) ? TargetSceneName : customTitle;
            TargetLevelSubtitle = "Cargando escenario...";
            LoadViaLoadingScreen();
        }

        public static void ReloadCurrentLevel()
        {
            string activeName = SceneManager.GetActiveScene().name;
            LevelCatalog cat = GetCatalog();
            int idx = cat != null ? cat.FindIndexByScene(activeName) : -1;
            if (idx >= 0)
            {
                LoadLevel(idx);
            }
            else
            {
                LoadLevelByScene(activeName, activeName);
            }
        }

        public static bool TryLoadNextLevel()
        {
            int next = GetActiveLevelIndex() + 1;
            LevelCatalog cat = GetCatalog();
            LevelCatalog.LevelEntry entry = cat != null ? cat.GetLevel(next) : null;
            if (entry == null || !CanLoadScene(entry.sceneName))
            {
                return false;
            }

            LoadLevel(next);
            return true;
        }

        private static void LoadViaLoadingScreen()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MonsterAI.UsarGameOverExterno = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UISettingsManager.Save();

            if (CanLoadScene(UISettingsManager.SceneGameLoading))
            {
                SceneManager.LoadScene(UISettingsManager.SceneGameLoading);
            }
            else if (CanLoadScene(TargetSceneName))
            {
                SceneManager.LoadScene(TargetSceneName);
            }
        }
    }
}
