using UnityEngine;
using System.Collections;
using System;

namespace Assets.codes.system
{
	/// <summary>
	/// Attach this to a empty gameobject, when starting a mission, instantiate that object
	/// Inherit from this for all mission managers
	/// </summary>
	public abstract class MissionController: MonoBehaviour
	{
		[SerializeField]
		private string missionName;
		[SerializeField]
		private string winConditionText;
		public virtual void StartMission()
		{

		}
		protected virtual void onMissionComplete()
		{

		}

		
	}

    [Obsolete("Use MissionController for mission-specific runtime behaviour. The global voting flow uses the root MissionManager class in Assets/codes/missions.")]
	public class MissionManager: MissionController
	{
	}
}
