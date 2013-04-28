using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DeadlyReentry
{
    public class DeadlyReentry : KSP.Testing.UnitTest
    {
        public DeadlyReentry()
        {
            GameObject ghost = new GameObject("DeadlyReentryGhost", typeof(DeadlyReentryGhost));
            GameObject.DontDestroyOnLoad(ghost);
        }
    }

    public class DeadlyReentryGhost : MonoBehaviour
    {
        AerodynamicsFX afx;

		public float Multiplier = 0.0000000126f;
        public float DensityExponent = 1.5f;
        public float VelocityExponent = 4.5f;

        public float CommandPodLeniency = 0.35f;
        public float EffectVariability = 0.1f;
    
        public float maxheat = 0;
        public float maxheatfail = 0;
        public float reentryTemperature = 0;

        public float HeatDiffusionHalflife = 0.3f; // seconds

        protected bool debugging = false;
        protected bool moreUI = false;
        protected Rect windowPos = new Rect(100, 100, 0, 0);
        protected bool heatEnabled = true;

        public void OnGUI()
        {
            if (debugging)
            {
                windowPos = GUILayout.Window("DeadlyReentry".GetHashCode(), windowPos, DrawWindow, "Deadly Reentry Setup");
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.D))
            {
                debugging = !debugging;
            }

            if (FlightGlobals.ready && (FlightGlobals.ActiveVessel != null))
            {
                Ray ray = new Ray();
				
                if (afx == null)
                {
                    GameObject fx = GameObject.Find("FXLogic");
                    if (fx != null)
                    {
                        afx = fx.GetComponent<AerodynamicsFX>();
                    }
                }

                foreach (var vessel in FlightGlobals.Vessels)
                {
                    if (vessel.packed)
                        continue;

                    float atmDensity = (float)FlightGlobals.getAtmDensity(
                        FlightGlobals.getStaticPressure(vessel.findWorldCenterOfMass()));

                    if (atmDensity < 0.0001f)
                        continue;

                    float airspeed = (float)vessel.srf_velocity.magnitude;
                    
                    reentryTemperature = ReentryTemperature(airspeed, atmDensity);

                    if (heatEnabled && (afx != null) && afx.FxScalar > 0)
                    {
                        foreach (Part p in vessel.Parts)
                        {
                            ray.direction = (p.Rigidbody.GetPointVelocity(p.transform.position) + Krakensbane.GetFrameVelocityV3f() - Krakensbane.GetLastCorrection() * TimeWarp.fixedDeltaTime).normalized;
                            ray.origin = p.transform.position;

							var forwardFacing = !Physics.Raycast(ray, 10);
                            p.temperature = Heating(p, forwardFacing);
                        }
                    }
                }

                if (debugging)
                {
                    maxheat = (from p in FlightGlobals.ActiveVessel.Parts 
                               select p.temperature).Max();
                    maxheatfail = (from p in FlightGlobals.ActiveVessel.Parts 
                                   select p.temperature / p.maxTemp).Max();
                }
            }
        }

        private float ReentryTemperature(float airspeed, float airdensity)
        {
            return Multiplier * Mathf.Pow(airspeed, VelocityExponent) * Mathf.Pow (airdensity, DensityExponent);
        }

        private static float ln_of_2 = Mathf.Log(2);

        private float Heating (Part part, bool forwardFacing)
        {
            var temperature = part.temperature;
            var effectiveReentryTemperature = reentryTemperature;

            if (part.isControlSource)
            {
                // give command pods some leniency, so player can watch the rest 
                // of the craft disintegrate.
                effectiveReentryTemperature *= Mathf.Clamp01(1.0f-CommandPodLeniency);
            }
            else
            {
                // vary rate on parts so they fail at different times. Use uid for repeatability.
                effectiveReentryTemperature *= Mathf.Clamp01(1.0f - ((part.uid&0xff)/255.0f*EffectVariability));
            }

			if (!forwardFacing)
				effectiveReentryTemperature *= .5f;

            if (effectiveReentryTemperature < temperature) 
            {
                // disallow cooling (already stock)
                return temperature;
            }

            // small timestep approximation:
            var dTemp = (effectiveReentryTemperature - temperature) * (TimeWarp.deltaTime / HeatDiffusionHalflife) * ln_of_2;

            // blend in effect
            return temperature + dTemp * Mathf.Clamp01(4 * afx.FxScalar);
        }

        public void DrawWindow(int windowID)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(5, 5, 3, 0);
            buttonStyle.margin = new RectOffset(1, 1, 1, 1);
            buttonStyle.stretchWidth = false;
            buttonStyle.stretchHeight = false;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.wordWrap = false;

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(moreUI ? "-" : "+", buttonStyle))
            {
                moreUI = !moreUI;
            }
            if (GUILayout.Button("X", buttonStyle))
            {
                debugging = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Heat: {0:0.0} %", Mathf.Clamp01(maxheatfail)*100));
            GUILayout.EndHorizontal();

            if (moreUI)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Enabled:", labelStyle);
                if (GUILayout.Button(heatEnabled ? "1" : "0", buttonStyle))
                {
                    heatEnabled = !heatEnabled;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Multiplier:", labelStyle);
                string newMultiplier = GUILayout.TextField(Multiplier.ToString(), GUILayout.MinWidth(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("DensityExponent:", labelStyle);
                string newDensityExponent = GUILayout.TextField(DensityExponent.ToString(), GUILayout.MinWidth(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("VelocityExponent:", labelStyle);
                string newVelocityExponent = GUILayout.TextField(VelocityExponent.ToString(), GUILayout.MinWidth(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("CommandPodLeniency:", labelStyle);
                string newCommandPodLeniency = GUILayout.TextField(CommandPodLeniency.ToString(), GUILayout.MinWidth(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("EffectVariability:", labelStyle);
                string newEffectVariability = GUILayout.TextField(EffectVariability.ToString(), GUILayout.MinWidth(100));
                GUILayout.EndHorizontal();

                GUILayout.Label(string.Format("max heat: {0:0.0}", maxheat));
                GUILayout.Label(string.Format("reentry temp: {0:0.0}", reentryTemperature));

                if (afx != null)
                {
                    GUILayout.Label(string.Format("afx.FxScalar: {0:0.000}", afx.FxScalar));
                    GUILayout.Label(string.Format("afx.state: {0:0.000}", afx.state));
                    GUILayout.Label(string.Format("afx.heatFlux: {0:0.000} M", afx.heatFlux / 1e6));
                    GUILayout.Label(string.Format("afx.airspeed: {0:0.0} m/s", afx.airspeed));
                    GUILayout.Label(string.Format("afx.airPresure: {0:0.0000} atm", FlightGlobals.getStaticPressure ()));
                    GUILayout.Label(string.Format("afx.airDensity: {0:0.0000}", afx.airDensity));
                }

                if (GUI.changed)
                {
                    float newValue;
                    
                    if (float.TryParse(newMultiplier, out newValue))
                    {
                        Multiplier = newValue;
                    }

                    if (float.TryParse(newDensityExponent, out newValue))
                    {
                        DensityExponent = newValue;
                    }

                    if (float.TryParse(newVelocityExponent, out newValue))
                    {
                        VelocityExponent = newValue;
                    }

                    if (float.TryParse(newCommandPodLeniency, out newValue))
                    {
                        CommandPodLeniency = newValue;
                    }

                    if (float.TryParse(newEffectVariability, out newValue))
                    {
                        EffectVariability = newValue;
                    }
                }
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
