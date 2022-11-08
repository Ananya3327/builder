// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2022 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Properties;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Settings;

namespace VRBuilder.Core
{
    /// <summary>
    /// Collection of <see cref="ISceneObject"/>s that can be locked and unlocked during a step.
    /// Additionally, checks if objects are automatically or manually unlocked.
    /// </summary>
    internal class LockableObjectsCollection
    {
        private List<LockablePropertyData> toUnlock;

        private List<Guid> tagsToUnlock;

        private Step.EntityData data;

        public List<ISceneObject> SceneObjects { get; set; } = new List<ISceneObject>();

        public LockableObjectsCollection(Step.EntityData entityData)
        {
            toUnlock = PropertyReflectionHelper.ExtractLockablePropertiesFromStep(entityData).ToList();
            tagsToUnlock = new List<Guid>();
            data = entityData;

            CreateSceneObjects();
        }

        private void CreateSceneObjects()
        {
            CleanProperties();

            foreach (LockablePropertyReference propertyReference in data.ToUnlock)
            {
                AddSceneObject(propertyReference.Target.Value);
            }

            foreach (LockablePropertyData propertyData in toUnlock)
            {
                AddSceneObject(propertyData.Property.SceneObject);
            }
        }

        public void AddSceneObject(ISceneObject sceneObject)
        {
            if (SceneObjects.Contains(sceneObject) == false)
            {
                SceneObjects.Add(sceneObject);
                SortSceneObjectList();
            }
        }

        private void SortSceneObjectList()
        {
            SceneObjects.Sort((obj1, obj2) => obj1.GameObject.ToString().CompareTo(obj2.GameObject.ToString()));
        }

        public void RemoveSceneObject(ISceneObject sceneObject)
        {
            if (SceneObjects.Remove(sceneObject))
            {
                data.ToUnlock = data.ToUnlock.Where(property =>
                {
                    if (property.GetProperty() == null)
                    {
                        return false;
                    }

                    return property.GetProperty().SceneObject != sceneObject;
                }).ToList();
            }
        }

        public bool IsInManualUnlockList(LockableProperty property)
        {
            foreach (LockablePropertyReference lockableProperty in data.ToUnlock)
            {
                if (property == lockableProperty.GetProperty())
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsUsedInAutoUnlock(ISceneObject sceneObject)
        {
            return toUnlock.Any(propertyData => propertyData.Property.SceneObject == sceneObject);
        }

        public bool IsInAutoUnlockList(LockableProperty property)
        {
            foreach (LockablePropertyData lockableProperty in toUnlock)
            {
                if (property == lockableProperty.Property)
                {
                    return true;
                }
            }

            return false;
        }

        public void Remove(LockableProperty property)
        {
            data.ToUnlock = data.ToUnlock.Where(reference => reference.GetProperty() != property).ToList();
        }

        public void Add(LockableProperty property)
        {
            data.ToUnlock = data.ToUnlock.Union(new [] {new LockablePropertyReference(property), }).ToList();
        }

        public void AddTag(Guid tag)
        {
            UnityEngine.Debug.Log($"Added tag [{SceneObjectTags.Instance.GetLabel(tag)}]");
            data.TagsToUnlock = data.TagsToUnlock.Union(new [] {tag});
        }

        public void RemoveTag(Guid tag)
        {
            UnityEngine.Debug.Log($"Removed tag [{SceneObjectTags.Instance.GetLabel(tag)}]");
            data.TagsToUnlock = data.TagsToUnlock.Where(element => element != tag); 
        }

        private void CleanProperties()
        {
            data.ToUnlock = data.ToUnlock.Where(reference => reference.Target.IsEmpty() == false).ToList();
        }
    }
}
