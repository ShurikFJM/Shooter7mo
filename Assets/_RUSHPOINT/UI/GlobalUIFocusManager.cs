using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EventSystem))]
public class GlobalUIFocusManager : MonoBehaviour
{
    private EventSystem _eventSystem;

    private void Awake()
    {
        _eventSystem = GetComponent<EventSystem>();
    }

    private void Update()
    {
        if (_eventSystem == null) return;

        if (_eventSystem.currentSelectedGameObject != null &&
            _eventSystem.currentSelectedGameObject.activeInHierarchy)
        {
            Selectable currentSel = _eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
            if (currentSel != null && currentSel.IsInteractable()) return;
        }

        AssignFocusToFrontmostSelectable();
    }

    private void AssignFocusToFrontmostSelectable()
    {
        Selectable[] allSelectables = FindObjectsByType<Selectable>(FindObjectsSortMode.None);
        Selectable bestCandidate = null;
        int highestSiblingIndex = -1;

        for (int i = 0; i < allSelectables.Length; i++)
        {
            Selectable item = allSelectables[i];

            if (item == null || !item.gameObject.activeInHierarchy || !item.IsInteractable()) continue;

            int hierarchyDepth = item.transform.GetSiblingIndex();
            if (hierarchyDepth > highestSiblingIndex || bestCandidate == null)
            {
                highestSiblingIndex = hierarchyDepth;
                bestCandidate = item;
            }
        }

        if (bestCandidate != null)
        {
            _eventSystem.SetSelectedGameObject(null);
            _eventSystem.SetSelectedGameObject(bestCandidate.gameObject);
        }
    }
}