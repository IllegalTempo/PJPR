using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.codes.spaceship.mechanics
{
    public class RotationController : Interactable
    {
        public float rotationSpeed = 300f;
        private Vector3 _angularVelocityAxisAngle = Vector3.zero; // axis * degrees/sec, built up while dragging
        public float momentumDecay = 2f; // higher = stops faster
        private bool _wasDraggingLastFrame = false;

        private bool isDragging = false;
        protected override void Update()
        {

            if (isDragging)
            {
                PlayerMain player = GameCore.Instance.Local_Player;
                float mouseX = player.lookinput.x;
                float mouseY = player.lookinput.y;

                if (Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f)
                {
                    Vector3 camRight = player.head.transform.right;
                    Vector3 camUp = player.head.transform.up;

                    // Build a world-space rotation axis perpendicular to the drag direction.
                    // Dragging right -> ball rotates around the camera's "up" axis.
                    // Dragging up -> ball rotates around the camera's "right" axis.
                    Vector3 rotationAxis = (camUp * mouseX - camRight * mouseY);

                    if (rotationAxis.sqrMagnitude > 0.0000001f)
                    {
                        rotationAxis.Normalize();

                        float angleThisFrame = new Vector2(mouseX, mouseY).magnitude * rotationSpeed * Time.deltaTime;

                        transform.Rotate(rotationAxis, angleThisFrame, Space.World);

                            _angularVelocityAxisAngle = rotationAxis * (angleThisFrame / Time.deltaTime);
                        
                    }
                }
                else
                {
                    _angularVelocityAxisAngle = Vector3.zero;
                }
            }
            else if (_angularVelocityAxisAngle.sqrMagnitude > 0.0001f)
            {
                // Continue spinning from the last "flick" and decay over time.
                float angleThisFrame = _angularVelocityAxisAngle.magnitude * Time.deltaTime;
                transform.Rotate(_angularVelocityAxisAngle.normalized, angleThisFrame, Space.World);

                _angularVelocityAxisAngle = Vector3.Lerp(
                    _angularVelocityAxisAngle,
                    Vector3.zero,
                    momentumDecay * Time.deltaTime
                );

                if (_angularVelocityAxisAngle.magnitude < 1f)
                    _angularVelocityAxisAngle = Vector3.zero;
            }

            _wasDraggingLastFrame = isDragging;
        }

        public override void OnInteract_press(PlayerMain who)
        {
            isDragging = true;
        }
        public override void OnInteract_release(PlayerMain who)
        {
            isDragging = false;
        }
    }
}
