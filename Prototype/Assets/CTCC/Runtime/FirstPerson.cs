using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPerson : MonoBehaviour
    {
        internal const float WalkSpeed = 2.2f;
        internal static float MovementDeltaTime => Mathf.Min(Time.deltaTime, 0.045f);

        private CharacterController capsule;
        private Camera eye;
        private Transform torchPivot;
        private Light torch;
        private Light torchFlood;
        private Light torchSpill;
        private AudioSource footfalls;
        private EnvironmentalAudio audioBus;
        private VisitSettings settings;
        private Vector3 currentVelocity;
        private float verticalSpeed, pitch, breathAfter, stepTravel, bobPhase;
        private float torchLagX, torchLagY;
        private float stamina = 7f;
        private bool torchOn;
        public bool ReportLayer { get; set; }
        public int BatterySeconds => Mathf.CeilToInt(battery);
        private float battery = 360f;
        public Vector3 Feet => transform.position;
        public Camera Eye => eye;
        public bool IsTorchOn => torchOn && battery > 0f;
        public float TorchIntensity => torch != null && torch.enabled ? torch.intensity : 0f;
        public float TorchRange => torch != null ? torch.range : 0f;
        public bool CanPlay { get; set; }
        public bool LookSuppressed { get; set; }
        public bool ExternalCameraControl { get; set; }
        public bool IsCrouching => capsule != null && capsule.height < 1.35f;
        public bool IsRunning => currentVelocity.sqrMagnitude > 9.0f;
        public bool IsMoving => currentVelocity.sqrMagnitude > 0.05f;
        private readonly Collider[] nearby = new Collider[64];
        private SceneTarget focusedTarget;
        internal SceneTarget BeyondReach { get; private set; }

        public struct CameraPoseSnapshot
        {
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
            public Vector3 LocalPosition;
            public Quaternion BodyRotation;
            public float Pitch;
            public float Fov;
        }

        public CameraPoseSnapshot CaptureCameraPose() => new()
        {
            WorldPosition = eye != null ? eye.transform.position : transform.position + Vector3.up * 1.62f,
            WorldRotation = eye != null ? eye.transform.rotation : transform.rotation,
            LocalPosition = eye != null ? eye.transform.localPosition : Vector3.up * 1.62f,
            BodyRotation = transform.rotation,
            Pitch = pitch,
            Fov = eye != null ? eye.fieldOfView : 68f
        };

        public void RestoreCameraPose(CameraPoseSnapshot snap)
        {
            ExternalCameraControl = false;
            transform.rotation = snap.BodyRotation;
            pitch = Mathf.Clamp(snap.Pitch, -86f, 86f);
            if (eye != null)
            {
                eye.transform.localPosition = snap.LocalPosition;
                eye.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                eye.fieldOfView = Mathf.Clamp(settings != null ? settings.fov : snap.Fov, 55f, 82f);
            }
        }

        public void Initialize(Camera camera, EnvironmentalAudio bus)
        {
            capsule = GetComponent<CharacterController>();
            capsule.radius = HospitalScene.Radius;
            capsule.height = HospitalScene.Height;
            capsule.center = Vector3.up * (capsule.height * 0.5f);
            capsule.slopeLimit = 43f;
            capsule.stepOffset = 0.23f;
            capsule.skinWidth = 0.041f;
            capsule.minMoveDistance = 0f;

            eye = camera;
            camera.transform.SetParent(transform, false);
            camera.transform.localPosition = Vector3.up * 1.62f;
            camera.transform.localRotation = Quaternion.identity;

            torchPivot = new GameObject("Flashlight Hand Rig").transform;
            torchPivot.SetParent(eye.transform, false);
            torchPivot.localPosition = new Vector3(0.14f, -0.13f, 0.06f);

            torch = torchPivot.gameObject.AddComponent<Light>();
            torch.type = LightType.Spot;
            // Single unified wide-halo flashlight cone (34° bright core + smooth 82° reflector spill) eliminates redundant 2nd full-screen SpotLight pass!
            torch.spotAngle = 82f;
            torch.innerSpotAngle = 34f;
            torch.color = new Color(1.0f, 0.96f, 0.88f);
            torch.range = 34.0f;
            torch.intensity = 38.0f;
            torch.shadows = LightShadows.Soft;
            torch.shadowStrength = 0.86f;
            torch.shadowBias = 0.03f;
            torch.shadowNormalBias = 0.25f;
            torch.shadowNearPlane = 0.15f;
            torch.enabled = false;

            // Legacy secondary flood object retained (disabled) to keep zero overhead while preserving rig hierarchy
            var floodGo = new GameObject("Flashlight Wide Flood");
            floodGo.transform.SetParent(torchPivot, false);
            floodGo.transform.localPosition = Vector3.zero;
            torchFlood = floodGo.AddComponent<Light>();
            torchFlood.type = LightType.Spot;
            torchFlood.spotAngle = 92f;
            torchFlood.innerSpotAngle = 48f;
            torchFlood.color = new Color(1.0f, 0.95f, 0.86f);
            torchFlood.range = 10.0f;
            torchFlood.intensity = 0f;
            torchFlood.shadows = LightShadows.None;
            torchFlood.enabled = false;

            // Soft close-range bounce spill light so nearby walls/floors stay clearly readable when flashlight is ON
            var spillGo = new GameObject("Flashlight Bounce Spill");
            spillGo.transform.SetParent(torchPivot, false);
            spillGo.transform.localPosition = new Vector3(0f, 0.04f, 0.22f);
            torchSpill = spillGo.AddComponent<Light>();
            torchSpill.type = LightType.Point;
            torchSpill.color = new Color(1.0f, 0.95f, 0.86f);
            torchSpill.range = 6.8f;
            torchSpill.intensity = 2.6f;
            torchSpill.shadows = LightShadows.None;
            torchSpill.enabled = false;

            footfalls = gameObject.AddComponent<AudioSource>();
            footfalls.playOnAwake = false;
            audioBus = bus;
            settings = new VisitSettings();
        }

        public void Apply(VisitSettings options)
        {
            settings = options;
            if (eye != null)
                eye.fieldOfView = Mathf.Clamp(options.fov, 55f, 82f);
        }

        public void RestoreBattery(int charge) => battery = Mathf.Max(120, charge);
        internal void SetBatteryForQa(float seconds) => battery = Mathf.Max(0f, seconds);

        /// <summary>
        /// Adapts flashlight ranges & shadow atlas tier cleanly for enclosed indoor rooms (Ward 101 / Corridor / Store)
        /// vs wide exterior courtyard so SpotLight shadows never pull distant exterior campus geometry indoors!
        /// </summary>
        internal void SetIndoorBudget(bool indoor)
        {
            if (torch != null)
            {
                torch.range = indoor ? 15.0f : 34.0f;
                var urp = torch.GetUniversalAdditionalLightData();
                if (urp != null)
                {
                    // URP 17 native PCF shadow filter quality: 4-tap Low on Medium/Low saves 4x per-pixel shadow bandwidth on iGPU!
                    urp.softShadowQuality = QualitySettings.GetQualityLevel() <= 1
                        ? SoftShadowQuality.Low
                        : SoftShadowQuality.High;
                }
            }
            if (torchFlood != null) torchFlood.enabled = false;
            if (torchSpill != null) torchSpill.range = indoor ? 4.6f : 6.8f;
        }

        public void SetTorchEnabled(bool on)
        {
            SetIndoorBudget(HospitalScene.IsEnclosedIndoorBudget(transform.position));
            torchOn = on;
            bool active = on && battery > 0f;
            float vis = settings != null ? settings.visibility : 0f;
            if (torch != null)
            {
                torch.enabled = active;
                if (active) torch.intensity = 38.0f + vis * 8.5f;
            }
            if (torchFlood != null)
                torchFlood.enabled = false;
            if (torchSpill != null)
            {
                torchSpill.enabled = active;
                if (active) torchSpill.intensity = 2.6f + vis * 0.7f;
            }
        }

        public void Teleport(Transform target) => Teleport(target.position, target.rotation);

        public void Teleport(Vector3 point, Quaternion rotation)
        {
            ClearFocus();
            capsule.enabled = false;
            transform.SetPositionAndRotation(point, rotation);
            pitch = 0f;
            currentVelocity = Vector3.zero;
            eye.transform.localRotation = Quaternion.identity;
            capsule.enabled = true;
            verticalSpeed = 0f;
            stamina = Mathf.Max(stamina, 3f);
            Physics.SyncTransforms();
            SetIndoorBudget(HospitalScene.IsEnclosedIndoorBudget(point));
        }

        private void Update()
        {
            bool focusedOrIgnored = Application.isFocused ||
                InputSystem.settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus;
            if (!CanPlay || ExternalCameraControl || eye == null || !focusedOrIgnored)
                return;
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k == null) return;
            if (LookSuppressed)
            {
                currentVelocity = Vector3.zero;
                Vector3 rest = Vector3.up * (capsule.height - .16f);
                eye.transform.localPosition = Vector3.Lerp(eye.transform.localPosition, rest,
                    1f - Mathf.Exp(-9f * Time.unscaledDeltaTime));
                return;
            }

            float dt = MovementDeltaTime;
            float sens = settings.mouseSensitivity > 0.05f ? settings.mouseSensitivity : 1f;
            Vector2 lookDelta = Vector2.zero;
            if (!LookSuppressed && m != null && Cursor.lockState == CursorLockMode.Locked)
            {
                lookDelta = m.delta.ReadValue() * (0.079f * sens);
                transform.Rotate(Vector3.up, lookDelta.x);
                pitch = Mathf.Clamp(pitch - lookDelta.y, -86f, 86f);
                eye.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            bool alternate = settings.alternateBindings;
            float x = Axis(k, alternate ? Key.LeftArrow : Key.A, alternate ? Key.RightArrow : Key.D);
            float z = Axis(k, alternate ? Key.DownArrow : Key.S, alternate ? Key.UpArrow : Key.W);
            bool crouch = k.leftCtrlKey.isPressed || k.cKey.isPressed;
            bool running = (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed) && z > 0f && !crouch && stamina > 0.01f;
            if (running)
            {
                stamina = Mathf.Max(0f, stamina - dt);
                breathAfter = Time.unscaledTime + 2f;
            }
            else if (Time.unscaledTime > breathAfter)
                stamina = Mathf.Min(7f, stamina + dt * (7f / 6f));

            float height = crouch ? 1.10f : HospitalScene.Height;
            if (!crouch && capsule.height < height && HospitalScene.Occupied(Feet))
                height = capsule.height;
            capsule.height = Mathf.MoveTowards(capsule.height, height, dt * 3.6f);
            capsule.center = Vector3.up * (capsule.height * 0.5f);

            float speed = crouch ? 1.2f : running ? 4.2f : WalkSpeed;
            Vector3 desired = transform.TransformDirection(Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f) * speed);
            // Weighty grounded acceleration & deceleration for realistic first-person horror feel
            float accel = desired.sqrMagnitude > 0.01f ? 14f : 11f;
            currentVelocity = Vector3.MoveTowards(currentVelocity, desired, accel * dt);

            Vector3 start = Feet;
            Move(currentVelocity, dt);
            float moved = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(Feet.x, Feet.z));
            stepTravel += moved;
            if (stepTravel > (running ? 1.08f : 0.72f))
            {
                audioBus?.Step(footfalls, running, crouch, Feet, ReportLayer);
                stepTravel = 0f;
            }

            float y = capsule.height - 0.13f;
            float bobX = 0f;
            if (!settings.reduceMotion)
            {
                if (moved > 0.002f)
                {
                    bobPhase += dt * (running ? 13.5f : crouch ? 6.5f : 9.2f);
                    y += Mathf.Sin(bobPhase) * (running ? 0.022f : 0.013f);
                    bobX = Mathf.Cos(bobPhase * 0.5f) * (running ? 0.012f : 0.007f);
                }
                else
                {
                    // Gentle idle breathing sway
                    y += Mathf.Sin(Time.unscaledTime * 1.45f) * 0.004f;
                }
            }
            Vector3 targetEyePos = new Vector3(bobX, y, 0f);
            eye.transform.localPosition = Vector3.Lerp(eye.transform.localPosition, targetEyePos,
                1f - Mathf.Exp(-16f * dt));

            // Subtle natural handheld flashlight lag & low-battery flicker warning
            if (k[alternate ? Key.L : Key.F].wasPressedThisFrame)
            {
                SetTorchEnabled(!torchOn);
                audioBus?.From("doorLatch", eye.transform.position);
            }
            if (torchOn && !torchSuppressed && battery > 0f) battery = Mathf.Max(0f, battery - dt);
            bool activeTorch = !torchSuppressed && torchOn && battery > 0f;
            torch.enabled = activeTorch;
            float flicker = (!settings.reduceMotion && battery < 45f)
                ? 0.72f + 0.28f * Mathf.PerlinNoise(Time.unscaledTime * 14f, 2.3f)
                : 1.0f;
            torch.intensity = (38.0f + settings.visibility * 8.5f) * flicker;
            if (torchFlood != null)
                torchFlood.enabled = false;
            if (torchSpill != null)
            {
                torchSpill.enabled = activeTorch;
                torchSpill.intensity = (2.6f + settings.visibility * 0.7f) * flicker;
            }

            if (!settings.reduceMotion)
            {
                torchLagX = Mathf.Lerp(torchLagX, Mathf.Clamp(-lookDelta.x * 1.6f, -5.5f, 5.5f), dt * 8f);
                torchLagY = Mathf.Lerp(torchLagY, Mathf.Clamp(lookDelta.y * 1.4f, -4.5f, 4.5f), dt * 8f);
                torchPivot.localRotation = Quaternion.Euler(torchLagY, torchLagX, 0f);
            }
            else
            {
                torchPivot.localRotation = Quaternion.identity;
            }
        }

        private static float Axis(Keyboard keyboard, Key left, Key right) =>
            (keyboard[right].isPressed ? 1 : 0) - (keyboard[left].isPressed ? 1 : 0);

        public void NudgeWithinScene(Vector3 displacement)
        {
            capsule.Move(displacement);
            currentVelocity = Vector3.zero;
        }

        public void Move(Vector3 planarMetresPerSecond, float seconds)
        {
            verticalSpeed = capsule.isGrounded ? -1.15f : Mathf.Max(-15f, verticalSpeed - 18f * seconds);
            capsule.Move((planarMetresPerSecond + Vector3.up * verticalSpeed) * seconds);
        }

        public SceneTarget Focus(FuncTarget allowed)
        {
            var camera = eye.transform;
            Vector3 origin = camera.position;
            Vector3 forward = camera.forward;
            int count = Physics.OverlapSphereNonAlloc(origin + forward * 1.55f, 1.78f, nearby,
                HospitalScene.InteractMask, QueryTriggerInteraction.Collide);
            var aim = new Ray(origin, forward);
            SceneTarget best = null;
            BeyondReach = null;
            float farDistance = 3.15f;
            float bestWeight = float.NegativeInfinity;
            float heldWeight = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = nearby[i];
                var target = candidate.GetComponent<SceneTarget>();
                if (target == null || !allowed(target)) continue;
                Vector3 center = target.handhold != null ? target.Contact : candidate.bounds.center;
                Vector3 offset = center - origin;
                float distance = offset.magnitude;
                if (distance < 0.01f || distance > 3.4f) continue;
                float alignment = Vector3.Dot(forward, offset / distance);
                bool direct = candidate.Raycast(aim, out var hit, 3.15f);
                bool reachable = target.handhold != null ? distance <= SceneTarget.Reach :
                    direct ? hit.distance <= SceneTarget.Reach && distance <= SceneTarget.Reach + .30f : distance <= SceneTarget.Reach;
                if (!reachable)
                {
                    if (direct && hit.distance < farDistance && target.VisibleFrom(origin, center))
                    {
                        farDistance = hit.distance;
                        BeyondReach = target;
                    }
                    continue;
                }
                if (!direct && alignment < (target == focusedTarget ? 0.945f : 0.965f))
                    continue;

                if (!target.VisibleFrom(origin, center)) continue;
                float weight = alignment - (direct ? hit.distance : distance) * 0.028f +
                    (direct ? 0.08f : 0f) + target.focusPriority * 0.025f;
                if (target == focusedTarget) heldWeight = weight;
                if (weight <= bestWeight) continue;
                best = target;
                bestWeight = weight;
            }
            // Hold only while the previous target still passes reach and visibility.
            if (heldWeight > float.NegativeInfinity && heldWeight + 0.025f >= bestWeight)
                return focusedTarget;
            return focusedTarget = best;
        }

        private bool torchSuppressed;
        public bool TorchSuppressed
        {
            get => torchSuppressed;
            set
            {
                if (torchSuppressed == value) return;
                torchSuppressed = value;
                SyncTorchVisuals();
            }
        }

        private void SyncTorchVisuals()
        {
            bool activeTorch = !torchSuppressed && torchOn && battery > 0f;
            if (torch != null) torch.enabled = activeTorch;
            if (torchFlood != null) torchFlood.enabled = activeTorch;
            if (torchSpill != null) torchSpill.enabled = activeTorch;
        }

        public void ClearFocus()
        {
            focusedTarget = null;
            BeyondReach = null;
        }

        internal void SetViewRotation(Quaternion rotation)
        {
            var angles = rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, angles.y, 0f);
            pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angles.x), -86f, 86f);
            eye.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public delegate bool FuncTarget(SceneTarget target);
    }
}
