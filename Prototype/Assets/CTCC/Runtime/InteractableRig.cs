using System.Collections.Generic;
using UnityEngine;

namespace CTCC
{
    /// <summary>
    /// Responsive physical interaction driver for hinged doors (DOOR-101, DOOR-STAFF),
    /// sliding hospital drawers (DRAWER-BED3, DRAWER-DESK, DRAWER-NURSE), track-anchored privacy curtains,
    /// and the rolling steel cabinet blocking Room 101 Store.
    /// Supports both accessible click/toggle AND optional hold-and-drag manual manipulation with
    /// believable resistance, smooth easing, and spatial foley cues.
    /// </summary>
    public sealed class InteractableRig : MonoBehaviour
    {
        public Transform curtainCloth;
        public Vector3 curtainClosedScale = Vector3.one;
        public Vector3 curtainOpenScale = new Vector3(0.28f, 1f, 1.26f);
        public Vector3 curtainClosedPos;
        public Vector3 curtainOpenPos;
        public bool curtainHasPosTrack;
        public bool curtainPulled;

        private sealed class DoorChannel
        {
            public Transform pivot;
            public float currentAngle;
            public float targetAngle;
            public float maxOpenAngle;
            public bool moving;
        }

        private sealed class DrawerChannel
        {
            public Transform transform;
            public Vector3 closedLocalPos;
            public Vector3 openLocalOffset;
            public Quaternion restRotation;
            public float openYaw;
            public float currentOpen;
            public float targetOpen;
            public bool moving;
        }

        private readonly Dictionary<string, DoorChannel> doors = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DrawerChannel> drawers = new(System.StringComparer.OrdinalIgnoreCase);

        private float curtainBlend;
        private float curtainTargetBlend;
        private float curtainDragVelocity;
        private float curtainDraftImpulse;
        private Quaternion curtainBaseRot = Quaternion.identity;
        private bool curtainBaseRotCaptured;
        private Transform cabinet;
        private Vector3 cabinetTarget;
        private bool cabinetMoving;
        private bool reducedMotion;

        // Active hold-and-drag state
        private string draggingId;
        private float dragAudioCooldown;
        private float dragTravelAccum;

        public bool IsDragging => !string.IsNullOrEmpty(draggingId);
        public string DraggingId => draggingId ?? "";
        public float CurtainOpenness => curtainBlend;

        public void SetReducedMotion(bool reduced) => reducedMotion = reduced;

        public void ResetSessionProps()
        {
            draggingId = null;
            ResetCurtainPose();
            foreach (var kv in drawers)
            {
                var dr = kv.Value;
                if (dr.transform == null) continue;
                dr.currentOpen = 0f;
                dr.targetOpen = 0f;
                dr.moving = false;
                PlaceSlide(dr, 0f);
            }
            if (doors.TryGetValue("DOOR-STAFF", out var staff) && staff.pivot != null)
            {
                staff.currentAngle = 0f;
                staff.targetAngle = 0f;
                staff.moving = false;
                staff.pivot.localRotation = Quaternion.identity;
            }
            Physics.SyncTransforms();
        }

        public void ConfigureCabinet(Transform cab, Vector3 initialPos)
        {
            cabinet = cab;
            cabinetTarget = initialPos;
            cabinetMoving = false;
            if (cabinet != null) cabinet.localPosition = initialPos;
        }

        public void ConfigureCurtain(Transform cloth, Vector3 closedScale, Vector3 openScale, Vector3 closedPos, Vector3 openPos, bool hasPosTrack = true)
        {
            if (curtainCloth != cloth) curtainBaseRotCaptured = false;
            curtainCloth = cloth;
            curtainClosedScale = closedScale;
            curtainOpenScale = openScale;
            curtainClosedPos = closedPos;
            curtainOpenPos = openPos;
            curtainHasPosTrack = hasPosTrack;
            ResetCurtainPose();
        }

        internal void ResetCurtainPose()
        {
            if (draggingId == "CURTAIN-101") draggingId = null;
            curtainDraftImpulse = 0f;
            SetCurtainPulled(false, instant: true);
        }

        public void ConfigureDoor(Transform pivot, float initialAngle) =>
            ConfigureDoor("DOOR-101", pivot, initialAngle, initialAngle != 0f ? initialAngle : 102f);

        public void ConfigureDoor(string id, Transform pivot, float initialAngle, float maxOpenAngle)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (pivot == null) { doors.Remove(id); return; }
            doors[id] = new DoorChannel
            {
                pivot = pivot,
                currentAngle = initialAngle,
                targetAngle = initialAngle,
                maxOpenAngle = maxOpenAngle,
                moving = false
            };
            pivot.localRotation = Quaternion.Euler(0f, initialAngle, 0f);
        }

        public void ConfigureDrawer(string id, Transform drawer, Vector3 closedLocalPos, Vector3 openLocalOffset, float openYaw = 0f)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (drawer == null) { drawers.Remove(id); return; }
            drawers[id] = new DrawerChannel
            {
                transform = drawer,
                closedLocalPos = closedLocalPos,
                openLocalOffset = openLocalOffset,
                restRotation = drawer.localRotation,
                openYaw = openYaw,
                currentOpen = 0f,
                targetOpen = 0f,
                moving = false
            };
            drawer.localPosition = closedLocalPos;
        }

        public bool IsDrawerOpen(string id) =>
            !string.IsNullOrEmpty(id) && drawers.TryGetValue(id, out var ch) &&
                ch.targetOpen > 0.5f && ch.currentOpen > 0.54f;

        public bool IsDoorOpen(string id) =>
            !string.IsNullOrEmpty(id) && doors.TryGetValue(id, out var ch) && Mathf.Abs(ch.targetAngle) > 12f;

        public bool ToggleDrawer(string id, EnvironmentalAudio audioBus, bool instant = false, bool quiet = false)
        {
            if (string.IsNullOrEmpty(id) || !drawers.TryGetValue(id, out var ch) || ch.transform == null)
                return false;
            ch.targetOpen = ch.targetOpen > 0.5f ? 0f : 1f;
            if (instant || reducedMotion)
            {
                ch.currentOpen = ch.targetOpen;
                PlaceSlide(ch, ch.currentOpen);
                ch.moving = false;
                Physics.SyncTransforms();
            }
            else
            {
                ch.moving = true;
            }
            if (quiet && id == "DRAWER-NURSE")
            {
                audioBus?.From("latchClick", ch.transform.position, 0.58f);
                audioBus?.From("drawerSlide", ch.transform.position, 0.34f);
            }
            else
            {
                audioBus?.From(id == "CHAIR-STAFF" ? "chairScrape" : "drawerSlide", ch.transform.position,
                    ch.targetOpen > 0.5f ? (id == "DRAWER-NURSE" ? 0.94f : 0.82f) : 0.64f);
            }
            return ch.targetOpen > 0.5f;
        }

        public bool ToggleDoor(string id, EnvironmentalAudio audioBus, bool instant = false)
        {
            if (string.IsNullOrEmpty(id) || !doors.TryGetValue(id, out var ch) || ch.pivot == null)
                return false;
            float ratio = Mathf.Abs(ch.targetAngle) / Mathf.Max(1f, Mathf.Abs(ch.maxOpenAngle));
            bool open = ratio < 0.35f;
            MoveDoor(id, ch.pivot, open ? ch.maxOpenAngle : 0f, instant, audioBus);
            return open;
        }

        public void MoveCabinet(Vector3 target, bool instant, EnvironmentalAudio audioBus)
        {
            cabinetTarget = target;
            if (cabinet == null) return;
            if (instant || reducedMotion)
            {
                cabinet.localPosition = target;
                cabinetMoving = false;
                Physics.SyncTransforms();
                return;
            }
            if ((cabinet.localPosition - target).sqrMagnitude > 0.01f)
            {
                cabinetMoving = true;
                audioBus?.From("cabinetRoll", cabinet.position + Vector3.up * 0.8f);
            }
        }

        public void MoveDoor(Transform pivot, float targetAngle, bool instant, EnvironmentalAudio audioBus) =>
            MoveDoor("DOOR-101", pivot, targetAngle, instant, audioBus);

        public void MoveDoor(string id, Transform pivot, float targetAngle, bool instant, EnvironmentalAudio audioBus)
        {
            if (pivot == null || string.IsNullOrEmpty(id)) return;
            if (!doors.TryGetValue(id, out var ch))
            {
                ch = new DoorChannel { pivot = pivot, currentAngle = 0f, maxOpenAngle = Mathf.Abs(targetAngle) > 1f ? targetAngle : 102f };
                doors[id] = ch;
            }
            ch.pivot = pivot;
            ch.targetAngle = targetAngle;
            if (instant || reducedMotion)
            {
                ch.currentAngle = targetAngle;
                ch.pivot.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
                ch.moving = false;
                Physics.SyncTransforms();
                return;
            }
            if (Mathf.Abs(Mathf.DeltaAngle(ch.currentAngle, targetAngle)) > 1.5f)
            {
                ch.moving = true;
                audioBus?.From(Mathf.Abs(targetAngle) > 10f ? "doorCreak" : "doorLatch", ch.pivot.position + Vector3.up * 1.1f);
            }
        }

        public bool ToggleCurtain(EnvironmentalAudio audioBus)
        {
            SetCurtainAmount(curtainPulled ? 0f : 1f, audioBus);
            return curtainPulled;
        }

        internal void SetCurtainAmount(float amount, EnvironmentalAudio audioBus)
        {
            curtainTargetBlend = Mathf.Clamp01(amount);
            curtainPulled = curtainTargetBlend > .5f;
            if (curtainCloth != null)
                audioBus?.From("curtainSlide", curtainCloth.position);
            if (reducedMotion)
            {
                curtainBlend = curtainTargetBlend;
                ApplyCurtainPose();
            }
        }

        private readonly Collider[] chairObstacles = new Collider[16];

        internal bool StaffChairClear()
        {
            if (!drawers.TryGetValue("CHAIR-STAFF", out var chair) || chair.transform == null) return true;
            var body = chair.transform.GetComponentInChildren<BoxCollider>();
            if (body == null) return true;
            var geometry = body.transform;
            Vector3 half = Vector3.Scale(body.size * .5f, geometry.lossyScale) - Vector3.one * .012f;
            int count = Physics.OverlapBoxNonAlloc(geometry.TransformPoint(body.center), half, chairObstacles,
                geometry.rotation, HospitalScene.SolidMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!chairObstacles[i].transform.IsChildOf(chair.transform)) return false;
            return true;
        }

        public void SetCurtainPulled(bool pulled, bool instant = false)
        {
            curtainPulled = pulled;
            curtainTargetBlend = pulled ? 1f : 0f;
            curtainDragVelocity = 0f;
            if (instant || reducedMotion)
            {
                curtainBlend = curtainTargetBlend;
                ApplyCurtainPose();
            }
        }

        /// <summary>
        /// Deferred dread micro-shift (Upgrade #2): subtly draws an opened Bed-4 curtain partially shut
        /// right as the player lowers an inspected document.
        /// </summary>
        public bool NudgeCurtainClosed(float delta, EnvironmentalAudio audioBus)
        {
            if (curtainCloth == null || draggingId == "CURTAIN-101") return false;
            float next = Mathf.Clamp01(curtainTargetBlend - Mathf.Clamp(delta, 0.12f, 0.50f));
            if (Mathf.Abs(next - curtainTargetBlend) < 0.04f) return false;
            curtainTargetBlend = next;
            curtainPulled = curtainTargetBlend > 0.5f;
            if (reducedMotion)
            {
                curtainBlend = curtainTargetBlend;
                ApplyCurtainPose();
            }
            else
            {
                curtainDraftImpulse = Mathf.Max(curtainDraftImpulse, 0.65f);
            }
            audioBus?.From("curtainSlide", curtainCloth.position, 0.42f);
            return true;
        }

        public void PulseCurtainDraft()
        {
            if (reducedMotion || curtainCloth == null) return;
            curtainDraftImpulse = 1f;
        }

        public float GetDoorAngle(string id) =>
            !string.IsNullOrEmpty(id) && doors.TryGetValue(id, out var ch) ? ch.currentAngle : 0f;

        public float GetDragOpenness01(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0f;
            if (id == "CURTAIN-101") return Mathf.Clamp01(curtainBlend);
            if (drawers.TryGetValue(id, out var dr)) return Mathf.Clamp01(dr.currentOpen);
            if (doors.TryGetValue(id, out var door))
            {
                float maxMag = Mathf.Max(1f, Mathf.Abs(door.maxOpenAngle));
                return Mathf.Clamp01(Mathf.Abs(door.currentAngle) / maxMag);
            }
            return 0f;
        }

        internal float IntendedOpenness(string id)
        {
            if (id == "CURTAIN-101") return curtainTargetBlend;
            if (drawers.TryGetValue(id, out var drawer)) return drawer.targetOpen;
            if (doors.TryGetValue(id, out var door))
                return Mathf.Clamp01(Mathf.Abs(door.targetAngle) / Mathf.Max(1f, Mathf.Abs(door.maxOpenAngle)));
            return 0f;
        }

        internal void RestorePose(string id, float openness)
        {
            if (id == "CURTAIN-101")
            {
                curtainTargetBlend = curtainBlend = openness;
                curtainPulled = openness > 0.5f;
                curtainDragVelocity = curtainDraftImpulse = 0f;
                ApplyCurtainPose();
            }
            else if (drawers.TryGetValue(id, out var drawer) && drawer.transform != null)
            {
                drawer.targetOpen = drawer.currentOpen = openness;
                drawer.moving = false;
                PlaceSlide(drawer, openness);
            }
            else if (doors.TryGetValue(id, out var door))
            {
                MoveDoor(id, door.pivot, door.maxOpenAngle * openness, true, null);
            }
            Physics.SyncTransforms();
        }

        internal List<(string Id, float Openness)> CapturePoses()
        {
            var poses = new List<(string, float)>(doors.Count + drawers.Count + 1);
            foreach (var item in doors)
                if (item.Value.pivot != null) poses.Add((item.Key, GetDragOpenness01(item.Key)));
            foreach (var item in drawers)
                if (item.Value.transform != null) poses.Add((item.Key, item.Value.currentOpen));
            if (curtainCloth != null) poses.Add(("CURTAIN-101", curtainBlend));
            return poses;
        }

        internal void RestorePoses(List<(string Id, float Openness)> poses)
        {
            foreach (var pose in poses) RestorePose(pose.Id, pose.Openness);
        }

        private static void PlaceSlide(DrawerChannel drawer, float openness)
        {
            drawer.transform.localPosition = drawer.closedLocalPos + drawer.openLocalOffset * openness;
            drawer.transform.localRotation = drawer.restRotation * Quaternion.Euler(0f, drawer.openYaw * openness, 0f);
        }

        /// <summary>
        /// Tactile analog hold-and-drag entry for doors (DOOR-101, DOOR-STAFF), drawers, and CURTAIN-101 (Upgrade #8).
        /// Click/tap (E/K) remains instant & fully accessible; dragging adjusts openness continuously with damped
        /// resistance and preserves ANY intermediate cracked-open peek position upon release!
        /// </summary>
        public bool BeginDrag(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            dragTravelAccum = 0f;
            curtainDragVelocity = 0f;
            if (id == "CURTAIN-101" && curtainCloth != null)
            {
                draggingId = id;
                curtainTargetBlend = curtainBlend;
                return true;
            }
            if (drawers.ContainsKey(id) || doors.ContainsKey(id))
            {
                draggingId = id;
                return true;
            }
            return false;
        }

        public void UpdateDrag(Vector2 mouseDelta, EnvironmentalAudio audioBus)
        {
            if (string.IsNullOrEmpty(draggingId)) return;
            float rawImpulse = (mouseDelta.x - mouseDelta.y * 0.55f) * 0.0040f;
            if (Mathf.Abs(rawImpulse) < 0.0003f) return;

            // Clamp burst deltas so heavy doors and cloth curtains feel weighted rather than twitchy.
            float impulse = Mathf.Clamp(rawImpulse, -0.055f, 0.055f);
            float speedGain = Mathf.Lerp(0.24f, 0.68f, Mathf.Clamp01(Mathf.Abs(impulse) / 0.032f));
            if (dragAudioCooldown > 0f) dragAudioCooldown -= Time.deltaTime;

            if (draggingId == "CURTAIN-101" && curtainCloth != null)
            {
                float prev = curtainBlend;
                curtainBlend = Mathf.Clamp01(curtainBlend + impulse * 1.10f);
                curtainTargetBlend = curtainBlend;
                curtainPulled = curtainBlend > 0.5f;
                float moved = curtainBlend - prev;
                curtainDragVelocity = Mathf.Lerp(curtainDragVelocity, moved * 42f, 0.45f);
                dragTravelAccum += Mathf.Abs(moved);
                if (!reducedMotion && Mathf.Abs(moved) > 0.002f)
                    curtainDraftImpulse = Mathf.Clamp01(curtainDraftImpulse + Mathf.Abs(moved) * 2.4f);
                ApplyCurtainPose();
                if (dragAudioCooldown <= 0f && dragTravelAccum > 0.04f)
                {
                    audioBus?.From("curtainSlide", curtainCloth.position, speedGain);
                    dragAudioCooldown = 0.36f;
                    dragTravelAccum = 0f;
                }
                return;
            }

            if (drawers.TryGetValue(draggingId, out var dr) && dr.transform != null)
            {
                float prev = dr.currentOpen;
                dr.currentOpen = Mathf.Clamp01(dr.currentOpen + impulse * 1.18f);
                dr.targetOpen = dr.currentOpen;
                dr.moving = false;
                PlaceSlide(dr, dr.currentOpen);
                Physics.SyncTransforms();
                dragTravelAccum += Mathf.Abs(dr.currentOpen - prev);
                if (dragAudioCooldown <= 0f && dragTravelAccum > 0.05f)
                {
                    audioBus?.From(draggingId == "CHAIR-STAFF" ? "chairScrape" : "drawerSlide", dr.transform.position, speedGain);
                    dragAudioCooldown = 0.40f;
                    dragTravelAccum = 0f;
                }
                return;
            }

            if (doors.TryGetValue(draggingId, out var door) && door.pivot != null)
            {
                float sign = Mathf.Sign(door.maxOpenAngle != 0f ? door.maxOpenAngle : 1f);
                float maxMag = Mathf.Abs(door.maxOpenAngle != 0f ? door.maxOpenAngle : 96f);
                float prevMag = Mathf.Abs(door.currentAngle);
                // Mild hinge resistance near closed latch so cracking DOOR-101 open feels deliberate & tense
                float resist = prevMag < 8f ? 0.72f : 1.0f;
                float curMag = Mathf.Clamp(prevMag + impulse * 88f * resist, 0f, maxMag);
                door.currentAngle = curMag * sign;
                door.targetAngle = door.currentAngle;
                door.moving = false;
                door.pivot.localRotation = Quaternion.Euler(0f, door.currentAngle, 0f);
                Physics.SyncTransforms();
                dragTravelAccum += Mathf.Abs(curMag - prevMag);
                if (prevMag < 2f && curMag >= 2f)
                    audioBus?.From("doorLatch", door.pivot.position + Vector3.up * 1.1f, 0.52f);
                else if (dragAudioCooldown <= 0f && dragTravelAccum > 4.5f)
                {
                    audioBus?.From("doorCreak", door.pivot.position + Vector3.up * 1.1f, speedGain);
                    dragAudioCooldown = 0.48f;
                    dragTravelAccum = 0f;
                }
            }
        }

        public void EndDrag(EnvironmentalAudio audioBus)
        {
            if (string.IsNullOrEmpty(draggingId)) return;
            if (draggingId == "CURTAIN-101")
            {
                // Preserve intermediate analog curtain position; only snap inside tiny 5% end-stops
                if (curtainBlend < 0.05f) curtainTargetBlend = 0f;
                else if (curtainBlend > 0.95f) curtainTargetBlend = 1f;
                else curtainTargetBlend = curtainBlend;
                curtainPulled = curtainTargetBlend > 0.5f;
                curtainDragVelocity = 0f;
            }
            if (drawers.TryGetValue(draggingId, out var dr))
            {
                if (dr.currentOpen < 0.06f) { dr.targetOpen = 0f; dr.moving = true; }
                else if (dr.currentOpen > 0.94f) { dr.targetOpen = 1f; dr.moving = true; }
                else { dr.targetOpen = dr.currentOpen; dr.moving = false; }
            }
            if (doors.TryGetValue(draggingId, out var door))
            {
                float mag = Mathf.Abs(door.currentAngle);
                if (mag < 4.5f)
                {
                    door.targetAngle = 0f;
                    door.moving = true;
                    if (door.pivot != null)
                        audioBus?.From("doorLatch", door.pivot.position + Vector3.up * 1.1f, 0.65f);
                }
                else
                {
                    // Hold the exact cracked-open angle so players control how far they peek into Room 101!
                    door.targetAngle = door.currentAngle;
                    door.moving = false;
                }
            }
            draggingId = null;
            dragTravelAccum = 0f;
        }

        private void ApplyCurtainPose()
        {
            if (curtainCloth == null) return;
            if (!curtainBaseRotCaptured)
            {
                curtainBaseRot = curtainCloth.localRotation;
                curtainBaseRotCaptured = true;
            }
            float ease = curtainBlend * curtainBlend * (3f - 2f * curtainBlend);
            curtainCloth.localScale = Vector3.Lerp(curtainClosedScale, curtainOpenScale, ease);
            if (curtainHasPosTrack)
                curtainCloth.localPosition = Vector3.Lerp(curtainClosedPos, curtainOpenPos, ease);
            if (curtainDraftImpulse > 0.001f)
            {
                float sway = Mathf.Sin(Time.time * 5.2f) * curtainDraftImpulse * 3.8f;
                curtainCloth.localRotation = curtainBaseRot * Quaternion.Euler(sway, 0f, sway * 0.4f);
            }
            else
            {
                curtainCloth.localRotation = curtainBaseRot;
            }
            Physics.SyncTransforms();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool syncPhysics = false;

            if (curtainDraftImpulse > 0f)
                curtainDraftImpulse = Mathf.MoveTowards(curtainDraftImpulse, 0f, dt * 0.42f);

            if (curtainCloth != null && draggingId != "CURTAIN-101")
            {
                float target = curtainTargetBlend;
                if (Mathf.Abs(curtainBlend - target) > 0.001f || curtainDraftImpulse > 0.001f)
                {
                    curtainBlend = Mathf.MoveTowards(curtainBlend, target, dt * (reducedMotion ? 8f : 1.85f));
                    ApplyCurtainPose();
                }
            }

            if (cabinetMoving && cabinet != null)
            {
                cabinet.localPosition = Vector3.MoveTowards(cabinet.localPosition, cabinetTarget, dt * 1.25f);
                if ((cabinet.localPosition - cabinetTarget).sqrMagnitude < 0.0004f)
                {
                    cabinet.localPosition = cabinetTarget;
                    cabinetMoving = false;
                }
                syncPhysics = true;
            }

            foreach (var kv in doors)
            {
                var d = kv.Value;
                if (!d.moving || d.pivot == null || draggingId == kv.Key) continue;
                // Smooth damped hinge motion with natural latch deceleration
                float delta = Mathf.DeltaAngle(d.currentAngle, d.targetAngle);
                float speed = Mathf.Lerp(55f, 150f, Mathf.Clamp01(Mathf.Abs(delta) / 45f));
                d.currentAngle = Mathf.MoveTowardsAngle(d.currentAngle, d.targetAngle, dt * speed);
                d.pivot.localRotation = Quaternion.Euler(0f, d.currentAngle, 0f);
                if (Mathf.Abs(Mathf.DeltaAngle(d.currentAngle, d.targetAngle)) < 0.25f)
                {
                    d.currentAngle = d.targetAngle;
                    d.pivot.localRotation = Quaternion.Euler(0f, d.targetAngle, 0f);
                    d.moving = false;
                }
                syncPhysics = true;
            }

            foreach (var kv in drawers)
            {
                var dr = kv.Value;
                if (!dr.moving || dr.transform == null || draggingId == kv.Key) continue;
                dr.currentOpen = Mathf.MoveTowards(dr.currentOpen, dr.targetOpen, dt * (reducedMotion ? 10f : 2.8f));
                float ease = dr.currentOpen * dr.currentOpen * (3f - 2f * dr.currentOpen);
                PlaceSlide(dr, ease);
                if (Mathf.Abs(dr.currentOpen - dr.targetOpen) < 0.002f)
                {
                    dr.currentOpen = dr.targetOpen;
                    PlaceSlide(dr, dr.targetOpen);
                    dr.moving = false;
                }
                syncPhysics = true;
            }

            if (syncPhysics)
                Physics.SyncTransforms();
        }
    }
}
