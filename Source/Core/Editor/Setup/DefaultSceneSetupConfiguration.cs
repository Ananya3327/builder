using System.Collections.Generic;

namespace VRBuilder.Editor.Setup
{
    public class DefaultSceneSetupConfiguration : ISceneSetupConfiguration
    {
        public int Priority => 256;

        public string Name => "Single user (default rig)";

        public IEnumerable<string> GetSetupNames()
        {
            return new string[]
            {
                "VRBuilder.Editor.RuntimeConfigurationSetup",
                "VRBuilder.Editor.BasicInteraction.RigSetup.DefaultRigSceneSetup",
                "VRBuilder.Editor.UX.ProcessControllerSceneSetup",
                "VRBuilder.Editor.XRInteraction.XRInteractionSceneSetup"
            };
        }
    }
}
