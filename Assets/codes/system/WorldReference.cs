using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.codes.system
{
    public class WorldReference : MonoBehaviour
    {

        private Vector3 velocity;
        private Vector3 rotation;

        public static WorldReference Instance;
        private void OnEnable()
        {
            if (Instance != null)
            {
                Debug.LogError("Multiple instances of WorldReference detected. There should only be one instance.");
                Destroy(this);
                return;
            }
            Instance = this;


        }
        private void FixedUpdate()
        {
            //rb.linearVelocity = velocity;
            Quaternion deltaRotation = Quaternion.Euler(rotation * Time.fixedDeltaTime);
            //rb.MoveRotation(rb.rotation * deltaRotation);

        }
        public void SetVelocity(Vector3 velocity)
        {
            this.velocity = velocity;
        }
        public void SetRotation(Vector3 rotation)
        {
            this.rotation = rotation;
        }
    }
}
