//v1.4.0

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

	private int Power = 0;
	private int LargeTime = 0; // seconds * 3^8

	private Vector3Double SunAxisA;
	private Vector3Double SunAxisB;
	private Vector3Double MoonAxisA;
	private Vector3Double MoonAxisB;
	private Vector3Double Sun3dPos;
	private Vector3Double Moon3dPos;

	//radians
	private double SunTheta;
	private double SunPhi;
	private double MoonTheta;
	private double MoonPhi;
	private double ViewTheta;
	private double ViewPhi;

	private double SunDist;
	private double MoonDist;
	private double SunVeloFactor;
	private double MoonVeloFactor;

	//==========================================//

	private class Vector3Double {
		public double x;
		public double y;
		public double z;
		public Vector3Double(double passx, double passy, double passz){
			this.x = passx;
			this.y = passy;
			this.z = passz;
		}
		public Vector3Double(Vector3Double former){
			this.x = former.x;
			this.y = former.y;
			this.z = former.z;
		}
		public double GetMagnitude() {
			return Math.Pow(this.x*this.x + this.y*this.y + this.z*this.z, 0.5f);
		}
		public Vector3Double GetNormalized() {
			double mg = this.GetMagnitude();
			return new Vector3Double(this.x / mg,  this.y / mg, this.z / mg);
		}

		public static Vector3Double operator *(double scale, Vector3Double former){
			return new Vector3Double(scale*former.x, scale*former.y, scale*former.z);
		}
		public static Vector3Double operator +(Vector3Double left, Vector3Double right){
			return new Vector3Double(left.x + right.x, left.y + right.y, left.z + right.z);
		}
		public static double DotProduct(Vector3Double a, Vector3Double b){
			return a.x*b.x + a.y*b.y + a.z*b.z;
		}
		public static double Angle(Vector3Double a, Vector3Double b){
			return Math.Acos(Vector3Double.DotProduct(a, b) / a.GetMagnitude() / b.GetMagnitude());
		}
		public override string ToString() {
			return "(" + this.x + ", " + this.y + ", " + this.z + ")";
		}
	}

	//==========================================//

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
			ViewTheta += (Math.Pow(3, Power)*(2*Math.PI)/Math.Pow(3,8));
			DisplayRadians(ViewTheta);
			break;

			case 1: //Time +
			LargeTime += (int)Math.Pow(3, Power + 8);
			break;

			case 2: //ViewPhi -
			ViewPhi -= (Math.Pow(3, Power)*(2*Math.PI)/Math.Pow(3,8));
			DisplayRadians(ViewPhi);
			break;

			case 3: //Power -
			Power = Math.Max(Power-1, -8);
			break;

			case 4: //ViewTheta -
			ViewTheta -= (Math.Pow(3, Power)*(2*Math.PI)/Math.Pow(3,8));
			DisplayRadians(ViewTheta);
			break;

			case 5: //Time -
			LargeTime -= (int)Math.Pow(3, Power + 8);
			break;

			case 6: //ViewPhi +
			ViewPhi += (Math.Pow(3, Power)*(2*Math.PI)/	Math.Pow(3,8));
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
			ViewTheta = 0d;
			break;

			case 1: //Time
			DisplayLargeInt(LargeTime);
			break;

			case 2: //SunPhi
			DisplayRadians(SunPhi);
			ViewPhi = 0d;
			break;

			case 3: //MoonDist
			DisplayDouble(MoonDist);
			break;

			case 4: //MoonTheta
			DisplayRadians(MoonTheta);
			ViewTheta = 0d;
			break;

			case 5: //Power
			DisplayIndex(Power);
			break;

			case 6: //MoonPhi
			DisplayRadians(MoonPhi);
			ViewPhi = 0d;
			break;

			case 7: //SunDist
			DisplayDouble(SunDist);
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

		LargeTime = 0;
		Power = 0;
		ViewTheta = 0d;
		ViewPhi = 0d;
		Recalc();
		DisplayTernary("0000000000000000");
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
		List<Vector3Double> vects = new List<Vector3Double>();

		while(true){
			Vector3Double contender = new Vector3Double(Rnd.Range(-9,10), Rnd.Range(-9,10), Rnd.Range(-9,10));

			//regen if vector too small
			if(contender.GetMagnitude() < 2d) continue;
			
			//regen if too close to colinear with previous vects
			for(int i = 0; i < vects.Count -1; i++){
				if(Vector3Double.Angle(contender, vects[i]) < 0.2d || Vector3Double.Angle(contender, vects[i]) > 6.1d) continue;
			}

			vects.Add(contender);
			if(vects.Count == 4) break;
		}

		SunAxisA =  vects[0];
		SunAxisB =  vects[1];
		MoonAxisA = vects[2];
		MoonAxisB = vects[3];
		
		int moonvelosqr = Rnd.Range(4,9);
		while(DeafMath.IsSquare(moonvelosqr)) moonvelosqr++;
		MoonVeloFactor = Math.Pow(moonvelosqr, 0.5f);
		SunVeloFactor = Rnd.Range(6,15)/8.0d;

		Debug.LogFormat("<The Eclipse #{0}> Body A axis C: {1}", ModuleId, SunAxisA);
		Debug.LogFormat("<The Eclipse #{0}> Body A axis D: {1}", ModuleId, SunAxisB);
		Debug.LogFormat("<The Eclipse #{0}> Body B axis C: {1}", ModuleId, MoonAxisA);
		Debug.LogFormat("<The Eclipse #{0}> Body B axis D: {1}", ModuleId, MoonAxisB);
		Debug.LogFormat("<The Eclipse #{0}> Body A velocity factor: {1}", ModuleId, SunVeloFactor);
		Debug.LogFormat("<The Eclipse #{0}> Body B velocity factor: {1}", ModuleId, MoonVeloFactor);
		
		Recalc();
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

	void DisplayRadians (double radian) {
		double bendian = radian /(2*Math.PI) * 6561d;
		DisplayDouble(bendian);
	}

	void DisplayDouble (double num) {
		while(num >= 6561d) num -= 6561d;
		while(num < 0d) num += 6561d;

		string msg = DeafMath.ConvertToBase((int)Math.Floor(num * Math.Pow(3, 8)), 3);
		msg = msg.PadLeft(16, '0');
		DisplayTernary(msg);
	}

	void DisplayLargeInt (int num){
		string msg = DeafMath.ConvertToBase(num, 3);
		msg = msg.PadLeft(16, '0');
		msg = msg.Substring(msg.Length - 16);
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
			if(i < 8) StartCoroutine(FadeButton(SunButtons[i], 2 - (i%2) ));
			else StartCoroutine(FadeButton(MoonButtons[i-8], 1 - (i%2) ));
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
		if(LargeTime < 0) LargeTime = 0;
		double time = LargeTime / Math.Pow(3, 8);

		double sunApparentTime = time;
		double moonApparentTime = time;

		while(sunApparentTime >= 2*Math.PI/SunVeloFactor) sunApparentTime -= (2*Math.PI/SunVeloFactor);
		while(moonApparentTime >= 2*Math.PI/MoonVeloFactor) moonApparentTime -= (2*Math.PI/MoonVeloFactor);

		Sun3dPos  = Math.Cos(time*SunVeloFactor )*SunAxisA  + Math.Sin(time*SunVeloFactor )*SunAxisB;
		Moon3dPos = Math.Cos(time*MoonVeloFactor)*MoonAxisA + Math.Sin(time*MoonVeloFactor)*MoonAxisB;

		SunDist  = Sun3dPos.GetMagnitude();
		MoonDist = Moon3dPos.GetMagnitude();

		Vector3Double unifySun  = Sun3dPos.GetNormalized();
		Vector3Double unifyMoon = Moon3dPos.GetNormalized();

		SunTheta  = Math.Asin(unifySun.z);
		SunPhi    = Math.Atan2(unifySun.y, unifySun.x);
		MoonTheta = Math.Asin(unifyMoon.z);
		MoonPhi   = Math.Atan2(unifyMoon.y, unifyMoon.x);

		if(SunTheta < 0f) SunTheta += 2*Math.PI;
		if(SunPhi < 0f) SunPhi += 2*Math.PI;
		if(MoonTheta < 0f) MoonTheta += 2*Math.PI;
		if(MoonPhi < 0f) MoonPhi += 2*Math.PI;

		if(CheckAns()) Solve();
	}

	bool CheckAns() {
		double[] checkList = new double[] {
			SunTheta - MoonTheta,
			SunTheta - ViewTheta,
			MoonTheta - ViewTheta,
			SunPhi - MoonPhi,
			SunPhi - ViewPhi,
			MoonPhi - ViewPhi
		};

		foreach(double c in checkList){
			double f = c;
			f = Math.Abs(f);
			
			while(f < 0f) f += 2*Math.PI;
			while(f > 2*Math.PI) f -= 2*Math.PI;

			if(f > Math.PI) f = 2*Math.PI - f;

			if(f > 0.05d) return false;
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
