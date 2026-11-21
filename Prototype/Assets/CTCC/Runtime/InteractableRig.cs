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
            public float currentOpen;
            public float targetOpen;
            public bool moving;
        }

        private readonly Dictionary<string, DoorChannel> doors = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DrawerChannel> drawers = new(System.StringComparer.OrdinalIgnoreCase);

        private float curtainBlend;
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

        public bool IsDragging => !string.IsNullOrEmpty(draggingId);
        public string DraggingId => draggingId ?? "";

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
                dr.transform.localPosition = dr.closedLocalPos;
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

        public void ConfigureDrawer(string id, Transform drawer, Vector3 closedLocalPos, Vector3 openLocalOffset)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (drawer == null) { drawers.Remove(id); return; }
            drawers[id] = new DrawerChannel
            {
                transform = drawer,
                closedLocalPos = closedLocalPos,
                openLocalOffset = openLocalOffset,
                currentOpen = 0f,
                targetOpen = 0f,
                moving = false
            };
            drawer.localPosition = closedLocalPos;
        }

        public bool IsDrawerOpen(string id) =>
            !string.IsNullOrEmpty(id) && drawers.TryGetValue(id, out var ch) && ch.targetOpen > 0.5f;

        public bool IsDoorOpen(string id) =>
            !string.IsNullOrEmpty(id) && doors.TryGetValue(id, out var ch) && Mathf.Abs(ch.targetAngle) > 12f;

        public bool ToggleDrawer(string id, EnvironmentalAudio audioBus, bool instant = false)
        {
            if (string.IsNullOrEmpty(id) || !drawers.TryGetValue(id, out var ch) || ch.transform == null)
                return false;
            ch.targetOpen = ch.targetOpen > 0.5f ? 0f : 1f;
            if (instant || reducedMotion)
            {
                ch.currentOpen = ch.targetOpen;
                ch.transform.localPosition = ch.closedLocalPos + ch.openLocalOffset * ch.currentOpen;
                ch.moving = false;
                Physics.SyncTransforms();
            }
            else
            {
                ch.moving = true;
            }
            audioBus?.From("drawerSlide", ch.transform.position, ch.targetOpen > 0.5f ? 0.92f : 0.78f);
            return ch.targetOpen > 0.5f;
        }

        public bool ToggleDoor(string id, EnvironmentalAudio audioBus, bool instant = false)
        {
            if (string.IsNullOrEmpty(id) || !doors.TryGetValue(id, out var ch) || ch.pivot == null)
                return false;
            bool open = Mathf.Abs(ch.targetAngle) < 12f;
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
            curtainPulled = !curtainPulled;
            if (curtainCloth != null)
                audioBus?.From("curtainSlide", curtainCloth.position);
            return curtainPulled;
        }

        public void SetCurtainPulled(bool pulled, bool instant = false)
        {
            curtainPulled = pulled;
            if (instant || reducedMotion)
            {
                curtainBlend = pulled ? 1f : 0f;
                ApplyCurtainPose();
            }
        }

        public void PulseCurtainDraft()
        {
            if (reducedMotion || curtainCloth == null) return;
            curtainDraftImpulse = 1f;
        }

        /// <summary>
        /// Optional tactile hold-and-drag entry for doors, drawers, and privacy curtains.
        /// Click/tap remains instant & fully accessible; dragging adjusts openness continuously with damped resistance.
        /// </summary>
        public bool BeginDrag(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (id == "CURTAIN-101" && curtainCloth != null) { draggingId = id; return true; }
            if (drawers.ContainsKey(id) || doors.ContainsKey(id)) { draggingId = id; return true; }
            return false;
        }

        public void UpdateDrag(Vector2 mouseDelta, EnvironmentalAudio audioBus)
        {
            if (string.IsNullOrEmpty(draggingId)) return;
            float impulse = (mouseDelta.x - mouseDelta.y * 0.55f) * 0.0045f;
            if (Mathf.Abs(impulse) < 0.0005f) return;

            if (dragAudioCooldown > 0f) dragAudioCooldown -= Time.deltaTime;

            if (draggingId == "CURTAIN-101" && curtainCloth != null)
            {
                curtainBlend = Mathf.Clamp01(curtainBlend + impulse * 1.15f);
                curtainPulled = curtainBlend > 0.5f;
                ApplyCurtainPose();
                if (dragAudioCooldown <= 0f)
                {
                    audioBus?.From("curtainSlide", curtainCloth.position, 0.35f);
                    dragAudioCooldown = 0.42f;
                }
                return;
            }

            if (drawers.TryGetValue(draggingId, out var dr) && dr.transform != null)
            {
                dr.currentOpen = Mathf.Clamp01(dr.currentOpen + impulse * 1.25f);
                dr.targetOpen = dr.currentOpen > 0.5f ? 1f : 0f;
                dr.moving = false;
                dr.transform.localPosition = dr.closedLocalPos + dr.openLocalOffset * dr.currentOpen;
                Physics.SyncTransforms();
                if (dragAudioCooldown <= 0f)
                {
                    audioBus?.From("drawerSlide", dr.transform.position, 0.42f);
                    dragAudioCooldown = 0.45f;
                }
                return;
            }

            if (doors.TryGetValue(draggingId, out var door) && door.pivot != null)
            {
                float sign = Mathf.Sign(door.maxOpenAngle != 0f ? door.maxOpenAngle : 1f);
                float maxMag = Mathf.Abs(door.maxOpenAngle != 0f ? door.maxOpenAngle : 96f);
                float curMag = Mathf.Clamp(Mathf.Abs(door.currentAngle) + impulse * 95f, 0f, maxMag);
                door.currentAngle = curMag * sign;
                door.targetAngle = curMag > maxMag * 0.35f ? door.maxOpenAngle : 0f;
                door.moving = false;
                door.pivot.localRotation = Quaternion.Euler(0f, door.currentAngle, 0f);
                Physics.SyncTransforms();
                if (dragAudioCooldown <= 0f)
                {
                    audioBus?.From("doorCreak", door.pivot.position + Vector3.up * 1.1f, 0.38f);
                    dragAudioCooldown = 0.55f;
                }
            }
        }

        public void EndDrag(EnvironmentalAudio audioBus)
        {
            if (string.IsNullOrEmpty(draggingId)) return;
            if (drawers.TryGetValue(draggingId, out var dr)) dr.moving = true;
            if (doors.TryGetValue(draggingId, out var door))
            {
                door.moving = true;
                if (Mathf.Abs(door.targetAngle) < 2f && door.pivot != null)
                    audioBus?.From("doorLatch", door.pivot.position + Vector3.up * 1.1f, 0.65f);
            }
            draggingId = null;
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
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool syncPhysics = false;

            if (curtainDraftImpulse > 0f)
                curtainDraftImpulse = Mathf.MoveTowards(curtainDraftImpulse, 0f, dt * 0.42f);

            if (curtainCloth != null && draggingId != "CURTAIN-101")
            {
                float target = curtainPulled ? 1f : 0f;
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
                dr.transform.localPosition = dr.closedLocalPos + dr.openLocalOffset * ease;
                if (Mathf.Abs(dr.currentOpen - dr.targetOpen) < 0.002f)
                {
                    dr.currentOpen = dr.targetOpen;
                    dr.transform.localPosition = dr.closedLocalPos + dr.openLocalOffset * dr.targetOpen;
                    dr.moving = false;
                }
                syncPhysics = true;
            }

            if (syncPhysics)
                Physics.SyncTransforms();
        }
    }
}
