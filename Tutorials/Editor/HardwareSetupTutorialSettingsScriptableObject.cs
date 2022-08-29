using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_XR_MANAGEMENT
using UnityEditor.XR.Management;
#endif

#if OPEN_XR
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

namespace VRBuilder.Editor.Tutorials.HardwareSetup
{
    //[CreateAssetMenu(fileName = "Hardware Setup Tutorial Settings", menuName = "Tutorial Objects/Hardware Setup Settings", order = 1)]
    public class HardwareSetupTutorialSettingsScriptableObject : ScriptableObject
    {
        public bool IsXRManagementInstalled()
        {
#if UNITY_XR_MANAGEMENT
            return true;
#else
            return false;
#endif
        }
        public bool IsOpenXREnabled()
        {
#if UNITY_XR_MANAGEMENT
            return XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(UnityEditor.BuildTargetGroup.Standalone).Manager.activeLoaders != null &&
            XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(UnityEditor.BuildTargetGroup.Standalone).Manager.activeLoaders.Any(l => l.GetType().Name == "OpenXRLoader");
#else
            return false;
#endif
        }

        public bool IsOculusTouchControllerProfileSelected()
        {
#if OPEN_XR            
            OpenXRFeature controllerProfile = OpenXRSettings.GetSettingsForBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup).GetFeature<OculusTouchControllerProfile>();            
            return controllerProfile != null && controllerProfile.enabled;
#else
            return false;
#endif
        }

        public bool IsOpenXRWithOculusTouch()
        {
            return IsOpenXREnabled() && IsOculusTouchControllerProfileSelected();
        }
    }
}
