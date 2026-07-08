using UnityEngine;
using System.Collections;
using Assets.codes.Network.Messages;

namespace Assets.codes.spaceship.mechanics
{
    public class handlecontrol : Machine
    {
        private bool isGrabbed; //this is only limited to local player

        public float pitchSensitivity = 3f;
        public float minPitch = -45f;
        public float maxPitch = 45f;

        public float rotationSmoothing = 12f;

        [Header("Step Settings")]
        [Min(2)]
        public int stepCount = 3;
        public int CurrentStep { get; private set; }

        private float rawPitch;   // Continuous pitch tracked from mouse input, before snapping to a step.
        private float StepAngle => (maxPitch - minPitch) / (stepCount - 1);

        public override void ServerActionOnInteract()
        {
        }

        public override void ShareActionOnInteract()
        {
        }
        public override void OnInteract_press(PlayerMain who)
        {
            base.OnInteract_press(who);
            isGrabbed = true;
        }
        public override void OnInteract_release(PlayerMain who)
        {
            base.OnInteract_release(who);
            isGrabbed = false;
        }
        protected override void Update()
        {
            base.Update();
            if (isGrabbed)
                RotateHandle(GameCore.Instance.Local_Player);
        }
        private void RotateHandle(PlayerMain who)
        {

            // Mouse Y movement adjusts the raw (continuous) pitch.
            float mouseY = who.lookinput.y;
            rawPitch = Mathf.Clamp(rawPitch - mouseY, minPitch, maxPitch);

            // Snap the raw pitch to the nearest step.
            int newStep = PitchToStep(rawPitch);
            float steppedPitch = StepToPitch(newStep);

            // Yaw follows the player's current facing direction.
            float targetYaw = who.cam.transform.eulerAngles.y;

            Quaternion targetRotation = Quaternion.Euler(steppedPitch, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);

            if (newStep != CurrentStep)
            {
                NMS_Both_Handle_OnReleaseUpdateLevel msg = new NMS_Both_Handle_OnReleaseUpdateLevel(identity.Identifier, newStep);
                msg.SendMessageAsServerOrClient();
            }
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
        public virtual void OnStepChanged(int newStep)
        {
            
            CurrentStep = newStep;

        }
        public virtual void OnStepChanged_Server(int newStep)
        {

        }
    }
}