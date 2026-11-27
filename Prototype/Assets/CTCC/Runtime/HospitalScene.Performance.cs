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
            Ward = 1 << 2,
            Emergency = 1 << 3,
            Upper = 1 << 4,
            AllAboveGround = Exterior | Reception | Ward | Emergency | Upper
        }

        private struct TrackedCampusLight
        {
            public ZoneRegion Owner;
            public Light Light;
            public CampusLightMask AllowedBuckets;
        }

        private struct TrackedShadowLight
        {
            public ZoneRegion Owner;
            public Light Light;
            public LightShadows OriginalShadows;
            public bool IsCanopy;
            public bool IsWardWindow;
            public bool IsCorridorSpill;
        }

        private struct TrackedCampusShadowCaster
        {
            public ZoneRegion Owner;
            public Renderer Renderer;
            public ShadowCastingMode OriginalMode;
        }

        private struct TrackedExteriorParticle
        {
            public ZoneRegion Owner;
            public ParticleSystemRenderer Renderer;
        }

        private readonly List<TrackedCampusLight> trackedCampusLights = new();
        private readonly List<TrackedShadowLight> trackedShadowLights = new();
        private readonly List<TrackedCampusShadowCaster> trackedCampusShadowCasters = new();
        private readonly List<TrackedExteriorParticle> trackedSouthExteriorParticles = new();
        private RenderBudgetBucket activeRenderBucket = RenderBudgetBucket.None;
        private bool performanceBudgetSubscribed;
        private bool shadowsAblated;
        private Light cachedDirectionalMoon;
        private LightShadows directionalMoonOrigShadows = LightShadows.Soft;
        private FirstPerson cachedPlayer;
        private UniversalRenderPipelineAsset cachedPipeline;
        private float originalPipelineShadowDistance = -1f;
        private int originalPipelineCascadeCount = -1;

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
            }
            else
            {
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
            if (cachedPipeline != null && originalPipelineShadowDistance > 0f)
            {
                cachedPipeline.shadowDistance = originalPipelineShadowDistance;
                if (originalPipelineCascadeCount > 0)
                    cachedPipeline.shadowCascadeCount = originalPipelineCascadeCount;
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
                        AllowedBuckets = ClassifyCampusLight(light)
                    });
                }

                foreach (var r in region.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || r.shadowCastingMode == ShadowCastingMode.Off) continue;
                    trackedCampusShadowCasters.Add(new TrackedCampusShadowCaster
                    {
                        Owner = region,
                        Renderer = r,
                        OriginalMode = r.shadowCastingMode
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
        }

        private void UnregisterPerformanceEntries(ZoneRegion region)
        {
            if (region == null) return;
            trackedCampusLights.RemoveAll(e => e.Owner == region || e.Light == null);
            trackedShadowLights.RemoveAll(e => e.Owner == region || e.Light == null);
            trackedCampusShadowCasters.RemoveAll(e => e.Owner == region || e.Renderer == null);
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

            if (n.IndexOf("Inner Light Court", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.Exterior | CampusLightMask.Ward | CampusLightMask.Emergency | CampusLightMask.Upper;

            if (n.IndexOf("KD South Emergency", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("K-C South balconies", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Link KA-KC", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.Exterior | CampusLightMask.Emergency | CampusLightMask.Upper;

            if (n.IndexOf("S1 West Gallery", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Water tower", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.Exterior | CampusLightMask.Upper;

            if (n.IndexOf("K-A Upper Facade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Canopy Sign", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("K-B Courtyard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Main gate", StringComparison.OrdinalIgnoreCase) >= 0)
                return CampusLightMask.Exterior | CampusLightMask.Reception | CampusLightMask.Upper;

            if (p.z < -4f && p.x > -28f && p.x < 24f)
                return CampusLightMask.Exterior | CampusLightMask.Reception | CampusLightMask.Upper;

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

        private void UpdateRenderBudgetForPosition(Vector3 camPos)
        {
            var bucket = ResolveRenderBudgetBucket(camPos);
            if (bucket == activeRenderBucket) return;
            activeRenderBucket = bucket;

            if (cachedDirectionalMoon == null)
            {
                foreach (var l in GetComponentsInChildren<Light>(true))
                {
                    if (l != null && l.type == LightType.Directional)
                    {
                        cachedDirectionalMoon = l;
                        directionalMoonOrigShadows = l.shadows;
                        break;
                    }
                }
            }
            if (cachedPlayer == null)
                cachedPlayer = FindFirstObjectByType<FirstPerson>();

            bool isIndoorTight = bucket is RenderBudgetBucket.WardRoom or RenderBudgetBucket.Corridor or RenderBudgetBucket.Reception or RenderBudgetBucket.Emergency or RenderBudgetBucket.Basement;
            cachedPlayer?.SetIndoorBudget(isIndoorTight);

            // Inside enclosed WardRoom/Corridor/Basement, local SpotLights cast all window moonlight & doorway shadows;
            // gating exterior Directional moonlight shadow pass avoids rendering an invisible 2048² shadow pass every frame!
            if (cachedDirectionalMoon != null && !shadowsAblated)
            {
                bool allowDirShadow = bucket is RenderBudgetBucket.Exterior or RenderBudgetBucket.Reception or RenderBudgetBucket.Emergency or RenderBudgetBucket.UpperFloor;
                var wantDir = allowDirShadow ? directionalMoonOrigShadows : LightShadows.None;
                if (cachedDirectionalMoon.shadows != wantDir)
                    cachedDirectionalMoon.shadows = wantDir;
            }

            CampusLightMask mask = bucket switch
            {
                RenderBudgetBucket.Exterior => CampusLightMask.Exterior,
                RenderBudgetBucket.Reception => CampusLightMask.Reception,
                RenderBudgetBucket.Corridor or RenderBudgetBucket.WardRoom => CampusLightMask.Ward,
                RenderBudgetBucket.Emergency => CampusLightMask.Emergency,
                RenderBudgetBucket.UpperFloor => CampusLightMask.Upper,
                _ => CampusLightMask.None
            };

            for (int i = 0; i < trackedCampusLights.Count; i++)
            {
                var entry = trackedCampusLights[i];
                if (entry.Light == null) continue;
                bool want = (entry.AllowedBuckets & mask) != 0;
                if (entry.Light.enabled != want) entry.Light.enabled = want;
            }

            bool allowCampusShadowCasters = bucket is RenderBudgetBucket.Exterior or RenderBudgetBucket.Reception or RenderBudgetBucket.UpperFloor;
            for (int i = 0; i < trackedCampusShadowCasters.Count; i++)
            {
                var entry = trackedCampusShadowCasters[i];
                if (entry.Renderer == null) continue;
                var wantMode = allowCampusShadowCasters ? entry.OriginalMode : ShadowCastingMode.Off;
                if (entry.Renderer.shadowCastingMode != wantMode) entry.Renderer.shadowCastingMode = wantMode;
            }

            bool showSouthParticles = bucket is RenderBudgetBucket.Exterior or RenderBudgetBucket.Reception or RenderBudgetBucket.UpperFloor;
            for (int i = 0; i < trackedSouthExteriorParticles.Count; i++)
            {
                var psr = trackedSouthExteriorParticles[i].Renderer;
                if (psr != null && psr.enabled != showSouthParticles)
                    psr.enabled = showSouthParticles;
            }

            if (!shadowsAblated)
            {
                bool medOrLow = QualitySettings.GetQualityLevel() <= 1;
                for (int i = 0; i < trackedShadowLights.Count; i++)
                {
                    var entry = trackedShadowLights[i];
                    if (entry.Light == null) continue;
                    bool wantShadow =
                        (entry.IsCanopy && bucket is RenderBudgetBucket.Exterior or RenderBudgetBucket.Reception) ||
                        (entry.IsWardWindow && bucket is RenderBudgetBucket.Corridor or RenderBudgetBucket.WardRoom) ||
                        (entry.IsCorridorSpill && bucket is RenderBudgetBucket.Corridor or RenderBudgetBucket.Reception);
                    var mode = wantShadow ? entry.OriginalShadows : LightShadows.None;
                    if (entry.Light.shadows != mode) entry.Light.shadows = mode;
                    if (wantShadow && entry.IsWardWindow)
                    {
                        var urp = entry.Light.GetUniversalAdditionalLightData();
                        if (urp != null)
                            urp.softShadowQuality = medOrLow ? SoftShadowQuality.Low : SoftShadowQuality.Medium;
                    }
                }
            }

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                if (cachedPipeline != pipeline)
                {
                    cachedPipeline = pipeline;
                    originalPipelineShadowDistance = pipeline.shadowDistance;
                    originalPipelineCascadeCount = pipeline.shadowCascadeCount;
                }
                // Respect Low quality tighter shadowDistance; adapt Medium/High cleanly by indoor/outdoor sightlines.
                float targetDist = bucket switch
                {
                    RenderBudgetBucket.WardRoom => 15f,
                    RenderBudgetBucket.Corridor => 17f,
                    RenderBudgetBucket.Emergency => 18f,
                    RenderBudgetBucket.Basement => 14f,
                    RenderBudgetBucket.Reception => 22f,
                    RenderBudgetBucket.UpperFloor => 20f,
                    _ => 24f
                };
                float clamped = originalPipelineShadowDistance > 0f
                    ? Mathf.Min(originalPipelineShadowDistance, targetDist)
                    : targetDist;
                if (Mathf.Abs(pipeline.shadowDistance - clamped) > 0.1f)
                    pipeline.shadowDistance = clamped;
                if (QualitySettings.GetQualityLevel() <= 1 && pipeline.shadowCascadeCount != 1)
                    pipeline.shadowCascadeCount = 1;
            }
        }
    }
}
