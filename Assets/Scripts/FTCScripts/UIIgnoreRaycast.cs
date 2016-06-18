using UnityEngine;
using System.Collections;

namespace FTC
{
	public class UIIgnoreRaycast : MonoBehaviour, ICanvasRaycastFilter 
	{
		public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
		{
			return false;
		}
	}
}
