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
        private GameObject lever;

        [SerializeField]
        private UnityEvent<int> OnChangeSpeed;

        [SerializeField]
        private float rotationSpeed = 5f;

        private float maxRotationX = 145f;
        private float minRotationX = 22f;
        private Coroutine rotationCoroutine;

        public void OnInteract(PlayerMain who)
        {
            level = (level + 1) % maxlevel;
            OnChangeSpeed.Invoke(level);
            UpdateLeverRotation();
        }

        private void UpdateLeverRotation()
        {
            if (lever == null) return;

            if (rotationCoroutine != null)
            {
                StopCoroutine(rotationCoroutine);
            }

            rotationCoroutine = StartCoroutine(RotateLeverCoroutine());
        }

        private IEnumerator RotateLeverCoroutine()
        {
            float targetRotationX = Mathf.Lerp(maxRotationX, minRotationX, level / maxlevel);
            Quaternion targetRotation = Quaternion.Euler(targetRotationX, lever.transform.localEulerAngles.y, lever.transform.localEulerAngles.z);

            while (Quaternion.Angle(lever.transform.localRotation, targetRotation) > 0.01f)
            {
                lever.transform.localRotation = Quaternion.Lerp(lever.transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
                yield return null;
            }

            lever.transform.localRotation = targetRotation;
            rotationCoroutine = null;
        }
    }
}