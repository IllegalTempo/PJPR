using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

namespace Assets.codes.spaceship.modules
{
	public class Booster: Module<int>
	{
		private float spl = 100;
		[SerializeField]
		private ParticleSystem SpeedParticles;
		protected override void ModuleUpdate()
		{
            MainSpaceship.Instance.AddNonCentralForce(transform.forward * GetModuleData() * spl, transform.position);


        }
		protected override void OnDataChanged(int newData)
		{
			base.OnDataChanged(newData);
			if (SpeedParticles == null)
				return;

			ParticleSystem.EmissionModule emission = SpeedParticles.emission;
			emission.rateOverTime = Mathf.Pow(10,newData); // Adjust the multiplier as needed
        }



    }
}
