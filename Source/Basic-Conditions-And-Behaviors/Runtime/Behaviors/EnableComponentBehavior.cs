using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Scripting;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Utils;

namespace VRBuilder.Core.Behaviors
{
    /// <summary>
    /// Enables gameObject of target ISceneObject.
    /// </summary>
    [DataContract(IsReference = true)]
    [HelpLink("https://www.mindport.co/vr-builder/manual/default-behaviors/enable-object")]
    public class SetComponentEnabledBehavior : Behavior<SetComponentEnabledBehavior.EntityData>
    {
        /// <summary>
        /// "Enable game object" behavior's data.
        /// </summary>
        [DisplayName("Set Component Enabled")]
        [DataContract(IsReference = true)]
        public class EntityData : IBehaviorData
        {
            /// <summary>
            /// The object to enable.
            /// </summary>
            [DataMember]
            [HideInProcessInspector]
            public SceneObjectReference Target { get; set; }

            [DataMember]
            [HideInProcessInspector]
            public string ComponentType { get; set; }

            [DataMember]
            [HideInProcessInspector]
            public bool SetEnabled { get; set; }

            /// <inheritdoc />
            public Metadata Metadata { get; set; }

            [DataMember]
            [HideInProcessInspector]
            public bool RevertOnDeactivation { get; set; }

            /// <inheritdoc />
            public string Name { get; set; }
        }

        private class ActivatingProcess : InstantProcess<EntityData>
        {
            public ActivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                IEnumerable<Component> components = Data.Target.Value.GameObject.GetComponents<Component>().Where(c => c.GetType().Name == Data.ComponentType);

                foreach(Component component in components)
                {
                    Type componentType = component.GetType();

                    if (componentType.GetProperty("enabled") != null)
                    {
                        componentType.GetProperty("enabled").SetValue(component, Data.SetEnabled, null);
                    }
                }
            }
        }

        private class DeactivatingProcess : InstantProcess<EntityData>
        {
            public DeactivatingProcess(EntityData data) : base(data)
            {
            }

            /// <inheritdoc />
            public override void Start()
            {
                if (Data.RevertOnDeactivation)
                {
                    IEnumerable<Component> components = Data.Target.Value.GameObject.GetComponents<Component>().Where(c => c.GetType().Name == Data.ComponentType);

                    foreach (Component component in components)
                    {
                        Type componentType = component.GetType();

                        if (componentType.GetProperty("enabled") != null)
                        {
                            componentType.GetProperty("enabled").SetValue(component, !Data.SetEnabled, null);
                        }
                    }
                }
            }
        }

        [JsonConstructor, Preserve]
        public SetComponentEnabledBehavior() : this("", "", false, false, "")
        {
        }

        public SetComponentEnabledBehavior(bool setEnabled, string name = "Set Component Enabled") : this("", "", setEnabled, false, name)
        {
        }

        /// <param name="targetObject">Object to enable.</param>
        public SetComponentEnabledBehavior(ISceneObject targetObject, string componentType, bool setEnabled, bool revertOnDeactivate, string name = "Set Component Enabled") : this(ProcessReferenceUtils.GetNameFrom(targetObject), componentType, setEnabled, revertOnDeactivate, name)
        {
        }

        /// <param name="targetObject">Name of the object to enable.</param>
        public SetComponentEnabledBehavior(string targetObject, string componentType, bool setEnabled, bool revertOnDeactivate, string name = "Set Component Enabled")
        {
            Data.Target = new SceneObjectReference(targetObject);
            Data.ComponentType = componentType;
            Data.SetEnabled = setEnabled;
            Data.RevertOnDeactivation = revertOnDeactivate;
            Data.Name = name;
        }

        /// <inheritdoc />
        public override IStageProcess GetActivatingProcess()
        {
            return new ActivatingProcess(Data);
        }

        public override IStageProcess GetDeactivatingProcess()
        {
            return new DeactivatingProcess(Data);
        }
    }
}
