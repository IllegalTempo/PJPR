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
		public void SetWaterAmount(int newvalue)
		{
			waterAmount.text = newvalue.ToString();
		}
	}
}
