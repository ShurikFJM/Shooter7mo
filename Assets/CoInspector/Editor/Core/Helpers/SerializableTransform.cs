using System.Collections.Generic;
using UnityEngine;
using System;

namespace CoInspector
{   
[Serializable]
internal class SerializableTransform
    {
        public Transform owner;

        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 originalPosition;
        public Quaternion originalRotation;
        public Vector3 originalLocalScale;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot;
        public Vector2 originalAnchorMin;
        public Vector2 originalAnchorMax;
        public Vector2 originalAnchoredPosition;
        public Vector2 originalSizeDelta;
        public Vector2 originalPivot;

        public bool isRectTransform;
        [SerializeField] public bool appliedTransform;
        public bool undidTransform;
        public bool autoSave;

        public SerializableTransform(Transform transform, bool isAutoSave = false)
        {
            if (transform == null || transform.gameObject == null)
            {
                return;
            }
            owner = transform;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
            autoSave = isAutoSave;

            if (transform is RectTransform rectTransform)
            {
                isRectTransform = true;
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
                pivot = rectTransform.pivot;
            }
        }
        public void UpdateValues()
        {
            if (owner == null || owner.gameObject == null)
            {
                return;
            }
            localPosition = owner.localPosition;
            localRotation = owner.localRotation;
            localScale = owner.localScale;
            if (isRectTransform)
            {
                RectTransform rectTransform = owner as RectTransform;
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
                pivot = rectTransform.pivot;
            }
        }

        public static bool DoTheyMatch(List<SerializableTransform> transforms, GameObject[] gameObjects)
        {
            if (transforms == null || gameObjects == null)
            {
                return false;
            }
            if (transforms.Count != gameObjects.Length)
            {
                return false;
            }
            for (int i = 0; i < transforms.Count; i++)
            {
                if (transforms[i] == null || gameObjects[i] == null)
                {
                    return false;
                }
                if (transforms[i].owner != gameObjects[i].transform)
                {
                    return false;
                }
            }
            return true;
        }
        public SerializableTransform(SerializableTransform transform)
        {
            if (transform == null || transform.owner == null)
            {
                return;
            }
            owner = transform.owner;
            appliedTransform = transform.appliedTransform;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
            isRectTransform = transform.isRectTransform;
            anchorMin = transform.anchorMin;
            anchorMax = transform.anchorMax;
            anchoredPosition = transform.anchoredPosition;
            sizeDelta = transform.sizeDelta;
            pivot = transform.pivot;
            originalPosition = transform.originalPosition;
            originalRotation = transform.originalRotation;
            originalLocalScale = transform.originalLocalScale;
            originalAnchorMin = transform.originalAnchorMin;
            originalAnchorMax = transform.originalAnchorMax;
            originalAnchoredPosition = transform.originalAnchoredPosition;
            originalSizeDelta = transform.originalSizeDelta;
            originalPivot = transform.originalPivot;
        }

        public static bool SameTransforms(SerializableTransform transform1, SerializableTransform transform2)
        {
            if (transform1 == null || transform2 == null)
            {
                return false;
            }

            if (!transform1.isRectTransform)
            {
                return transform1.owner == transform2.owner &&
                       transform1.localPosition == transform2.localPosition &&
                       transform1.localRotation == transform2.localRotation &&
                       transform1.localScale == transform2.localScale &&
                       transform1.isRectTransform == transform2.isRectTransform;
            }

            return transform1.owner == transform2.owner &&
                   transform1.localPosition == transform2.localPosition &&
                   transform1.localRotation == transform2.localRotation &&
                   transform1.localScale == transform2.localScale &&
                   transform1.isRectTransform == transform2.isRectTransform &&
                   transform1.anchorMin == transform2.anchorMin &&
                   transform1.anchorMax == transform2.anchorMax &&
                   transform1.anchoredPosition == transform2.anchoredPosition &&
                   transform1.sizeDelta == transform2.sizeDelta &&
                   transform1.pivot == transform2.pivot;
        }

        public static bool SameTransforms(Transform transform, SerializableTransform serializableTransform)
        {
            if (transform == null || serializableTransform == null)
            {
                return false;
            }

            bool isRectTransform = transform is RectTransform;
            if (transform != serializableTransform.owner ||
                transform.localPosition != serializableTransform.localPosition ||
                transform.localRotation != serializableTransform.localRotation ||
                transform.localScale != serializableTransform.localScale ||
                isRectTransform != serializableTransform.isRectTransform)
            {
                return false;
            }

            if (isRectTransform)
            {
                RectTransform rectTransform = transform as RectTransform;
                return rectTransform.anchorMin == serializableTransform.anchorMin &&
                       rectTransform.anchorMax == serializableTransform.anchorMax &&
                       rectTransform.anchoredPosition == serializableTransform.anchoredPosition &&
                       rectTransform.sizeDelta == serializableTransform.sizeDelta &&
                       rectTransform.pivot == serializableTransform.pivot;
            }

            return true;
        }
        public void Apply()
        {
            if (owner == null || owner.gameObject == null)
            {
                return;
            }
            RectTransform rectTransform;
            bool isNowRectTransform = owner is RectTransform;
            if (isNowRectTransform)
            {
                rectTransform = owner as RectTransform;
            }
            else
            {
                rectTransform = null;
            }
            if (!appliedTransform)
            {
                originalPosition = owner.localPosition;
                originalRotation = owner.localRotation;
                originalLocalScale = owner.localScale;
                if (isNowRectTransform && isRectTransform)
                {
                    originalAnchorMin = rectTransform.anchorMin;
                    originalAnchorMax = rectTransform.anchorMax;
                    originalAnchoredPosition = rectTransform.anchoredPosition;
                    originalSizeDelta = rectTransform.sizeDelta;
                    originalPivot = rectTransform.pivot;
                }
                owner.localPosition = localPosition;
                owner.localRotation = localRotation;
                owner.localScale = localScale;
                if (isNowRectTransform && isRectTransform)
                {
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    rectTransform.anchoredPosition = anchoredPosition;
                    rectTransform.sizeDelta = sizeDelta;
                    rectTransform.pivot = pivot;
                }
                appliedTransform = true;
            }
            else
            {
                owner.localPosition = originalPosition;
                owner.localRotation = originalRotation;
                owner.localScale = originalLocalScale;
                if (isNowRectTransform && isRectTransform)
                {
                    rectTransform.anchorMin = originalAnchorMin;
                    rectTransform.anchorMax = originalAnchorMax;
                    rectTransform.anchoredPosition = originalAnchoredPosition;
                    rectTransform.sizeDelta = originalSizeDelta;
                    rectTransform.pivot = originalPivot;
                }
                appliedTransform = false;
            }
        }
    }
    }