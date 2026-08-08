using UnityEngine;

namespace Assets.codes.machines
{
    public class RotationController : SyncedMachine
    {
        public float headRotationMultiplier = 1f;
        public float returnSpeed = 2f;
        public float spaceshipRotationSpeed = 10f;
        private Quaternion originalRotation;
        private Quaternion lastHeadRotation;
        private bool hasLastHeadRotation;

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
                }

                lastHeadRotation = currentHeadRotation;
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, returnSpeed * Time.deltaTime);
                hasLastHeadRotation = false;
            }

            MainSpaceship.Instance?.RotateToward(transform.rotation, spaceshipRotationSpeed);
        }

    }
}
