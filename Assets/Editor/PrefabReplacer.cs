using UnityEngine;
using UnityEditor;

public class PrefabReplacer : ScriptableWizard
{
    [Header("Settings")]
    public GameObject newPrefab;
    public bool keepOriginalNames = true;

    [Header("Transform Adjustments")]
    [Tooltip("Adds this rotation to the original rotation (in degrees).")]
    public Vector3 additionalRotation = Vector3.zero;

    [Tooltip("Multiplies the original scale by this factor. Set to (1,1,1) to keep original size.")]
    public Vector3 scaleMultiplier = Vector3.one;

    [MenuItem("Tools/Replace Selected Objects")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<PrefabReplacer>("Replace Selection", "Replace");
    }

    void OnWizardCreate()
    {
        if (newPrefab == null)
        {
            Debug.LogError("Please assign a prefab first!");
            return;
        }

        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            // Skip assets in project folder
            if (selectedObject.scene.rootCount == 0) continue;

            // Instantiate the new prefab keeping the link
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);

            // 1. Position: Copy exactly
            newObject.transform.position = selectedObject.transform.position;

            // 2. Rotation: Original Rotation combined with Additional Rotation
            // We multiply the quaternions to combine rotations.
            newObject.transform.rotation = selectedObject.transform.rotation * Quaternion.Euler(additionalRotation);

            // 3. Scale: Original Scale multiplied by the Factor (component-wise)
            newObject.transform.localScale = Vector3.Scale(selectedObject.transform.localScale, scaleMultiplier);

            // Restore hierarchy
            newObject.transform.parent = selectedObject.transform.parent;
            newObject.transform.SetSiblingIndex(selectedObject.transform.GetSiblingIndex());

            // Restore name
            if (keepOriginalNames)
            {
                newObject.name = selectedObject.name;
            }

            // Undo system registration
            Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefab");
            Undo.DestroyObjectImmediate(selectedObject);
        }
    }
}