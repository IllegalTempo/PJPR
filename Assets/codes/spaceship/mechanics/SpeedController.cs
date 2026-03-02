using UnityEngine;
using System.Collections;
using Assets.codes.items;
using UnityEngine.Events;

namespace Assets.codes.spaceship.mechanics
{
    public class SpeedController : Selectable, IUsable
    {
        [SerializeField]
        private int maxlevel = 6;
        private int level = 0;

        [SerializeField]
        private UnityEvent<int> OnChangeSpeed;
        public void OnInteract(PlayerMain who)
        {
            level = (level + 1) % maxlevel;
            OnChangeSpeed.Invoke(level);

        }
    }
}