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

        public UnityEvent<Quaternion> onRotationChanged;
        private void Awake()
        {
            originalRotation = transform.rotation;
        }

        protected override void ShareActionOnInteract_press(PlayerMain who)
        {
            base.ShareActionOnInteract_press(who);

            if (who != null && who.head != null)
            {
                lastHeadRotation = who.head.transform.rotation;
                hasLastHeadRotation = true;
            }
        }

        protected override void ShareActionOnInteract_release()
        {
            base.ShareActionOnInteract_release();
            hasLastHeadRotation = false;
        }

        protected override void Update()
        {

            if (IsPressed)
            {
                PlayerMain player = pressedByPlayer;
                if (player == null || player.head == null)
                {
                    return;
                }

                Quaternion currentHeadRotation = player.head.transform.rotation;
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
                    transform.Rotate(axis.normalized, angle * headRotationMultiplier, Space.World);
                    ApplyAxisLock();
                }

                lastHeadRotation = currentHeadRotation;

            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, MainSpaceship.Instance.transform.rotation, returnSpeed * Time.deltaTime);
                ApplyAxisLock();

                hasLastHeadRotation = false;
            }
            onRotationChanged?.Invoke(transform.rotation);

        }

        private void ApplyAxisLock()
        {
            if (lockedAxis == RotationAxisLock.None)
            {
                return;
            }

            Vector3 rotation = transform.rotation.eulerAngles;
            Vector3 lockedRotation = GetAxisLockReferenceRotation().eulerAngles;

            switch (lockedAxis)
            {
                case RotationAxisLock.X:
                    rotation.x = lockedRotation.x;
                    break;
                case RotationAxisLock.Y:
                    rotation.y = lockedRotation.y;
                    break;
                case RotationAxisLock.Z:
                    rotation.z = lockedRotation.z;
                    break;
            }

            transform.rotation = Quaternion.Euler(rotation);
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
