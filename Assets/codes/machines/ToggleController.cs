using UnityEngine;
using UnityEngine.Events;

namespace Assets.codes.machines
{
    public class ToggleController : SyncedMachine
    {
        [SerializeField] private bool isOn;

        public UnityEvent<bool> onToggleChanged;

        public bool IsOn => isOn;

        protected override void Start()
        {
            base.Start();
        }

        protected override void ShareActionOnInteract_press(PlayerMain who)
        {
            base.ShareActionOnInteract_press(who);
            SetToggleState(!isOn);
        }

        public void SetToggleState(bool newState)
        {
            if (isOn == newState)
            {
                return;
            }

            isOn = newState;
            onToggleChanged?.Invoke(isOn);
        }

        public void Toggle()
        {
            SetToggleState(!isOn);
        }

    }
}
