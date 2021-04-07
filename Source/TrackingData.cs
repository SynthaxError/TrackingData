using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrackingData
{
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class TrackingData : MonoBehaviour
    {
        public static TrackingData Instance;
        bool show = true;
        int windowId;
        int toolbarSelect = 0;
        Rect windowRect = new Rect();
        Vector2 scrollView = new Vector2(0.0f, 0.0f);

        MapObject target;
        float trackingLevel;
        Vessel.ControlLevel controlLevel;

        TrackingData()
        {
            if (Instance != null && Instance.GetInstanceID() != this.GetInstanceID()) Destroy(Instance);
            Instance = this;
        }

#pragma warning disable IDE0051
        void Start()
        {
            windowId = this.GetInstanceID();
            GameEvents.onPlanetariumTargetChanged.Add(TargetChange);
            GameEvents.onGameSceneLoadRequested.Add(CloseWindow);
            trackingLevel = ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.TrackingStation);
        }
        void OnDestroy()
        {
            GameEvents.onPlanetariumTargetChanged.Remove(TargetChange);
            GameEvents.onGameSceneLoadRequested.Remove(CloseWindow);
            Instance = null;
        }
        void OnGUI()
        {
            if (!show || !IsVessel(target)) return;
            controlLevel = target.vessel.connection.GetControlLevel();
            GUI.skin = HighLogic.Skin;
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "Tracking Data", GUILayout.Width(300.0f), GUILayout.Height(1.0f));
        }
#pragma warning restore IDE0051

        private void CloseWindow(GameScenes data)
        {
            show = false;
        }

        void TargetChange(MapObject obj)
        {
            target = obj;
        }

        bool IsVessel(MapObject target)
        {
            return target != null && target.type == MapObject.ObjectType.Vessel && target.vessel != null;
        }

        void DrawWindow(int id)
        {
            Utils.TwoValuesLabel("Name:", target.GetName());
            Utils.TwoValuesLabel("Type:", Enum.GetName(typeof(VesselType), target.vessel.vesselType));
            toolbarSelect = GUILayout.Toolbar(toolbarSelect, new string[] { "Crew", "Resources" });
            if ((int)controlLevel > 0)
            {
                switch (toolbarSelect)
                {
                    case 0:
                        if (trackingLevel > 0.0f) ShowCrewData();
                        else TrackingLevelUpgradeMessage();
                        break;
                    case 1:
                        if (trackingLevel > 0.5f) ShowResourcesData();
                        else TrackingLevelUpgradeMessage();
                        break;
                }
            }
            else GUILayout.Label("Unable to establish connection with the craft.", Utils.LabelStyle(FontStyle.Normal, Color.yellow));
            GUI.DragWindow();
        }
        void TrackingLevelUpgradeMessage()
        {
            GUILayout.Label("Upgrade Tracking Station to access this data.", Utils.LabelStyle(FontStyle.Normal, Color.yellow));
        }
        void ShowCrewData()
        {
            int crewCount = target.vessel.GetCrewCount();
            if (crewCount < 1)
            {
                GUILayout.Label("There's no crew on this craft.");
                return;
            }
            scrollView = GUILayout.BeginScrollView(scrollView, GUILayout.Height(200.0f));
            {
                foreach (ProtoCrewMember kerbal in target.vessel.GetVesselCrew())
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(kerbal.name);
                        if ((int)controlLevel > 1) GUILayout.Label($"({kerbal.type}{(kerbal.type == ProtoCrewMember.KerbalType.Tourist ? "" : $", {kerbal.trait}")})", Utils.LabelStyle(FontStyle.Normal, Color.white));
                        else GUILayout.Label("(???)", Utils.LabelStyle(FontStyle.Normal, Color.white));
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }
        void ShowResourcesData()
        {
            List<VesselResource> resources = GetVesselResources();
            scrollView = GUILayout.BeginScrollView(scrollView, GUILayout.Height(200.0f));
            {
                foreach (VesselResource res in resources)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(res.name);
                        if ((int)controlLevel > 2) GUILayout.Label($"{Math.Round(res.amount, 3)}/{Math.Round(res.maxAmount, 3)} ({Math.Round(res.amount / res.maxAmount * 100.0, 1)}%)", Utils.LabelStyle(FontStyle.Normal, Color.white));
                        else GUILayout.Label("???/??? (???%)", Utils.LabelStyle(FontStyle.Normal, Color.white));
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        List<VesselResource> GetVesselResources()
        {
            ProtoVessel pvsl = target.vessel.protoVessel;
            List<VesselResource> list = new List<VesselResource>();
            foreach (ProtoPartSnapshot part in pvsl.protoPartSnapshots)
            {
                foreach (ProtoPartResourceSnapshot resource in part.resources)
                {
                    VesselResource vres = list.FirstOrDefault(r => r.name == resource.resourceName);
                    if (vres == default(VesselResource))
                    {
                        vres = new VesselResource(resource.resourceName);
                        list.Add(vres);
                    }
                    vres.UpdateAmounts(resource);
                }
            }
            return list;
        }
    }

    class VesselResource
    {
        public string name;
        public double amount, maxAmount = 0.0;
        public VesselResource(string resourceName)
        {
            name = resourceName;
        }
        public void UpdateAmounts(ProtoPartResourceSnapshot snapshot)
        {
            amount += snapshot.amount;
            maxAmount += snapshot.maxAmount;
        }
    }
}
