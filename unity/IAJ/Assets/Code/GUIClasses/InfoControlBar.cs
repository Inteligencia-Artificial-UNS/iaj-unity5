using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class InfoControlBar : MonoBehaviour
{
	Vector2 scrollPosition;
	GUIStyle styleBlack;

	public const int infoBarWidth = 150;

	void OnGUI () {
		//GUI.Window(0, new Rect(Screen.width - 190, 10, 180, 640), WindowFunction, "Game Control / Info");		
		GUI.Window(0, new Rect(Screen.width - infoBarWidth - 10, 10, infoBarWidth, 640), WindowFunction, "Game Control / Info");		
	}
	
	public GUIStyle timeLabelStyle;
	
	void WindowFunction(int windowId) {				
		GUILayout.BeginVertical();
			GUILayout.Label(SimulationState.getInstance().gameTime.ToString(), timeLabelStyle);
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);
			GUILayout.BeginHorizontal();			
			foreach (Home home in SimulationState.getInstance().homes.Values) {				
				HomePanel(home);				
			}			
			GUILayout.EndHorizontal();
			foreach (AgentState agentState in SimulationState.getInstance().agents.Values) {
				AgentPanel(agentState.agentController);
			}
			GUILayout.EndScrollView();
		GUILayout.EndVertical();
	}



	void HomePanel(Home home) {
		GUIStyle style = new GUIStyle(GUI.skin.box);
		style.normal.background = home.getTexture();			
		GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
				GUILayout.Box("", style, GUILayout.Width(15), GUILayout.Height(15));
				GUILayout.Label(home.content.Count.ToString());
			GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}

GUIStyle getBlackStyle() {
		if (styleBlack == null) {
			styleBlack = new GUIStyle (GUI.skin.box);
			styleBlack.normal.background = Agent.MakeTex (2, 2, Color.black);
		}
		return styleBlack;
	}
		
	void AgentPanel(Agent agent) {
		GUIStyle style = new GUIStyle(GUI.skin.box);
		style.normal.background = agent.getHome ().getTexture ();
		GUILayout.BeginVertical (style);
		GUILayout.BeginVertical(getBlackStyle());	
			//GUILayout.Box(agent._name, GUILayout.Height(100f));
//			GUILayout.BeginVertical (style);
			GUILayout.Box(agent._name, getBlackStyle());			
//			GUILayout.EndVertical ();
			GUILayout.Label("HP: "+agent.life //+ "/"+agent.lifeTotal 
                            + "  " + "XP: "+agent.skill + "  " + "GP: " + agent.gold);
			GUILayout.BeginHorizontal();
			 GUILayout.Label("BP:");
			 Dictionary<Type, int> typeToCount = new Dictionary<Type, int>();
			 Dictionary<Type, Texture> typeToIcon = new Dictionary<Type, Texture>();			 			
			 foreach (EObject obj in agent.backpack) {
				if (typeToCount.ContainsKey(obj.GetType()))
					typeToCount[obj.GetType()] = typeToCount[obj.GetType()] + 1;
				else {
					typeToCount.Add(obj.GetType(), 1);
					typeToIcon.Add(obj.GetType(), obj.getIcon());
				}				
			 }
			foreach (Type type in typeToCount.Keys) {
			GUILayout.BeginHorizontal();
				GUIStyle iconStyle = new GUIStyle();
				iconStyle.margin = new RectOffset(0, 0, 5, 0);				
				GUILayout.Label(typeToIcon[type], iconStyle, GUILayout.Width (10), GUILayout.Height (15));
			    GUIStyle textStyle = new GUIStyle();				
				//textStyle.margin = new RectOffset(0, 10, 5, 0);				
			    textStyle.margin = new RectOffset(0, 0, 5, 0);				
				textStyle.normal.textColor = Color.white;
			    GUILayout.Label(" " + typeToCount[type], textStyle);
			GUILayout.EndHorizontal();
			}
			GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.EndVertical ();
	}
}

