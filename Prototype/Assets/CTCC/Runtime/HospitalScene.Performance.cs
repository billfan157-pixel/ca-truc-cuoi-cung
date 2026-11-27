using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    public sealed partial class HospitalScene
    {
        private enum RenderBudgetBucket
        {
            None = -1,
            Exterior = 0,
            Reception = 1,
            Corridor = 2,
            WardRoom = 3,
            Emergency = 4,
            UpperFloor = 5,
            Basement = 6
        }

        [Flags]
        private enum CampusLightMask
        {
            None = 0,
            Exterior = 1 << 0,
            Reception = 1 << 1,
            Corridor = 1 << 2,
            Ward = 1 << 3,
            Emergency = 1 << 4,
            Upper = 1 << 5,
            AllAboveGround = Exterior | Reception | Corridor | Ward | Emergency | Upper
        }

        private struct TrackedCampusLight
        {
            public ZoneRegion Owner;
            public Light Light;
            public CampusLightMask AllowedBuckets;
            public float OriginalIntensity;
            public float CurrentFactor;
        }

        private struct TrackedShadowLight
        {
            public ZoneRegion Owner;
            public Light Light;
            public LightShadows OriginalShadows;
            public float OriginalIntensity;
            public float CurrentFactor;
            public bool IsCanopy;
            public bool IsWardWindow;
            public bool IsCorridorSpill;
        }

        private struct TrackedExteriorParticle
        {
            public ZoneRegion Owner;
            public ParticleSystemRenderer Renderer;
        }

        private readonly List<TrackedCampusLight> trackedCampusLights = new();
        private readonly List<TrackedShadowLight> trackedShadowLights = new();
        private readonly List<TrackedExteriorParticle> trackedSouthExteriorParticles = new();
        private RenderBudgetBucket activeRenderBucket = RenderBudgetBucket.None;
        private bool performanceBudgetSubscribed;
        private bool shadowsAblated;
        private Light cachedDirectionalMoon;
        private LightShadows directionalMoonOrigShadows = LightShadows.Soft;
        private float directionalMoonOrigIntensity = 1.38f;
        private float directionalMoonFactor = 1f;
        private FirstPerson cachedPlayer;
        private Vector3 lastBudgetCamPos;
        private bool hasLastBudgetCamPos;
        private int lastAppliedShadowTierQuality = -1;

        internal void SetShadowsAblated(bool ablated)
        {
            shadowsAblated = ablated;
            if (ablated)
            {
                for (int i = 0; i < trackedShadowLights.Count; i++)
                {
                    var light = trackedShadowLights[i].Light;
                    if (light != null) light.shadows = LightShadows.None;
                }
                if (cachedDirectionalMoon != null)
                    cachedDirectionalMoon.shadows = LightShadows.None;
            }
            else
            {
                if (cachedDirectionalMoon != null)
                    cachedDirectionalMoon.shadows = directionalMoonOrigShadows;
                for (int i = 0; i < trackedShadowLights.Count; i++)
                {
                    var entry = trackedShadowLights[i];
                    if (entry.Light != null) entry.Light.shadows = entry.OriginalShadows;
                }
                activeRenderBucket = RenderBudgetBucket.None;
            }
        }

        private void SubscribeRenderBudget()
        {
            if (performanceBudgetSubscribed || !Application.isPlaying) return;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRenderingBudget;
            performanceBudgetSubscribed = true;
        }

        private void UnsubscribeRenderBudget()
        {
            if (performanceBudgetSubscribed)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRenderingBudget;
                performanceBudgetSubscribed = false;
            }
        }

        private void OnBeginCameraRenderingBudget(ScriptableRenderContext context, Camera cam)
        {
            if (cam == null || cam.cameraType != CameraType.Game || !cam.CompareTag("MainCamera"))
                return;
            UpdateRenderBudgetForPosition(cam.transform.position);
        }

        private void RegisterPerformanceEntries(ZoneRegion region)
        {
            if (region == null || !Application.isPlaying) return;

            if (string.Equals(region.zoneId, "CAMPUS_EXT", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var light in region.GetComponentsInChildren<Light>(true))
                {
                    if (light == null || light.type == LightType.Directional) continue;
                    TightenCampusLightRange(light);
                    trackedCampusLights.Add(new TrackedCampusLight
                    {
                        Owner = region,
                        Light = light,
                        AllowedBuckets = ClassifyCampusLight(light),
                        OriginalIntensity = light.intensity,
                        CurrentFactor = 1f
                    });
                }
            }
            else if (string.Equals(region.zoneId, "LOC01", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var light in region.GetComponentsInChildren<Light>(true))
                {
                    if (light == null || light.shadows == LightShadows.None) continue;
                    if (light.name.IndexOf("canopy", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        trackedShadowLights.Add(new TrackedShadowLight
                        {
                            Owner = region,
                            Light = light,
                            OriginalShadows = light.shadows,
                            OriginalIntensity = light.intensity,
                            CurrentFactor = 1f,
                            IsCanopy = true
                        });
                    }
                }

                foreach (var psr in region.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (psr == null) continue;
                    if (psr.transform.position.z < 0f)
                    {
                        trackedSouthExteriorParticles.Add(new TrackedExteriorParticle
                        {
                            Owner = region,
                            Renderer = psr
                        });
                    }
                }
            }
            else if (string.Equals(region.zoneId, "LOC03", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var light in region.GetComponentsInChildren<Light>(true))
                {
                    if (light == null) continue;
                    if (light.name.IndexOf("Courtyard distant amber glow", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (light.range > 10.5f) light.range = 10.5f;
                    }
                    if (light.shadows == LightShadows.None) continue;

                    bool isWindow = light.name.IndexOf("Window key moonlight", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isSpill = light.name.IndexOf("Corridor doorway spill", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!isWindow && !isSpill) continue;

                    trackedShadowLights.Add(new TrackedShadowLight
                    {
                        Owner = region,
                        Light = light,
                        OriginalShadows = light.shadows,
                        OriginalIntensity = light.intensity,
                        CurrentFactor = 1f,
                        IsWardWindow = isWindow,
                        IsCorridorSpill = isSpill
                    });
                }

                foreach (var decal in region.GetComponentsInChildren<DecalProjector>(true))
                {
                    if (decal != null && decal.drawDistance > 16f)
                        decal.drawDistance = 16f;
                }
            }
            else if (string.Equals(region.zoneId, "LOC02", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(region.zoneId, "LOC04", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var decal in region.GetComponentsInChildren<DecalProjector>(true))
                {
                    if (decal != null && decal.drawDistance > 16f)
                        decal.drawDistance = 16f;
                }
            }

            activeRenderBucket = RenderBudgetBucket.None;
            lastAppliedShadowTierQuality = -1;
        }

        private void UnregisterPerformanceEntries(ZoneRegion region)
        {
            if (region == null) return;
            trackedCampusLights.RemoveAll(e => e.Owner == region || e.Light == null);
            trackedShadowLights.RemoveAll(e => e.Owner == region || e.Light == null);
            trackedSouthExteriorParticles.RemoveAll(e => e.Owner == region || e.Renderer == null);
            activeRenderBucket = RenderBudgetBucket.None;
        }

        private static void TightenCampusLightRange(Light light)
        {
            string n = light.name;
            if (n.IndexOf("Inner Light Court", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Inner Light Court spans x=[6..14], z=[8..14]; 8.8m lights courtyard trees cleanly without spilling into Room 101 south wall!
                if (light.range > 8.8f) light.range = 8.8f;
            }
            else if (n.IndexOf("Upper Facade Moon Wash", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     n.IndexOf("Upper Facade Night Glow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (light.range > 22f) light.range = 22f;
            }
            else if (n.IndexOf("Upper Facade Flank", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (light.range > 18f) light.range = 18f;
            }
            else if (n.IndexOf("K-B Courtyard", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (light.range > 24f) light.range = 24f;
            }
            else if (n.IndexOf("East Wing Overview", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (light.range > 20f) light.range = 20f;
            }
            else if (n.IndexOf("KD South Emergency", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (light.range > 14f) light.range = 14f;
            }
            else if (n.IndexOf("K-C South balconies", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (light.range > 18f) light.range = 18f;
            }
            else if (n.IndexOf("Campus sodium streetlamp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (light.range > 16f) light.range = 16f;
            }
        }

        private static CampusLightMask ClassifyCampusLight(Light light)
        {
            string n = light.name;
            Vector3 p = light.transform.position;

            // Generous sightline overlap so visible lights never vanish when stepping across doorways:
            if (n.IndexOf("Inner Light Court", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.AllAboveGround;

            if (n.IndexOf("KD South Emergency", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("K-C South balconies", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Link KA-KC", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.Exterior | CampusLightMask.Corridor | CampusLightMask.Emergency | CampusLightMask.Upper;

            if (n.IndexOf("S1 West Gallery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Water tower", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.Exterior | CampusLightMask.Reception | CampusLightMask.Upper;

            if (n.IndexOf("K-A Upper Facade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Canopy Sign", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("K-B Courtyard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Main gate", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.Exterior | CampusLightMask.Reception | CampusLightMask.Corridor | CampusLightMask.Upper;

            if (p.z < -4f && p.x > -28f && p.x < 24f)
                return CampusLightMask.Exterior | CampusLightMask.Reception | CampusLightMask.Corridor | CampusLightMask.Upper;

            return CampusLightMask.Exterior | CampusLightMask.Upper;
        }

        private static RenderBudgetBucket ResolveRenderBudgetBucket(Vector3 pos)
        {
            if (pos.y < -0.6f) return RenderBudgetBucket.Basement;
            if (pos.y >= 2.9f) return RenderBudgetBucket.UpperFloor;
            if (pos.x < -2.0f || pos.x > 18.0f || pos.z < -7.6f) return RenderBudgetBucket.Exterior;
            if (pos.z <= 0.6f) return RenderBudgetBucket.Reception;
            if (pos.z < 15.2f)
                return (pos.x >= 5.65f && pos.z >= 1.85f && pos.z <= 8.25f)
                    ? RenderBudgetBucket.WardRoom
                    : RenderBudgetBucket.Corridor;
            return RenderBudgetBucket.Emergency;
        }

        internal static bool IsEnclosedIndoorBudget(Vector3 pos)
        {
            var bucket = ResolveRenderBudgetBucket(pos);
            return bucket is RenderBudgetBucket.WardRoom or RenderBudgetBucket.Corridor
                or RenderBudgetBucket.Reception or RenderBudgetBucket.Emergency
                or RenderBudgetBucket.Basement;
        }

        private void EnsureDirectionalMoonCached()
        {
            if (cachedDirectionalMoon != null) return;
            foreach (var l in GetComponentsInChildren<Light>(true))
            {
                if (l != null && l.type == LightType.Directional)
                {
                    cachedDirectionalMoon = l;
                    directionalMoonOrigShadows = l.shadows;
                    directionalMoonOrigIntensity = l.intensity;
                    directionalMoonFactor = 1f;
                    break;
                }
            }
        }

        private void UpdateRenderBudgetForPosition(Vector3 camPos)
        {
            EnsureDirectionalMoonCached();
            if (cachedPlayer == null)
                cachedPlayer = FindFirstObjectByType<FirstPerson>();

            // Detect teleports (>2.5m jump or first frame) so QA/benchmark snaps immediately while walking fades smoothly!
            bool teleported = !hasLastBudgetCamPos || (camPos - lastBudgetCamPos).sqrMagnitude > 6.25f;
            lastBudgetCamPos = camPos;
            hasLastBudgetCamPos = true;
            float maxDelta = teleported ? 1f : Mathf.Clamp(Time.unscaledDeltaTime * 2.4f, 0.005f, 0.25f);

            var bucket = ResolveRenderBudgetBucket(camPos);
            bool bucketChanged = bucket != activeRenderBucket;
            if (bucketChanged)
            {
                activeRenderBucket = bucket;
                bool isIndoorTight = bucket is RenderBudgetBucket.WardRoom or RenderBudgetBucket.Corridor
                    or RenderBudgetBucket.Reception or RenderBudgetBucket.Emergency or RenderBudgetBucket.Basement;
                cachedPlayer?.SetIndoorBudget(isIndoorTight);

                bool showSouthParticles = bucket is RenderBudgetBucket.Exterior or RenderBudgetBucket.Reception
                    or RenderBudgetBucket.Corridor or RenderBudgetBucket.UpperFloor;
                for (int i = 0; i < trackedSouthExteriorParticles.Count; i++)
                {
                    var psr = trackedSouthExteriorParticles[i].Renderer;
                    if (psr != null && psr.enabled != showSouthParticles)
                        psr.enabled = showSouthParticles;
                }
            }

            // Configure URP SoftShadowQuality tier once per quality-level change (never mutating shadows to None on lit lamps!).
            int qLevel = QualitySettings.GetQualityLevel();
            if (qLevel != lastAppliedShadowTierQuality)
            {
                lastAppliedShadowTierQuality = qLevel;
                bool medOrLow = qLevel <= 1;
                for (int i = 0; i < trackedShadowLights.Count; i++)
                {
                    var entry = trackedShadowLights[i];
                    if (entry.Light == null) continue;
                    if (!shadowsAblated && entry.Light.shadows != entry.OriginalShadows)
                        entry.Light.shadows = entry.OriginalShadows;
                    if (entry.IsWardWindow)
                    {
                        var urp = entry.Light.GetUniversalAdditionalLightData();
                        if (urp != null)
                            urp.softShadowQuality = medOrLow ? SoftShadowQuality.Low : SoftShadowQuality.Medium;
                    }
                }
            }

            // CRITICAL LIGHTING INTEGRITY RULE:
            // NEVER disable .shadows on Overcast monsoon moonlight while its intensity > 0!
            // Doing so removes roof/wall occlusion and blasts 1.38-intensity moonlight straight through ceilings & walls.
            // Above ground, always keep Directional Moonlight + soft shadows intact; only fade smoothly to 0 in deep Basement.
            if (cachedDirectionalMoon != null)
            {
                float wantMoon = bucket == RenderBudgetBucket.Basement ? 0f : 1f;
                if (Mathf.Abs(directionalMoonFactor - wantMoon) > 0.0005f || teleported)
                {
                    directionalMoonFactor = Mathf.MoveTowards(directionalMoonFactor, wantMoon, maxDelta);
                    bool moonActive = directionalMoonFactor > 0.001f;
                    if (cachedDirectionalMoon.enabled != moonActive)
                        cachedDirectionalMoon.enabled = moonActive;
                    cachedDirectionalMoon.intensity = directionalMoonOrigIntensity * directionalMoonFactor;
                }
                if (!shadowsAblated && cachedDirectionalMoon.shadows != directionalMoonOrigShadows)
                    cachedDirectionalMoon.shadows = directionalMoonOrigShadows;
            }

            // Smooth intensity fade for distant campus lights (zero 1-frame lighting pops across doorways!).
            CampusLightMask mask = bucket switch
            {
                RenderBudgetBucket.Exterior => CampusLightMask.Exterior,
                RenderBudgetBucket.Reception => CampusLightMask.Reception,
                RenderBudgetBucket.Corridor => CampusLightMask.Corridor,
                RenderBudgetBucket.WardRoom => CampusLightMask.Ward,
                RenderBudgetBucket.Emergency => CampusLightMask.Emergency,
                RenderBudgetBucket.UpperFloor => CampusLightMask.Upper,
                _ => CampusLightMask.None
            };

            for (int i = 0; i < trackedCampusLights.Count; i++)
            {
                var entry = trackedCampusLights[i];
                if (entry.Light == null) continue;
                float target = (entry.AllowedBuckets & mask) != 0 ? 1f : 0f;
                if (Mathf.Abs(entry.CurrentFactor - target) > 0.0005f || teleported)
                {
                    entry.CurrentFactor = Mathf.MoveTowards(entry.CurrentFactor, target, maxDelta);
                    bool shouldEnable = entry.CurrentFactor > 0.001f;
                    if (entry.Light.enabled != shouldEnable)
                        entry.Light.enabled = shouldEnable;
                    entry.Light.intensity = entry.OriginalIntensity * entry.CurrentFactor;
                    trackedCampusLights[i] = entry;
                }
            }

            // Local shadow-casting spotlights NEVER strip .shadows while lit!
            // Instead, keep shadows ALWAYS ON and only smoothly fade intensity to 0 when strictly behind multiple solid walls.
            for (int i = 0; i < trackedShadowLights.Count; i++)
            {
                var entry = trackedShadowLights[i];
                if (entry.Light == null) continue;
                bool visible;
                if (entry.IsCanopy)
                {
                    // Canopy spot at z=-8.65m is visible through front doors from Exterior, Reception, UpperFloor, and South Corridor (z < 7.5m).
                    visible = bucket is RenderBudgetBucket.Exterior or RenderBudgetBucket.Reception or RenderBudgetBucket.UpperFloor
                        || (bucket == RenderBudgetBucket.Corridor && camPos.z < 7.5f);
                }
                else if (entry.IsWardWindow)
                {
                    // Room 101 window moonlight stays active everywhere indoors on ground floor (Ward, Corridor, Reception, Emergency).
                    visible = bucket is RenderBudgetBucket.WardRoom or RenderBudgetBucket.Corridor
                        or RenderBudgetBucket.Reception or RenderBudgetBucket.Emergency;
                }
                else
                {
                    // Corridor doorway spill sits at Room 101 doorway (6.6, 2.45, 4.75) — MUST stay lit with shadows in Ward, Corridor, AND Reception!
                    visible = bucket is RenderBudgetBucket.WardRoom or RenderBudgetBucket.Corridor
                        or RenderBudgetBucket.Reception or RenderBudgetBucket.Emergency;
                }

                float target = visible ? 1f : 0f;
                if (Mathf.Abs(entry.CurrentFactor - target) > 0.0005f || teleported)
                {
                    entry.CurrentFactor = Mathf.MoveTowards(entry.CurrentFactor, target, maxDelta);
                    bool shouldEnable = entry.CurrentFactor > 0.001f;
                    if (entry.Light.enabled != shouldEnable)
                        entry.Light.enabled = shouldEnable;
                    entry.Light.intensity = entry.OriginalIntensity * entry.CurrentFactor;
                    trackedShadowLights[i] = entry;
                }
                if (!shadowsAblated && entry.Light.shadows != entry.OriginalShadows)
                    entry.Light.shadows = entry.OriginalShadows;
            }
        }
    }
}
