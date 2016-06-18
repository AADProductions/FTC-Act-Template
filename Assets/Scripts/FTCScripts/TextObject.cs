using UnityEngine;
using System.Collections;
using FTC.Interfaces;
using UnityEngine.UI;

namespace FTC {
	public class TextObject : MonoBehaviour, ITextObject {

		public string Text {
			get {
				if (t == null) {
					t = GetComponent <Text> ();
				}
				return t.text;
			}
			set {
				if (t == null) {
					t = GetComponent <Text> ();
				}
				t.text = value;
			}
		}
		public bool Enabled {
			get {
				if (t == null) {
					t = GetComponent <Text> ();
				}
				return t.enabled;
			}
			set {
				if (t == null) {
					t = GetComponent <Text> ();
				}
				t.enabled = value;
			}
		}
		Text t;
	}
}