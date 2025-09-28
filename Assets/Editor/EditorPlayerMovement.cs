using UnityEditor;
using UnityEngine;

public class EditorPlayerMovement : EditorWindow
{
    private GameObject playerObject;
    private float moveSpeed = 5f;

    [MenuItem("Tools/Editor Player Movement")]
    public static void ShowWindow()
    {
        GetWindow<EditorPlayerMovement>("Editor Player Movement");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Editor Player Movement Controls");
        playerObject = (GameObject)EditorGUILayout.ObjectField("Player Object", playerObject, typeof(GameObject), true);
        moveSpeed = EditorGUILayout.FloatField("Move Speed", moveSpeed);

        if (playerObject == null)
        {
            EditorGUILayout.HelpBox("Assign a GameObject to control.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Move Up (W)"))
        {
            MovePlayer(Vector3.forward);
        }
        if (GUILayout.Button("Move Down (S)"))
        {
            MovePlayer(Vector3.back);
        }
        if (GUILayout.Button("Move Left (A)"))
        {
            MovePlayer(Vector3.left);
        }
        if (GUILayout.Button("Move Right (D)"))
        {
            MovePlayer(Vector3.right);
        }
    }

    private void MovePlayer(Vector3 direction)
    {
        if (playerObject != null)
        {
            playerObject.transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}