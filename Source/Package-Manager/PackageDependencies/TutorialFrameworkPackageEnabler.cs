namespace VRBuilder.Editor.PackageManager
{
    /// <summary>
    /// Adds Unity's Tutorial Framework package as a dependency.
    /// </summary>
    public class TutorialFrameworkPackageEnabler : Dependency
    {
        /// <inheritdoc/>
        public override string Package { get; } = "com.unity.learn.iet-framework@2.2.2";

        /// <inheritdoc/>
        public override int Priority { get; } = 10;
    }
}
