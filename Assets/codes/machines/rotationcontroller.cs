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
        private Quaternion originalSpaceshipRotation;
        private Quaternion spaceshipTargetRotation;
        private Transform spaceshipTransform;
        private Rigidbody rotatingRigidbody;
        private Transform cachedRigidbodyTarget;

        public Transform RotatingTransform;
        public UnityEvent<Quaternion> onRotationChanged;
        private void Awake()
        {
            Transform rotationTarget = GetRotationTarget();
            originalRotation = rotationTarget.rotation;
            targetRotation = originalRotation;

            MainSpaceship spaceship = GetComponentInParent<MainSpaceship>();
            if (spaceship == null)
            {
                spaceship = MainSpaceship.Instance;
            }

            spaceshipTransform = spaceship != null ? spaceship.transform : null;
            originalSpaceshipRotation = spaceshipTransform != null ? spaceshipTransform.rotation : originalRotation;
            spaceshipTargetRotation = originalSpaceshipRotation;
            CacheRotatingRigidbody(rotationTarget);
        }

        protected override void ShareActionOnInteract_press(PlayerMain who)
        {
            base.ShareActionOnInteract_press(who);

            if (who != null && who.head != null)
            {
                lastHeadRotation = who.GetHeadRotation();
                hasLastHeadRotation = true;

                targetRotation = ApplyAxisLock(lastHeadRotation, originalRotation);
                GetRotationTarget().rotation = targetRotation;
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
                targetRotation = ApplyAxisLock(currentHeadRotation, originalRotation);
                if (!hasLastHeadRotation)
                {
                    lastHeadRotation = currentHeadRotation;
                    hasLastHeadRotation = true;
                }
                else
                {
                    Quaternion deltaRotation = currentHeadRotation * Quaternion.Inverse(lastHeadRotation);
                    deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                    if (axis.sqrMagnitude > 0.0000001f && angle > 0.0001f)
                    {
                        Quaternion appliedDeltaRotation = Quaternion.AngleAxis(angle * headRotationMultiplier, axis.normalized);
                        spaceshipTargetRotation = ApplyAxisLock(appliedDeltaRotation * spaceshipTargetRotation, originalSpaceshipRotation);
                    }
                }

                lastHeadRotation = currentHeadRotation;

            }
            else
            {
                hasLastHeadRotation = false;
            }
            onRotationChanged?.Invoke(spaceshipTargetRotation);

        }

        private void FixedUpdate()
        {
            Transform rotationTarget = GetRotationTarget();
            CacheRotatingRigidbody(rotationTarget);

            if (rotatingRigidbody != null)
            {
                //rotatingRigidbody.MoveRotation(targetRotation);
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

        private Quaternion ApplyAxisLock(Quaternion rotation, Quaternion referenceRotation)
        {
            if (lockedAxis == RotationAxisLock.None)
            {
                return rotation;
            }

            Vector3 rotationEuler = rotation.eulerAngles;
            Vector3 lockedRotation = referenceRotation.eulerAngles;

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

    }
}
