using UnityEngine;

namespace Assets.codes.machines
{
    public class HandleControl : SteppedController
    {
        public float minPitch = -45f;
        public float maxPitch = 45f;

        public Transform HandleTransform;

        public override void VisualOnStep(int step)
        {
            SetHandlePitch(StepToPitch(step));
        }

        protected override void ShareActionOnInteract_press(PlayerMain who)
        {
            int newStep = (CurrentStep + 1) % stepCount;
            CheckForStepChange(newStep);

        }


        protected override void DuringGrab(PlayerMain who)
        {
        }

        private float StepToPitch(int step)
        {
            float step01 = Mathf.Clamp01(step / (float)(stepCount - 1));
            return Mathf.Lerp(minPitch, maxPitch, step01);
        }

        private void SetHandlePitch(float pitch)
        {
            HandleTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
