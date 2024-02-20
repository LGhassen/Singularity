using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime;
using KSP;
using KSP.IO;
using UnityEngine;

namespace Singularity
{
	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
	public class SingularityUI: MonoBehaviour
	{
		String[] selStrings;
		
		bool uiVisible = false;
		Rect windowRect = new Rect (0, 0, 400, 50);
		int windowId;
		int selGridInt = 0;
		bool editing = false;
		string nodeText = "";
		private Vector2 scrollPosition = new Vector2();
		
		public SingularityUI ()
		{

		}

		public void Awake()
		{
			windowId = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

			// Wait for the main plugin to be initialized
			StartCoroutine (DelayedInit ());
		}
		
		IEnumerator DelayedInit()
		{
			for (int i=0; i<6; i++)
			{
				yield return new WaitForFixedUpdate ();
			}
			
			Init();
		}

		void Init()
		{
			selStrings = new string[Singularity.Instance.loadedObjects.Count];
			
			for (int i=0; i<Singularity.Instance.loadedObjects.Count; i++)
			{
				selStrings[i]=Singularity.Instance.loadedObjects[i].name;
			}
		}
		
		void OnGUI ()
		{
			if (uiVisible)
			{
				windowRect = GUILayout.Window (windowId, windowRect, DrawWindow,"Singularity 0.91");
				
				//prevent window from going offscreen
				windowRect.x = Mathf.Clamp(windowRect.x,0,Screen.width-windowRect.width);
				windowRect.y = Mathf.Clamp(windowRect.y,0,Screen.height-windowRect.height);
			}
		}
		
		internal void Update()
		{
			if (GameSettings.MODIFIER_KEY.GetKey () && Input.GetKeyDown (KeyCode.S))
			{
				uiVisible = !uiVisible;
			}
		}
		
		public void DrawWindow (int windowId)
		{
			if (Singularity.Instance.loadedObjects.Count != 0)
			{
				GUILayout.BeginVertical ();
				GUILayout.Label ("Choose singularity to edit");
				selGridInt = GUILayout.SelectionGrid (selGridInt, selStrings, 1);
				if (GUILayout.Button ("Edit Selected"))
				{
					if (!ReferenceEquals (Singularity.Instance.loadedObjects[selGridInt], null))
					{
						nodeText = string.Copy (Utils.WriteRootNode(ConfigNode.CreateConfigFromObject(Singularity.Instance.loadedObjects[selGridInt])));
						editing = true;
					}
				}
				GUILayout.EndVertical ();
				
				if (editing)
				{
					scrollPosition = GUILayout.BeginScrollView (scrollPosition, false, true, GUILayout.MinHeight (300));				
					nodeText = GUILayout.TextArea (nodeText, GUILayout.ExpandWidth (true), GUILayout.ExpandHeight (true));
					GUILayout.EndScrollView ();
					
					GUILayout.BeginHorizontal ();
					
					if (GUILayout.Button ("Reimport"))
					{
						nodeText = string.Copy (Utils.WriteRootNode(ConfigNode.CreateConfigFromObject(Singularity.Instance.loadedObjects[selGridInt])));
					}
					
					if (GUILayout.Button ("Apply"))
					{
						ConfigNode node = ConfigNode.Parse (nodeText);
						Singularity.Instance.loadedObjects[selGridInt].ApplyFromUI(node);
					}

					if (GUILayout.Button ("Copy to clipboard"))
					{
						GUIUtility.systemCopyBuffer = nodeText;
					}

					if (GUILayout.Button ("Print to Log"))
					{
						Utils.LogInfo("Config print to log:\r\n"+nodeText);
					}
					
					GUILayout.EndHorizontal ();
				}
			}
			else
			{
				GUILayout.Label ("No Singularity objects loaded");
			}
			
			GUI.DragWindow();
		}
	}
}

;