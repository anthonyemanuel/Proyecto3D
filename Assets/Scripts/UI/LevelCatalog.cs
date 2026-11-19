using System;
using System.Collections.Generic;
using UnityEngine;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Catálogo configurable de niveles jugables para el selector, la pantalla de carga y la transición al siguiente nivel.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Sneaky Recycling/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [Serializable]
        public class LevelEntry
        {
            public int levelNumber = 1;
            public string sceneName = "SampleScene";
            public string displayName = "Barrio Inundado";
            [TextArea(1, 3)]
            public string subtitle = "Noche de tormenta · Clasifica los residuos evitando el agua";
            public string difficulty = "Normal";
            public bool unlockedByDefault = false;
        }

        [Header("Opciones de Progresión")]
        [Tooltip("Si está activo, todos los niveles agregados a Build Settings aparecen desbloqueados en el selector.")]
        public bool unlockAllForTesting = false;

        [Header("Lista de Niveles")]
        public List<LevelEntry> levels = new List<LevelEntry>();

        public int Count => levels != null ? levels.Count : 0;

        public LevelEntry GetLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Count) return null;
            return levels[index];
        }

        public int FindIndexByScene(string sceneName)
        {
            if (levels == null || string.IsNullOrEmpty(sceneName)) return -1;
            for (int i = 0; i < levels.Count; i++)
            {
                if (string.Equals(levels[i].sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
