using UnityEngine;


namespace Assets.codes.machines
{
    public class SliderController : SteppedController
    {
        public Transform slider;
        public float minSlide = -0.5f;
        public float maxSlide = 0.5f;

        private Vector3 slideOriginLocalPosition;
        private bool hasSlideOrigin;
        private float currentSlide;

        
        public override void VisualOnStep(int step)
        {
            currentSlide = StepToSlide(step);
            SetSlidePosition(currentSlide);
            
        }

        protected override void DuringGrab(PlayerMain who)
        {
            if (who == null)
                return;

            Transform cameraTransform = who.head.transform;


            Vector3 lineOrigin = GetSlideOriginWorldPosition();
            Vector3 lineDirection = slider.forward.normalized;
            Vector3 cameraDirection = cameraTransform.forward.normalized;

            float slide = ProjectRayOntoSlideLine(
                cameraTransform.position,
                cameraDirection,
                lineOrigin,
                lineDirection,
                currentSlide);

            slide = Mathf.Clamp(slide, minSlide, maxSlide);
            int newStep = SlideToStep(slide);
            currentSlide = StepToSlide(newStep);

            SetSlidePosition(currentSlide);
            CheckForStepChange(newStep);
        }
        private Vector3 GetSlideOriginWorldPosition()
        {
            if (slider.parent == null)
                return slideOriginLocalPosition;

            return slider.parent.TransformPoint(slideOriginLocalPosition);
        }

        private void SetSlidePosition(float slide)
        {

            Vector3 targetPosition = GetSlideOriginWorldPosition() + slider.forward.normalized * slide;
            slider.position = targetPosition;
        }

        private float ProjectRayOntoSlideLine(Vector3 rayOrigin, Vector3 rayDirection, Vector3 lineOrigin, Vector3 lineDirection, float fallbackSlide)
        {
            float dot = Vector3.Dot(rayDirection, lineDirection);
            float denominator = 1f - dot * dot;
            Vector3 offset = rayOrigin - lineOrigin;

            if (denominator <= 0.0001f)
                return fallbackSlide;

            float rayOffset = Vector3.Dot(rayDirection, offset);
            float lineOffset = Vector3.Dot(lineDirection, offset);
            float rayDistance = (dot * lineOffset - rayOffset) / denominator;

            if (rayDistance < 0f)
                return fallbackSlide;

            return (lineOffset - dot * rayOffset) / denominator;
        }

        private int SlideToStep(float slide)
        {
            float slide01 = Mathf.InverseLerp(minSlide, maxSlide, slide);
            return Mathf.Clamp(Mathf.RoundToInt(slide01 * (stepCount - 1)), 0, stepCount - 1);
        }

        private float StepToSlide(int step)
        {
            float step01 = Mathf.Clamp01(step / (float)(stepCount - 1));
            return Mathf.Lerp(minSlide, maxSlide, step01);
        }
    }
}
