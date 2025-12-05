using UnityEngine;

namespace PhygitalIntimacy
{
    using UnityEngine;
    using UnityEngine.Events;

    [CreateAssetMenu(menuName = "Events/Object Reference Channel")]
    public class ObjectReferenceChannelSO : ScriptableObject
    {
        public GameObject ObjectReference;

        public UnityAction<GameObject> OnObjectProvided;

        public void Provide(GameObject obj)
        {
            ObjectReference = obj;
            OnObjectProvided?.Invoke(obj);
        }
    }

}
