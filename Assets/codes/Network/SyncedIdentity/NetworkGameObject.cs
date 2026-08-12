using Assets.codes.Network.Messages;
using System.Collections;
using System.Security.Principal;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Identifiers;

namespace Assets.codes.Network.SyncedIdentity
{
    [RequireComponent(typeof(NetworkIdentity))]
	public class NetworkGameObject : MonoBehaviour
    {
        [Header("NetworkObject Setting")]
        [SerializeField]
        public bool Sync_Transform = true;
        [SerializeField]
        private float TransformSendInterval = 0.1f;
        public Vector3 NetworkPos;
        public Quaternion NetworkRot;

        private Vector3 prevPos;
        private Quaternion prevRot;
        private float nextTransformSendTime;
        private bool initializedTransformSendDelay;
        private Rigidbody rb;
        private int inSpaceshipTriggerCount;
        private Vector3 previousSpaceshipVelocity;
        public PrefabDefinition AbstractObject;
        public bool InSpaceship;



        public NetworkIdentity Identity;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        // Use this for initialization
        protected virtual void FindIdentity()
        {
            Identity = GetComponent<NetworkIdentity>();
            if (Identity == null)
            {
                Debug.LogError("NetworkGameObject requires a NetworkIdentity component.");
            }
        }
        public void OnInstantiate(string uid, string PrefabID, ulong sovereignty)
        {
            if (Identity == null)
            {
                FindIdentity();
            }

            if(Identity is NetworkPrefabIdentity i)
            {
                i.OnInstantiate(uid, PrefabID, sovereignty);
                Identity.Reinitialize(uid);

            } else
            {
                Debug.LogError(gameObject.name + " requires a NetworkPrefabIdentity component for instantiation.");
            }
        }
        public void UpdateActive(bool status)
        {
            new NMS_Both_NetworkObjectActive(Identity.Identifier, status).SendMessageAsServerOrClient();
            
        }
        public bool IsLocalSovereignty()
        {
            return GameCore.Instance.IsLocal(Identity.Sovereignty);
        }
        public void SetMovement(Vector3 pos, Quaternion rot)
        {
            NetworkPos = pos;
            NetworkRot = rot;
        }
        public void SetServerMovement(Vector3 pos, Quaternion rot)
        {
            SetMovement(pos, rot);
            transform.position = pos;
            transform.rotation = rot;
        }
        protected virtual void FixedUpdate()
        {
            FollowSpaceshipVelocity();
        }

        private void FollowSpaceshipVelocity()
        {
            if (rb == null || rb.isKinematic)
            {
                previousSpaceshipVelocity = Vector3.zero;
                return;
            }

            if (!InSpaceship)
            {
                previousSpaceshipVelocity = Vector3.zero;
                return;
            }

            Vector3 spaceshipVelocity = GetSpaceshipVelocityAtObject();
            Vector3 relativeVelocity = rb.linearVelocity - previousSpaceshipVelocity;
            rb.linearVelocity = relativeVelocity + spaceshipVelocity;
            previousSpaceshipVelocity = spaceshipVelocity;
        }

        private Vector3 GetSpaceshipVelocityAtObject()
        {
            if (MainSpaceship.Instance == null)
            {
                return Vector3.zero;
            }

            Rigidbody spaceshipRigidbody = MainSpaceship.Instance.GetComponent<Rigidbody>();
            if (spaceshipRigidbody == null)
            {
                return Vector3.zero;
            }

            return spaceshipRigidbody.GetPointVelocity(rb.position);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsInSpaceshipDetector(other))
            {
                return;
            }

            inSpaceshipTriggerCount++;
            InSpaceship = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsInSpaceshipDetector(other))
            {
                return;
            }

            inSpaceshipTriggerCount = Mathf.Max(0, inSpaceshipTriggerCount - 1);
            InSpaceship = inSpaceshipTriggerCount > 0;
            if (!InSpaceship)
            {
                previousSpaceshipVelocity = Vector3.zero;
            }
        }

        private bool IsInSpaceshipDetector(Collider other)
        {
            if (GameCore.Instance == null || GameCore.Instance.Masks == null)
            {
                return false;
            }

            return (GameCore.Instance.Masks.InSpaceshipDetect.value & (1 << other.gameObject.layer)) != 0;
        }
        private void SendTransform()
        {
            if (!NetworkSystem.Instance.IsOnline) return;
            if (prevPos == transform.position && prevRot == transform.rotation) return;
            NMS_Both_NetworkObjectInfo message = new NMS_Both_NetworkObjectInfo(Identity.Identifier, transform.position, transform.rotation);
            if (NetworkSystem.Instance.IsServer)
            {
                NetworkRouter.Instance.DistributeMessageToReady(message, sendType: NetworkSendProfiles.State);

            }
            else if (GameCore.Instance.IsLocal(Identity.Sovereignty))
            {
                NetworkRouter.Instance.SendMessageToServer(message, NetworkSendProfiles.State);

            }
            prevPos = transform.position;
            prevRot = transform.rotation;

        }
        private void Update()
        {
            if (NetworkSystem.Instance == null)
            {
                return;
            }
            if (Sync_Transform && !initializedTransformSendDelay)
            {
                nextTransformSendTime = Time.time + 0.1f;
                initializedTransformSendDelay = true;
            }
            if (Sync_Transform && Time.time >= nextTransformSendTime)
            {
                SendTransform();
                nextTransformSendTime = Time.time + TransformSendInterval;
            }
            if (NetworkSystem.Instance.IsWorldManager) return;
            if (GameCore.Instance != null && GameCore.Instance.IsLocal(Identity.Sovereignty)) return;
            if (Sync_Transform)
            {
                transform.position = Vector3.Lerp(transform.position, NetworkPos, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Slerp(transform.rotation, NetworkRot, Time.deltaTime * 10f);
            }

        }
    }
}
