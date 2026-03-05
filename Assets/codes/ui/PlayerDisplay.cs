using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

namespace Assets.codes.ui
{
	public class PlayerDisplay: MonoBehaviour
	{
		[SerializeField]
		private Image playerIcon;
		[SerializeField]
		private TMP_Text playerName;
		public void Init(Sprite playericon, string playername)
		{
			playerIcon.sprite = playericon;
			playerName.text = playername;
        }
	}
}