using UnityEngine;

namespace Assets.codes.machines
{
    public class HandleControl : SteppedController
    {
        public float minPitch = -45f;
        public float maxPitch = 45f;

        public Transform HandleTransform;

        protected override void Start()
        {
            base.Start();
            VisualOnStep(CurrentStep);
        }

        public override void VisualOnStep(int step)
        {
            SetHandlePitch(StepToPitch(step));
        }

        protected override void ServerActionOnInteract_press(PlayerMain who)
        {
            CheckForStepChange(GetNextStep(1));
        }

        protected override void ServerActionOnSecondaryInteract_press(PlayerMain who)
        {
            CheckForStepChange(GetNextStep(-1));
        }


        protected override void DuringGrab(PlayerMain who)
        {
        }

        private float StepToPitch(int step)
        {
            int maximumStep = (stepCount - 1) / 2;
            float step01 = Mathf.InverseLerp(-maximumStep, maximumStep, step);
            return Mathf.Lerp(minPitch, maxPitch, step01);
        }

        public int GetNextStep(int direction)
        {
            int maximumStep = (stepCount - 1) / 2;
            int normalizedDirection = direction == 0 ? 0 : (direction > 0 ? 1 : -1);
            return Mathf.Clamp(CurrentStep + normalizedDirection, -maximumStep, maximumStep);
        }

        private void SetHandlePitch(float pitch)
        {
            HandleTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
