// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;

namespace VRBuilder.Editor.UI
{
    /// <summary>
    /// This class adds names to newly added entities.
    /// </summary>
    [CustomEditor(typeof(ProcessSceneObject))]
    [CanEditMultipleObjects]
    internal class SceneObjectEditor : UnityEditor.Editor
    {
        int selectedTagIndex = 0;
        string newTag = "";
        private static EditorIcon deleteIcon;

        private void OnEnable()
        {
            if (deleteIcon == null)
            {
                deleteIcon = new EditorIcon("icon_delete");
            }
        }

        [MenuItem("CONTEXT/ProcessSceneObject/Remove Process Properties", false)]
        private static void RemoveProcessProperties()
        {
            Component[] processProperties = Selection.activeGameObject.GetComponents(typeof(ProcessSceneObjectProperty));
            ISceneObject sceneObject = Selection.activeGameObject.GetComponent(typeof(ISceneObject)) as ISceneObject;

            foreach (Component processProperty in processProperties)
            {
                sceneObject.RemoveProcessProperty(processProperty, true);
            }
        }

        [MenuItem("CONTEXT/ProcessSceneObject/Remove Process Properties", true)]
        private static bool ValidateRemoveProcessProperties()
        {
            return Selection.activeGameObject.GetComponents(typeof(ProcessSceneObjectProperty)) != null;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);

            // It can be a MonoBehaviour or a ScriptableObject
            var monoScript = (target as MonoBehaviour) != null
                ? MonoScript.FromMonoBehaviour((MonoBehaviour)target)
                : MonoScript.FromScriptableObject((ScriptableObject)target);

            EditorGUILayout.ObjectField("Script", monoScript, GetType(), false);

            var monoScript2 = MonoScript.FromScriptableObject((ScriptableObject)this);

            EditorGUILayout.ObjectField("EditorScript", monoScript2, GetType(), false);

            EditorGUI.EndDisabledGroup();

            DrawUniqueId();
            DrawTags();
        }

        private void DrawUniqueId()
        {
            if (targets.Count() == 1)
            {
                EditorGUILayout.LabelField("Unique Id:");
                EditorGUI.BeginDisabledGroup(true);
                ISceneObject sceneObject = targets.First(t => t is ISceneObject) as ISceneObject;
                EditorGUILayout.LabelField($"{sceneObject.Guid}");
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.LabelField("[Multiple objects selected]");
            }
        }

        private void DrawTags()
        {
            List<ITagContainer> tagContainers = targets.Where(t => t is ITagContainer).Cast<ITagContainer>().ToList();

            List<SceneObjectTags.Tag> availableTags = new List<SceneObjectTags.Tag>(SceneObjectTags.Instance.Tags);

            EditorGUILayout.LabelField("Scene object tags:");

            AddNewTag(tagContainers);
            AddExistingTags(tagContainers, availableTags);
        }

        private void AddNewTag(List<ITagContainer> tagContainers)
        {
            // Add and create new tag
            EditorGUILayout.BeginHorizontal();

            newTag = EditorGUILayout.TextField(newTag);

            EditorGUI.BeginDisabledGroup(SceneObjectTags.Instance.CanCreateTag(newTag) == false);

            if (GUILayout.Button("Add New", GUILayout.Width(128)))
            {
                Guid guid = Guid.NewGuid();
                Undo.RecordObject(SceneObjectTags.Instance, "Created tag");
                SceneObjectTags.Instance.CreateTag(newTag, guid);
                EditorUtility.SetDirty(SceneObjectTags.Instance);

                foreach (ITagContainer container in tagContainers)
                {
                    Undo.RecordObject((UnityEngine.Object)container, "Added tag");
                    container.AddTag(guid);
                    PrefabUtility.RecordPrefabInstancePropertyModifications((UnityEngine.Object)container);
                }

                GUI.FocusControl("");
                newTag = "";
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void AddExistingTags(List<ITagContainer> tagContainers, List<SceneObjectTags.Tag> availableTags)
        {
            foreach (SceneObjectTags.Tag tag in SceneObjectTags.Instance.Tags)
            {
                if (tagContainers.All(c => c.HasTag(tag.Guid)))
                {
                    availableTags.RemoveAll(t => t.Guid == tag.Guid);
                }
            }

            if (selectedTagIndex >= availableTags.Count() && availableTags.Count() > 0)
            {
                selectedTagIndex = availableTags.Count() - 1;
            }

            // Add existing tag
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(availableTags.Count() == 0);
            selectedTagIndex = EditorGUILayout.Popup(selectedTagIndex, availableTags.Select(tag => tag.Label).ToArray());

            if (GUILayout.Button("Add Tag", GUILayout.Width(128)))
            {
                List<ITagContainer> processedContainers = tagContainers.Where(container => container.HasTag(availableTags[selectedTagIndex].Guid) == false).ToList();

                foreach (ITagContainer container in processedContainers)
                {
                    Undo.RecordObject((UnityEngine.Object)container, "Added tag");
                    container.AddTag(availableTags[selectedTagIndex].Guid);
                    PrefabUtility.RecordPrefabInstancePropertyModifications((UnityEngine.Object)container);
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            List<SceneObjectTags.Tag> usedTags = new List<SceneObjectTags.Tag>(SceneObjectTags.Instance.Tags);

            foreach (SceneObjectTags.Tag tag in SceneObjectTags.Instance.Tags)
            {
                if (tagContainers.All(c => c.HasTag(tag.Guid) == false))
                {
                    usedTags.RemoveAll(t => t.Guid == tag.Guid);
                }
            }

            // List tags
            foreach (Guid guid in usedTags.Select(t => t.Guid))
            {
                if (SceneObjectTags.Instance.TagExists(guid) == false)
                {
                    tagContainers.ForEach(c => c.RemoveTag(guid));
                    break;
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(deleteIcon.Texture, GUILayout.Height(EditorDrawingHelper.SingleLineHeight)))
                {
                    List<ITagContainer> processedContainers = tagContainers.Where(container => container.HasTag(guid)).ToList();

                    foreach (ITagContainer container in processedContainers)
                    {
                        Undo.RecordObject((UnityEngine.Object)container, "Removed tag");
                        container.RemoveTag(guid);
                        PrefabUtility.RecordPrefabInstancePropertyModifications((UnityEngine.Object)container);
                    }

                    break;
                }

                string label = SceneObjectTags.Instance.GetLabel(guid);
                if (tagContainers.Any(container => container.HasTag(guid) == false))
                {
                    label = $"<i>{label}</i>";
                }

                EditorGUILayout.LabelField(label, BuilderEditorStyles.Label);

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
