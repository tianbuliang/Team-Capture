﻿using UnityEngine;

namespace Tweens
{
	/// <summary>
	/// The base for a tween event
	/// </summary>
	public class TweenEvent : ScriptableObject
	{
		/// <summary>
		/// The duration of the tween
		/// </summary>
		public float duration;

		/// <summary>
		/// Will the gameobject being tweened be active on end
		/// </summary>
		public bool activeOnEnd = true;
	}
}