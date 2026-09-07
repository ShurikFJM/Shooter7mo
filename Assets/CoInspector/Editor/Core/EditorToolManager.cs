using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UIElements;

namespace CoInspector
{
    internal static class EditorToolManager
    {
        private static readonly MethodInfo spriteRendererGetSpriteBounds;
        private static readonly MethodInfo spriteMaskGetSpriteBounds;
        private static readonly MethodInfo spriteShapeRendererGetLocalAABB;
        private static MethodInfo _calculateBoundsMethod;
        private static Vector3 handlePosition = Vector3.zero;
        private static Vector3 cachedSelectionCenter = Vector3.zero;
        private static bool refreshHandlePosition = true;
        private static bool refreshCache = true;
        private static Collider[] colliders;
        private static Collider2D[] colliders2D;
        public static GameObject[] lastSelection = new GameObject[0];
        public static List<GameObject> lastMultiFiltered = new List<GameObject>();



        static EditorToolManager()
        {
            EditorApplication.hierarchyChanged -= RefreshCache;
            EditorApplication.hierarchyChanged += RefreshCache;
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.quitting += OnEditorQuitting;
            spriteRendererGetSpriteBounds = typeof(SpriteRenderer)
                .GetMethod("GetSpriteBounds",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            spriteMaskGetSpriteBounds = typeof(SpriteMask)
                .GetMethod("GetSpriteBounds",
                    BindingFlags.NonPublic | BindingFlags.Instance);

            spriteShapeRendererGetLocalAABB = typeof(SpriteShapeRenderer)
                .GetMethod("GetLocalAABB",
                    BindingFlags.NonPublic | BindingFlags.Instance);
        }
        public static void RefreshCache()
        {
            refreshCache = true;
        }
        public static void RefreshHandlePosition()
        {
            refreshHandlePosition = true;
        }


        private static void OnEditorQuitting()
        {
            EditorApplication.hierarchyChanged -= RefreshCache;
            EditorApplication.quitting -= OnEditorQuitting;
        }

        public static void DrawCustomHandles(GameObject target)
        {
            Tools.hidden = false;
            if (target == null || !CoInspectorWindow.overrideSceneTools)
            {
                return;
            }
            if (IsInSelection(target))
            {
                return;
            }
            HaveTargetsChanged(target);
            DrawBorder(target);
            Matrix4x4 originalMatrix = Handles.matrix;
            string activeTool = ToolManager.activeToolType.ToString();
            if (activeTool == "UnityEditor.MoveTool")
            {
                DrawMoveHandle(target);
                Tools.hidden = true;
            }
            else if (activeTool == "UnityEditor.RotateTool")
            {
                DrawRotationHandle(target);
                Tools.hidden = true;
            }
            else if (activeTool == "UnityEditor.ScaleTool")
            {
                DrawScaleHandle(target);
                Tools.hidden = true;
            }
            else if (activeTool == "UnityEditor.RectTool")
            {
                SceneToolsOverlay.DrawSceneViewWarning("Rect Tool not supported yet!\nTargetting active Selection");
                return;
                //  DrawRectHandle(target);
                //  Tools.hidden = true;
            }
            else if (activeTool == "UnityEditor.TransformTool")
            {
                DrawTransformHandle(target);
                Tools.hidden = true;
            }
            else
            {
                Tools.hidden = false;
            }
            Handles.matrix = originalMatrix;
        }
        private static List<GameObject> GetFilteredTargets(GameObject[] targets)
        {
            List<GameObject> rootTargets = new List<GameObject>();
            if (targets == null || targets.Length == 0) return rootTargets;

            foreach (GameObject target in targets)
            {
                bool isChildOfSelection = false;
                Transform parent = target.transform.parent;
                while (parent != null)
                {
                    if (targets.Contains(parent.gameObject))
                    {
                        isChildOfSelection = true;
                        break;
                    }
                    parent = parent.parent;
                }
                if (!isChildOfSelection)
                {
                    rootTargets.Add(target);
                }
            }
            return rootTargets;
        }


        public static void DrawCustomHandles(GameObject[] targets)
        {

            Tools.hidden = false;
            if (!CoInspectorWindow.overrideSceneTools || targets == null || targets.Length == 0) return;
            if (AreInSelection(targets))
            {
                return;
            }
            HaveTargetsChanged(targets);
            DrawBorder(targets);
            List<GameObject> rootTargets = lastMultiFiltered;
            if (rootTargets == null || rootTargets.Count == 0)
            {
                return;
            }
            Matrix4x4 originalMatrix = Handles.matrix;
            string activeTool = ToolManager.activeToolType.ToString();
            switch (activeTool)
            {
                case "UnityEditor.MoveTool":
                    DrawMoveHandle(rootTargets);
                    Tools.hidden = true;
                    break;
                case "UnityEditor.RotateTool":
                    DrawRotationHandle(rootTargets);
                    Tools.hidden = true;
                    break;
                case "UnityEditor.ScaleTool":
                    DrawScaleHandle(rootTargets);
                    Tools.hidden = true;
                    break;
                case "UnityEditor.RectTool":
                    SceneToolsOverlay.DrawSceneViewWarning("Rect Tool override not supported yet!\nTargetting active Selection");
                    return;
                case "UnityEditor.TransformTool":
                    DrawTransformHandle(rootTargets);
                    Tools.hidden = true;
                    break;
                default:
                    Tools.hidden = false;
                    break;
            }

            Handles.matrix = originalMatrix;
        }

        private static void DrawMoveHandle(GameObject target)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 position = GetHandlePosition(target);
            bool isCenter = Tools.pivotMode == PivotMode.Center;

            Vector3 newPosition = Handles.PositionHandle(position,
                Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : target.transform.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target.transform, "Move Object");

                if (CanSnap())
                {
                    if (EditorSnapSettings.move.x != 0)
                        newPosition.x = Mathf.Round(newPosition.x / EditorSnapSettings.move.x) * EditorSnapSettings.move.x;
                    if (EditorSnapSettings.move.y != 0)
                        newPosition.y = Mathf.Round(newPosition.y / EditorSnapSettings.move.y) * EditorSnapSettings.move.y;
                    if (EditorSnapSettings.move.z != 0)
                        newPosition.z = Mathf.Round(newPosition.z / EditorSnapSettings.move.z) * EditorSnapSettings.move.z;
                }

                if (isCenter)
                {
                    Vector3 delta = newPosition - GetHandlePosition(target);
                    target.transform.position += delta;
                    RefreshHandlePosition();
#if !UNITY_2022_1_OR_NEWER
           CoInspectorWindow.RefreshAllCoInspectors();
#endif
                }
                else
                {
                    target.transform.position = newPosition;
                }
            }
        }

        static bool CanSnap()
        {
#if UNITY_2022_1_OR_NEWER
            return EditorSnapSettings.gridSnapActive;
#else
            return EditorSnapSettings.gridSnapEnabled;
#endif

        }

        private static void DrawMoveHandle(List<GameObject> targets)
        {
            if (targets.Count == 0) return;

            bool isCenter = Tools.pivotMode == PivotMode.Center;
            bool globalMode = Tools.pivotRotation == PivotRotation.Global;
            GameObject primaryTarget = targets[targets.Count - 1];

            Quaternion handleRotation = globalMode ? Quaternion.identity : primaryTarget.transform.rotation;
            Vector3 handlePosition = isCenter ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(handlePosition, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = newPosition - handlePosition;
                if (CanSnap())
                {
                    if (EditorSnapSettings.move.x != 0)
                        delta.x = Mathf.Round(delta.x / EditorSnapSettings.move.x) * EditorSnapSettings.move.x;
                    if (EditorSnapSettings.move.y != 0)
                        delta.y = Mathf.Round(delta.y / EditorSnapSettings.move.y) * EditorSnapSettings.move.y;
                    if (EditorSnapSettings.move.z != 0)
                        delta.z = Mathf.Round(delta.z / EditorSnapSettings.move.z) * EditorSnapSettings.move.z;
                }

                Undo.RecordObjects(targets.Select(t => t.transform).ToArray(), "Move Objects");

                foreach (GameObject target in targets)
                {
                    if (isCenter)
                    {
                        target.transform.position += delta;
                    }
                    else
                    {
                        if (target == primaryTarget)
                        {
                            target.transform.position = newPosition;
                        }
                        else
                        {
                            target.transform.position += delta;
                        }
                    }
                }
                RefreshHandlePosition();
                CoInspectorWindow.RefreshAllCoInspectors();
            }
        }
        static bool HaveTargetsChanged(GameObject target)
        {
            bool changed = false;
            if (lastSelection == null || lastSelection.Length != 1)
            {
                changed = true;
            }
            if (!changed)
            {
                if (lastSelection[0] != target || lastSelection[0] == null)
                {
                    changed = true;
                }
            }
            if (changed)
            {
                lastSelection = new GameObject[] { target };
                lastMultiFiltered = null;
                RefreshCache();
                RefreshHandlePosition();
            }
            return changed;
        }
        static bool HaveTargetsChanged(GameObject[] targets)
        {
            bool changed = false;
            if (lastSelection == null || lastSelection.Length != targets.Length)
            {
                changed = true;
            }
            if (!changed)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (lastSelection[i] != targets[i] || lastSelection[i] == null)
                    {
                        changed = true;
                        break;
                    }
                }
            }
            if (changed)
            {
                lastSelection = new GameObject[targets.Length];
                Array.Copy(targets, lastSelection, targets.Length);
                lastMultiFiltered = GetFilteredTargets(targets);
                RefreshCache();
                RefreshHandlePosition();
            }
            return changed;
        }

        static bool IsInSelection(GameObject target)
        {
            if (Selection.gameObjects != null && Selection.gameObjects.Length == 1)
            {
                return Selection.gameObjects[0] == target;
            }

            return false;
        }
        static bool AreInSelection(GameObject[] targets)
        {
            return CoInspectorWindow.IsAlreadySelected(targets);
        }
        private static Vector3 GetSelectionCenter(List<GameObject> targets)
        {
            if (targets.Count == 0) return Vector3.zero;
            if (targets.Count == 1) return GetHandlePosition(targets[0]);
            if (refreshHandlePosition || cachedSelectionCenter == Vector3.zero)
            {
                Vector3 sum = Vector3.zero;
                foreach (GameObject target in targets)
                {
                    sum += target.transform.position;
                }
                cachedSelectionCenter = sum / targets.Count;
                refreshHandlePosition = false;
            }

            return cachedSelectionCenter;
        }

        private static Quaternion previousRotation = Quaternion.identity;
        private static Quaternion originalRotation = Quaternion.identity;
        private static Vector3 initialPosition = Vector3.zero;
        private static Vector3 rotationCenter = Vector3.zero;
        private static bool isDragging = false;

        private static void DrawRotationHandle(GameObject target)
        {
            bool globalMode = Tools.pivotRotation == PivotRotation.Global;
            bool centerMode = Tools.pivotMode == PivotMode.Center;
            if (Event.current.rawType == EventType.MouseDown)
            {
                isDragging = true;
                rotationCenter = GetHandlePosition(target);
                previousRotation = Quaternion.identity;
                originalRotation = target.transform.rotation;
                initialPosition = target.transform.position;
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isDragging = false;
                if (globalMode)
                {
                    previousRotation = Quaternion.identity;
                    originalRotation = target.transform.rotation;
                }
            }

            Vector3 position = isDragging ? rotationCenter : GetHandlePosition(target);

            EditorGUI.BeginChangeCheck();

            Quaternion handleRotation = globalMode
                ? Quaternion.identity
                : target.transform.rotation;

            Quaternion newHandleRotation;

            if (globalMode)
            {
                newHandleRotation = Handles.RotationHandle(previousRotation, position);
            }
            else
            {
                newHandleRotation = Handles.RotationHandle(handleRotation, position);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target.transform, "Rotate Object");

                if (centerMode)
                {
                    if (globalMode)
                    {
                        Quaternion totalRotation = newHandleRotation;
                        Vector3 centerToObject = initialPosition - position;
                        Vector3 rotatedOffset = totalRotation * centerToObject;

                        target.transform.position = position + rotatedOffset;
                        target.transform.rotation = totalRotation * originalRotation;
                        previousRotation = newHandleRotation;
                    }
                    else
                    {
                        Quaternion rotationDelta = newHandleRotation * Quaternion.Inverse(handleRotation);
                        target.transform.rotation = rotationDelta * target.transform.rotation;
                        target.transform.position = position + rotationDelta * (target.transform.position - position);
                    }
                    RefreshHandlePosition();

                }
                else
                {
                    if (globalMode)
                    {
                        target.transform.rotation = newHandleRotation * originalRotation;
                        previousRotation = newHandleRotation;
                    }
                    else
                    {
                        target.transform.rotation = newHandleRotation;
                    }
                }
            }
        }

        private static Dictionary<GameObject, Quaternion> originalRotations = new Dictionary<GameObject, Quaternion>();
        private static Dictionary<GameObject, Vector3> initialPositions = new Dictionary<GameObject, Vector3>();

        private static void DrawRotationHandle(List<GameObject> targets)
        {
            if (targets.Count == 0) return;

            bool globalMode = Tools.pivotRotation == PivotRotation.Global;
            bool centerMode = Tools.pivotMode == PivotMode.Center;
            GameObject primaryTarget = targets[targets.Count - 1];

            if (Event.current.rawType == EventType.MouseDown)
            {
                isDragging = true;
                rotationCenter = centerMode ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget);
                previousRotation = Quaternion.identity;

                originalRotations.Clear();
                initialPositions.Clear();
                foreach (GameObject target in targets)
                {
                    originalRotations[target] = target.transform.rotation;
                    initialPositions[target] = target.transform.position;
                }
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isDragging = false;
                if (globalMode)
                {
                    previousRotation = Quaternion.identity;
                    originalRotations.Clear();
                }
            }

            Vector3 position = isDragging ? rotationCenter :
                (centerMode ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget));

            EditorGUI.BeginChangeCheck();

            Quaternion handleRotation = globalMode ? Quaternion.identity : primaryTarget.transform.rotation;
            Quaternion newHandleRotation = globalMode ?
                Handles.RotationHandle(previousRotation, position) :
                Handles.RotationHandle(handleRotation, position);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets.Select(t => t.transform).ToArray(), "Rotate Objects");

                if (centerMode)
                {
                    if (globalMode)
                    {
                        Quaternion totalRotation = newHandleRotation;

                        foreach (GameObject target in targets)
                        {
                            Vector3 centerToObject = initialPositions[target] - position;
                            Vector3 rotatedOffset = totalRotation * centerToObject;

                            target.transform.position = position + rotatedOffset;
                            target.transform.rotation = totalRotation * originalRotations[target];
                        }
                        previousRotation = newHandleRotation;
                    }
                    else
                    {
                        Quaternion rotationDelta = newHandleRotation * Quaternion.Inverse(handleRotation);

                        foreach (GameObject target in targets)
                        {
                            target.transform.rotation = rotationDelta * target.transform.rotation;
                            target.transform.position = position +
                                rotationDelta * (target.transform.position - position);
                        }
                    }
                }
                else
                {
                    if (globalMode)
                    {
                        foreach (GameObject target in targets)
                        {
                            target.transform.rotation = newHandleRotation * originalRotations[target];
                        }
                        previousRotation = newHandleRotation;
                    }
                    else
                    {
                        Quaternion rotationDelta = newHandleRotation * Quaternion.Inverse(handleRotation);

                        foreach (GameObject target in targets)
                        {
                            target.transform.rotation = rotationDelta * target.transform.rotation;
                        }
                    }
                }
                RefreshHandlePosition();
                CoInspectorWindow.RefreshAllCoInspectors();
            }
        }

        private static bool isScaleDragging = false;
        private static Vector3 scaleCenter = Vector3.zero;
        private static Vector3 initialScale = Vector3.one;
        private static Quaternion initialRotation = Quaternion.identity;

        private static void DrawScaleHandle(GameObject target)
        {
            EditorGUI.BeginChangeCheck();
            bool centerMode = Tools.pivotMode == PivotMode.Center;
            if (Event.current.rawType == EventType.MouseDown)
            {
                isScaleDragging = true;
                scaleCenter = GetHandlePosition(target);
                initialPosition = target.transform.position;
                initialScale = target.transform.localScale;
                initialRotation = target.transform.rotation;
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isScaleDragging = false;
            }
            Vector3 position = isScaleDragging ? scaleCenter : GetHandlePosition(target);
            Quaternion handleRotation = target.transform.rotation;
            Vector3 newScale = Handles.ScaleHandle(
                target.transform.localScale,
                position,
                handleRotation,
                GetHandleSize(position)
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target.transform, "Scale Object");

                if (CanSnap() && EditorSnapSettings.scale != 0)
                {
                    newScale.x = Mathf.Round(newScale.x / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                    newScale.y = Mathf.Round(newScale.y / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                    newScale.z = Mathf.Round(newScale.z / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                }

                if (centerMode)
                {
                    Vector3 localCenterOffset = Quaternion.Inverse(handleRotation) * (initialPosition - position);
                    Vector3 scaleRatio = new Vector3(
                        newScale.x / initialScale.x,
                        newScale.y / initialScale.y,
                        newScale.z / initialScale.z
                    );
                    Vector3 scaledLocalOffset = Vector3.Scale(localCenterOffset, scaleRatio);
                    Vector3 scaledOffset = handleRotation * scaledLocalOffset;
                    target.transform.localScale = newScale;
                    target.transform.position = position + scaledOffset;
                    RefreshHandlePosition();
                }
                else
                {
                    target.transform.localScale = newScale;
                }
            }
        }
        private static Dictionary<GameObject, Vector3> initialScales = new Dictionary<GameObject, Vector3>();
        private static Dictionary<GameObject, Quaternion> initialRotations = new Dictionary<GameObject, Quaternion>();
        private static Vector3 initialHandleScale = Vector3.one;


        private static void DrawScaleHandle(List<GameObject> targets)
        {
            if (targets.Count == 0) return;

            bool centerMode = Tools.pivotMode == PivotMode.Center;
            bool globalMode = Tools.pivotRotation == PivotRotation.Global;
            GameObject primaryTarget = targets[targets.Count - 1];

            if (Event.current.rawType == EventType.MouseDown)
            {
                isScaleDragging = true;
                scaleCenter = centerMode ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget);

                initialScales.Clear();
                initialPositions.Clear();
                foreach (GameObject target in targets)
                {
                    initialScales[target] = target.transform.localScale;
                    initialPositions[target] = target.transform.position;
                }
                initialHandleScale = primaryTarget.transform.localScale;
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isScaleDragging = false;
            }

            Vector3 position = isScaleDragging ? scaleCenter :
                (centerMode ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget));

            Quaternion handleRotation = primaryTarget.transform.rotation;

            EditorGUI.BeginChangeCheck();
            Vector3 newScale = Handles.ScaleHandle(
                primaryTarget.transform.localScale,
                position,
                handleRotation,
                GetHandleSize(position)
            );

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets.Select(t => t.transform).ToArray(), "Scale Objects");

                Vector3 scaleRatio = new Vector3(
                    newScale.x / initialHandleScale.x,
                    newScale.y / initialHandleScale.y,
                    newScale.z / initialHandleScale.z
                );

                if (CanSnap() && EditorSnapSettings.scale != 0)
                {
                    scaleRatio.x = Mathf.Round(scaleRatio.x / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                    scaleRatio.y = Mathf.Round(scaleRatio.y / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                    scaleRatio.z = Mathf.Round(scaleRatio.z / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                }

                foreach (GameObject target in targets)
                {
                    if (centerMode)
                    {
                        Vector3 localCenterOffset = Quaternion.Inverse(target.transform.rotation) *
                            (initialPositions[target] - position);
                        Vector3 scaledLocalOffset = Vector3.Scale(localCenterOffset, scaleRatio);
                        Vector3 scaledOffset = target.transform.rotation * scaledLocalOffset;

                        target.transform.position = position + scaledOffset;
                    }

                    target.transform.localScale = Vector3.Scale(initialScales[target], scaleRatio);
                }
                RefreshHandlePosition();
                CoInspectorWindow.RefreshAllCoInspectors();
            }
        }


        private static void DrawRectHandle(GameObject target)
        {

        }

        static Vector3 previousPosition = Vector3.zero;
        static bool positionOperation = false;
        static bool scaleOperation = false;
        static bool rotationOperation = false;

        static bool DidRotate(Quaternion quaternion)
        {
            string quatString = $"{quaternion.x:F5},{quaternion.y:F5},{quaternion.z:F5},{quaternion.w:F5}";
            string identityString = "0.00000,0.00000,0.00000,1.00000";

            return quatString != identityString;
        }
        private static void DrawTransformHandle(GameObject target)
        {
            bool globalMode = Tools.pivotRotation == PivotRotation.Global;
            bool centerMode = Tools.pivotMode == PivotMode.Center;
            Transform transform = target.transform;
            if (Event.current.rawType == EventType.MouseDown)
            {
                isDragging = true;
                rotationCenter = GetHandlePosition(target);
                previousRotation = Quaternion.identity;
                originalRotation = transform.rotation;
                initialPosition = transform.position;
                initialScale = transform.localScale;
                previousPosition = rotationCenter;
                positionOperation = false;
                scaleOperation = false;
                rotationOperation = false;
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isDragging = false;
                previousRotation = Quaternion.identity;
                originalRotation = transform.rotation;
                initialScale = transform.localScale;
                initialPosition = transform.position;
                previousPosition = Vector3.zero;
                positionOperation = false;
                scaleOperation = false;
                rotationOperation = false;
            }
            Vector3 position = isDragging && centerMode ? rotationCenter : GetHandlePosition(target);
            if (positionOperation && centerMode)
            {
                position = previousPosition;
            }
            Quaternion rotation = globalMode ? previousRotation : transform.rotation;
            Vector3 scale = transform.localScale;
            EditorGUI.BeginChangeCheck();
            Handles.TransformHandle(ref position, ref rotation, ref scale);
            if (EditorGUI.EndChangeCheck())
            {

                if (!positionOperation && !scaleOperation && !rotationOperation)
                {
                    Vector3 scaleDelta = scale - initialScale;
                    Quaternion rotationDelta = globalMode ?
                        rotation * Quaternion.Inverse(previousRotation) :
                        rotation * Quaternion.Inverse(transform.rotation);
                    Vector3 positionDelta = position - GetHandlePosition(target);
                    scaleOperation = scaleDelta.sqrMagnitude != 0;
                    rotationOperation = DidRotate(rotationDelta);
                    if (!rotationOperation && !scaleOperation)
                    {
                        positionOperation = positionDelta.sqrMagnitude > 0.0001f;
                    }
                    if (!rotationOperation && !scaleOperation && !positionOperation)
                    {
                        return;
                    }
                }
                Undo.RecordObject(transform, "Transform Object");
                if (!centerMode)
                {
                    if (positionOperation)
                    {
                        ApplyPositionSnapping(ref position);
                        transform.position = position;
                    }
                    else if (rotationOperation)
                    {
                        if (globalMode)
                        {
                            transform.rotation = rotation * originalRotation;
                            previousRotation = rotation;
                        }
                        else
                        {
                            transform.rotation = rotation;
                        }
                    }
                    else if (scaleOperation)
                    {
                        ApplyScaleSnapping(ref scale);
                        transform.localScale = scale;
                    }

                }
                else
                {
                    if (positionOperation)
                    {
                        ApplyPositionSnapping(ref position);
                        Vector3 delta = position - GetHandlePosition(target);
                        transform.position += delta;
                        previousPosition = position;
                    }
                    else if (rotationOperation)
                    {
                        if (globalMode)
                        {
                            Quaternion deltaRotation = rotation * Quaternion.Inverse(previousRotation);
                            previousRotation = rotation;
                            Vector3 pivot = rotationCenter;
                            Vector3 direction = transform.position - pivot;
                            direction = deltaRotation * direction;
                            transform.position = pivot + direction;
                            transform.rotation = deltaRotation * transform.rotation;
                        }
                        else
                        {
                            Quaternion deltaRotation = rotation * Quaternion.Inverse(transform.rotation);
                            Vector3 pivot = rotationCenter;
                            Vector3 direction = transform.position - pivot;
                            direction = deltaRotation * direction;
                            transform.position = pivot + direction;
                            transform.rotation = deltaRotation * transform.rotation;
                        }
                    }
                    else if (scaleOperation)
                    {
                        ApplyScaleSnapping(ref scale);
                        Vector3 pivot = rotationCenter;
                        Vector3 localCenterOffset = Quaternion.Inverse(transform.rotation) * (initialPosition - pivot);
                        Vector3 scaleRatio = new Vector3(
                            scale.x / initialScale.x,
                            scale.y / initialScale.y,
                            scale.z / initialScale.z
                        );
                        Vector3 scaledLocalOffset = Vector3.Scale(localCenterOffset, scaleRatio);
                        Vector3 scaledOffset = transform.rotation * scaledLocalOffset;
                        transform.localScale = scale;
                        transform.position = pivot + scaledOffset;
                    }
                    RefreshHandlePosition();
#if !UNITY_2022_1_OR_NEWER
                    CoInspectorWindow.RefreshAllCoInspectors();
#endif
                }
            }
        }

        private static void DrawTransformHandle(List<GameObject> targets)
        {
            if (targets.Count == 0) return;

            bool globalMode = Tools.pivotRotation == PivotRotation.Global;
            bool centerMode = Tools.pivotMode == PivotMode.Center;
            GameObject primaryTarget = targets[targets.Count - 1];

            if (Event.current.rawType == EventType.MouseDown)
            {
                isDragging = true;
                rotationCenter = centerMode ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget);
                previousRotation = Quaternion.identity;

                originalRotations.Clear();
                initialPositions.Clear();
                initialScales.Clear();
                foreach (GameObject target in targets)
                {
                    originalRotations[target] = target.transform.rotation;
                    initialPositions[target] = target.transform.position;
                    initialScales[target] = target.transform.localScale;
                }

                previousPosition = rotationCenter;
                positionOperation = false;
                scaleOperation = false;
                rotationOperation = false;
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isDragging = false;
                previousRotation = Quaternion.identity;
                foreach (GameObject target in targets)
                {
                    originalRotations[target] = target.transform.rotation;
                    initialScales[target] = target.transform.localScale;
                    initialPositions[target] = target.transform.position;
                }
                previousPosition = Vector3.zero;
                positionOperation = false;
                scaleOperation = false;
                rotationOperation = false;
            }

            Vector3 position = isDragging && centerMode ? rotationCenter :
                (centerMode ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget));


            if (positionOperation && centerMode)
            {
                position = previousPosition;
            }

            Quaternion rotation = globalMode ? previousRotation : primaryTarget.transform.rotation;
            Vector3 scale = primaryTarget.transform.localScale;

            EditorGUI.BeginChangeCheck();
            Handles.TransformHandle(ref position, ref rotation, ref scale);

            if (EditorGUI.EndChangeCheck())
            {
                if (!positionOperation && !scaleOperation && !rotationOperation)
                {
                    Vector3 scaleDelta = scale - initialScales[primaryTarget];
                    Quaternion rotationDelta = globalMode ?
                        rotation * Quaternion.Inverse(previousRotation) :
                        rotation * Quaternion.Inverse(primaryTarget.transform.rotation);
                    Vector3 comparePosition = centerMode ? GetSelectionCenter(targets) : GetHandlePosition(primaryTarget);
                    Vector3 positionDelta = position - comparePosition;
                    scaleOperation = scaleDelta.sqrMagnitude != 0;
                    rotationOperation = DidRotate(rotationDelta);
                    if (!rotationOperation && !scaleOperation)
                    {
                        positionOperation = positionDelta.sqrMagnitude > 0.0001f;
                    }
                    if (!rotationOperation && !scaleOperation && !positionOperation)
                    {
                        return;
                    }
                }
                Undo.RecordObjects(targets.Select(t => t.transform).ToArray(), "Transform Objects");
                if (!centerMode)
                {
                    if (positionOperation)
                    {
                        ApplyPositionSnapping(ref position);
                        Vector3 delta = position - primaryTarget.transform.position;
                        foreach (GameObject target in targets)
                        {
                            target.transform.position += delta;
                        }
                    }
                    else if (rotationOperation)
                    {
                        if (globalMode)
                        {
                            foreach (GameObject target in targets)
                            {
                                target.transform.rotation = rotation * originalRotations[target];
                            }
                            previousRotation = rotation;
                        }
                        else
                        {
                            Quaternion rotationDelta = rotation * Quaternion.Inverse(primaryTarget.transform.rotation);
                            foreach (GameObject target in targets)
                            {
                                target.transform.rotation = rotationDelta * target.transform.rotation;
                            }
                        }
                    }
                    else if (scaleOperation)
                    {
                        foreach (GameObject target in targets)
                        {
                            ApplyScaleSnapping(ref scale);
                            target.transform.localScale = scale;
                        }
                    }
                }
                else
                {

                    if (positionOperation)
                    {
                        ApplyPositionSnapping(ref position);
                        Vector3 delta = position - GetSelectionCenter(targets);
                        foreach (GameObject target in targets)
                        {
                            target.transform.position += delta;
                        }
                        previousPosition = position;
                    }
                    else if (rotationOperation)
                    {
                        if (globalMode)
                        {
                            Quaternion deltaRotation = rotation * Quaternion.Inverse(previousRotation);
                            previousRotation = rotation; // Update this after delta calculation

                            Vector3 pivot = rotationCenter;

                            foreach (GameObject target in targets)
                            {
                                Transform transform = target.transform;
                                Vector3 direction = transform.position - pivot;
                                direction = deltaRotation * direction;
                                transform.position = pivot + direction;
                                transform.rotation = deltaRotation * transform.rotation;
                            }
                        }
                        else
                        {
                            Transform primaryTransform = primaryTarget.transform;
                            Quaternion deltaRotation = rotation * Quaternion.Inverse(primaryTransform.rotation);

                            Vector3 pivot = rotationCenter;

                            foreach (GameObject target in targets)
                            {
                                Transform transform = target.transform;
                                Vector3 direction = transform.position - pivot;
                                direction = deltaRotation * direction;
                                transform.position = pivot + direction;
                                transform.rotation = deltaRotation * transform.rotation;
                            }
                        }
                    }
                    else if (scaleOperation)
                    {
                        ApplyScaleSnapping(ref scale);
                        Vector3 pivot = rotationCenter;

                        foreach (GameObject target in targets)
                        {
                            Transform transform = target.transform;
                            Vector3 localCenterOffset = Quaternion.Inverse(transform.rotation) *
                                (initialPositions[target] - pivot);
                            Vector3 scaleRatio = new Vector3(
                                scale.x / initialScales[primaryTarget].x,
                                scale.y / initialScales[primaryTarget].y,
                                scale.z / initialScales[primaryTarget].z
                            );
                            Vector3 scaledLocalOffset = Vector3.Scale(localCenterOffset, scaleRatio);
                            Vector3 scaledOffset = transform.rotation * scaledLocalOffset;

                            transform.localScale = scale;
                            transform.position = pivot + scaledOffset;
                        }
                    }
                }
                RefreshHandlePosition();
                CoInspectorWindow.RefreshAllCoInspectors();
            }
        }

        private static void ApplyPositionSnapping(ref Vector3 position)
        {
            if (CanSnap())
            {
                if (EditorSnapSettings.move.x != 0)
                    position.x = Mathf.Round(position.x / EditorSnapSettings.move.x) * EditorSnapSettings.move.x;
                if (EditorSnapSettings.move.y != 0)
                    position.y = Mathf.Round(position.y / EditorSnapSettings.move.y) * EditorSnapSettings.move.y;
                if (EditorSnapSettings.move.z != 0)
                    position.z = Mathf.Round(position.z / EditorSnapSettings.move.z) * EditorSnapSettings.move.z;
            }
        }

        private static void ApplyScaleSnapping(ref Vector3 scale)
        {
            if (CanSnap() && EditorSnapSettings.scale != 0)
            {
                scale.x = Mathf.Round(scale.x / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                scale.y = Mathf.Round(scale.y / EditorSnapSettings.scale) * EditorSnapSettings.scale;
                scale.z = Mathf.Round(scale.z / EditorSnapSettings.scale) * EditorSnapSettings.scale;
            }
        }
        public static float GetHandleSize(Vector3 position)
        {
            Camera current = Camera.current;
            position = Handles.matrix.MultiplyPoint(position);
            if ((bool)current)
            {
                Transform transform = current.transform;
                Vector3 position2 = transform.position;
                float z = Vector3.Dot(position - position2, transform.TransformDirection(new Vector3(0f, 0f, 1f)));
                Vector3 vector = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(0f, 0f, z)));
                Vector3 vector2 = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(1f, 0f, z)));
                float magnitude = (vector - vector2).magnitude;
                return 80f / Mathf.Max(magnitude, 0.0001f) * EditorGUIUtility.pixelsPerPoint;
            }

            return 20f;
        }

        public static Vector3 GetHandlePosition(GameObject target, bool multi = false)
        {
            if (target == null || target.transform == null)
            {
                return Vector3.zero;
            }

            bool isPivot = Tools.pivotMode == PivotMode.Pivot;

            if (isPivot)
            {
                DrawPivotColliders(target);
                return target.transform.position;
            }
            return GetHandle(target);
        }

        private static Vector3 GetHandle(GameObject target)
        {
            if (target == null || target.transform == null)
            {
                return Vector3.zero;
            }
            if (refreshHandlePosition)
            {
                handlePosition = GetObjectTreeBounds(target).center;
                refreshHandlePosition = false;
            }
            DrawColliders(new GameObject[] { target });
            return handlePosition;
        }

        private static MethodInfo GetCalculateBoundsMethod()
        {
            if (_calculateBoundsMethod == null)
            {
                _calculateBoundsMethod = typeof(GameObject).GetMethod("CalculateBounds",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
            }
            return _calculateBoundsMethod;
        }
        private static Bounds GetObjectBounds(GameObject go)
        {
            if (GetCalculateBoundsMethod() == null)
            {
                return new Bounds();
            }
            Bounds bounds = (Bounds)GetCalculateBoundsMethod().Invoke(go, null);
            return bounds;
        }
        private static GameObject[] objectsCache;
        private static void DrawPivotColliders(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            DrawColliders(objectsCache);
        }
        private static readonly GameObject[] singleObjectArray = new GameObject[1];
        private static readonly Color parentColor = new Color(0, 1, 0, 0.5f);
        private static readonly Color childColor = new Color(0, 1, 01, 0.5f);
        private static readonly Color totalColor = new Color(0.53f, 0.53f, 1, 0.5f);

        private static Bounds GetObjectTreeBounds(GameObject go, bool multi = false)
        {
            if (GetCalculateBoundsMethod() == null)
            {
                return new Bounds();
            }

            Bounds totalBounds = (Bounds)GetCalculateBoundsMethod().Invoke(go, null);
            bool isFirst = true;
            Transform[] transforms = go.GetComponentsInChildren<Transform>();

            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject obj = transforms[i].gameObject;
                Bounds objBounds = (Bounds)GetCalculateBoundsMethod().Invoke(obj, null);

                if (objBounds.size == Vector3.zero)
                {
                    //continue;
                }

                singleObjectArray[0] = obj;
                DrawColliders(singleObjectArray);
                Handles.color = (obj == go) ? parentColor : childColor;

                if (isFirst)
                {
                    Handles.color = totalColor;
                    totalBounds = objBounds;
                    isFirst = false;
                }
                else
                {
                    totalBounds.Encapsulate(objBounds);
                }
            }

            if (!isFirst)
            {
                Handles.color = totalColor;
            }
            return totalBounds;
        }

        private static void RefreshColliderCache(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0)
            {
                colliders = new Collider[0];
                colliders2D = new Collider2D[0];
                objectsCache = new GameObject[0];
                return;
            }

            if (refreshCache)
            {
                List<Collider> _colliders = new List<Collider>();
                List<Collider2D> _colliders2D = new List<Collider2D>();
                List<GameObject> _objectCache = new List<GameObject>();

                foreach (GameObject obj in objects)
                {
                    if (obj != null)
                    {
                        Transform[] transforms = obj.GetComponentsInChildren<Transform>(true);
                        foreach (Transform t in transforms)
                        {
                            GameObject go = t.gameObject;
                            _objectCache.Add(go);

                            _colliders.AddRange(go.GetComponents<Collider>());
                            _colliders2D.AddRange(go.GetComponents<Collider2D>());
                        }
                    }
                }

                objectsCache = _objectCache.ToArray();
                colliders = _colliders.ToArray();
                colliders2D = _colliders2D.ToArray();
                refreshCache = false;
            }
        }

        private static void DrawColliders(GameObject[] objects)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color _colliderColor = new Color(1f, 1f, 0.5f, 0.5f);
            Handles.color = _colliderColor;
            RefreshColliderCache(objects);
            if (colliders != null)
            {
                foreach (var collider in colliders)
                {
                    if (!collider.enabled || Mathf.Approximately(collider.transform.lossyScale.sqrMagnitude, 0f))
                    {
                        continue;
                    }

                    switch (collider)
                    {
                        case SphereCollider sphere:
                            Vector3 sphereCenter = sphere.transform.TransformPoint(sphere.center);
                            float sphereRadius = sphere.radius * Mathf.Max(
                                Mathf.Abs(sphere.transform.lossyScale.x),
                                Mathf.Abs(sphere.transform.lossyScale.y),
                                Mathf.Abs(sphere.transform.lossyScale.z)
                            );

                            Handles.DrawWireDisc(sphereCenter, sphere.transform.right, sphereRadius);
                            Handles.DrawWireDisc(sphereCenter, sphere.transform.up, sphereRadius);
                            Handles.DrawWireDisc(sphereCenter, sphere.transform.forward, sphereRadius);
                            break;

                        case CapsuleCollider capsule:
                            if (!Mathf.Approximately(capsule.transform.lossyScale.sqrMagnitude, 0f))
                            {
                                Vector3 center = capsule.transform.TransformPoint(capsule.center);
                                float radius = capsule.radius * Mathf.Max(
                                    Mathf.Abs(capsule.transform.lossyScale.x),
                                    Mathf.Abs(capsule.transform.lossyScale.y),
                                    Mathf.Abs(capsule.transform.lossyScale.z)
                                );

                                Vector3 direction = capsule.direction switch
                                {
                                    0 => capsule.transform.right,
                                    1 => capsule.transform.up,
                                    _ => capsule.transform.forward
                                };

                                float height = capsule.height * Mathf.Abs(
                                    capsule.direction switch
                                    {
                                        0 => capsule.transform.lossyScale.x,
                                        1 => capsule.transform.lossyScale.y,
                                        _ => capsule.transform.lossyScale.z
                                    }
                                );

                                Vector3 pointTop = center + direction * (height * 0.5f - radius);
                                Vector3 pointBottom = center - direction * (height * 0.5f - radius);

                                Handles.DrawWireDisc(pointTop, direction, radius);
                                Handles.DrawWireDisc(pointBottom, direction, radius);

                                Vector3 from = capsule.transform.up;
                                if (Vector3.Dot(from, direction) > 0.999f)
                                    from = capsule.transform.right;

                                Vector3 crossDir = Vector3.Cross(from, direction).normalized;

                                Handles.DrawWireArc(pointTop, from, -crossDir, 180f, radius);
                                Handles.DrawWireArc(pointBottom, from, crossDir, 180f, radius);
                                Handles.DrawWireArc(pointTop, crossDir, from, 180f, radius);
                                Handles.DrawWireArc(pointBottom, crossDir, -from, 180f, radius);

                                Handles.DrawLine(pointTop + from * radius, pointBottom + from * radius);
                                Handles.DrawLine(pointTop - from * radius, pointBottom - from * radius);
                                Handles.DrawLine(pointTop + crossDir * radius, pointBottom + crossDir * radius);
                                Handles.DrawLine(pointTop - crossDir * radius, pointBottom - crossDir * radius);
                            }
                            break;

                        case BoxCollider box:
                            Matrix4x4 matrix = box.transform.localToWorldMatrix;
                            using (new Handles.DrawingScope(matrix))
                            {
                                Handles.DrawWireCube(box.center, box.size);
                            }
                            break;

                        case WheelCollider wheel:
                            if (wheel.attachedRigidbody == null)
                            {
                                break;
                            }

                            Vector3 wheelCenter = wheel.transform.TransformPoint(wheel.center);
                            float wheelRadius = wheel.radius * Mathf.Max(
                                Mathf.Abs(wheel.transform.lossyScale.x),
                                Mathf.Abs(wheel.transform.lossyScale.y),
                                Mathf.Abs(wheel.transform.lossyScale.z)
                            );

                            float suspensionDistance = wheel.suspensionDistance;
                            Vector3 suspensionRestPos = wheelCenter - wheel.transform.up * wheel.suspensionDistance;

                            Vector3 wheelRestPos = wheelCenter - wheel.transform.up * wheel.suspensionDistance;
                            Handles.DrawWireDisc(wheelRestPos, wheel.transform.right, wheelRadius);

                            Handles.DrawLine(wheelRestPos - wheel.transform.forward * wheelRadius, wheelRestPos + wheel.transform.forward * wheelRadius);

                            float suspensionPoint = wheelRadius * 0.1f;
                            Vector3 suspensionPos = wheelRestPos - wheel.transform.up * wheelRadius;
                            Handles.DrawWireDisc(suspensionPos, wheel.transform.up, suspensionPoint);
                            Handles.DrawWireDisc(suspensionPos, wheel.transform.right, suspensionPoint);
                            Handles.DrawWireDisc(suspensionPos, wheel.transform.forward, suspensionPoint);
                            break;
                        case TerrainCollider terrain:
                            if (terrain.terrainData != null)
                            {
                                Matrix4x4 terrainMatrix = terrain.transform.localToWorldMatrix;
                                using (new Handles.DrawingScope(terrainMatrix))
                                {
                                    Vector3 size = terrain.terrainData.size;
                                    Handles.DrawWireCube(size * 0.5f, size);
                                }
                            }
                            break;
                    }
                }
            }
            if (colliders2D == null)
            {
                return;
            }

            foreach (var collider2D in colliders2D)
            {


                if (!collider2D.enabled || Mathf.Approximately(collider2D.transform.lossyScale.sqrMagnitude, 0f))
                    continue;

                // Create proper 2D projection matrix for all colliders
                Matrix4x4 _handleMatrix = collider2D.transform.localToWorldMatrix;
                _handleMatrix.SetRow(0, Vector4.Scale(_handleMatrix.GetRow(0), new Vector4(1f, 1f, 0f, 1f)));
                _handleMatrix.SetRow(1, Vector4.Scale(_handleMatrix.GetRow(1), new Vector4(1f, 1f, 0f, 1f)));
                _handleMatrix.SetRow(2, new Vector4(0f, 0f, 1f, collider2D.transform.position.z));

                using (new Handles.DrawingScope(_handleMatrix))
                {
                    Handles.color = _colliderColor;

                    switch (collider2D)
                    {
                        case BoxCollider2D box:
                            Vector2 _center = box.offset;
                            Vector2 _size = box.size;

                            Vector3 topLeft = new Vector3(_center.x - _size.x / 2, _center.y + _size.y / 2, 0);
                            Vector3 topRight = new Vector3(_center.x + _size.x / 2, _center.y + _size.y / 2, 0);
                            Vector3 bottomRight = new Vector3(_center.x + _size.x / 2, _center.y - _size.y / 2, 0);
                            Vector3 bottomLeft = new Vector3(_center.x - _size.x / 2, _center.y - _size.y / 2, 0);

                            Handles.DrawLine(topLeft, topRight);
                            Handles.DrawLine(topRight, bottomRight);
                            Handles.DrawLine(bottomRight, bottomLeft);
                            Handles.DrawLine(bottomLeft, topLeft);
                            break;

                        case CircleCollider2D circle:
                            Handles.DrawWireDisc(circle.offset, Vector3.forward, circle.radius);
                            break;

                        case CapsuleCollider2D capsule:
                            Vector2 _capsuleSize = capsule.size;
                            float _radius = _capsuleSize.x * 0.5f;
                            float _height = _capsuleSize.y;
                            Vector2 _offset = capsule.offset;

                            if (capsule.direction == CapsuleDirection2D.Vertical)
                            {
                                float _halfHeight = (_height - 2 * _radius) * 0.5f;
                                Vector3 _top = new Vector3(_offset.x, _offset.y + _halfHeight, 0);
                                Vector3 _bottom = new Vector3(_offset.x, _offset.y - _halfHeight, 0);

                                Handles.DrawWireArc(_top, Vector3.forward, Vector3.right, 180f, _radius);
                                Handles.DrawWireArc(_bottom, Vector3.forward, Vector3.left, 180f, _radius);

                                Handles.DrawLine(_top + Vector3.right * _radius, _bottom + Vector3.right * _radius);
                                Handles.DrawLine(_top + Vector3.left * _radius, _bottom + Vector3.left * _radius);
                            }
                            else
                            {
                                float _halfWidth = (_height - 2 * _radius) * 0.5f;
                                Vector3 _left = new Vector3(_offset.x - _halfWidth, _offset.y, 0);
                                Vector3 _right = new Vector3(_offset.x + _halfWidth, _offset.y, 0);

                                Handles.DrawWireArc(_left, Vector3.forward, Vector3.down, 180f, _radius);
                                Handles.DrawWireArc(_right, Vector3.forward, Vector3.up, 180f, _radius);

                                Handles.DrawLine(_left + Vector3.up * _radius, _right + Vector3.up * _radius);
                                Handles.DrawLine(_left + Vector3.down * _radius, _right + Vector3.down * _radius);
                            }
                            break;

                        case PolygonCollider2D polygon:
                            Vector2[] _points = polygon.points;
                            for (int i = 0; i < _points.Length; i++)
                            {
                                Vector3 _point = _points[i] + polygon.offset;
                                Vector3 _nextPoint = _points[(i + 1) % _points.Length] + polygon.offset;
                                Handles.DrawLine(_point, _nextPoint);
                            }
                            break;

                        case EdgeCollider2D edge:
                            Vector2[] _edgePoints = edge.points;
                            for (int i = 0; i < _edgePoints.Length - 1; i++)
                            {
                                Vector3 _point = _edgePoints[i] + edge.offset;
                                Vector3 _nextPoint = _edgePoints[i + 1] + edge.offset;
                                Handles.DrawLine(_point, _nextPoint);
                            }
                            break;
                    }
                }
            }
        }



        public static GameObject[] GetSelfAndChildren(GameObject target, bool includeInactive = true)
        {
            if (!target) return new GameObject[0];

            Transform[] transforms = target.GetComponentsInChildren<Transform>(includeInactive);
            GameObject[] objects = new GameObject[transforms.Length];

            for (int i = 0; i < transforms.Length; i++)
            {
                objects[i] = transforms[i].gameObject;
            }

            return objects;
        }

        public static Vector3[] GetChildrenPositions(GameObject target, bool includeInactive = false)
        {
            if (!target) return new Vector3[0];

            Transform[] transforms = target.GetComponentsInChildren<Transform>(includeInactive);
            Vector3[] positions = new Vector3[transforms.Length];
            positions[0] = target.transform.position;
            for (int i = 1; i < transforms.Length; i++)
            {
                positions[i] = transforms[i].position;
            }
            return positions;
        }
        internal static Bounds CalculateSelectionBoundsInSpace(Vector3 position, Quaternion rotation, bool rectBlueprintMode, GameObject[] gameObjects)
        {
            Quaternion quaternion = Quaternion.Inverse(rotation);
            Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3[] array = new Vector3[2];
            foreach (GameObject gameObject in gameObjects)
            {
                Bounds localBounds = GetLocalBounds(gameObject);
                array[0] = localBounds.min;
                array[1] = localBounds.max;
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        for (int l = 0; l < 2; l++)
                        {
                            Vector3 vector3 = new Vector3(array[j].x, array[k].y, array[l].z);
                            if (rectBlueprintMode && SupportsRectLayout(gameObject.transform))
                            {
                                Vector3 localPosition = gameObject.transform.localPosition;
                                localPosition.z = 0f;
                                vector3 = gameObject.transform.parent.TransformPoint(vector3 + localPosition);
                            }
                            else
                            {
                                vector3 = gameObject.transform.TransformPoint(vector3);
                            }

                            vector3 = quaternion * (vector3 - position);
                            for (int m = 0; m < 3; m++)
                            {
                                vector[m] = Mathf.Min(vector[m], vector3[m]);
                                vector2[m] = Mathf.Max(vector2[m], vector3[m]);
                            }
                        }
                    }
                }
            }
            return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
        }



        private static Bounds GetLocalBounds(GameObject gameObject)
        {
            if (gameObject.TryGetComponent<RectTransform>(out var component))
            {
                return new Bounds(component.rect.center, component.rect.size);
            }

            if (gameObject.TryGetComponent<MeshFilter>(out var component2) && component2.sharedMesh != null)
            {
                return component2.sharedMesh.bounds;
            }

            if (gameObject.TryGetComponent<Renderer>(out var component3))
            {
                if (component3 is SpriteRenderer)
                {
                    return ((SpriteRenderer)component3).GetSpriteBounds();
                }

                if (component3 is SpriteMask)
                {
                    return ((SpriteMask)component3).GetSpriteBounds();
                }

                if (component3 is SpriteShapeRenderer)
                {
                    return ((SpriteShapeRenderer)component3).GetLocalAABB();
                }

                if (component3 is TilemapRenderer && component3.TryGetComponent<Tilemap>(out var component4))
                {
                    return component4.localBounds;
                }
            }
            return new Bounds(Vector3.zero, Vector3.zero);
        }
        static Bounds GetSpriteBounds(this SpriteRenderer spriteRenderer)
        {
            if (spriteRendererGetSpriteBounds == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
            return (Bounds)spriteRendererGetSpriteBounds.Invoke(spriteRenderer, null);
        }

        static Bounds GetSpriteBounds(this SpriteMask spriteMask)
        {
            if (spriteMaskGetSpriteBounds == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            return (Bounds)spriteMaskGetSpriteBounds.Invoke(spriteMask, null);
        }
        public static void DrawBorder(GameObject target)
        {
#if UNITY_2022_1_OR_NEWER
            if (!ShouldDrawBorder())
            {
                return;
            }

            if (target == null) { return; }
            if (Event.current.type != EventType.Repaint) { return; }
            singleObjectArray[0] = target;
            Handles.DrawOutline(singleObjectArray, pink, Color.cyan);
#endif
        }
        private static void DrawBorder(GameObject[] targets)
        {
            if (Event.current.type != EventType.Repaint) return;

#if UNITY_2022_1_OR_NEWER
            if (!ShouldDrawBorder())
            {
                return;
            }
            if (targets == null)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            Handles.DrawOutline(targets, pink, Color.cyan);
#endif
        }
        static Color pink = new Color(1f, 0.35f, 0.6f, 1f);

        public static bool ShouldDrawGizmos()
        {
            return SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.drawGizmos;
        }
        private static PropertyInfo _showSelectionOutlineProperty;

        private static bool ShouldDrawBorder()
        {
            if (_showSelectionOutlineProperty == null)
            {
                var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
                var annotationType = assembly.GetType("UnityEditor.AnnotationUtility");
                _showSelectionOutlineProperty = annotationType.GetProperty("showSelectionOutline",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            }
            return (bool)_showSelectionOutlineProperty.GetValue(null);
        }


        static Bounds GetLocalAABB(this SpriteShapeRenderer spriteShapeRenderer)
        {
            if (spriteShapeRendererGetLocalAABB == null)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            return (Bounds)spriteShapeRendererGetLocalAABB.Invoke(spriteShapeRenderer, null);
        }
        private static bool SupportsRectLayout(Transform transform) => transform.GetComponent<RectTransform>() != null;
    }
    [Overlay(typeof(SceneView), "CoInspector Scene Tools", true)]
    internal class SceneToolsOverlay : ToolbarOverlay
    {
        static VisualElement sceneToolbarButton;
        static Texture2D activeTexture;
        static bool overrideSceneTools = false;

        SceneToolsOverlay() : base(SceneToolbarButton.id)
        {
            EditorApplication.update += OnEditorUpdate;
        }

        [EditorToolbarElement(id, typeof(SceneView))]
        class SceneToolbarButton : EditorToolbarToggle
        {
            internal const string id = "CoInspector/SceneToolsButton";

            internal SceneToolbarButton()
            {
                value = overrideSceneTools;
                icon = value ? CustomGUIContents.SceneToolsButtonImageSelected : CustomGUIContents.SceneToolsButtonImage;
                tooltip = "Override Scene Tools\n\n(Move, Rotate, Scale… etc, will target the active Tab instead of Selection)";
                this.RegisterValueChangedCallback(evt =>
                {
                    CoInspectorWindow.overrideSceneTools = evt.newValue;
                    UpdateVisualState();
                    if (CoInspectorWindow.overrideSceneTools)
                    {
                        EditorUtils.ShowSceneToolsMessage();
                    }
                    var userSaveData = CoInspectorWindow.FindSettingsObject();
                    if (userSaveData != null)
                    {
                        userSaveData.overrideSceneTools = CoInspectorWindow.overrideSceneTools;
                        EditorUtility.SetDirty(userSaveData);
                    }
                });
                sceneToolbarButton = this;
            }

            internal Texture2D UpdateVisualState()
            {
                value = CoInspectorWindow.overrideSceneTools;
                icon = value ? CustomGUIContents.SceneToolsButtonImageSelected : CustomGUIContents.SceneToolsButtonImage;
                return icon;
            }
        }
        private void OnEditorUpdate()
        {
            if (sceneToolbarButton == null)
            {
                return;
            }
            bool isMainCoInspectorAvailable = CoInspectorWindow.MainCoInspector != null;
            string activeTool = ToolManager.activeToolType.ToString();
            if (overrideSceneTools != CoInspectorWindow.overrideSceneTools && sceneToolbarButton != null)
            {
                ((SceneToolbarButton)sceneToolbarButton).UpdateVisualState();
                overrideSceneTools = CoInspectorWindow.overrideSceneTools;
            }
            if (isMainCoInspectorAvailable && !sceneToolbarButton.enabledSelf)
            {
                sceneToolbarButton.SetEnabled(true);
            }
            else if (!isMainCoInspectorAvailable && sceneToolbarButton.enabledSelf)
            {
                sceneToolbarButton.SetEnabled(false);
            }
            if ((activeTool == "UnityEditor.RectTool" && sceneToolbarButton.enabledSelf) || !CoInspectorWindow.softSelection)
            {
                sceneToolbarButton.SetEnabled(false);
            }
            if (activeTexture == null && sceneToolbarButton != null)
            {
                activeTexture = ((SceneToolbarButton)sceneToolbarButton).UpdateVisualState();
            }
        }
        ~SceneToolsOverlay()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        public static void DrawSceneViewWarning(string message)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;

            Handles.BeginGUI();

            GUIStyle style = new GUIStyle(EditorStyles.helpBox);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 11;
            style.normal.textColor = Color.white;
            style.padding = new RectOffset(10, 10, 5, 5);

            Vector2 size = style.CalcSize(new GUIContent(message));
            float padding = 10f;

            Rect rect = new Rect(
                (sceneView.position.width - size.x) * 0.5f,
                padding,
                size.x,
                size.y
            );
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Box(rect, "", EditorStyles.helpBox);
            GUI.color = Color.white;
            GUI.Label(rect, message, style);
            Handles.EndGUI();
        }
    }

}