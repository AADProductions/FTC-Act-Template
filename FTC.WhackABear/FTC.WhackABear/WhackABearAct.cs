using System;
using UnityEngine;
using FTC.Core;

namespace FTC.WhackABear
{
	public class WhackABearAct : FtcAct
	{
		public override void Update ()
		{
			base.Update ();
		}

		public override void PrepareForScoring ()
		{
			base.PrepareForScoring ();
		}

		public override void LookForObjects ()
		{
			base.LookForObjects ();
		}

		public override PlayerScore GetActBannerScore ()
		{
			return base.GetActBannerScore ();
		}

		public override System.Collections.IEnumerator ActWaitForTutorials ()
		{
			return base.ActWaitForTutorials ();
		}

		public override System.Collections.IEnumerator ActUpdateOverTime ()
		{
			return base.ActUpdateOverTime ();
		}

		public override void ActSetup ()
		{
			base.ActSetup ();
		}

		public override System.Collections.IEnumerator ActIntroduceOverTime ()
		{
			return base.ActIntroduceOverTime ();
		}

		public override System.Collections.IEnumerator ActFinishOverTime ()
		{
			return base.ActFinishOverTime ();
		}
	}
}

