using Assets.codes.Network.Messages;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.codes.machines
{
	public abstract class SteppedController: SyncedMachine
	{
        public UnityEvent<int> onStepChanged;
        public float smoothing = 12f;

        [Header("Step Settings")]
        [Min(2)]
        public int stepCount = 3;
        public int CurrentStep { get; private set; }


        //public override void OnInteract_press(PlayerMain who)
        //{
        //    base.OnInteract_press(who);
        //    isGrabbed = true;
        //}
        //public override void OnInteract_release(PlayerMain who)
        //{
        //    base.OnInteract_release(who);
        //    isGrabbed = false;
        //}
        protected override void Update()
        {
            base.Update();
            if (IsPressed)
                DuringGrab(pressedByPlayer);
        }
        protected abstract void DuringGrab(PlayerMain who);

        /// <summary>
        /// Called whenever the handle settles on a new step. Override this in a
        /// subclass for custom behaviour (e.g. playing a click sound, triggering
        /// a puzzle event), or just hook up onStepChanged in the Inspector or
        /// via code (handle.onStepChanged.AddListener(...)) instead.
        /// </summary>
        protected void CheckForStepChange(int newStep)
        {
            if (newStep != CurrentStep)
            {
                NMS_Both_Handle_OnReleaseUpdateLevel msg = new NMS_Both_Handle_OnReleaseUpdateLevel(identity.Identifier, newStep);
                msg.SendMessageAsServerOrClient();

            }
        }
        public virtual void OnStepChanged(int newStep) //run by netmessage
        {
            CurrentStep = newStep;
            VisualOnStep(newStep);
            onStepChanged?.Invoke(newStep);
        }
        public virtual void OnStepChanged_Server(int newStep) //Server only when step is changed
        {
        }
        public abstract void VisualOnStep(int step);
    }
}