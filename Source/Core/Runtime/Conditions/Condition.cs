// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2024 MindPort GmbH

using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using VRBuilder.Core.RestrictiveEnvironment;
using VRBuilder.Core.Utils.Logging;
using VRBuilder.Unity;

namespace VRBuilder.Core.Conditions
{
    /// <summary>
    /// An implementation of <see cref="ICondition"/>. Use it as the base class for your custom conditions.
    /// </summary>
    [DataContract(IsReference = true)]
    public abstract class Condition<TData> : CompletableEntity<TData>, ICondition, ILockablePropertiesProvider where TData : class, IConditionData, new()
    {
        protected Condition()
        {
            if (LifeCycleLoggingConfig.Instance.LogConditions)
            {
                LifeCycle.StageChanged += (sender, args) =>
                {
                    Debug.Log($"{ConsoleUtils.GetTabs(2)}<b>{GetType().Name}</b> <i>'{Data.Name}'</i> is <b>{LifeCycle.Stage}</b>.\n");
                };
            }
        }

        /// <inheritdoc />
        IConditionData IDataOwner<IConditionData>.Data
        {
            get
            {
                return Data;
            }
        }

        /// <inheritdoc />
        public virtual ICondition Clone()
        {
            return MemberwiseClone() as ICondition;
        }

        /// <inheritdoc />
        public virtual IEnumerable<LockablePropertyData> GetLockableProperties()
        {
            return PropertyReflectionHelper.ExtractLockablePropertiesFromCondition(Data);
        }
    }
}
