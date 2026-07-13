using UnityEngine;
using System.Collections;
using Assets.codes.Network.Messages;

namespace Assets.codes.spaceship.mechanics
{
    public class slidercontroller : stepcontroller
    {
        public float sliderSensitivity = 3f;
        public float minSlide = -0.5f;
        public float maxSlide = 0.5f;

        public Vector3 slideAxis = Vector3.up;


        private float rawSlide;
        private Vector3 originalLocalPosition;
        private float StepDistance => (maxSlide - minSlide) / (stepCount - 1);

        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
        }

        protected override void DuringGrab(PlayerMain who)
        {
            float mouseY = who.lookinput.x;
            rawSlide = Mathf.Clamp(rawSlide - (mouseY * sliderSensitivity * Time.deltaTime), minSlide, maxSlide);

            int newStep = SlideToStep(rawSlide);

            CheckForStepChange(newStep);
        }

        private int SlideToStep(float slide)
        {
            return Mathf.Clamp(Mathf.RoundToInt((slide - minSlide) / StepDistance), 0, stepCount - 1);
        }

        private float StepToSlide(int step)
        {
            return minSlide + step * StepDistance;
        }

        public override void VisualOnStep(int step)
        {
            float steppedSlide = StepToSlide(step);

            Vector3 targetPosition = originalLocalPosition + (slideAxis.normalized * steppedSlide);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothing);
        }
    }
}