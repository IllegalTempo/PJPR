using UnityEngine;
using System.Collections;
using Assets.codes.Network.Messages;

namespace Assets.codes.spaceship.mechanics
{
    public class handlecontrol : stepcontroller
    {
        public float pitchSensitivity = 3f;
        public float minPitch = -45f;
        public float maxPitch = 45f;



        private float rawPitch;   // Continuous pitch tracked from mouse input, before snapping to a step.
        private float StepAngle => (maxPitch - minPitch) / (stepCount - 1);

        public override void VisualOnStep(int step)
        {
            float steppedPitch = StepToPitch(step);


            Quaternion targetRotation = Quaternion.Euler(steppedPitch, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothing);
            
        }

        protected override void DuringGrab(PlayerMain who)
        {

            // Mouse Y movement adjusts the raw (continuous) pitch.
            float mouseY = who.lookinput.y;
            rawPitch = Mathf.Clamp(rawPitch - mouseY, minPitch, maxPitch);

            // Snap the raw pitch to the nearest step.
            int newStep = PitchToStep(rawPitch);
            CheckForStepChange(newStep);

        }

        private int PitchToStep(float pitch)
        {
            return Mathf.Clamp(Mathf.RoundToInt((pitch - minPitch) / StepAngle), 0, stepCount - 1);
        }

        private float StepToPitch(int step)
        {
            return minPitch + step * StepAngle;
        }
        
        /// <summary>
        /// Called whenever the handle settles on a new step. Override this in a
        /// subclass for custom behaviour (e.g. playing a click sound, triggering
        /// a puzzle event), or just hook up onStepChanged in the Inspector or
        /// via code (handle.onStepChanged.AddListener(...)) instead.
        /// </summary>
    }
}