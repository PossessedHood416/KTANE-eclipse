using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using DeafMath = ExMath;

public class Eclipse : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;

	public KMSelectable[] SunButtons;
	public KMSelectable[] MoonButtons;
	public KMSelectable CenterButton;

	public GameObject SunParent;
	public GameObject MoonParent;

	public Material[] ButtonMats;

	static int ModuleIdCounter = 1;
	int ModuleId;
	private bool ModuleSolved = false;
	private static bool FirstActivation = true;
	private string[] SunDingList = new string[] {"Bh", "B", "C#", "D", "E", "F#", "G", "A"};
	private string[] MoonDingList = new string[] {"Ch", "C", "D", "D#", "F", "G", "G#", "A#"};
	private bool isAni = false;

	int Power = 0;
	float Time = 0f;

	private Vector3 SunAxisA;
	private Vector3 SunAxisB;
	private Vector3 MoonAxisA;
	private Vector3 MoonAxisB;
	private Vector3 Sun3dPos;
	private Vector3 Moon3dPos;

	//radians
	private float SunTheta;
	private float SunPhi;
	private float MoonTheta;
	private float MoonPhi;
	private float ViewTheta;
	private float ViewPhi;

	private float SunDist;
	private float MoonDist;
	private float MoonVeloFactor;


	void Awake () {
		ModuleId = ModuleIdCounter++;
		GetComponent<KMBombModule>().OnActivate += Activate;
		FirstActivation = true; //setup in Activate()
		
		foreach (KMSelectable bu in SunButtons) {
			bu.OnInteract += delegate () { SunPress(bu); return false; };
		}
		
		foreach (KMSelectable bu in MoonButtons) {
			bu.OnInteract += delegate () { MoonPress(bu); return false; };
		}
		
		CenterButton.OnInteract += delegate () { CenterPress(); return false; };
	}

	void SunPress(KMSelectable bu){
		if(isAni) return;
		int i = 0;
		for(i = 0; i < 8; i++) if(SunButtons[i] == bu) break;

		Ding(i);
		if(ModuleSolved) return;

		switch (i) {
			case 0: //ViewTheta +
			ViewTheta += (float)(Math.Pow(3, Power)*(2*Math.PI)/Math.Pow(3,8));
			DisplayRadians(ViewTheta);
			break;

			case 1: //Time +
			Time += (float)Math.Pow(3, Power);
			break;

			case 2: //ViewPhi -
			ViewPhi -= (float)(Math.Pow(3, Power)*(2*Math.PI)/Math.Pow(3,8));
			DisplayRadians(ViewPhi);
			break;

			case 3: //Power -
			Power = Math.Max(Power-1, -8);
			break;

			case 4: //ViewTheta -
			ViewTheta -= (float)(Math.Pow(3, Power)*(2*Math.PI)/Math.Pow(3,8));
			DisplayRadians(ViewTheta);
			break;

			case 5: //Time -
			Time -= (float)Math.Pow(3, Power);
			break;

			case 6: //ViewPhi +
			ViewPhi += (float)(Math.Pow(3, Power)*(2*Math.PI)/	Math.Pow(3,8));
			DisplayRadians(ViewPhi);
			break;

			case 7: //Power +
			Power = Math.Min(Power+1, 7);
			break;

			default: break;
		}
		Recalc();
	}

	void MoonPress(KMSelectable bu){
		if(isAni) return;
		int i = 0;
		for(i = 0; i < 8; i++) if(MoonButtons[i] == bu) break;

		Ding(i+8);
		if(ModuleSolved) return;

		switch (i) {
			case 0: //SunTheta
			DisplayRadians(SunTheta);
			ViewTheta = 0f;
			break;

			case 1: //Time
			DisplayFloat(Time);
			break;

			case 2: //SunPhi
			DisplayRadians(SunPhi);
			ViewPhi = 0f;
			break;

			case 3: //MoonDist
			DisplayFloat(MoonDist);
			break;

			case 4: //MoonTheta
			DisplayRadians(MoonTheta);
			ViewTheta = 0f;
			break;

			case 5: //Power
			DisplayIndex(Power);
			break;

			case 6: //MoonPhi
			DisplayRadians(MoonPhi);
			ViewPhi = 0f;
			break;

			case 7: //SunDist
			DisplayFloat(SunDist);
			break;

			default: break;
		}

		Recalc();
	}

	void CenterPress(){
		if(isAni) return;

		Ding(6);
		Ding(0);
		
		if(ModuleSolved) return;

		Time = 0f;
		Power = 0;
		ViewTheta = 0;
		ViewPhi = 0;
		Recalc();
		DisplayFloat(0f);
	}

	void OnDestroy () {
		
	}

	void Activate () { //Lightson
		if(FirstActivation){
			FirstActivation = false;
			Audio.PlaySoundAtTransform("13", CenterButton.transform);
		}
		isAni = true;
		StartCoroutine(StartAni());
		StartCoroutine(Fluxem(SunParent));
		StartCoroutine(Fluxem(MoonParent));
	}

	void Start () { //Calc
		List<Vector3> vects = new List<Vector3>();

		while(true){
			Vector3 contender = new Vector3(Rnd.Range(-9,10), Rnd.Range(-9,10), Rnd.Range(-9,10));

			//regen if vector too small
			if(Magnitude(contender) < 2f) continue;
			
			//regen if too close to colinear with previous vects
			for(int i = 0; i < vects.Count -1; i++){
				if(Vector3.Angle(contender, vects[i]) < 5f || Vector3.Angle(contender, vects[i]) > 175f) continue;
			}

			vects.Add(contender);
			if(vects.Count == 4) break;
		}

		SunAxisA =  vects[0];
		SunAxisB =  vects[1];
		MoonAxisA = vects[2];
		MoonAxisB = vects[3];
		
		int concatSN = 0;
		foreach (int num in Bomb.GetSerialNumberNumbers()){
			concatSN *= 10;
			concatSN += num;
		}

		while(DeafMath.IsSquare(concatSN)) concatSN++;
		MoonVeloFactor = (float)Math.Pow(concatSN, 0.5f);

		Recalc();
	}

	void Update () {

	}

	void Solve () {
		ModuleSolved = true;

		isAni = true;
		StartCoroutine(EndAni());
	}

	void Strike () {
		GetComponent<KMBombModule>().HandleStrike();
	}

	//===================Display=========================//

	void DisplayRadians (float radian) {
		float bendian = radian /(2*(float)Math.PI) * 6561f;
		DisplayFloat(bendian);
	}

	void DisplayFloat (float num) {
		while(num >= 6561f) num -= 6561f;
		while(num < 0f) num += 6561f;

		string msg = DeafMath.ConvertToBase((int)Math.Floor(num * Math.Pow(3, 8)), 3);
		msg = msg.PadLeft(16, '0');

		DisplayTernary(msg);
	}

	void DisplayIndex (int j) {
		string tern = "";
		for(int i = 7; i >= -8; i--) tern += (i==j)? "2" : "0";
		DisplayTernary(tern);
	}

	void DisplayTernary(string t) {
		for(int i = 0; i < 16; i++){
			if(i < 8) StartCoroutine(FadeButton(SunButtons[i], "012".IndexOf(t[i])));
			else StartCoroutine(FadeButton(MoonButtons[i-8], "012".IndexOf(t[i])));
		}
	}

	//===================Ani / Sound=========================//

	IEnumerator StartAni(){
		yield return new WaitForSeconds(0.1f);
		const float beatTime = 0.78947f;
		for(int i = 0; i < 16; i++){
			if(i < 8) StartCoroutine(FadeButton(SunButtons[i], Rnd.Range(0,3)));
			else StartCoroutine(FadeButton(MoonButtons[i-8], Rnd.Range(0,3)));
			yield return new WaitForSeconds(beatTime);
		}
		isAni = false;
	}

	IEnumerator EndAni(){
		float beatTime = 0.727f;
		DisplayTernary("0000000000000000");
		yield return new WaitForSeconds(beatTime);
		Audio.PlaySoundAtTransform("RulerOfEverything", CenterButton.transform);

		for(int i = 0; i < 8; i++){
			StartCoroutine(FadeButton(SunButtons[i], 2));
			StartCoroutine(FadeButton(MoonButtons[7-i], 1));
			yield return new WaitForSeconds(beatTime);
		}

		for(int i = 0; i < 16; i++){
			if(i < 8) StartCoroutine(FadeButton(SunButtons[i], 3));
			else StartCoroutine(FadeButton(MoonButtons[i-8], 3));
			yield return new WaitForSeconds(beatTime);
		}

		GetComponent<KMBombModule>().HandlePass();
		Debug.LogFormat("[The Eclipse #{0}] Solved.", ModuleId);
		isAni = false;
	}

	IEnumerator FadeButton(KMSelectable kms, int i) {
		double t = 0.01f;
		Transform child = kms.transform.GetChild(0);
		Material tempMat = new Material(child.GetComponent<MeshRenderer>().material);

		Color32 fro = tempMat.color;
		Color32 to = ButtonMats[i].color;

		child.GetComponent<MeshRenderer>().material = tempMat;

		while (t < 0.99f) {
			tempMat.color = Color32.Lerp(fro, to, (float)t);
			t = Math.Pow(t, 0.76f);
			yield return new WaitForSeconds(0.03f);
		}
		tempMat.color = Color32.Lerp(fro, to, 1f);
	}

	IEnumerator Fluxem(GameObject obj, bool isChild = false) {
		yield return null;

		if(!isChild){
			foreach (Transform childTransform in obj.transform){
				GameObject childObj = childTransform.gameObject;
				StartCoroutine(Fluxem(childObj, true));
			}
		}

		Vector3 euler = obj.transform.localEulerAngles;
		float offsBeta = Rnd.Range(0,360)/180.0f * (float)Math.PI;
		float initBeta = euler.y;
		float veloBeta = Rnd.Range(0,12)/1200f;

		while(true){
			offsBeta += veloBeta;
			offsBeta = offsBeta > 2*(float)Math.PI ? offsBeta - 2*(float)Math.PI : offsBeta;
			euler.y = (float)Math.Cos(offsBeta)*3f + initBeta;
			
			obj.transform.localEulerAngles = euler;
			yield return new WaitForSeconds(0.02f);
		}
	}

	void Ding(int i) {
		string note = i < 8 ? SunDingList[i] : MoonDingList[i-8];
		Audio.PlaySoundAtTransform("Ding" + note, CenterButton.transform);
	}

	//===================Calc=========================//

	void Recalc() {
		if(Time < 0f) Time = 0f;

		Sun3dPos = (float)Math.Cos(Time)*SunAxisA + (float)Math.Sin(Time)*SunAxisB;
		Moon3dPos = (float)Math.Cos(Time*MoonVeloFactor)*MoonAxisA + (float)Math.Sin(Time*MoonVeloFactor)*MoonAxisB;

		SunDist = Magnitude(Sun3dPos);
		MoonDist = Magnitude(Moon3dPos);

		Vector3 unifySun = Vector3.Normalize(Sun3dPos);
		Vector3 unifyMoon = Vector3.Normalize(Moon3dPos);

		SunTheta = (float)Math.Asin(unifySun.z);
		SunPhi = (float) Math.Atan2(unifySun.y, unifySun.x);
		MoonTheta = (float)Math.Asin(unifyMoon.z);
		MoonPhi = (float)Math.Atan2(unifyMoon.y, unifyMoon.x);

		if(SunTheta < 0f) SunTheta += 2*(float)Math.PI;
		if(SunPhi < 0f) SunPhi += 2*(float)Math.PI;
		if(MoonTheta < 0f) MoonTheta += 2*(float)Math.PI;
		if(MoonPhi < 0f) MoonPhi += 2*(float)Math.PI;

		if(Math.Abs(Time) < Math.Pow(3, -8)) Time = 0f;

		Debug.LogFormat("//////////////////////////////////////////");
		Debug.LogFormat("Time: {0}, Power {1}", Time, Power);
		Debug.LogFormat("Sun3dPos {0}, SunDist {1}", Sun3dPos, SunDist);
		Debug.LogFormat("Moon3dPos {0}, MoonDist {1}", Moon3dPos, MoonDist);
		Debug.LogFormat("SunTheta {0}, SunPhi {1}", SunTheta, SunPhi);
		Debug.LogFormat("MoonTheta {0}, MoonPhi {1}", MoonTheta, MoonPhi);
		Debug.LogFormat("ViewTheta {0}, ViewPhi {1}", ViewTheta, ViewPhi);

		if(CheckAns()) Solve();
	}

	float Magnitude(Vector3 n) {
		return (float)Math.Pow(n.x*n.x + n.y*n.y + n.z*n.z, 0.5f);
	}

	bool CheckAns() {

		float[] checkList = new float[] {
			SunTheta - MoonTheta,
			SunTheta - ViewTheta,
			MoonTheta - ViewTheta,
			SunPhi - MoonPhi,
			SunPhi - ViewPhi,
			MoonPhi - ViewPhi
		};

		foreach(float c in checkList){
			float f = c;
			f = Math.Abs(f);
			
			while(f < 0f) f += 2*(float)Math.PI;
			while(f > 2*Math.PI) f -= 2*(float)Math.PI;

			if(f > Math.PI) f = 2*(float)Math.PI - f;

			if(f > 0.01f) return false;
		}
		return true;
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string Command) {
		yield return null;
		Solve();
	}

	IEnumerator TwitchHandleForcedSolve () {
		yield return null;
	}
}
