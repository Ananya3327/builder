using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace VRBuilder.Core.SceneObjects
{
    [DataContract(IsReference = true)]
    public abstract class MultipleSceneReference<T> : ProcessSceneReference<T> where T : class
    {
        private IEnumerable<T> cachedValue;

        /// <summary>
        /// The referenced values.
        /// </summary>
        public IEnumerable<T> Values
        {
            get
            {
                cachedValue = DetermineValue(cachedValue);
                return cachedValue;
            }
        }

        protected abstract IEnumerable<T> DetermineValue(IEnumerable<T> cachedValue);

        /// <inheritdoc/>
        internal override bool AllowMultipleValues => true;


        public static implicit operator List<T>(MultipleSceneReference<T> reference)
        {
            return reference.Values.ToList();
        }
    }
}
