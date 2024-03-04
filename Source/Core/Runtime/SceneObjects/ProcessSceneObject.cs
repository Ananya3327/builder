// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Exceptions;
using VRBuilder.Core.Properties;
using VRBuilder.Core.Utils.Logging;

namespace VRBuilder.Core.SceneObjects
{
    /// <inheritdoc cref="ISceneObject"/>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class ProcessSceneObject : MonoBehaviour, ISceneObject
    {
        public event EventHandler<LockStateChangedEventArgs> Locked;
        public event EventHandler<LockStateChangedEventArgs> Unlocked;

        [Obsolete("This event is no longer used and will be removed in the next major release.")]
        public event EventHandler<SceneObjectNameChanged> UniqueNameChanged;

        public GameObject GameObject => gameObject;

        [SerializeField]
        [Tooltip("Unique name which identifies an object in scene, can be null or empty, but has to be unique in the scene.")]
        [Obsolete("This exists for backwards compatibility. Use the uniqueId field to store the object's unique identifier.")]
        protected string uniqueName = null;

        [SerializeField]
        protected string uniqueId = null;

        /// <inheritdoc />
        public string UniqueName => Guid.ToString();

        private List<IStepData> unlockers = new List<IStepData>();

        /// <inheritdoc />
        public Guid Guid
        {
            get
            {
                if (uniqueId == null || Guid.TryParse(uniqueId, out Guid guid) == false)
                {
                    uniqueId = Guid.NewGuid().ToString();
                }

                return Guid.Parse(uniqueId);
            }
        }

        /// <summary>
        /// Properties associated with this scene object.
        /// </summary>
        public ICollection<ISceneObjectProperty> Properties
        {
            get { return GetComponents<ISceneObjectProperty>(); }
        }

        /// <inheritdoc />
        public bool IsLocked { get; private set; }

        [SerializeField]
        protected List<string> tags = new List<string>();

        /// <inheritdoc />
        public IEnumerable<Guid> Tags => tags.Select(tag => Guid.Parse(tag));

        /// <inheritdoc />
        public event EventHandler<TaggableObjectEventArgs> TagAdded;

        /// <inheritdoc />
        public event EventHandler<TaggableObjectEventArgs> TagRemoved;

        protected void Awake()
        {
            Init();

            var processSceneObjects = GetComponentsInChildren<ProcessSceneObject>(true);
            for (int i = 0; i < processSceneObjects.Length; i++)
            {
                if (!processSceneObjects[i].isActiveAndEnabled)
                {
                    processSceneObjects[i].Init();
                }
            }
        }

        /// <inheritdoc />
        public void SetUniqueId(Guid guid)
        {
            uniqueId = guid.ToString();
        }

        private void Reset()
        {
            Init();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Overriding the Reset context menu entry in order to unregister the object before invalidating the unique id.
        /// </summary>
        [ContextMenu("Reset", false, 0)]
        protected void ResetContextMenu()
        {
            if (RuntimeConfigurator.Exists)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(this);
            }

            uniqueId = null;
            tags = new List<string>();
            Init();

            UnityEditor.EditorUtility.SetDirty(this);
        }

        [ContextMenu("Reset Unique ID")]
        protected void MakeUnique()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Reset Unique Id", "Warning! This will change the object's unique id.\n" +
                "All reference to this object in the Process Editor will become invalid.\n" +
                "Proceed?", "Yes", "No"))
            {
                if (RuntimeConfigurator.Exists)
                {
                    RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(this);

                    uniqueId = null;
                    Init();

                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }
#endif
        protected void Init()
        {
            if (RuntimeConfigurator.Exists == false)
            {
                Debug.LogWarning($"Not registering {gameObject.name} due to runtime configurator not present.");
                return;
            }

#if UNITY_EDITOR
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene))
            {
                Debug.Log($"Not registering {gameObject.name} due because it is in a preview scene.");
                return;
            }
#endif

            RuntimeConfigurator.Configuration.SceneObjectRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (RuntimeConfigurator.Exists)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(this);
            }
        }

        /// <inheritdoc />
        public bool CheckHasProperty<T>() where T : ISceneObjectProperty
        {
            return CheckHasProperty(typeof(T));
        }

        /// <inheritdoc />
        public bool CheckHasProperty(Type type)
        {
            return FindProperty(type) != null;
        }

        /// <inheritdoc />
        public T GetProperty<T>() where T : ISceneObjectProperty
        {
            ISceneObjectProperty property = FindProperty(typeof(T));
            if (property == null)
            {
                throw new PropertyNotFoundException(this, typeof(T));
            }

            return (T)property;
        }

        /// <inheritdoc />
        public void ValidateProperties(IEnumerable<Type> properties)
        {
            bool hasFailed = false;
            foreach (Type propertyType in properties)
            {
                // ReSharper disable once InvertIf
                if (CheckHasProperty(propertyType) == false)
                {
                    Debug.LogErrorFormat("Property of type '{0}' is not attached to SceneObject '{1}'", propertyType.Name, gameObject.name);
                    hasFailed = true;
                }
            }

            if (hasFailed)
            {
                throw new PropertyNotFoundException("One or more SceneObjectProperties could not be found, check your log entries for more information.");
            }
        }

        /// <inheritdoc />
        public void SetLocked(bool lockState)
        {
            if (IsLocked == lockState)
            {
                return;
            }

            IsLocked = lockState;

            if (IsLocked)
            {
                if (Locked != null)
                {
                    Locked.Invoke(this, new LockStateChangedEventArgs(IsLocked));
                }
            }
            else
            {
                if (Unlocked != null)
                {
                    Unlocked.Invoke(this, new LockStateChangedEventArgs(IsLocked));
                }
            }
        }

        /// <inheritdoc/>
        public virtual void RequestLocked(bool lockState, IStepData stepData)
        {
            if (lockState == false && unlockers.Contains(stepData) == false)
            {
                unlockers.Add(stepData);
            }

            if (lockState && unlockers.Contains(stepData))
            {
                unlockers.Remove(stepData);
            }

            bool canLock = unlockers.Count == 0;

            if (LifeCycleLoggingConfig.Instance.LogLockState)
            {
                string lockType = lockState ? "lock" : "unlock";
                string requester = stepData == null ? "NULL" : stepData.Name;
                StringBuilder unlockerList = new StringBuilder();

                foreach (IStepData unlocker in unlockers)
                {
                    unlockerList.Append($"\n<i>{unlocker.Name}</i>");
                }

                string listUnlockers = unlockers.Count == 0 ? "" : $"\nSteps keeping this object unlocked:{unlockerList}";

                Debug.Log($"<i>{this.GetType().Name}</i> on <i>{gameObject.name}</i> received a <b>{lockType}</b> request from <i>{requester}</i>." +
                    $"\nCurrent lock state: <b>{IsLocked}</b>. Future lock state: <b>{lockState && canLock}</b>{listUnlockers}");
            }

            SetLocked(lockState && canLock);
        }

        /// <inheritdoc/>
        public bool RemoveUnlocker(IStepData data)
        {
            return unlockers.Remove(data);
        }

        /// <summary>
        /// Tries to find property which is assignable to given type, this method
        /// will return null if none is found.
        /// </summary>
        private ISceneObjectProperty FindProperty(Type type)
        {
            return GetComponent(type) as ISceneObjectProperty;
        }

        [Obsolete("Use ChangeUniqueId instead.")]
        public void ChangeUniqueName(string newName = "")
        {
            Guid guid = Guid.Empty;
            Guid.TryParse(newName, out guid);
            ChangeUniqueId(guid);
        }

        /// <inheritdoc />
        public void ChangeUniqueId(Guid newGuid)
        {
            if (RuntimeConfigurator.Exists)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Unregister(this);
            }

            if (newGuid == Guid.Empty)
            {
                newGuid = Guid.NewGuid();
            }

            uniqueId = newGuid.ToString();

            if (RuntimeConfigurator.Exists)
            {
                RuntimeConfigurator.Configuration.SceneObjectRegistry.Register(this);
            }
        }

        /// <inheritdoc />
        public void AddTag(Guid tag)
        {
            if (Tags.Contains(tag) == false)
            {
                tags.Add(tag.ToString());
                TagAdded?.Invoke(this, new TaggableObjectEventArgs(tag));
            }
        }

        /// <inheritdoc />
        public bool HasTag(Guid tag)
        {
            return Tags.Contains(tag);
        }

        /// <inheritdoc />
        public bool RemoveTag(Guid tag)
        {
            if (tags.Remove(tag.ToString()))
            {
                TagRemoved?.Invoke(this, new TaggableObjectEventArgs(tag));
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return GameObject.name;
        }
    }
}
