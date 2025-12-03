using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PortalTriggerController : MonoBehaviour
{
    public enum DetectionMode
    {
        SpecificObject,
        Tag,
        Name
    }

    [Header("Trigger Settings")]
    [Tooltip("Choose how the target should be identified.")]
    public DetectionMode detectionMode;

    [Header("Target Criteria")]
    [Tooltip("Drag the specific GameObject here if Mode is 'SpecificObject'.")]
    public Collider targetObjectCollider;

    [Tooltip("Enter the Tag here if Mode is 'Tag'.")]
    public string targetTag;

    [Tooltip("Enter the Name here if Mode is 'Name'.")]
    public string targetName;

    [Header("Layer Change Settings")]
    [Tooltip("The EXACT name of the layer you want to assign (Case Sensitive!).")]
    public string newLayerName = "Default";

    [Tooltip("The specific object to change layers. Ignored if 'Affect Colliding Object' is true.")]
    public GameObject layerChangeTarget;

    [Header("Response")]
    [Tooltip("Actions to execute when the trigger is activated.")]
    public UnityEvent onTriggerEnter;

    [Header("Transition Effect")]
    [Tooltip("Time in seconds to wait between changing the layer of each child object.")]
    [Range(0f, 0.5f)]
    public float transitionDelay = 0.05f;

    [Tooltip("If true, child objects will change layers in random order. If false, they change top-down.")]
    public bool randomizeOrder = false;


    private void OnTriggerEnter(Collider other)
    {
        if (CheckTarget(other.gameObject))
        {
            Debug.Log($"Trigger activated by: {other.name}");

            if (layerChangeTarget != null)
            {
                StartCoroutine(ChangeLayerRoutine(layerChangeTarget, newLayerName));
            }

            onTriggerEnter.Invoke();
        }
    }

    private bool CheckTarget(GameObject incomingObject)
    {
        switch (detectionMode)
        {
            case DetectionMode.SpecificObject:
                return incomingObject == targetObjectCollider.gameObject;

            case DetectionMode.Tag:
                return incomingObject.CompareTag(targetTag);

            case DetectionMode.Name:
                return incomingObject.name == targetName;

            default:
                return false;
        }
    }

    private void ChangeLayerRecursively(GameObject obj, string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);

        if (layerIndex == -1)
        {
            Debug.LogError($"TriggerController: Layer '{layerName}' does not exist. Check your spelling!");
            return;
        }

        obj.layer = layerIndex;

        foreach (Transform child in obj.transform)
        {
            ChangeLayerRecursively(child.gameObject, layerName);
        }
    }

    private IEnumerator ChangeLayerRoutine(GameObject rootObj, string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);

        if (layerIndex == -1)
        {
            Debug.LogError($"TriggerController: Layer '{layerName}' does not exist.");
            yield break;
        }

        Transform[] allChildren = rootObj.GetComponentsInChildren<Transform>();

        List<Transform> partsToConvert = new List<Transform>(allChildren);

        if (randomizeOrder)
        {
            for (int i = 0; i < partsToConvert.Count; i++)
            {
                Transform temp = partsToConvert[i];
                int randomIndex = Random.Range(i, partsToConvert.Count);
                partsToConvert[i] = partsToConvert[randomIndex];
                partsToConvert[randomIndex] = temp;
            }
        }

        foreach (Transform part in partsToConvert)
        {
            if (part != null)
            {
                part.gameObject.layer = layerIndex;
            }

            if (transitionDelay > 0f)
            {
                yield return new WaitForSeconds(transitionDelay);
            }
        }
    }
}