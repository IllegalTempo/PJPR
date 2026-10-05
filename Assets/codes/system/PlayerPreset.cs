using System.Collections;
using UnityEngine;

namespace Assets.codes.system
{
    public class PlayerPreset : MonoBehaviour
    {
        public static PlayerPreset Playerpreset;
        private void Start()
        {
            Playerpreset = this;
        }
        [Min(0.01f)] public float MoveAcceleration = 40f;
        [Min(0.01f)] public float MoveDeceleration = 60f;
        public float tapDropPlaceDistance = 1.2f;

        public float dropCollisionRadius = 0.35f;

        public float dropCollisionPadding = 0.05f;

        public LayerMask dropCollisionMask = Physics.DefaultRaycastLayers;

        public float throwHoldThreshold = 0.25f;

        public float throwFullChargeTime = 1.5f;

        public float maxThrowForce = 10f;

        public float throwChargeFieldOfView = 45f;

        public float throwCameraZoomTransitionSpeed = 60f;
    }
}