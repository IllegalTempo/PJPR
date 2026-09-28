using UnityEngine;
using UnityEngine.Events;

namespace Assets.codes.machines
{
    public class RotationController : SyncedMachine
    {
        [Min(0f)]
        public float degreesPerClick = 15f;

        public Transform RotatingTransform;
        public UnityEvent<Quaternion> onRotationChanged;

        private Quaternion targetRotation;
        private Rigidbody rotatingRigidbody;

        private void Awake()
        {
            Transform rotationTarget = GetRotationTarget();

            targetRotation = rotationTarget.rotation;
            rotatingRigidbody = rotationTarget.GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (InteractHolding)
            {
                Rotate(-degreesPerClick * Time.fixedDeltaTime);
            }

            if (SecondInteractHolding)
            {
                Rotate(degreesPerClick * Time.fixedDeltaTime);
            }


                RotatingTransform.rotation = targetRotation;
            
        }

        // Left click turns left.
        protected override void ShareActionOnInteract_press(PlayerMain who)
        {
            base.ShareActionOnInteract_press(who);
            Rotate(-degreesPerClick);
        }

        // Right click turns right.
        protected override void ShareActionOnSecondaryInteract_press(PlayerMain who)
        {
            base.ShareActionOnSecondaryInteract_press(who);
            Rotate(degreesPerClick);
        }

        private void Rotate(float degrees)
        {
            targetRotation *= Quaternion.Euler(0f, degrees, 0f);
            onRotationChanged?.Invoke(targetRotation);
        }

        private Transform GetRotationTarget()
        {
            return RotatingTransform != null
                ? RotatingTransform
                : transform;
        }
    }
}