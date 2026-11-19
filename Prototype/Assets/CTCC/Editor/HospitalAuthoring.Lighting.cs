using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        private void LightAndStage()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline)
            {
                pipeline.renderScale=art?.88f:1;
                pipeline.shadowDistance=32f;
                pipeline.useSRPBatcher=true;
                pipeline.supportsCameraDepthTexture=true;
                pipeline.supportsCameraOpaqueTexture=false;
                EditorUtility.SetDirty(pipeline);
            }
            RenderSettings.ambientMode=AmbientMode.Flat;
            RenderSettings.fog=art;
            RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogDensity=.024f;
            RenderSettings.reflectionIntensity=.83f;

            var rig=Group("Seeded player view",world.transform,world.Pad("AP00").position+Vector3.up*1.62f);
            var camera=rig.gameObject.AddComponent<Camera>();
            rig.tag="MainCamera";
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.16f,.23f,.26f);
            camera.fieldOfView=62f;
            camera.nearClipPlane=.055f;
            camera.farClipPlane=53;
            camera.allowHDR=true;
            camera.allowMSAA=false;
            var parameters=camera.GetUniversalAdditionalCameraData();
            parameters.renderPostProcessing=art;
            parameters.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            rig.gameObject.AddComponent<AudioListener>();

            var moon=Group("Overcast blue from yard",permanent).gameObject.AddComponent<Light>();
            moon.type=LightType.Directional;
            moon.transform.rotation=Quaternion.Euler(36,186,0);
            moon.color=new Color(.63f,.77f,.86f);
            moon.intensity=art?1.1f:1.8f;
            moon.shadows=LightShadows.Soft;
            moon.shadowStrength=.83f;
            moon.shadowNormalBias=.49f;

            var courtyard=new Color(.56f,.71f,.82f);
            Lamp("Key below entry canopy",LightType.Point,new Vector3(4.2f,2.34f,-9.55f),.97f,9.0f,courtyard,permanent);
            Lamp("Bounced entry spill",LightType.Point,new Vector3(5.1f,2.32f,-5.75f),.73f,8.4f,courtyard,permanent);
            Lamp("Street wash at the gate",LightType.Point,new Vector3(6.03f,3.60f,-19.75f),1.44f,11.1f,
                new Color(.70f,.78f,.78f),permanent);
            Lamp("Emergency glass return",LightType.Point,new Vector3(6.20f,2.65f,19.48f),.94f,9.8f,courtyard,permanent);
            Lamp("Outer-room base fill",LightType.Point,new Vector3(9.5f,2.31f,6.45f),1.36f,7.4f,
                new Color(.65f,.78f,.83f),real);
            Lamp("Back wall path separation",LightType.Point,new Vector3(12.18f,2.28f,4.85f),.62f,6.5f,
                new Color(.86f,.83f,.71f),real);
            Lamp("Dry store readable bounce",LightType.Point,new Vector3(15.75f,2.45f,3.8f),.72f,5.1f,new Color(.84f,.80f,.67f),real);
            Lamp("Dull corridor bounce",LightType.Point,new Vector3(4.1f,2.55f,4.93f),1.36f,8.5f,
                new Color(.60f,.77f,.78f),real);
            Lamp("Frosted end of corridor",LightType.Point,new Vector3(4.72f,2.32f,11.68f),.49f,7.2f,courtyard,real);
            Lamp("Wayfinding on staff corridor",LightType.Point,new Vector3(.2f,2.66f,8.3f),.50f,11.0f,courtyard,permanent);
            Lamp("Desk readable spill",LightType.Point,new Vector3(2.05f,2.71f,-3.7f),.64f,6.6f,
                new Color(.65f,.81f,.76f),real);

            var clean=new Color(.88f,.97f,.89f);
            Tube(new Vector3(4.07f,3.45f,3.62f),0,clean,1.45f);
            Tube(new Vector3(4.07f,3.45f,9.38f),0,clean,1.23f);
            Tube(new Vector3(1.90f,3.45f,-3.2f),0,clean,1.64f);
            Tube(new Vector3(6.32f,3.45f,-4.58f),0,clean,1.33f);
            Tube(new Vector3(7.77f,3.40f,5.01f),90,clean,1.55f);
            Tube(new Vector3(11.42f,3.44f,5.66f),90,clean,1.48f);
            if (!art)
                return;

            for(int i=0;i<2;i++)
            {
                var from = new Vector3(7.50f + 2.77f*i,2.29f,10.05f);
                var to = new Vector3(8.23f + 2.76f*i,.21f,4.42f);
                var beam=Lamp("Window key from just beyond the sash",LightType.Spot,from,i==0?6.2f:4.75f,
                    10.75f,new Color(.65f,.82f,.91f),real);
                beam.transform.LookAt(to);
                beam.spotAngle=60;
                beam.innerSpotAngle=37f;
                if(i==0)
                {
                    beam.shadows=LightShadows.Soft;
                    beam.shadowStrength=.87f;
                }
                LightShaft(new Vector3(from.x,1.89f,7.995f),to,.90f,1.04f,real);
            }

            var reflection=Group("Local reflection · LOC03",permanent,new Vector3(9.71f,1.83f,5.18f))
                .gameObject.AddComponent<ReflectionProbe>();
            reflection.mode=ReflectionProbeMode.Realtime;
            reflection.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            reflection.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
            reflection.resolution=128;
            reflection.boxProjection=true;
            reflection.size=new Vector3(9.2f,3.6f,6.12f);
            reflection.intensity=.67f;
            reflection.clearFlags=ReflectionProbeClearFlags.SolidColor;
            reflection.backgroundColor=new Color(.16f,.23f,.25f);
            world.localReflection=reflection;

            Fan(new Vector3(7.4f,3.30f,5.12f));
            Fan(new Vector3(11.12f,3.30f,5.09f));
            Fan(new Vector3(4.30f,3.28f,-3.94f));
            Rain(new Vector3(9.72f,4.1f,8.99f),new Vector3(8.4f,.18f,1.64f),320);
            Rain(new Vector3(4,4.6f,-13.6f),new Vector3(13,.21f,10.8f),420);
            world.realGrade=Grade("2012",.49f,-13f,5.5f,new Color(.94f,.98f,1),.14f);
            world.reportGrade=Grade("2005",.08f,-11f,6f,new Color(.97f,1,.95f),.13f);

            var motion=Group("Ceiling fans and fabric",permanent).gameObject.AddComponent<EnvironmentMotion>();
            motion.blades=movingFans.ToArray();
            motion.tubes=livingTubes.ToArray();
            motion.curtains=movingCloth.ToArray();
            world.motion=motion;
            world.atmosphere=BuildAudio();
        }

        private static Light Lamp(string label,LightType type,Vector3 p,float intensity,float range,Color color,Transform parent)
        {
            var l=Group(label,parent,p).gameObject.AddComponent<Light>();
            l.type=type;
            l.range=range;
            l.intensity=intensity;
            l.color=color;
            l.shadows=LightShadows.None;
            return l;
        }

        private void Tube(Vector3 p,float yaw,Color clean,float intensity)
        {
            var lit=Lamp("Mains tube pool",LightType.Point,p+Vector3.down*.16f,intensity,6.5f,clean,report);
            livingTubes.Add(lit);
            if(!art) return;
            var mount=Group("Fluorescent fitting · backing trough",permanent,p);
            mount.localRotation=Quaternion.Euler(0,yaw,0);
            Box("Metal ballast trough",new Vector3(0,.052f,0),new Vector3(.174f,.08f,1.27f),Mat("Frame"),mount);
            for(int i=0;i<2;i++)
            {
                var bulb=Box("Opaline T8 tube",new Vector3(-.049f+i*.098f,0,0),new Vector3(.041f,.045f,1.18f),Mat("DeadTube"),mount);
                world.finishes.Add(new LayerFinish{renderer=bulb.GetComponent<Renderer>(),real=Mat("DeadTube"),report=Mat("LiveTube")});
            }
            for(int i=-1;i<=1;i+=2)
                Box("Socket housing",new Vector3(0,-.014f,i*.613f),new Vector3(.169f,.076f,.064f),Mat("Ceramic"),mount);
        }

        private void Fan(Vector3 p)
        {
            var fan=Group("Three-blade enamel ceiling fan",permanent,p);
            Round("Drop stem",new Vector3(0,.16f,0),new Vector3(.041f,.162f,.041f),Mat("Frame"),fan);
            Round("Motor dome",new Vector3(0,.024f,0),new Vector3(.205f,.068f,.207f),Mat("MetalPaint"),fan);
            var rotor=Group("Rotor",fan);
            for(int i=0;i<3;i++)
            {
                var blade=Group("Pressed blade",rotor);
                blade.localRotation=Quaternion.Euler(0,i*120,0);
                var root=Box("Cast blade arm",new Vector3(0,-.028f,.181f),new Vector3(.065f,.025f,.27f),Mat("MetalOld"),blade);
                root.transform.localRotation=Quaternion.Euler(0,9,0);
                var tip=Box("Airfoil",new Vector3(.06f,-.042f,.51f),new Vector3(.155f,.025f,.58f),Mat("Ceramic"),blade);
                tip.transform.localRotation=Quaternion.Euler(0,13,5);
            }
            movingFans.Add(rotor);
        }

        private Volume Grade(string key,float exposure,float saturation,float contrast,Color filter,float vignette)
        {
            string path=Root+"/Generated/"+key+"-grade.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile,path);
            }
            T Part<T>() where T:VolumeComponent
            {
                if(profile.TryGet<T>(out var component)) return component;
                component=profile.Add<T>(true);
                AssetDatabase.AddObjectToAsset(component,profile);
                return component;
            }
            var tone=Part<Tonemapping>();
            tone.mode.Override(TonemappingMode.ACES);
            var color=Part<ColorAdjustments>();
            color.postExposure.Override(exposure);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.colorFilter.Override(filter);
            var bloom=Part<Bloom>();
            bloom.intensity.Override(.24f);
            bloom.threshold.Override(1.09f);
            bloom.scatter.Override(.56f);
            var vig=Part<Vignette>();
            vig.intensity.Override(vignette);
            vig.smoothness.Override(.40f);
            foreach(var comp in profile.components) EditorUtility.SetDirty(comp);
            EditorUtility.SetDirty(profile);
            var v=Group("Color grade · "+key,permanent).gameObject.AddComponent<Volume>();
            v.isGlobal=true;
            v.sharedProfile=profile;
            return v;
        }

        private void LightShaft(Vector3 source,Vector3 end,float width,float height,Transform parent)
        {
            var node=Group("Soft window scatter",parent);
            Vector3 left=Vector3.right*(width*.5f), up=Vector3.up*(height*.5f);
            var mesh=new Mesh {name="Authored window shaft"};
            mesh.vertices=new[]{source-left,source+left,end+left*2.0f,end-left*2.0f,
                source-up*.9f,source+up*.9f,end+up*1.35f,end-up*.52f};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1),
                new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            mesh.triangles=new[]{0,1,2,2,3,0,4,5,6,6,7,4};
            mesh.RecalculateBounds();
            StoreMesh(mesh);
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=Mat("Beam");
            renderer.shadowCastingMode=ShadowCastingMode.Off;
        }

        private void Rain(Vector3 position,Vector3 footprint,float rate)
        {
            var p=Group("Exterior rain",real,position).gameObject.AddComponent<ParticleSystem>();
            var main=p.main;
            main.loop=true;
            main.startLifetime=.68f;
            main.startSpeed=0;
            main.startSize=.011f;
            main.startColor=new Color(.61f,.74f,.81f,.32f);
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.maxParticles=560;
            var shape=p.shape;
            shape.shapeType=ParticleSystemShapeType.Box;
            shape.scale=footprint;
            var emission=p.emission;
            emission.rateOverTime=rate;
            var velocity=p.velocityOverLifetime;
            velocity.enabled=true;
            velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=.84f;
            velocity.y=-8.7f;
            velocity.z=-.28f;
            const string path=Root+"/Generated/Materials/WeatherRain.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null)
            {
                mat=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat,path);
            }
            Transparent(mat,new Color(.71f,.81f,.89f,.3f));
            var renderer=p.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=mat;
            renderer.renderMode=ParticleSystemRenderMode.Stretch;
            renderer.lengthScale=2.6f;
            renderer.velocityScale=.081f;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
        }

        private EnvironmentalAudio BuildAudio()
        {
            const string audioPath=Root+"/Art/Audio/";
            AudioClip Clip(string id) => AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath+id+".wav");
            var root=Group("Spatial ambience and prop cues",permanent);
            var bus=root.gameObject.AddComponent<EnvironmentalAudio>();
            AudioSource Source(string name,Vector3 point,float max)
            {
                var item=Group(name,root,point).gameObject.AddComponent<AudioSource>();
                item.playOnAwake=false;
                item.dopplerLevel=0;
                item.rolloffMode=AudioRolloffMode.Linear;
                item.minDistance=1.5f;
                item.maxDistance=max;
                item.spatialBlend=.96f;
                return item;
            }
            RoomLoop Loop(string id,Vector3 point,float level,float max,bool b)
            {
                var item=Source(id,point,max);
                item.loop=true;
                item.clip=Clip(id);
                return new RoomLoop{source=item,level=level,report=b};
            }
            bus.loops=new[]
            {
                Loop("Rain",new Vector3(4,1.2f,-12.2f),.59f,22,false),
                Loop("Roof",new Vector3(8.81f,2.1f,8.46f),.70f,13,false),
                Loop("Rumble",new Vector3(5.52f,2.2f,.10f),.45f,21,false),
                Loop("Drips",new Vector3(9.56f,.39f,7.71f),.68f,8,false),
                Loop("Fan",new Vector3(1.36f,2.85f,-3.77f),.55f,13,true),
                Loop("Fan",new Vector3(9.31f,2.94f,5.41f),.64f,10,true),
                Loop("Tube",new Vector3(4.07f,3.31f,3.66f),.49f,9,true)
            };
            bus.propCue=Source("Localized interaction cue",Vector3.zero,13);
            bus.paper=Clip("Paper"); bus.stamp=Clip("Stamp"); bus.slippers=Clip("Slipper");
            bus.step=Clip("Step"); bus.switchLayer=Clip("Switch");
            var reverb=Group("Short tiled room reflections",root,new Vector3(9.64f,1.65f,4.98f))
                .gameObject.AddComponent<AudioReverbZone>();
            reverb.minDistance=3.1f; reverb.maxDistance=6.2f; reverb.reverbPreset=AudioReverbPreset.Room;
            return bus;
        }
    }
}
