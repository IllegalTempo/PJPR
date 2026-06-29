using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.codes.system
{
    public class WorldReference : MonoBehaviour
    {
        private Vector3 movement = Vector3.zero;
        private Vector3 angularMovement = Vector3.zero;
        [SerializeField] private Transform movementPivot;
        [SerializeField] private float rotationResponse = 1f;
        [SerializeField] private float minRotationSpeed = 0f;
        private Dictionary<int, ThrustSource> SourceToVelocity = new Dictionary<int, ThrustSource>();

        private struct ThrustSource
        {
            public Vector3 Velocity;
            public Vector3 Position;
            public bool HasPosition;
        }

        public void UpdateSourceVelocity(int sourceModuleInstanceID, Vector3 newVelocity)
        {
            SourceToVelocity[sourceModuleInstanceID] = new ThrustSource
            {
                Velocity = newVelocity,
                Position = Vector3.zero,
                HasPosition = false,
            };

            UpdateMovement();
        }

        public void UpdateSourceVelocity(int sourceModuleInstanceID, Vector3 newVelocity, Vector3 sourcePosition)
        {
            SourceToVelocity[sourceModuleInstanceID] = new ThrustSource
            {
                Velocity = newVelocity,
                Position = sourcePosition,
                HasPosition = true,
            };

            UpdateMovement();
        }

        private void UpdateMovement()
        {
            movement = calculateFinalVelocity();
            angularMovement = calculateFinalAngularVelocity();
        }

        private Vector3 calculateFinalVelocity()
        {
            Vector3 sum = Vector3.zero;
            foreach(ThrustSource source in SourceToVelocity.Values)
            {
                sum += source.Velocity;
            }
            return sum;
        }

        private Vector3 calculateFinalAngularVelocity()
        {
            Vector3 pivot = GetPivotPosition();
            Vector3 sum = Vector3.zero;
            foreach (ThrustSource source in SourceToVelocity.Values)
            {
                if (!source.HasPosition)
                {
                    continue;
                }

                Vector3 offsetFromPivot = source.Position - pivot;
                sum += Vector3.Cross(offsetFromPivot, source.Velocity) * rotationResponse;
            }

            return sum;
        }

        private Vector3 GetPivotPosition()
        {
            if (movementPivot != null)
            {
                return movementPivot.position;
            }

            if (GameCore.Instance != null && MainSpaceship.Instance != null)
            {
                return MainSpaceship.Instance.transform.position;
            }

            return transform.position;
        }

        private void LateUpdate()
        {
            if (movement != Vector3.zero)
            {
                transform.position -= movement * Time.deltaTime;
            }

            if (angularMovement.sqrMagnitude >= minRotationSpeed * minRotationSpeed)
            {
                float angle = angularMovement.magnitude * Time.deltaTime;
                Vector3 pivot = GetPivotPosition();
                transform.RotateAround(pivot, angularMovement.normalized, -angle);

            }
        }
    }
}
