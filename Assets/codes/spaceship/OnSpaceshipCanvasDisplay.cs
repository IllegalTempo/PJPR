using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

namespace Assets.codes.spaceship
{
	public class OnSpaceshipCanvasDisplay: MonoBehaviour
	{
		[SerializeField]
		private TMP_Text waterAmount;
		[SerializeField]
		private Slider Weight;
		public void SetWaterAmount(int newvalue)
		{
			waterAmount.text = newvalue.ToString();
		}
		public void SetWeight(float newvalue) //0.5 = neutral, 0 = left, 1 = right
        {
            Weight.value = Mathf.Clamp01(newvalue);
        }
	}
}
