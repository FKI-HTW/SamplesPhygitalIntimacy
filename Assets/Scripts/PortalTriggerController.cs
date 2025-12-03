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

    [Header("Safety Settings")]
    [Tooltip("Time in seconds to ignore subsequent triggers after activation.")]
    public float triggerCooldown = 1.0f;
    private float lastTriggerTime = -Mathf.Infinity;

    [Header("Layer Toggle Settings")]
    [Tooltip("The layer name for the 'Normal' state (e.g., 'Default').")]
    public string layerNormal = "Default";

    [Tooltip("The layer name for the 'Passthrough' state (e.g., 'Passthrough').")]
    public string layerPassthrough = "VirtualWorld";

    [Tooltip("The specific object to change layers. Ignored if 'Affect Colliding Object' is true.")]
    public GameObject layerChangeTarget;

    [Header("Transition Effect")]
    [Tooltip("Time in seconds to wait between changing the layer of each child object.")]
    [Range(0f, 0.5f)]
    public float transitionDelay = 0.05f;

    [Tooltip("If true, child objects will change layers in random order. If false, they change top-down.")]
    public bool randomizeOrder = false;

    [Header("Response")]
    [Tooltip("Actions to execute when the trigger is activated.")]
    public UnityEvent onTriggerEnterToVR;
    public UnityEvent onTriggerEnterToMR;

    private Coroutine activeCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < lastTriggerTime + triggerCooldown)
        {
            return;
        }

        if (CheckTarget(other.gameObject))
        {
            lastTriggerTime = Time.time;

            Debug.Log($"Trigger activated by: {other.name}");

            if (layerChangeTarget != null)
            {
                TogglePassthroughState();
            }
        }
    }

    public void TogglePassthroughState()
    {
        if (layerChangeTarget == null) return;

        int normalLayerIndex = LayerMask.NameToLayer(layerNormal);
        int passLayerIndex = LayerMask.NameToLayer(layerPassthrough);

        if (normalLayerIndex == -1 || passLayerIndex == -1)
        {
            Debug.LogError("TriggerController: One of the layer names is invalid. Check Project Settings!");
            return;
        }

        int targetLayerIndex;
        if (layerChangeTarget.layer == normalLayerIndex)
        {
            targetLayerIndex = passLayerIndex;
            onTriggerEnterToMR.Invoke();
        }
        else
        {
            targetLayerIndex = normalLayerIndex;
            onTriggerEnterToVR.Invoke();
        }

        //if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        //activeCoroutine = StartCoroutine(ChangeLayerRoutine(layerChangeTarget, targetLayerIndex));

        ChangeLayerRecursively(layerChangeTarget, targetLayerIndex);

    }

    private bool CheckTarget(GameObject incomingObject)
    {
        switch (detectionMode)
        {
            case DetectionMode.SpecificObject:
                return targetObjectCollider != null && incomingObject == targetObjectCollider.gameObject;

            case DetectionMode.Tag:
                return incomingObject.CompareTag(targetTag);

            case DetectionMode.Name:
                return incomingObject.name == targetName;

            default:
                return false;
        }
    }

    private IEnumerator ChangeLayerRoutine(GameObject rootObj, int targetLayerIndex)
    {
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
                part.gameObject.layer = targetLayerIndex;
            }

            if (transitionDelay > 0f)
            {
                yield return new WaitForSeconds(transitionDelay);
            }
        }
    }

    private void ChangeLayerRecursively(GameObject obj, int layerIndex)
    {

        if (layerIndex == -1)
        {
            return;
        }

        obj.layer = layerIndex;

        foreach (Transform child in obj.transform)
        {
            ChangeLayerRecursively(child.gameObject, layerIndex);
        }
    }

}