using System.Collections;
using System.Linq;
using UnityEngine;

namespace VRBuilder.Core.EntityOwners.ParallelEntityCollection
{
    /// <summary>
    /// A process over a collection of entities which aborts them at the same time, in parallel.
    /// </summary>
    internal class ParallelAbortingProcess<TCollectionData> : StageProcess<TCollectionData> where TCollectionData : class, IEntityCollectionData
    {
        public ParallelAbortingProcess(TCollectionData data) : base(data)
        {
        }

        public override void End()
        {
        }

        public override void FastForward()
        {
        }

        /// <inheritdoc />
        public override void Start()
        {
            foreach (IEntity child in Data.GetChildren().Where(child => child.LifeCycle.Stage != Stage.Inactive))
            {
                Debug.Log($"Aborting ´{child}");
                child.LifeCycle.Abort();
            }
        }

        public override IEnumerator Update()
        {
            while (Data.GetChildren().Any(child => child.LifeCycle.Stage != Stage.Inactive))
            {
                yield return null;
            }
        }
    }
}
