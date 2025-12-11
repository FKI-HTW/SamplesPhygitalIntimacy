using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace PhygitalIntimacy.Multiplayer
{
    public class NetworkedActor : NetworkBehaviour, IStateAuthorityChanged
    {
        [Header("Rig & Streaming")]
        [SerializeField] private Animator animator;
        [SerializeField] private float lerpSpeed = 15f;
        [SerializeField] private List<GameObject> rokokoObjects = new();

        [Networked, Capacity(50)]
        private NetworkArray<Quaternion> NetworkedBoneRotations => default;

        private Vector3 _lastPlanarHeading = Vector3.forward;

        [Header("Bones")]
        public Transform[] bones;
        private HumanBodyBones[] boneList;

        public override void Spawned()
        {
            if (animator == null)
            {
                Debug.LogError("No animator. Use one with humanoid rig");
                return;
            }

            InitializeBones();

            ToggleRokokoObjects(HasStateAuthority);

            if (Runner.IsSharedModeMasterClient)
            {
                Object.RequestStateAuthority();
            }
        }

        private void OnValidate()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            //ToggleRokokoObjects(false);
        }

        private void ToggleRokokoObjects(bool activate)
        {
            foreach (GameObject item in rokokoObjects) item.SetActive(activate);
        }

        public void StateAuthorityChanged()
        {
            if (HasStateAuthority)
            {
                Debug.Log("Authority granted! Activating Rokoko objects.");
                ToggleRokokoObjects(true);
            }
            else
            {
                ToggleRokokoObjects(false);
            }
        }

        private void InitializeBones()
        {
            // Humanoid bones that will be synchronized
            boneList = new HumanBodyBones[]
            {
                HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
                HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot
            };

            bones = new Transform[boneList.Length];

            for (int i = 0; i < boneList.Length; i++)
            {
                bones[i] = animator.GetBoneTransform(boneList[i]);
                if (bones[i] == null)
                    Debug.LogWarning($"Bone {boneList[i]} not found!");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Runner.IsSharedModeMasterClient && !Object.HasStateAuthority)
            {
                Object.RequestStateAuthority();
            }

            // Only state authority writes networked bone rotations and root transform
            if (!HasStateAuthority)
                return;

            // Write bone rotations to network buffer (only when changed enough)
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null)
                    continue;

                Quaternion newRotation = bones[i].rotation;
                if (Quaternion.Angle(NetworkedBoneRotations.Get(i), newRotation) > 0.01f)
                    NetworkedBoneRotations.Set(i, newRotation);
            }
        }

        // Non-authority clients lerp toward received bone rotations
        public override void Render()
        {
            if (!HasStateAuthority)
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] == null)
                        continue;

                    Quaternion targetRotation = NetworkedBoneRotations.Get(i);
                    bones[i].rotation = Quaternion.Lerp(bones[i].rotation, targetRotation, Time.deltaTime * lerpSpeed);
                }
            }
        }

        /// <summary>
        /// Computes a stable yaw-only rotation from a performer transform.
        /// Uses forward projected onto XZ; if invalid (e.g., performer looks straight up),
        /// falls back to local right, and if still invalid, keeps last valid heading.
        /// </summary>
        private Quaternion ComputeYawOnly(Transform t)
        {
            // Project forward onto ground plane
            Vector3 planarFwd = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            float magFwd = planarFwd.sqrMagnitude;

            // If forward is nearly vertical, try right axis
            if (magFwd < 0.0001f)
            {
                Vector3 planarRight = Vector3.ProjectOnPlane(t.right, Vector3.up);
                if (planarRight.sqrMagnitude >= 0.0001f)
                {
                    planarRight.Normalize();
                    // Build a forward from right so that up is still +Y
                    planarFwd = Vector3.Cross(Vector3.up, planarRight).normalized;
                }
            }

            // If still invalid, reuse last good heading
            if (planarFwd.sqrMagnitude < 0.0001f)
                planarFwd = _lastPlanarHeading;
            else
                planarFwd.Normalize();

            _lastPlanarHeading = planarFwd;

            return Quaternion.LookRotation(planarFwd, Vector3.up);
        }


    }
}
