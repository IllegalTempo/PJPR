using System.Collections;
using UnityEngine;

namespace Assets.codes.system
{
    public class WorldReference : MonoBehaviour
    {
        private Vector3 movement = Vector3.zero;
        public void SetMovement(Vector3 movement)
        {
            this.movement = movement; 
        }

        private void LateUpdate()
        {
            if (movement != Vector3.zero)
            {
                transform.position -= movement * Time.deltaTime;

            }
        }
    }
}