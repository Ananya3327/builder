// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2022 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.Setup;

namespace VRBuilder.Editor.UI.Wizard
{
    /// <summary>
    /// Wizard page which handles the process scene setup.
    /// </summary>
    internal class ProcessSceneSetupPage : WizardPage
    {
        private const int MaxProcessNameLength = 40;
        private const int MinHeightOfInfoText = 30;

        [SerializeField]
        private bool useCurrentScene = true;

        [SerializeField]
        private bool createNewScene = false;

        [SerializeField]
        private bool createNewProcess = true;

        [SerializeField]
        private string processName = "My VR Process";

        [SerializeField]
        private string lastCreatedProcess = null;

        private int selectedIndex = 0;
        private ISceneSetupConfiguration[] configurations;

        private readonly GUIContent infoContent;
        private readonly GUIContent warningContent;

        public ProcessSceneSetupPage() : base("Setup Process")
        {
            infoContent = EditorGUIUtility.IconContent("console.infoicon.inactive.sml");
            warningContent = EditorGUIUtility.IconContent("console.warnicon.sml");

            configurations = ReflectionUtils.GetConcreteImplementationsOf<ISceneSetupConfiguration>()
                .Select(type => ReflectionUtils.CreateInstanceOfType(type))
                .Cast<ISceneSetupConfiguration>()
                .ToArray();
        }

        /// <inheritdoc />
        public override void Draw(Rect window)
        {
            GUILayout.BeginArea(window);

            GUILayout.Label("Setup Process", BuilderEditorStyles.Title);

            EditorGUI.BeginDisabledGroup(createNewProcess == false);
            GUILayout.Label("Name of your VR Process", BuilderEditorStyles.Header);
            processName = BuilderGUILayout.DrawTextField(processName, MaxProcessNameLength, GUILayout.Width(window.width * 0.7f));
            GUI.enabled = true;

            if (ProcessAssetUtils.CanCreate(processName, out string errorMessage) == false && lastCreatedProcess != processName)
            {
                GUIContent processWarningContent = warningContent;
                processWarningContent.text = errorMessage;
                if (createNewProcess)
                {
                    GUILayout.Label(processWarningContent, BuilderEditorStyles.Label, GUILayout.MinHeight(MinHeightOfInfoText));
                }
                CanProceed = false;
            }
            else
            {
                GUILayout.Space(MinHeightOfInfoText + BuilderEditorStyles.BaseIndent);
                CanProceed = true;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.BeginHorizontal();
            GUILayout.Space(BuilderEditorStyles.Indent);
            GUILayout.BeginVertical();
            EditorGUI.BeginDisabledGroup (createNewProcess == false);
            bool isUseCurrentScene = GUILayout.Toggle(useCurrentScene, "Take my current scene", BuilderEditorStyles.RadioButton);
            if (useCurrentScene == false && isUseCurrentScene)
            {
                useCurrentScene = true;
                createNewScene = false;
            }

            bool isCreateNewScene = GUILayout.Toggle(createNewScene, "Create a new scene", BuilderEditorStyles.RadioButton);
            if (createNewScene == false && isCreateNewScene)
            {
                createNewScene = true;
                useCurrentScene = false;
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (createNewScene && createNewProcess)
            {
                GUIContent helpContent;
                string sceneInfoText = "Scene will have the same name as the process.";
                if (SceneSetupUtils.SceneExists(processName))
                {
                    sceneInfoText += " Scene already exists";
                    CanProceed = false;
                    helpContent = warningContent;
                }
                else
                {
                    helpContent = infoContent;
                }

                helpContent.text = sceneInfoText;
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(BuilderEditorStyles.Indent);
                    EditorGUILayout.LabelField(helpContent, BuilderEditorStyles.Label, GUILayout.MinHeight(MinHeightOfInfoText));
                }
                GUILayout.EndHorizontal();
            }

            createNewProcess = GUILayout.Toggle(createNewProcess, "Create new process", BuilderEditorStyles.Toggle);
            CanProceed |= createNewProcess == false;

            selectedIndex = EditorGUILayout.Popup(selectedIndex, configurations.Select(config => config.Name).ToArray());

            GUILayout.EndArea();
        }

        /// <inheritdoc />
        public override void Apply()
        {
            if (processName == lastCreatedProcess)
            {
                return;
            }

            if (useCurrentScene == false)
            {
                SceneSetupUtils.CreateNewScene(processName);
            }

            ProcessSceneSetup.Run(configurations[selectedIndex]);

            if(createNewProcess)
            {
                SceneSetupUtils.SetupProcess(processName);
            }

            lastCreatedProcess = processName;
            EditorWindow.FocusWindowIfItsOpen<WizardWindow>();
        }
    }
}
