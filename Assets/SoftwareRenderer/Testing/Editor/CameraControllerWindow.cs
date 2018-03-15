using UnityEditor;
using UnityEngine;

public sealed class CameraControllerWindow : EditorWindow
{
    private Camera camera;

    [MenuItem("Window/Camera Controller")]
    public static void ShowWindow()
    {
        GetWindow<CameraControllerWindow>("Camera Controller");
    }

    private void OnGUI()
    {
        camera = EditorGUILayout.ObjectField("Camera", camera, typeof(Camera), true) as Camera;
	}

    private void Update()
    {
        if (camera == null || SceneView.lastActiveSceneView == null)
            return;

        var sceneView = SceneView.lastActiveSceneView.camera.transform;
        camera.transform.SetPositionAndRotation(sceneView.position, sceneView.rotation);
    }
}