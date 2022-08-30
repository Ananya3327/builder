using UnityEngine;
using VRBuilder.Editor.DemoScene;

namespace VRBuilder.Editor.Tutorials.HardwareSetup
{
    //[CreateAssetMenu(fileName = "Demo Scene Tutorial States", menuName = "Tutorial Objects/Demo Scene States", order = 1)]
    public class DemoSceneTutorialStatesScriptableObject : ScriptableObject
    {
        public void OpenDemoScene()
        {
            DemoSceneLoader.LoadDemoScene();
            Debug.Log("Loading demo scene");
        }
    }
}