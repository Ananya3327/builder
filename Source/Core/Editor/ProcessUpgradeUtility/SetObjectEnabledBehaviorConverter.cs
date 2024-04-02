using VRBuilder.Core.Behaviors;

namespace VRBuilder.Editor.ProcessUpdater
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class SetObjectEnabledBehaviorConverter : EntityConverter<SetObjectsWithTagEnabledBehavior, SetObjectsEnabledBehavior>
    {
        protected override SetObjectsEnabledBehavior PerformConversion(SetObjectsWithTagEnabledBehavior oldBehavior)
        {
#pragma warning restore CS0618 // Type or member is obsolete

            SetObjectsEnabledBehavior newBehavior = new SetObjectsEnabledBehavior();
            newBehavior.Data.SetEnabled = oldBehavior.Data.SetEnabled;
            newBehavior.Data.TargetObjects = oldBehavior.Data.TargetObjects;
            newBehavior.Data.RevertOnDeactivation = oldBehavior.Data.RevertOnDeactivation;

            return newBehavior;
        }
    }
}
