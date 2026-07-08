using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.codes.spaceship.mechanics
{
    public class RotationController : Interactable
    {
        public float rotationSpeed = 300f;
        public float returnSpeed = 2f;
        private Quaternion originalRotation;

        private bool isDragging = false;
        private void Awake()
        {
            originalRotation = transform.rotation;
        }

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
                    }
                }
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, returnSpeed * Time.deltaTime);
            }

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
