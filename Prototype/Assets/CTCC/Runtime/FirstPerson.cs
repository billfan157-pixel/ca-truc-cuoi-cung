using UnityEngine;
using UnityEngine.InputSystem;

namespace CTCC
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPerson : MonoBehaviour
    {
        private CharacterController capsule;
        private Camera eye;
        private Transform torchPivot;
        private Light torch;
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
        public bool CanPlay { get; set; }
        public bool IsCrouching => capsule != null && capsule.height < 1.35f;
        public bool IsRunning => currentVelocity.sqrMagnitude > 9.0f;
        public bool IsMoving => currentVelocity.sqrMagnitude > 0.05f;
        private readonly Collider[] nearby = new Collider[18];

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
            torch.spotAngle = 52f;
            torch.innerSpotAngle = 24f;
            torch.color = new Color(1.0f, 0.92f, 0.78f);
            torch.range = 15.5f;
            torch.intensity = 5.4f;
            // Real soft shadows from the player's flashlight — huge atmospheric horror upgrade!
            torch.shadows = LightShadows.Soft;
            torch.shadowStrength = 0.90f;
            torch.shadowBias = 0.03f;
            torch.shadowNormalBias = 0.25f;
            torch.shadowNearPlane = 0.15f;
            torch.enabled = false;

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

        public void SetTorchEnabled(bool on)
        {
            torchOn = on;
            if (torch != null)
                torch.enabled = on && battery > 0f;
        }

        public void Teleport(Transform target) => Teleport(target.position, target.rotation);

        public void Teleport(Vector3 point, Quaternion rotation)
        {
            capsule.enabled = false;
            transform.SetPositionAndRotation(point, rotation);
            pitch = 0f;
            currentVelocity = Vector3.zero;
            eye.transform.localRotation = Quaternion.identity;
            capsule.enabled = true;
            verticalSpeed = 0f;
            stamina = Mathf.Max(stamina, 3f);
            Physics.SyncTransforms();
        }

        private void Update()
        {
            bool focusedOrIgnored = Application.isFocused ||
                InputSystem.settings.backgroundBehavior == InputSettings.BackgroundBehavior.IgnoreFocus;
            if (!CanPlay || eye == null || !focusedOrIgnored)
                return;
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k == null) return;

            float dt = Mathf.Min(Time.deltaTime, 0.045f);
            float sens = settings.mouseSensitivity > 0.05f ? settings.mouseSensitivity : 1f;
            Vector2 lookDelta = Vector2.zero;
            if (m != null && Cursor.lockState == CursorLockMode.Locked)
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

            float speed = crouch ? 1.2f : running ? 4.2f : 2.2f;
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
            eye.transform.localPosition = new Vector3(bobX, y, 0f);

            // Subtle natural handheld flashlight lag & low-battery flicker warning
            if (k[alternate ? Key.L : Key.F].wasPressedThisFrame)
            {
                torchOn = !torchOn;
                audioBus?.From("doorLatch", eye.transform.position);
            }
            if (torchOn && battery > 0f) battery = Mathf.Max(0f, battery - dt);
            torch.enabled = torchOn && battery > 0f;
            float flicker = (!settings.reduceMotion && battery < 45f)
                ? 0.72f + 0.28f * Mathf.PerlinNoise(Time.unscaledTime * 14f, 2.3f)
                : 1.0f;
            torch.intensity = (5.2f + settings.visibility * 1.6f) * flicker;

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

        public void Move(Vector3 planarMetresPerSecond, float seconds)
        {
            verticalSpeed = capsule.isGrounded ? -1.15f : Mathf.Max(-15f, verticalSpeed - 18f * seconds);
            capsule.Move((planarMetresPerSecond + Vector3.up * verticalSpeed) * seconds);
        }

        public SceneTarget Focus(FuncTarget allowed)
        {
            var camera = eye.transform;
            const float range = 2.60f;
            if (Physics.Raycast(camera.position, camera.forward, out var ray, range,
                HospitalScene.InteractMask | HospitalScene.SolidMask, QueryTriggerInteraction.Collide))
            {
                var target = ray.collider.GetComponent<SceneTarget>();
                if (target != null && allowed(target)) return target;
            }
            Vector3 seed = camera.position + camera.forward * 0.62f;
            int count = Physics.OverlapSphereNonAlloc(seed, 1.25f, nearby, HospitalScene.InteractMask, QueryTriggerInteraction.Collide);
            SceneTarget best = null;
            float bestWeight = 0.64f;
            for (int i = 0; i < count; i++)
            {
                var target = nearby[i].GetComponent<SceneTarget>();
                if (target == null || !allowed(target)) continue;
                Vector3 to = target.transform.position - camera.position;
                float dist = to.magnitude;
                if (dist < 0.001f || dist > range) continue;
                float weight = Vector3.Dot(camera.forward, to / dist) - dist * 0.052f;
                if (weight <= bestWeight || Physics.Raycast(camera.position, to / dist, dist - 0.095f, HospitalScene.SolidMask,
                    QueryTriggerInteraction.Ignore)) continue;
                best = target;
                bestWeight = weight;
            }
            return best;
        }

        public delegate bool FuncTarget(SceneTarget target);
    }
}
