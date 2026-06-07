using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.codes.system
{
    public class WorldReference : MonoBehaviour
    {
        private Vector3 movement = Vector3.zero;
        private Dictionary<int,Vector3> SourceToVelocity = new Dictionary<int,Vector3>();
        public void UpdateSourceVelocity(int sourceModuleInstanceID, Vector3 newVelocity)
        {
            SourceToVelocity[sourceModuleInstanceID] = newVelocity;
            movement = calculateFinalVelocity();
        }
        private Vector3 calculateFinalVelocity()
        {
            Vector3 sum = Vector3.zero;
            foreach(Vector3 v in SourceToVelocity.Values)
            {
                sum += v;
            }
            return sum;
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