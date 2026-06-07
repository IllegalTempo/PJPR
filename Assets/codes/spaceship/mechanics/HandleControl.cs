using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using System;
using Assets.codes.spaceship.modules;

namespace Assets.codes.spaceship.mechanics
{
    public class HandleControl : Interactable
    {
        [SerializeField]
        private Transform handle;
        [SerializeField]
        private Transform handleTop;
        [SerializeField]
        private float rotationSpeed = 10f;
        [SerializeField]
        private Vector3 rotationOffset;
        private const float MIN_ROTATION_X = 0f;
        private const float MAX_ROTATION_X = 75f;
        [SerializeField]
        private int rotationSteps = 4;
        [SerializeField]
        private float lookDegreesForFullRotation = 25f;
        [SerializeField]
        private booster ControllingBooster;
        private int currentStep = -1;
        private Quaternion startLocalRotation;
        private Vector3 startLocalTopDirection;
        private float controlStartLookX;
        private float controlStartRotationX;
        private PlayerMain controllingPlayer;

        protected override void OnEnable()
        {
            base.OnEnable();

            Transform handleTransform = GetHandleTransform();
            startLocalRotation = handleTransform.localRotation;
            startLocalTopDirection = GetStartLocalTopDirection(handleTransform);
        }

        public override void OnInteract(PlayerMain who)
        {
            if (who == null)
            {
                return;
            }

            if (controllingPlayer == who)
            {
                controllingPlayer.ClearActiveUsable(this);
                controllingPlayer = null;
                return;
            }

            if (controllingPlayer != null)
            {
                return;
            }

            controllingPlayer = who;
            BeginControl(who);
            controllingPlayer.SetActiveUsable(this);
        }

        protected override void Update()
        {
            base.Update();

            if (controllingPlayer == null)
            {
                return;
            }

            RotateWithPlayerLook();
        }

        private void OnDisable()
        {
            if (controllingPlayer != null)
            {
                controllingPlayer.ClearActiveUsable(this);
                controllingPlayer = null;
            }
        }

        private void RotateWithPlayerLook()
        {
            PlayerMain who = controllingPlayer;
            Transform lookSource = GetLookSource(who);
            if (lookSource == null)
            {
                who.ClearActiveUsable(this);
                controllingPlayer = null;
                return;
            }

            Transform handleTransform = GetHandleTransform();

            float targetX = lookSource.rotation.eulerAngles.x;
            targetX = GetSteppedRotationX(targetX);
            Quaternion targetRotation = startLocalRotation * Quaternion.AngleAxis(targetX, Vector3.right);
            handleTransform.localRotation = Quaternion.Slerp(handleTransform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        private void BeginControl(PlayerMain who)
        {
            Transform lookSource = GetLookSource(who);
            controlStartLookX = lookSource != null ? lookSource.rotation.eulerAngles.x : 0f;
            controlStartRotationX = GetCurrentStepRotationX();
        }

        private Transform GetLookSource(PlayerMain who)
        {
            if (who.cam != null && who.cam.activeInHierarchy)
            {
                return who.cam.transform;
            }

            if (who.head != null)
            {
                return who.head.transform;
            }

            if (who.cam != null)
            {
                return who.cam.transform;
            }

            return who.transform;
        }

        private Transform GetHandleTransform()
        {
            return handle != null ? handle : transform;
        }

        private Vector3 GetStartLocalTopDirection(Transform handleTransform)
        {
            if (handleTop == null)
            {
                return startLocalRotation * Vector3.forward;
            }

            return GetLocalDirection(handleTransform, handleTop.position - handleTransform.position);
        }

        private Vector3 GetLocalDirection(Transform target, Vector3 direction)
        {
            if (target.parent == null)
            {
                return direction;
            }

            return target.parent.InverseTransformDirection(direction);
        }

        private float GetSteppedRotationX(float targetX)
        {
            int stepCount = Mathf.Max(1, rotationSteps);
            float handleRange = MAX_ROTATION_X - MIN_ROTATION_X;
            float lookRange = Mathf.Max(0.01f, lookDegreesForFullRotation);
            float lookDelta = Mathf.DeltaAngle(controlStartLookX, targetX);
            float normalizedTargetX = controlStartRotationX + (lookDelta / lookRange * handleRange) + rotationOffset.x;
            float clampedTargetX = Mathf.Clamp(normalizedTargetX, MIN_ROTATION_X, MAX_ROTATION_X);

            if (stepCount == 1)
            {
                SetStep(0);
                return MIN_ROTATION_X;
            }

            float stepSize = (MAX_ROTATION_X - MIN_ROTATION_X) / (stepCount - 1);
            int step = Mathf.RoundToInt((clampedTargetX - MIN_ROTATION_X) / stepSize);
            step = Mathf.Clamp(step, 0, stepCount - 1);
            SetStep(step);

            return MIN_ROTATION_X + (step * stepSize);
        }

        private float GetCurrentStepRotationX()
        {
            int stepCount = Mathf.Max(1, rotationSteps);
            if (stepCount == 1 || currentStep < 0)
            {
                return MIN_ROTATION_X;
            }

            float stepSize = (MAX_ROTATION_X - MIN_ROTATION_X) / (stepCount - 1);
            int step = Mathf.Clamp(currentStep, 0, stepCount - 1);
            return MIN_ROTATION_X + (step * stepSize);
        }

        private void SetStep(int newstep)
        {
            if (currentStep == newstep)
            {
                return;
            }

            currentStep = newstep;
            OnStepChange(newstep);
        }

        private void OnStepChange(int newstep)
        {
            ControllingBooster.setSpeedLevel(newstep);
        }
    }
}
