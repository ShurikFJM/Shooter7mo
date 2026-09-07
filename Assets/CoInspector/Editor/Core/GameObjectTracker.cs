using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CoInspector
{
    [Serializable]
    internal class GameObjectTracker
    {
        [Serializable]
        private class GameObjectEntry
        {
            [SerializeField] private List<string> paths;
            [SerializeField] private int count;
            [SerializeField] private List<GameObject> gameObjects;

            public GameObjectEntry(List<string> paths, int count)
            {
                this.count = count;
                this.gameObjects = new List<GameObject>(paths.Count);
                this.paths = paths;

                for (int i = 0; i < paths.Count; i++)
                {
                    var go = EditorUtils.LoadGameObject(paths[i]);
                    if (go != null)
                    {
                        gameObjects.Add(go);
                    }
                    else
                    {
                        paths[i] = string.Empty;
                    }
                }
            }

            public bool IsItValid => paths.Any(p => !string.IsNullOrEmpty(p));

            public List<string> Paths => paths;
            public int Count => count;
            public void UpdatePaths()
            {
                var validGameObjects = gameObjects.Where(go => go != null).ToList();
                if (validGameObjects.Any())
                {
                    paths = validGameObjects.Select(go => EditorUtils.GatherGameObjectPath(go)).ToList();
                    gameObjects = validGameObjects;
                }
                else
                {
                    paths = new List<string>();
                    gameObjects = new List<GameObject>();
                }
            }
            public void IncrementCount()
            {
                count++;
            }
        }
        [SerializeField] private List<GameObjectEntry> mostClicked = new List<GameObjectEntry>();
        [SerializeField] private List<GameObjectEntry> recentlyClicked = new List<GameObjectEntry>();
        [SerializeField] private List<GUIContent> mostClickedContents = new List<GUIContent>();
        [SerializeField] private List<GUIContent> recentlyClickedContents = new List<GUIContent>();
        public GameObjectTracker()
        {

        }

        public GameObjectTracker(GameObjectTracker other)
        {
            if (other == null)
            {
                return;
            }
            mostClicked = other.mostClicked.Select(entry => new GameObjectEntry(entry.Paths, entry.Count)).ToList();
            recentlyClicked = other.recentlyClicked.Select(entry => new GameObjectEntry(entry.Paths, entry.Count)).ToList();
        }

        public void RefreshAllPaths()
        {
            foreach (GameObjectEntry entry in mostClicked)
            {
                entry.UpdatePaths();
            }
            foreach (GameObjectEntry entry in recentlyClicked)
            {
                entry.UpdatePaths();
            }
            List<GameObjectEntry> newMostClicked = new List<GameObjectEntry>();
            foreach (GameObjectEntry entry in mostClicked)
            {
                if (entry.Paths.Any())
                {
                    newMostClicked.Add(entry);
                }
            }
            mostClicked = newMostClicked;
            List<GameObjectEntry> newRecentlyClicked = new List<GameObjectEntry>();
            foreach (GameObjectEntry entry in recentlyClicked)
            {
                if (entry.Paths.Any())
                {
                    newRecentlyClicked.Add(entry);
                }
            }
            Dictionary<string, GameObject> newRuntimeGameObjects = new Dictionary<string, GameObject>();
            foreach (var entry in runtimeGameObjects)
            {
                var newKey = EditorUtils.GatherGameObjectPath(entry.Value);
                var value = entry.Value;
                if (!newRuntimeGameObjects.ContainsKey(newKey))
                { newRuntimeGameObjects.Add(newKey, value); }
            }
            runtimeGameObjects = newRuntimeGameObjects;
        }

        public void UpdateClicked(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            UpdateClicked(new List<GameObject> { gameObject });

        }

        public void UpdateClicked(GameObject[] gameObjects)
        {
            if (gameObjects == null)
            {
                return;
            }
            UpdateClicked(gameObjects.ToList());

        }

        public void UpdateClicked(List<GameObject> gameObjects)
        {
            runtimePaths = new Dictionary<GameObject, string>();
            List<string> paths = gameObjects.Select(go => GameObjectToPath(go)).ToList();
            RefreshAllPaths();
            UpdateMostClicked(paths);
            UpdateRecentlyClicked(paths);
            //UpdateContents();
            if (CoInspectorWindow.MainCoInspector && !CoInspectorWindow.MainCoInspector.exitingPlayMode)
            {
                CoInspectorWindow.MainCoInspector.SaveSettings();
            }
        }

        private void UpdateMostClicked(List<string> paths)
        {
            GameObjectEntry entry = mostClicked.Find(e => e.Paths.SequenceEqual(paths));
            if (entry != null)
            {
                entry.IncrementCount();
                mostClicked.Sort((a, b) => b.Count.CompareTo(a.Count));
            }
            else
            {
                GameObjectEntry gameObjectEntry = new GameObjectEntry(paths, 1);
                if (!gameObjectEntry.IsItValid)
                {
                    return;
                }
                if (mostClicked.Count >= 10)
                {
                    mostClicked.RemoveAt(mostClicked.Count - 1);
                }
                mostClicked.Add(gameObjectEntry);

            }
        }

        private void UpdateRecentlyClicked(List<string> paths)
        {
            GameObjectEntry entry = recentlyClicked.Find(e => e.Paths.SequenceEqual(paths));
            if (entry != null)
            {
                recentlyClicked.Remove(entry);
            }
            GameObjectEntry gameObjectEntry = new GameObjectEntry(paths, 1);
            if (!gameObjectEntry.IsItValid)
            {
                return;
            }
            else if (recentlyClicked.Count >= 10)
            {
                recentlyClicked.RemoveAt(recentlyClicked.Count - 1);
            }
            recentlyClicked.Insert(0, gameObjectEntry);


        }

        public List<List<GameObject>> GetMostClicked()
        {
            return GetGameObjectsList(mostClicked);
        }

        public List<List<GameObject>> GetRecentlyClicked()
        {
            return GetGameObjectsList(recentlyClicked);
        }

        public void RemoveFromMost(List<GameObject> gameObjects)
        {
            List<string> paths = gameObjects.Select(go => GameObjectToPath(go)).ToList();
            GameObjectEntry entry = mostClicked.Find(e => e.Paths.SequenceEqual(paths));
            if (entry != null)
            {
                mostClicked.Remove(entry);
            }
            UpdateContents();
        }

        public void RemoveFromLast(List<GameObject> gameObjects)
        {
            List<string> paths = gameObjects.Select(go => GameObjectToPath(go)).ToList();
            GameObjectEntry entry = recentlyClicked.Find(e => e.Paths.SequenceEqual(paths));
            if (entry != null)
            {
                recentlyClicked.Remove(entry);
            }
            UpdateContents();
        }

        private List<List<GameObject>> GetGameObjectsList(List<GameObjectEntry> list)
        {
            runtimeGameObjects = new Dictionary<string, GameObject>();
            List<List<GameObject>> gameObjectsList = new List<List<GameObject>>();
            foreach (GameObjectEntry entry in list)
            {
                List<GameObject> gameObjects = entry.Paths.Select(path => PathToGameObject(path)).Where(go => go != null).ToList();
                if (gameObjects.Count > 0)
                {
                    gameObjectsList.Add(gameObjects);
                }
            }
            return gameObjectsList;
        }
        private Dictionary<GameObject, string> runtimePaths = new Dictionary<GameObject, string>();
        private string GameObjectToPath(GameObject gameObject)
        {
            if (runtimePaths.ContainsKey(gameObject))
            {
                string _path = runtimePaths[gameObject];
                if (!string.IsNullOrEmpty(_path))
                {
                    return _path;
                }
                runtimePaths.Remove(gameObject);
            }
            string path = EditorUtils.GatherGameObjectPath(gameObject);
            if (!string.IsNullOrEmpty(path))
            {
                runtimePaths.Add(gameObject, path);
                return path;
            }
            return string.Empty;
        }
        private Dictionary<string, GameObject> runtimeGameObjects = new Dictionary<string, GameObject>();

        private GameObject PathToGameObject(string path)
        {
            if (runtimeGameObjects.ContainsKey(path))
            {
                GameObject go = runtimeGameObjects[path];
                if (go != null)
                {
                    return go;
                }
            }
            GameObject gameObject = EditorUtils.LoadGameObject(path);
            if (gameObject != null)
            {
                runtimeGameObjects.Add(path, gameObject);
            }
            else
            {
                runtimeGameObjects.Remove(path);
            }
            return gameObject;
        }
        internal void UpdateContents()
        {
            if (mostClickedContents == null)
            {
                mostClickedContents = new List<GUIContent>();
            }
            mostClickedContents.Clear();
            foreach (GameObjectEntry entry in mostClicked)
            {
                List<GameObject> gameObjects = entry.Paths.Select(path => PathToGameObject(path)).Where(go => go != null).ToList();
                if (gameObjects.Count > 0)
                {
                    GUIContent content;
                    if (gameObjects.Count > 1)
                    {
                        content = new GUIContent(CustomGUIContents.MultiTabButtonImage);
                        content.tooltip = string.Join("\n", gameObjects.ConvertAll(go => gameObjects.IndexOf(go) + 1 + ". " + go.name));
                    }
                    else
                    {
                        content = new GUIContent(EditorUtils.GetBestFittingIconForGameObject(gameObjects[0]));

                        content.tooltip = "Click: Set as Target\nMiddle-Click: Open in new Tab\nRight-Click: More Options";
                    }
                    mostClickedContents.Add(content);
                }
            }
            if (recentlyClickedContents == null)
            {
                recentlyClickedContents = new List<GUIContent>();
            }
            recentlyClickedContents.Clear();
            foreach (GameObjectEntry entry in recentlyClicked)
            {
                List<GameObject> gameObjects = entry.Paths.Select(path => PathToGameObject(path)).Where(go => go != null).ToList();
                if (gameObjects.Count > 0)
                {
                    GUIContent content;
                    if (gameObjects.Count > 1)
                    {
                        content = new GUIContent(CustomGUIContents.MultiTabButtonImage);
                        content.tooltip = string.Join("\n", gameObjects.ConvertAll(go => gameObjects.IndexOf(go) + 1 + ". " + go.name));
                    }
                    else
                    {
                        content = new GUIContent(EditorUtils.GetBestFittingIconForGameObject(gameObjects[0]));
                        content.tooltip = "Click: Set as Target\nMiddle Click: Open in a New Tab\nRight Click: More Options";
                    }
                    recentlyClickedContents.Add(content);
                }
            }
        }


        public GUIContent GetContentForMost(int index)
        {

            if (mostClickedContents == null)
            {

                mostClickedContents = new List<GUIContent>();
                UpdateContents();
            }
            if (index >= 0 && index < mostClickedContents.Count)
            {

                return mostClickedContents[index];
            }
            return null;
        }

        public GUIContent GetContentForLast(int index)
        {
            if (recentlyClickedContents == null)
            {
                recentlyClickedContents = new List<GUIContent>();
                UpdateContents();
            }
            if (index >= 0 && index < recentlyClickedContents.Count)
            {
                return recentlyClickedContents[index];
            }
            return null;
        }
    }
}