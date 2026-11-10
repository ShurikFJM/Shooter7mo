using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using INab.Common;
using System.Collections.Generic;


namespace INab.Demo
{
    public class ShowcaseAutoPlay : MonoBehaviour
    {
        public List<GameObject> trailCategories = new List<GameObject>();
        private int selectedClipIndex = 0;

        private void SetActiveCategory()
        {
            foreach (var item in trailCategories)
            {
                item.SetActive(false);
            }
            trailCategories[selectedClipIndex].SetActive(true);
        }

        private void Start()
        {
            SetActiveCategory();
        }

        private static bool WasKeyPressed(KeyCode keyCode)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;

            if (keyCode == KeyCode.Q) return Keyboard.current.qKey.wasPressedThisFrame;
            if (keyCode == KeyCode.E) return Keyboard.current.eKey.wasPressedThisFrame;
            return false;
#else
            return Input.GetKeyDown(keyCode);
#endif
        }

        void Update()
        {

            if (WasKeyPressed(KeyCode.Q))
            {
                selectedClipIndex = (selectedClipIndex - 1 + trailCategories.Count) % trailCategories.Count;
                SetActiveCategory();
            }

            if (WasKeyPressed(KeyCode.E))
            {
                selectedClipIndex = (selectedClipIndex + 1) % trailCategories.Count;
                SetActiveCategory();
            }
        }
    }
}