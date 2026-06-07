using UnityEngine;
using System.Collections;

namespace Assets.codes.spaceship.modules
{
	public class booster: module
	{
		private int speedlevel = 0;
		private float spl = 1;
		public void setSpeedLevel(int slv)
		{
			speedlevel = slv;
			GameCore.Instance.WorldReference.UpdateSourceVelocity(gameObject.GetInstanceID(), transform.forward * spl * speedlevel);
        }
		public override void ModuleUpdate()
		{
			

        }
	}
}