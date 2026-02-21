using UnityEngine;
using System.Collections;

namespace Assets.codes.items
{
	public interface IUsable
	{
        public void OnInteract(PlayerMain who);
    }
}