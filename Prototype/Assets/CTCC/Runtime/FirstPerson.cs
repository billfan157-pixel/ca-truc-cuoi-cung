using UnityEngine;
using UnityEngine.InputSystem;

namespace CTCC
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPerson : MonoBehaviour
    {
        private CharacterController capsule;
        private Camera eye;
        private Light torch;
        private AudioSource footfalls;
        private EnvironmentalAudio audioBus;
        private VisitSettings settings;
        private float verticalSpeed, pitch, breathAfter, stepTravel;
        private float stamina=7;
        private bool torchOn;
        public int BatterySeconds => Mathf.CeilToInt(battery);
        private float battery=360;
        public Vector3 Feet => transform.position;
        public Camera Eye => eye;
        public bool CanPlay { get; set; }
        private readonly Collider[] nearby=new Collider[18];

        public void Initialize(Camera camera,EnvironmentalAudio bus)
        {
            capsule=GetComponent<CharacterController>();
            capsule.radius=HospitalScene.Radius;
            capsule.height=HospitalScene.Height;
            capsule.center=Vector3.up*(capsule.height*.5f);
            capsule.slopeLimit=43;
            capsule.stepOffset=.23f;
            capsule.skinWidth=.041f;
            capsule.minMoveDistance=0;
            eye=camera;
            camera.transform.SetParent(transform,false);
            camera.transform.localPosition=Vector3.up*1.62f;
            camera.transform.localRotation=Quaternion.identity;
            var lightObj=new GameObject("Pocket flashlight");
            lightObj.transform.SetParent(eye.transform,false);
            lightObj.transform.localPosition=new Vector3(.13f,-.12f,.055f);
            torch=lightObj.AddComponent<Light>();
            torch.type=LightType.Spot;
            torch.spotAngle=48f;
            torch.innerSpotAngle=27f;
            torch.color=new Color(1,.93f,.78f);
            torch.range=13f;
            torch.intensity=4.8f;
            torch.enabled=false;
            footfalls=gameObject.AddComponent<AudioSource>();
            footfalls.playOnAwake=false;
            audioBus=bus;
            settings=new VisitSettings();
        }

        public void Apply(VisitSettings options)
        {
            settings=options;
            eye.fieldOfView=Mathf.Clamp(options.fov,55,82);
        }

        public void RestoreBattery(int charge) => battery=Mathf.Max(120,charge);

        public void Teleport(Transform target) => Teleport(target.position,target.rotation);

        public void Teleport(Vector3 point,Quaternion rotation)
        {
            capsule.enabled=false;
            transform.SetPositionAndRotation(point,rotation);
            pitch=0;
            eye.transform.localRotation=Quaternion.identity;
            capsule.enabled=true;
            verticalSpeed=0;
            stamina=Mathf.Max(stamina,3);
            Physics.SyncTransforms();
        }

        private void Update()
        {
            if(!CanPlay || eye==null || !Application.isFocused)
                return;
            var k=Keyboard.current;
            var m=Mouse.current;
            if(k==null) return;
            if(m!=null && Cursor.lockState==CursorLockMode.Locked)
            {
                Vector2 delta=m.delta.ReadValue()*.079f;
                transform.Rotate(Vector3.up,delta.x);
                pitch=Mathf.Clamp(pitch-delta.y,-86,86);
                eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
            }
            float dt=Mathf.Min(Time.deltaTime,.045f);
            bool alternate=settings.alternateBindings;
            float x=Axis(k,alternate?Key.LeftArrow:Key.A,alternate?Key.RightArrow:Key.D);
            float z=Axis(k,alternate?Key.DownArrow:Key.S,alternate?Key.UpArrow:Key.W);
            bool crouch=k.leftCtrlKey.isPressed || k.cKey.isPressed;
            bool running=(k.leftShiftKey.isPressed || k.rightShiftKey.isPressed) && z>0 && !crouch && stamina>.01f;
            if(running)
            {
                stamina=Mathf.Max(0,stamina-dt);
                breathAfter=Time.unscaledTime+2;
            }
            else if(Time.unscaledTime>breathAfter) stamina=Mathf.Min(7,stamina+dt*(7/6f));

            float height=crouch?1.10f:HospitalScene.Height;
            if(!crouch && capsule.height<height && HospitalScene.Occupied(Feet))
                height=capsule.height;
            capsule.height=Mathf.MoveTowards(capsule.height,height,dt*3.45f);
            capsule.center=Vector3.up*(capsule.height*.5f);
            float speed=crouch?1.2f:running?4.2f:2.2f;
            Vector3 motion=Vector3.ClampMagnitude(new Vector3(x,0,z),1)*speed;
            Vector3 start=Feet;
            Move(transform.TransformDirection(motion),dt);
            float moved=Vector2.Distance(new Vector2(start.x,start.z),new Vector2(Feet.x,Feet.z));
            stepTravel+=moved;
            if(stepTravel>(running?1.1f:.73f))
            {
                audioBus?.Step(footfalls,running,crouch);
                stepTravel=0;
            }
            float y=capsule.height-.13f;
            if(!settings.reduceMotion && moved>.003f)
                y+=Mathf.Sin(Time.unscaledTime*(running?15:10))*.014f;
            eye.transform.localPosition=new Vector3(0,y,0);
            if(k[alternate?Key.L:Key.F].wasPressedThisFrame) torchOn=!torchOn;
            if(torchOn && battery>0) battery=Mathf.Max(0,battery-dt);
            torch.enabled=torchOn && battery>0;
            torch.intensity=4.55f+settings.visibility*1.5f;
        }

        private static float Axis(Keyboard keyboard,Key left,Key right) =>
            (keyboard[right].isPressed?1:0)-(keyboard[left].isPressed?1:0);

        public void Move(Vector3 planarMetresPerSecond,float seconds)
        {
            verticalSpeed=capsule.isGrounded ? -1.15f : Mathf.Max(-15,verticalSpeed-18*seconds);
            capsule.Move((planarMetresPerSecond+Vector3.up*verticalSpeed)*seconds);
        }

        public SceneTarget Focus(FuncTarget allowed)
        {
            var camera=eye.transform;
            const float range=2.55f;
            if(Physics.Raycast(camera.position,camera.forward,out var ray,range,
                HospitalScene.InteractMask|HospitalScene.SolidMask,QueryTriggerInteraction.Collide))
            {
                var target=ray.collider.GetComponent<SceneTarget>();
                if(target!=null && allowed(target)) return target;
            }
            Vector3 seed=camera.position+camera.forward*.62f;
            int count=Physics.OverlapSphereNonAlloc(seed,1.25f,nearby,HospitalScene.InteractMask,QueryTriggerInteraction.Collide);
            SceneTarget best=null;
            float bestWeight=.62f;
            for(int i=0;i<count;i++)
            {
                var target=nearby[i].GetComponent<SceneTarget>();
                if(target==null || !allowed(target)) continue;
                Vector3 to=target.transform.position-camera.position;
                float dist=to.magnitude;
                if(dist<.001f || dist>range) continue;
                float weight=Vector3.Dot(camera.forward,to/dist)-dist*.052f;
                if(weight<=bestWeight || Physics.Raycast(camera.position,to/dist,dist-.095f,HospitalScene.SolidMask,
                    QueryTriggerInteraction.Ignore)) continue;
                best=target;
                bestWeight=weight;
            }
            return best;
        }

        public delegate bool FuncTarget(SceneTarget target);
    }
}
