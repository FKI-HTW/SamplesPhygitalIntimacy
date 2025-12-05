using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace PhygitalIntimacy.Multiplayer
{
    public class NetworkedActor : NetworkBehaviour
    {
        [Header("Rig & Streaming")]
        [SerializeField] private Animator animator;
        [SerializeField] private float lerpSpeed = 15f;
        [SerializeField] private List<GameObject> rokokoObjects = new();

        [SerializeField] private ObjectReferenceChannelSO performerObjectReference;

        private bool lastVisibilityState;

        [Networked, Capacity(50)]
        private NetworkArray<Quaternion> NetworkedBoneRotations => default;

        [Header("Performer Follow")]
        public Transform performerToFollow;

        // Stable yaw computation & smoothing
        [SerializeField] private float rootFollowLerp = 10f; // smoothing for root position/rotation
        [SerializeField] private float heightOffset = 0f;    // optional vertical offset, e.g., hips-to-root
        [SerializeField] private bool clampToGround = false; // lock to a minimum ground Y
        [SerializeField] private float groundY = 0f;         // ground plane height

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

            if (HasStateAuthority)
            {
                foreach (GameObject item in rokokoObjects)
                    item.SetActive(true);

                performerObjectReference.OnObjectProvided -= OnPerformerProvided;
                performerObjectReference.OnObjectProvided += OnPerformerProvided;
            }
        }

        private void OnDestroy()
        {
            if (HasStateAuthority && performerObjectReference != null)
                performerObjectReference.OnObjectProvided -= OnPerformerProvided;
        }

        private void OnPerformerProvided(GameObject performer)
        {
            performerToFollow = performer != null ? performer.transform : null;
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

            // Follow performer root with stable yaw-only rotation
            if (performerToFollow != null)
            {
                Vector3 targetPos = performerToFollow.position;

                // Optional adjustments
                targetPos.y += heightOffset;
                if (clampToGround)
                    targetPos.y = Mathf.Max(targetPos.y, groundY);

                Quaternion yawOnly = ComputeYawOnly(performerToFollow);

                Transform root = transform.root;

                // Smooth position & rotation to reduce jitter across network ticks
                root.position = Vector3.Lerp(root.position, targetPos, Time.deltaTime * rootFollowLerp);
                root.rotation = Quaternion.Slerp(root.rotation, yawOnly, Time.deltaTime * rootFollowLerp);
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
