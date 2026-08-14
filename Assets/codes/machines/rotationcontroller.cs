using UnityEngine;
using UnityEngine.Events;

namespace Assets.codes.machines
{
    public class RotationController : SyncedMachine
    {
        public enum RotationAxisLock
        {
            None,
            X,
            Y,
            Z
        }

        public float headRotationMultiplier = 1f;
        public float returnSpeed = 2f;
        public RotationAxisLock lockedAxis = RotationAxisLock.None;

        private Quaternion originalRotation;
        private Quaternion lastHeadRotation;
        private bool hasLastHeadRotation;
        private Quaternion targetRotation;
        private Rigidbody rotatingRigidbody;
        private Transform cachedRigidbodyTarget;

        public Transform RotatingTransform;
        public UnityEvent<Quaternion> onRotationChanged;
        private void Awake()
        {
            Transform rotationTarget = GetRotationTarget();
            originalRotation = rotationTarget.rotation;
            targetRotation = originalRotation;
            CacheRotatingRigidbody(rotationTarget);
        }

        protected override void ShareActionOnInteract_press(PlayerMain who)
        {
            base.ShareActionOnInteract_press(who);

            if (who != null && who.head != null)
            {
                lastHeadRotation = who.GetHeadRotation();
                hasLastHeadRotation = true;
            }
        }

        protected override void ShareActionOnInteract_release()
        {
            base.ShareActionOnInteract_release();
            hasLastHeadRotation = false;
        }

        void Update()
        {
            Transform rotationTarget = GetRotationTarget();
            CacheRotatingRigidbody(rotationTarget);

            if (IsPressed)
            {
                PlayerMain player = pressedByPlayer;
                if (player == null || player.head == null)
                {
                    return;
                }

                Quaternion currentHeadRotation = player.GetHeadRotation();
                if (!hasLastHeadRotation)
                {
                    lastHeadRotation = currentHeadRotation;
                    hasLastHeadRotation = true;
                    return;
                }

                Quaternion deltaRotation = currentHeadRotation * Quaternion.Inverse(lastHeadRotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                if (axis.sqrMagnitude > 0.0000001f && angle > 0.0001f)
                {
                    Quaternion appliedDeltaRotation = Quaternion.AngleAxis(angle * headRotationMultiplier, axis.normalized);
                    targetRotation = ApplyAxisLock(appliedDeltaRotation * targetRotation);
                }

                lastHeadRotation = currentHeadRotation;

            }
            else
            {
                targetRotation = ApplyAxisLock(Quaternion.Slerp(targetRotation, GetAxisLockReferenceRotation(), returnSpeed * Time.deltaTime));

                hasLastHeadRotation = false;
            }
            onRotationChanged?.Invoke(targetRotation);

        }

        private void FixedUpdate()
        {
            Transform rotationTarget = GetRotationTarget();
            CacheRotatingRigidbody(rotationTarget);

            if (rotatingRigidbody != null)
            {
                rotatingRigidbody.MoveRotation(targetRotation);
                return;
            }

            rotationTarget.rotation = targetRotation;
        }

        private Transform GetRotationTarget()
        {
            return RotatingTransform != null ? RotatingTransform : transform;
        }

        private void CacheRotatingRigidbody(Transform rotationTarget)
        {
            if (cachedRigidbodyTarget == rotationTarget)
            {
                return;
            }

            cachedRigidbodyTarget = rotationTarget;
            rotatingRigidbody = rotationTarget.GetComponent<Rigidbody>();
        }

        private Quaternion ApplyAxisLock(Quaternion rotation)
        {
            if (lockedAxis == RotationAxisLock.None)
            {
                return rotation;
            }

            Vector3 rotationEuler = rotation.eulerAngles;
            Vector3 lockedRotation = GetAxisLockReferenceRotation().eulerAngles;

            switch (lockedAxis)
            {
                case RotationAxisLock.X:
                    rotationEuler.x = lockedRotation.x;
                    break;
                case RotationAxisLock.Y:
                    rotationEuler.y = lockedRotation.y;
                    break;
                case RotationAxisLock.Z:
                    rotationEuler.z = lockedRotation.z;
                    break;
            }

            return Quaternion.Euler(rotationEuler);
        }

        private Quaternion GetAxisLockReferenceRotation()
        {
            if (MainSpaceship.Instance != null)
            {
                return MainSpaceship.Instance.transform.rotation;
            }

            return originalRotation;
        }

    }
}
