using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.codes.spaceship.modules.controller
{
    internal class Puller_Controller : MonoBehaviour
    {
        [SerializeField]
        private SkinnedMeshRenderer MeshRenderer;
        public void OnToggle(bool toggle)
        {
            if (toggle)
            {
                MeshRenderer.SetBlendShapeWeight(0, 100f);
            }
            else
            {
                MeshRenderer.SetBlendShapeWeight(0, 0f);
            }
        }
    }
}
