using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CTCC.Editor
{
    public sealed partial class HospitalAuthoring
    {
        private const string Maps = Root + "/Art/Textures/";
        private const string Models = Root + "/Art/Meshes/";
        private readonly Dictionary<string,Dictionary<string,Mesh>> mergedModels = new();

        private void PrepareMaterials()
        {
            if (art)
            {
                var files = Directory.GetFiles(Maps,"*.png");
                foreach (var f in files)
                {
                    string path = f.Replace('\\','/');
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    bool normal = path.EndsWith("_Normal.png",StringComparison.Ordinal);
                    bool map = path.EndsWith("_Mask.png",StringComparison.Ordinal);
                    var wantedType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    var wrap = path.EndsWith("_Color.png",StringComparison.Ordinal) || normal || map
                        ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    if (importer.textureType == wantedType && importer.sRGBTexture == !map &&
                        importer.wrapMode == wrap && importer.anisoLevel == 4 && importer.maxTextureSize == 2048)
                        continue;
                    importer.textureType = wantedType;
                    importer.sRGBTexture = !map;
                    importer.wrapMode = wrap;
                    importer.maxTextureSize = 2048;
                    importer.anisoLevel = 4;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.SaveAndReimport();
                }
            }

            Lit("PlasterR",.52f,.58f,.58f,maps:"PlasterR");
            Lit("PlasterB",.77f,.80f,.71f,maps:"PlasterB");
            Lit("DadoR",.30f,.39f,.38f,maps:"DadoR",smooth:.48f);
            Lit("DadoB",.43f,.62f,.49f,maps:"DadoB",smooth:.55f);
            Lit("FloorR",.44f,.46f,.44f,maps:"FloorR",smooth:.60f);
            Lit("FloorB",.66f,.66f,.57f,maps:"FloorB",smooth:.63f);
            Lit("BaseTrim",.26f,.30f,.27f,maps:"FloorR",tint:true);
            Lit("DoorWood",.32f,.25f,.16f,smooth:.42f);
            Lit("MetalPaint",.53f,.60f,.52f,smooth:.54f,maps:"PlasterB",tint:true);
            Lit("MetalOld",.32f,.36f,.28f,smooth:.48f,maps:"PlasterR",tint:true);
            Lit("CabinetPaint",.55f,.65f,.58f,smooth:.50f,maps:"PlasterB",tint:true);
            Lit("CabinetOld",.46f,.54f,.46f,smooth:.47f,maps:"PlasterR",tint:true);
            Lit("Steel",.59f,.62f,.61f,metal:.86f,smooth:.66f,maps:"Steel");
            Lit("Rubber",.095f,.105f,.096f,smooth:.41f);
            Lit("Cotton",.71f,.73f,.63f,maps:"Cotton");
            Lit("CottonOld",.58f,.58f,.47f,maps:"Cotton",tint:true);
            Lit("Textile",.53f,.71f,.60f,maps:"Cotton",tint:true);
            Lit("TextileOld",.42f,.53f,.49f,maps:"Cotton",tint:true);
            Lit("Mattress",.52f,.66f,.58f,maps:"Cotton",tint:true,smooth:.31f);
            Lit("MattressOld",.51f,.49f,.38f,maps:"Cotton",tint:true,smooth:.23f);
            Lit("Curtain",.73f,.82f,.77f,maps:"Cotton",tint:true,twoSided:true);
            Lit("CurtainOld",.52f,.57f,.48f,maps:"Cotton",tint:true,twoSided:true);
            Lit("Paper",.78f,.72f,.55f);
            Lit("Exterior",.28f,.32f,.31f,smooth:.43f,maps:"PlasterR",tint:true);
            Lit("Frame",.26f,.37f,.32f,metal:.24f,smooth:.53f);
            Lit("Bay",.37f,.57f,.49f,maps:"DadoB");
            Lit("Wood",.31f,.24f,.15f,smooth:.38f);
            Lit("Ceramic",.69f,.73f,.68f,smooth:.39f,maps:"Ceramic");
            Lit("Tree",.24f,.235f,.17f,maps:"PlasterR",tint:true);
            Lit("Foliage",.16f,.25f,.18f);
            Lit("FoliageLight",.21f,.31f,.23f);
            Lit("Black",.12f,.17f,.16f);
            Lit("Glass",.46f,.61f,.57f,smooth:.68f);
            Transparent(Mat("Glass"),new Color(.48f,.65f,.62f, art ? .10f : .22f));

            Lit("DeadTube",.38f,.43f,.42f, smooth:.68f);
            Lit("LiveTube",.77f,.84f,.75f, smooth:.63f);
            Mat("LiveTube").SetColor("_EmissionColor",new Color(.64f,.89f,.77f) * 2.75f);
            Mat("LiveTube").EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(Mat("LiveTube"));
            if (art)
            {
                foreach (string name in new[] {"Room101","Welcome","Emergency","Station","FileA","FileB","FileC",
                         "Demolition","Staff","Store","Socket1","Socket2","Socket3","Socket4","F101","ObsCard",
                         "BedList","Transfer","Drawing"})
                {
                    var m = Lit(name,.85f,.83f,.73f,smooth:.30f,twoSided:true);
                    m.SetTexture("_BaseMap",Tex(name));
                    m.SetColor("_BaseColor",Color.white);
                    EditorUtility.SetDirty(m);
                }
                foreach (string name in new[] {"WaterRun","PaintLoss","DampFloor","RustRun","WheelMarks","SootSwatch"})
                {
                    var m = Lit(name,.55f,.48f,.37f,smooth:name=="DampFloor"?.87f:.33f,twoSided:true);
                    m.SetTexture("_BaseMap",Tex(name));
                    Transparent(m,Color.white);
                }
                foreach(string name in new[]{"DrawingPiece1","DrawingPiece2","DrawingPiece3","ClockB","ClockR","BedContact"})
                {
                    var m=Lit(name,.81f,.79f,.65f,smooth:.26f,twoSided:true);
                    m.SetTexture("_BaseMap",Tex(name));
                    Transparent(m,Color.white);
                }
                var beamShader = Shader.Find("CTCC/Window Light");
                if (beamShader == null) throw new InvalidOperationException("CTCC window beam shader missing");
                var beam = MaterialAsset("Beam",beamShader);
                beam.SetColor("_Tint",new Color(.37f,.52f,.59f,.105f));
                EditorUtility.SetDirty(beam);
            }
        }

        private Material Lit(string key,float r,float g,float b,float metal=0,float smooth=.32f,
            string maps=null,bool tint=false,bool twoSided=false)
        {
            var mat=MaterialAsset(key,Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor",new Color(r,g,b));
            mat.SetFloat("_Metallic",metal);
            mat.SetFloat("_Smoothness",smooth);
            mat.SetFloat("_Cull",twoSided ? 0 : 2);
            if (art && maps!=null)
            {
                mat.SetTexture("_BaseMap",Tex(maps+"_Color"));
                if (!tint) mat.SetColor("_BaseColor",Color.white);
                mat.SetTexture("_BumpMap",Tex(maps+"_Normal"));
                mat.SetFloat("_BumpScale",.75f);
                mat.SetTexture("_MetallicGlossMap",Tex(maps+"_Mask"));
                mat.SetTexture("_OcclusionMap",Tex(maps+"_Mask"));
                mat.SetFloat("_OcclusionStrength",.78f);
                mat.SetFloat("_Smoothness",.93f);
                mat.EnableKeyword("_NORMALMAP");
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.EnableKeyword("_OCCLUSIONMAP");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private Material MaterialAsset(string key,Shader shader)
        {
            string flavor=art ? "Reference_" : "Blockout_";
            var path=Root+"/Generated/Materials/"+flavor+key+".mat";
            var item=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (item==null)
            {
                item=new Material(shader);
                AssetDatabase.CreateAsset(item,path);
            }
            item.name=key;
            materials.Add(key,item);
            return item;
        }

        private static void Transparent(Material m,Color tint)
        {
            m.SetColor("_BaseColor",tint);
            m.SetFloat("_Surface",1);
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            m.SetInteger("_ZWrite",0);
            m.SetOverrideTag("RenderType","Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster",false);
            m.renderQueue=(int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }

        private Material Mat(string key) => materials[key];

        private static Texture2D Tex(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Maps+name+".png") ?? throw new InvalidOperationException("Missing texture "+name);

        private GameObject Flat(string name,string material,Vector3 position,Vector3 normal,Vector2 scale,Transform parent=null)
        {
            var q=GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name=name;
            q.transform.SetParent(parent==null?permanent:parent,false);
            q.transform.localPosition=position;
            q.transform.localRotation=normal.y > .95f
                ? Quaternion.Euler(90,0,0)
                : Quaternion.LookRotation(-normal,Vector3.up);
            q.transform.localScale=new Vector3(scale.x,scale.y,1);
            UnityEngine.Object.DestroyImmediate(q.GetComponent<MeshCollider>());
            var renderer=q.GetComponent<Renderer>();
            renderer.sharedMaterial=Mat(material);
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            return q;
        }

        private void Sign(string name,string map,Vector3 position,Vector3 normal,Vector2 scale,Transform parent=null)
        {
            if (art) Flat(name,map,position,normal,scale,parent);
        }

        private GameObject CapsuleTarget(string id,string title,Vector3 position,Vector3 size,Transform parent=null)
        {
            var marker=Group(id,parent==null?permanent:parent,position).gameObject;
            marker.layer=HospitalScene.InteractLayer;
            var trigger=marker.AddComponent<BoxCollider>();
            trigger.size=size;
            trigger.isTrigger=true;
            var target=marker.AddComponent<SceneTarget>();
            target.id=id;
            target.title=title;
            return marker;
        }

        private void BuildFurnishing()
        {
            var desk = Group("Bàn trực · quầy hai cao độ",permanent,new Vector3(1.45f,0,-3.24f));
            Box("Terrazzo plinth",new Vector3(0,.06f,0),new Vector3(1.35f,.12f,3.88f),Mat("BaseTrim"),desk,true);
            Box("Counter apron",new Vector3(0,.55f,0),new Vector3(1.25f,.98f,3.8f),Mat("DadoR"),desk,true);
            Box("Wide counter top",new Vector3(0,1.08f,0),new Vector3(1.48f,.095f,4f),Mat("Ceramic"),desk);
            if (art)
            {
                Box("Lower accessible end",new Vector3(.05f,.78f,-2.32f),new Vector3(1.25f,.07f,.71f),Mat("Ceramic"),desk,true);
                Box("Duty ledger spine",new Vector3(.33f,1.15f,-.19f),new Vector3(.34f,.055f,.27f),Mat("Wood"),desk);
                Box("Duty ledger spread",new Vector3(.33f,1.18f,-.19f),new Vector3(.32f,.012f,.248f),Mat("Paper"),desk);
                Sign("Quầy", "Station",new Vector3(2.081f,.67f,-3.35f),Vector3.right,new Vector2(.93f,.35f));
                Sign("Foundation plaque","Welcome",new Vector3(-.986f,2.1f,-3.1f),Vector3.right,new Vector2(1.34f,.52f));
                var clock=Flat("Analog wall clock","ClockR",new Vector3(-.982f,2.78f,-3.26f),Vector3.right,new Vector2(.47f,.47f));
                world.finishes.Add(new LayerFinish{renderer=clock.GetComponent<Renderer>(),real=Mat("ClockR"),report=Mat("ClockB")});
            }

            foreach(var bed in layout.beds)
            {
                bool fourth=bed.id=="4";
                Vector3 p=new Vector3(bed.x,0,bed.z);
                if (!fourth)
                {
                    var root=Group("Bed "+bed.id,permanent,p);
                    root.localRotation=Quaternion.Euler(0,bed.yaw,0);
                    Box("Bed clearance volume",new Vector3(0,.405f,0),new Vector3(layout.bedWidth,.81f,layout.bedLength),
                        Mat("MetalPaint"),root,true);
                    if (art)
                    {
                        var proxy=root.GetChild(0).GetComponent<MeshRenderer>();
                        proxy.enabled=false;
                        var clean=Model("CTCC_PROP_WardBed",root,false);
                        clean.transform.localRotation=Quaternion.Euler(0,180,0);
                        Flat("Soft bed contact","BedContact",p+new Vector3(0,.004f,0),Vector3.up,new Vector2(1.28f,2.39f));
                        foreach(var rend in clean.GetComponentsInChildren<MeshRenderer>())
                        {
                            var orig=rend.sharedMaterials;
                            var old = Array.ConvertAll(orig,m=>Mat(AgeKey(m.name)));
                            for(int i=0;i<orig.Length;i++)
                            {
                                if (orig.Length != 1)
                                    throw new InvalidOperationException("FBX part needs one material.");
                                world.finishes.Add(new LayerFinish {renderer=rend,real=old[i],report=orig[i]});
                            }
                        }
                        var cabinetRoot=Group("Tủ đầu giường "+bed.id,permanent,p+new Vector3(.74f,0,bed.yaw==0?.66f:-.63f));
                        cabinetRoot.localRotation=root.localRotation;
                        Box("Cabinet clearance",new Vector3(0,.36f,0),new Vector3(.54f,.72f,.45f),Mat("CabinetOld"),cabinetRoot,true)
                            .GetComponent<Renderer>().enabled=false;
                        var cabinet=Model("CTCC_PROP_BedsideCabinet",cabinetRoot,false);
                        cabinet.transform.localRotation=Quaternion.Euler(0,180,0);
                        foreach(var ren in cabinet.GetComponentsInChildren<MeshRenderer>())
                            if (ren.sharedMaterials.Length==1)
                                world.finishes.Add(new LayerFinish {renderer=ren,real=Mat(AgeKey(ren.sharedMaterial.name)),report=ren.sharedMaterial});
                        if(bed.id=="2")
                            AttendantKit(p+new Vector3(.72f,.77f,.66f));
                    }
                }
                float back=bed.yaw==0?7.98f:2.02f;
                float height=bed.yaw==0?.92f:1.31f;
                var direction=bed.yaw==0?Vector3.back:Vector3.forward;
                Box("Outlet "+bed.id,new Vector3(bed.x,height,back),new Vector3(.16f,.12f,.038f),Mat("Paper"));
                if (art)
                {
                    Sign("Bed outlet number","Socket"+bed.id,new Vector3(bed.x+.14f,height+.10f,back+direction.z*.028f),direction,new Vector2(.135f,.119f));
                    for(int d=-1;d<=1;d+=2)
                        for(int s=-1;s<=1;s+=2)
                            Box("Outlet aperture",new Vector3(bed.x+.036f*d,height+.019f*s,back+.022f*direction.z),
                                new Vector3(.009f,.011f,.001f),Mat("Black"));
                }
            }

            var cupboard=Group("Tủ đẩy · dấu bánh hướng kho",permanent,new Vector3(13.34f,0,3.62f));
            world.cabinet=cupboard;
            world.cabinetClosed=cupboard.localPosition;
            world.cabinetOpen=cupboard.localPosition+new Vector3(.02f,0,1.61f);
            Box("Cabinet frame",new Vector3(0,1.05f,0),new Vector3(.48f,2.08f,1.37f),Mat("CabinetOld"),cupboard,true);
            if (art)
            {
                for(int i=0;i<2;i++)
                {
                    float z=-.34f+i*.68f;
                    Box("Cabinet door",new Vector3(-.255f,1.05f,z),new Vector3(.034f,1.98f,.64f),Mat("DadoR"),cupboard);
                    Box("Stamped upper panel",new Vector3(-.278f,1.50f,z),new Vector3(.016f,.33f,.52f),Mat("MetalOld"),cupboard);
                    Box("Steel grip",new Vector3(-.285f,.98f,z+.19f),new Vector3(.084f,.20f,.031f),Mat("Steel"),cupboard);
                }
                for(int i=0;i<5;i++)
                    Box("Louver slot",new Vector3(-.286f,1.47f+i*.039f,-.34f),new Vector3(.003f,.011f,.32f),Mat("Black"),cupboard);
                Sign("Store","Store",new Vector3(13.675f,2.49f,3.62f),Vector3.left,new Vector2(.7f,.26f));
            }
            var dryShelf=Group("Tủ khô giữ đồ vật 101",permanent,new Vector3(15.71f,0,2.61f));
            Box("Shelf base",new Vector3(0,.40f,0),new Vector3(2.08f,.80f,.52f),Mat("Wood"),dryShelf,true);
            if (art)
            {
                Box("Dry shelf top",new Vector3(0,.83f,0),new Vector3(2.13f,.073f,.56f),Mat("DoorWood"),dryShelf);
                Box("Shelf side",new Vector3(-1.01f,1.25f,0),new Vector3(.09f,2.50f,.56f),Mat("Wood"),dryShelf);
                Box("Shelf side",new Vector3(1.01f,1.25f,0),new Vector3(.09f,2.50f,.56f),Mat("Wood"),dryShelf);
                Box("Upper archive shelf",new Vector3(0,1.71f,0),new Vector3(2.0f,.06f,.51f),Mat("Wood"),dryShelf);
            }

            var hidden=Group("A · vách áp hồ sơ",report);
            world.deniedBay=hidden.gameObject;
            Box("Denied bay front",new Vector3(12.14f,1.8f,4.32f),new Vector3(3.14f,3.6f,.16f),Mat("Bay"),hidden,true);
            Box("Denied bay return",new Vector3(10.65f,1.80f,3.15f),new Vector3(.16f,3.6f,2.28f),Mat("Bay"),hidden,true);
            world.pendingBay=Group("B · rèm giường 4",report).gameObject;
            if (art)
            {
                Curtain(new Vector3(10.68f,2.74f,4.27f),2.4f,1.88f,world.pendingBay.transform,"Curtain");
                Curtain(new Vector3(6.58f,2.75f,5.32f),1.25f,1.94f,permanent,"Curtain");
                Curtain(new Vector3(10.79f,2.75f,4.28f),.91f,1.98f,real,"CurtainOld");
            }
            else
                Box("Soft partition proxy",new Vector3(11.85f,1.68f,4.25f),new Vector3(2.38f,1.85f,.035f),Mat("Bay"),world.pendingBay.transform);

            if (!art) return;
            DetailArchitecture();
            PropDetails();
        }

        private static string AgeKey(string material)
        {
            return material switch
            {
                "MetalPaint" => "MetalOld",
                "CabinetPaint" => "CabinetOld",
                "Cotton" => "CottonOld",
                "Textile" => "TextileOld",
                "Mattress" => "MattressOld",
                _ => material
            };
        }

        private GameObject Model(string name,Transform parent,bool aged)
        {
            if(!mergedModels.TryGetValue(name,out var meshByFinish))
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Models+name+".fbx");
                if(source==null) throw new InvalidOperationException("Missing model "+name);
                var byFinish=new Dictionary<string,List<CombineInstance>>();
                foreach(var renderer in source.GetComponentsInChildren<MeshRenderer>())
                {
                    var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                    var sourceMaterials=renderer.sharedMaterials;
                    Matrix4x4 local=source.transform.worldToLocalMatrix*renderer.localToWorldMatrix;
                    for(int i=0;i<mesh.subMeshCount;i++)
                    {
                        string finish=sourceMaterials[i].name;
                        if(!byFinish.TryGetValue(finish,out var slices))
                        {
                            slices=new List<CombineInstance>();
                            byFinish.Add(finish,slices);
                        }
                        slices.Add(new CombineInstance {mesh=mesh,subMeshIndex=i,transform=local});
                    }
                }
                meshByFinish=new Dictionary<string,Mesh>();
                foreach(var part in byFinish)
                {
                    var grouped=new Mesh {name=name+" · "+part.Key,indexFormat=IndexFormat.UInt32};
                    grouped.CombineMeshes(part.Value.ToArray());
                    StoreMesh(grouped);
                    meshByFinish.Add(part.Key,grouped);
                }
                mergedModels.Add(name,meshByFinish);
            }
            var instance=Group(name,parent);
            foreach(var part in meshByFinish)
            {
                var surface=Group(part.Key,instance).gameObject;
                surface.AddComponent<MeshFilter>().sharedMesh=part.Value;
                surface.AddComponent<MeshRenderer>().sharedMaterial=Mat(aged?AgeKey(part.Key):part.Key);
            }
            return instance.gameObject;
        }

        private void Curtain(Vector3 start,float width,float height,Transform parent,string finish)
        {
            var pivot=Group("Cotton privacy curtain",parent,start);
            Box("Galvanized track",new Vector3(width*.51f,.075f,0),new Vector3(width+.18f,.026f,.039f),Mat("Steel"),pivot);
            for(int i=0;i<10;i++)
                Box("Curtain loop",new Vector3(width*i/9,.03f,0),new Vector3(.023f,.089f,.023f),Mat("Steel"),pivot);
            int nx=32,ny=15;
            var verts=new List<Vector3>();
            var indices=new List<int>();
            var uv=new List<Vector2>();
            for(int y=0;y<=ny;y++)
                for(int x=0;x<=nx;x++)
                {
                    float u=x/(float)nx, v=y/(float)ny;
                    float folds=Mathf.Sin(u*6*Mathf.PI*2 + .23f*v) * (.045f+.017f*v);
                    verts.Add(new Vector3(width*u,y==0 ? 0 : -height*v-.016f*Mathf.Sin(17*u)*v,folds));
                    uv.Add(new Vector2(u*4,1-v*5));
                    if(x==0||y==0) continue;
                    int i=y*(nx+1)+x;
                    indices.AddRange(new[]{i-nx-2,i-nx-1,i,i-nx-2,i,i-1});
                }
            var mesh=new Mesh { name="Ribbed cloth · "+finish };
            mesh.SetVertices(verts);
            mesh.SetUVs(0,uv);
            mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            StoreMesh(mesh);
            var filter=pivot.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh=mesh;
            pivot.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Mat(finish);
            movingCloth.Add(filter);
        }

        private void AttendantKit(Vector3 p)
        {
            var tray=Group("Family kit · enamel cup and linen",permanent,p);
            Box("Folded cotton",new Vector3(-.08f,.020f,0),new Vector3(.20f,.046f,.16f),Mat("Textile"),tray);
            Round("Enamel cup",new Vector3(.11f,.039f,.038f),new Vector3(.076f,.037f,.076f),Mat("Ceramic"),tray);
            Round("Cup interior",new Vector3(.11f,.076f,.038f),new Vector3(.063f,.003f,.063f),Mat("Frame"),tray);
        }

        private GameObject Round(string name,Vector3 p,Vector3 size,Material material,Transform parent)
        {
            var piece=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.DestroyImmediate(piece.GetComponent<Collider>());
            piece.name=name;
            piece.transform.SetParent(parent,false);
            piece.transform.localPosition=p;
            piece.transform.localScale=size;
            piece.GetComponent<Renderer>().sharedMaterial=material;
            return piece;
        }

        private void DetailArchitecture()
        {
            foreach(float z in new[]{1.0f,5.25f,9.50f,13.75f})
            {
                Surface("Longitudinal lintel",new Vector3(4,3.39f,z),new Vector3(3,.36f,.34f),"PlasterR","PlasterB",tile:2.3f);
                Surface("Squared pier · shared landmark",new Vector3(2.64f,1.8f,z),new Vector3(.28f,3.6f,.38f),
                    "PlasterR","PlasterB",tile:2.3f);
            }
            for(int i=0;i<3;i++)
            {
                float z=i*4.22f+1.65f;
                Box("Handrail and crash strip",new Vector3(2.547f,.79f,z),new Vector3(.067f,.115f,3.7f),Mat("Wood"));
                Box("Surface conduit",new Vector3(5.452f,2.84f,z),new Vector3(.032f,.039f,4.01f),Mat("Frame"));
                for(int j=0;j<3;j++)
                    Box("Conduit saddle",new Vector3(5.426f,2.84f,z-1.22f+j*1.22f),new Vector3(.033f,.057f,.028f),Mat("MetalPaint"));
            }
            Box("Deep beam spanning 101",new Vector3(9.77f,3.397f,5.00f),new Vector3(.32f,.41f,6f),Mat("PlasterR"),tile:2.3f);
            Box("Curtain rail support",new Vector3(9.98f,3.0f,4.29f),new Vector3(3.27f,.025f,.037f),Mat("Frame"));
            Box("Courtyard entry canopy",new Vector3(4,2.85f,-9.45f),new Vector3(4.55f,.22f,3.13f),Mat("Exterior"),tile:2f);
            Box("Canopy column",new Vector3(1.88f,1.39f,-10.5f),new Vector3(.29f,2.78f,.29f),Mat("PlasterR"),solid:true,tile:2f);
            Box("Canopy column",new Vector3(6.12f,1.39f,-10.5f),new Vector3(.29f,2.78f,.29f),Mat("PlasterR"),solid:true,tile:2f);
            Box("Path edge",new Vector3(8.40f,.08f,-15.02f),new Vector3(4.5f,.17f,3.89f),Mat("Exterior"),solid:true);
            Box("Garden soil",new Vector3(8.40f,.182f,-15.02f),new Vector3(4.30f,.043f,3.69f),Mat("Tree"));
            Box("North exterior ground",new Vector3(11,-.24f,11.13f),new Vector3(12,.20f,6.1f),Mat("Exterior"),tile:2f);
            for(int i=0;i<8;i++)
                Tree(new Vector3(6.2f+i*1.35f,-.2f,10.05f+Mathf.Sin(2.16f*i)*.81f),1.15f+i%3*.3f);
            Tree(new Vector3(8.75f,.12f,-14.8f),2.25f);
            Sign("Room 101 enamel","Room101",new Vector3(5.483f,2.58f,4.75f),Vector3.left,new Vector2(.65f,.246f));
            Sign("Phòng 101 inside","Room101",new Vector3(5.706f,2.57f,4.74f),Vector3.right,new Vector2(.5f,.187f));
            Sign("Main emergency sign","Emergency",new Vector3(4,2.58f,15.886f),Vector3.back,new Vector2(1.14f,.429f));
            Sign("Staff sign","Staff",new Vector3(0,2.67f,.29f),Vector3.back,new Vector2(.97f,.365f));
            Sign("Canopy letterboard","Welcome",new Vector3(4,3.25f,-8.107f),Vector3.back,new Vector2(2.62f,.90f));
            Sign("Demolition board","Demolition",new Vector3(6.76f,1.20f,-20.116f),Vector3.back,new Vector2(1.48f,.557f),real);
            Sign("Demolition marker return","Demolition",new Vector3(6.76f,1.20f,-19.887f),Vector3.forward,new Vector2(1.48f,.557f),real);
            for(int i=0;i<4;i++)
            {
                Box("Bench seat",new Vector3(7.18f,.44f,-5.89f+.51f*i),new Vector3(.43f,.055f,.47f),Mat("Steel"),solid:true);
                Box("Pressed seat back",new Vector3(7.41f,.692f,-5.89f+.51f*i),new Vector3(.06f,.45f,.44f),Mat("Steel"));
            }
            Flat("Window return water streak","WaterRun",new Vector3(9.70f,.72f,7.996f),Vector3.back,new Vector2(1.75f,1.33f),real);
            Flat("Floor damp below window","DampFloor",new Vector3(9.57f,.006f,7.37f),Vector3.up,new Vector2(2.28f,1.11f),real);
            Flat("Bloom and salts along wall base","PaintLoss",new Vector3(13.694f,.31f,6.22f),Vector3.left,new Vector2(2.31f,.55f),real);
            Flat("Water from corridor pipe","WaterRun",new Vector3(2.501f,1.44f,4.39f),Vector3.right,new Vector2(1.9f,2.76f),real);
            Flat("Rust under window fixing","RustRun",new Vector3(7.98f,.86f,7.995f),Vector3.back,new Vector2(.24f,.61f),real);
            Flat("Foot traffic damp patches","DampFloor",new Vector3(4.68f,.006f,6.91f),Vector3.up,new Vector2(1.24f,4.31f),real);
            Flat("Fourth bed wheel shadows","WheelMarks",new Vector3(11.66f,.008f,3.25f),Vector3.up,new Vector2(1.21f,2.46f),real);
            Flat("Fourth bed report trace","WheelMarks",new Vector3(10.87f,.008f,4.44f),Vector3.up,new Vector2(.72f,.33f),report);
        }

        private void Tree(Vector3 root,float height)
        {
            Transform trunk=Group("Tree · yard vegetation",permanent,root);
            Round("Trunk",new Vector3(0,height*.58f,0),new Vector3(.09f,height*.60f,.09f),Mat("Tree"),trunk);
            var random=new System.Random(Mathf.RoundToInt(root.x*187+root.z*131));
            for(int tone=0;tone<2;tone++)
            {
                var vertices=new List<Vector3>();
                var triangles=new List<int>();
                for(int branch=0;branch<11;branch++)
                {
                    float bearing=branch*2.39996f;
                    var tuft=new Vector3(Mathf.Cos(bearing)*.62f,height*1.24f+.18f*(branch%3),Mathf.Sin(bearing)*.62f);
                    for(int i=0;i<38;i++)
                    {
                        float q() => (float)random.NextDouble()-.5f;
                        Vector3 at=tuft+new Vector3(q()*.83f,q()*.54f,q()*.87f);
                        Vector3 axis=new Vector3(q(),q()*.8f,q()).normalized*(.109f+(q()+.5f)*.074f);
                        Vector3 side=Vector3.Cross(axis,Vector3.up).normalized*axis.magnitude*.31f;
                        int v=vertices.Count;
                        vertices.AddRange(new[]{at-axis,at-side,at+axis,at+side,at+Vector3.up*.019f});
                        triangles.AddRange(new[]{v,v+1,v+4,v+1,v+2,v+4,v+2,v+3,v+4,v+3,v,v+4});
                    }
                }
                var mesh=new Mesh{name="Leaves · "+tone};
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles,0);
                mesh.RecalculateNormals();
                StoreMesh(mesh);
                var crown=Group("Canopy leaf mesh",trunk).gameObject;
                crown.AddComponent<MeshFilter>().sharedMesh=mesh;
                crown.AddComponent<MeshRenderer>().sharedMaterial=Mat(tone==0?"Foliage":"FoliageLight");
            }
        }

        private void GroupFixedSurfaces()
        {
            var changeable=new HashSet<Renderer>(world.finishes.Select(x=>x.renderer));
            var fixedTrim=permanent.Cast<Transform>()
                .Select(t=>t.GetComponent<MeshRenderer>())
                .Where(r=>r!=null && r.enabled && !changeable.Contains(r) && r.sharedMaterials.Length==1 &&
                          r.sharedMaterial.renderQueue<3000 && r.GetComponent<MeshFilter>()!=null)
                .GroupBy(r=>(r.sharedMaterial,
                    Mathf.FloorToInt(r.transform.position.x/7.5f),Mathf.FloorToInt(r.transform.position.z/6.5f)))
                .Where(g=>g.Count()>1).ToArray();
            foreach(var zone in fixedTrim)
            {
                var joins=zone.Select(r=>new CombineInstance
                {
                    mesh=r.GetComponent<MeshFilter>().sharedMesh,
                    transform=r.localToWorldMatrix,
                    subMeshIndex=0
                }).ToArray();
                var merged=new Mesh{name="Fixed trim · "+zone.Key.Item1.name};
                merged.CombineMeshes(joins);
                StoreMesh(merged);
                var item=Group(merged.name,permanent).gameObject;
                item.AddComponent<MeshFilter>().sharedMesh=merged;
                item.AddComponent<MeshRenderer>().sharedMaterial=zone.Key.Item1;
                foreach(var r in zone) r.enabled=false;
            }
        }

        private void PropDetails()
        {
            var trolley=Group("Cáng kiểm kích thước",permanent,new Vector3(7.17f,0,19.13f));
            trolley.localRotation=Quaternion.Euler(0,16,0);
            Model("CTCC_PROP_TransportTrolley",trolley,true);
            Box("Trolley clearance proxy",new Vector3(0,.43f,0),new Vector3(.72f,.86f,2.05f),Mat("Steel"),trolley,true)
                .GetComponent<Renderer>().enabled=false;
            var bag=Group("Túi đồ vải lưu tại kho",real,new Vector3(14.95f,.92f,2.65f));
            Box("Soft sided bag",new Vector3(0,.071f,0),new Vector3(.32f,.19f,.21f),Mat("TextileOld"),bag);
            Box("Woven strap",new Vector3(0,.132f,0),new Vector3(.23f,.028f,.042f),Mat("CottonOld"),bag);
            for(int i=0;i<3;i++)
            {
                Box("Folder",new Vector3(15.59f+.13f*i,.885f+i*.017f,2.51f),new Vector3(.28f,.014f,.26f),Mat(i==0?"Paper":"DoorWood"));
            }
        }

        private void MarkInteractions()
        {
            CapsuleTarget("GATE-CONG","Cổng phụ",new Vector3(4,1.28f,-20.12f),new Vector3(1.10f,1.18f,.22f));
            CapsuleTarget("SEAM","Thử ra cổng",new Vector3(4,1.09f,-20.46f),new Vector3(2,1.6f,.2f));
            CapsuleTarget("AN01","Sổ trực",new Vector3(2.18f,1.20f,-3.73f),new Vector3(.28f,.28f,.48f));
            CapsuleTarget("BOARD","Phiếu bàn giao 101",new Vector3(2.14f,1.21f,-2.91f),new Vector3(.30f,.30f,.55f));
            CapsuleTarget("E10a","Bì thư của Bảy",new Vector3(3.17f,.94f,-21.44f),new Vector3(.43f,.49f,.44f));
            CapsuleTarget("AN02","Điểm neo phòng 101",new Vector3(6.51f,.55f,5.14f),new Vector3(.59f,.45f,.33f));
            var file=Group("Bảng kẹp tại phòng 101",permanent,new Vector3(5.758f,1.37f,5.50f));
            Box("Wall board",Vector3.zero,new Vector3(.065f,.42f,.315f),Mat("DoorWood"),file);
            CapsuleTarget("F101","Xử lý phiếu 101",new Vector3(5.811f,1.37f,5.50f),new Vector3(.10f,.52f,.52f));
            CapsuleTarget("OBS101A","Vết bánh và ổ cắm thứ tư",new Vector3(10.92f,.096f,4.48f),new Vector3(.62f,.19f,.71f),report);
            CapsuleTarget("OBS101B","Thẻ bàn giao giường 4",new Vector3(11.35f,1.41f,4.453f),new Vector3(.44f,.47f,.104f),report);
            CapsuleTarget("LIST","Bản kê ba giường",new Vector3(6.65f,1.34f,2.127f),new Vector3(.54f,.58f,.13f),real);
            CapsuleTarget("SOCKETS","Bốn vị trí ổ cắm",new Vector3(11.54f,1.31f,2.115f),new Vector3(.53f,.37f,.18f),real);
            CapsuleTarget("WHEELS","Dấu bánh xe",new Vector3(11.66f,.10f,3.57f),new Vector3(.89f,.21f,1.05f),real);
            CapsuleTarget("PZ01","Đối chiếu chỗ kê giường",new Vector3(13.06f,1.28f,3.62f),new Vector3(.13f,.66f,.51f),real);
            world.storeClues=Group("E11 · physical provenance",permanent).gameObject;
            var store=world.storeClues.transform;
            CapsuleTarget("DRAW1","Mảnh tranh 1",new Vector3(14.68f,.98f,2.73f),new Vector3(.27f,.18f,.2f),store);
            CapsuleTarget("DRAW2","Mảnh tranh 2",new Vector3(15.27f,.98f,2.76f),new Vector3(.29f,.18f,.23f),store);
            CapsuleTarget("DRAW3","Mảnh tranh 3",new Vector3(16.30f,1.807f,2.76f),new Vector3(.30f,.20f,.26f),store);
            CapsuleTarget("WRIST","Vòng C01",new Vector3(15.71f,.972f,2.76f),new Vector3(.24f,.21f,.24f),store);
            CapsuleTarget("TRANSFER","Phiếu chuyển 101/4",new Vector3(16.13f,.98f,2.75f),new Vector3(.30f,.20f,.25f),store);
            CapsuleTarget("G02","Cửa cấp cứu",new Vector3(4,1.25f,15.82f),new Vector3(1.04f,.88f,.18f));
            CapsuleTarget("G02","Cửa cấp cứu",new Vector3(.05f,1.25f,15.82f),new Vector3(1.04f,.88f,.18f));
            CapsuleTarget("EXIT","Sổ giao ca · cấp cứu",new Vector3(4,1.05f,20.84f),new Vector3(.72f,.64f,.54f));

            world.statusA=Group("Biển xác nhận",permanent).gameObject;
            world.statusB=Group("Biển chờ",permanent).gameObject;
            world.statusC=Group("Biển phản chứng",permanent).gameObject;
            if(!art) return;
            world.inspectImages=Array.ConvertAll(new[]{"F101","ObsCard","BedList","Transfer","Drawing",
                    "DrawingPiece1","DrawingPiece2","DrawingPiece3"},
                id=>new InspectImage{id=id,image=Tex(id)});
            Box("Bì thư bên cổng",new Vector3(3.17f,.81f,-21.44f),new Vector3(.29f,.022f,.18f),Mat("Paper"));
            Box("Túi bên cổng",new Vector3(3.17f,.53f,-21.44f),new Vector3(.38f,.55f,.29f),Mat("TextileOld"));
            var notice=new Vector3(5.492f,1.92f,5.96f);
            Sign("Form A","FileA",notice,Vector3.left,new Vector2(.91f,.34f),world.statusA.transform);
            Sign("Form B","FileB",notice,Vector3.left,new Vector2(.91f,.34f),world.statusB.transform);
            Sign("Form C","FileC",notice,Vector3.left,new Vector2(.91f,.34f),world.statusC.transform);
            Flat("F101","F101",new Vector3(5.793f,1.37f,5.5f),Vector3.right,new Vector2(.25f,.35f));
            Flat("Spare signed board","F101",new Vector3(2.12f,1.133f,-2.90f),Vector3.up,new Vector2(.25f,.349f));
            Flat("OBS101B","ObsCard",new Vector3(11.35f,1.41f,4.461f),Vector3.forward,new Vector2(.162f,.22f),report);
            Flat("Bản kê","BedList",new Vector3(6.65f,1.34f,2.112f),Vector3.forward,new Vector2(.31f,.41f),real);
            Flat("Transfer paper","Transfer",new Vector3(16.13f,.869f,2.75f),Vector3.up,new Vector2(.224f,.30f),store);
            var drawingSpots=new[]{new Vector3(14.68f,.867f,2.73f),new Vector3(15.27f,.869f,2.76f),new Vector3(16.30f,1.744f,2.76f)};
            for(int i=0;i<drawingSpots.Length;i++)
                Flat("Drawing fragment","DrawingPiece"+(i+1),drawingSpots[i],Vector3.up,new Vector2(.091f,.19f),store);
            Round("C01 wristband",new Vector3(15.71f,.89f,2.76f),new Vector3(.101f,.019f,.076f),Mat("Rubber"),store);
            Box("C01 wristband slip",new Vector3(15.71f,.91f,2.76f),new Vector3(.067f,.004f,.04f),Mat("Paper"),store);
            Box("Pad · scored threshold marker",new Vector3(6.52f,.015f,4.95f),new Vector3(.94f,.021f,.097f),Mat("MetalOld"));
            Box("Hub ledger ledge",new Vector3(4,.91f,21.02f),new Vector3(1.17f,.10f,.72f),Mat("Ceramic"),solid:true);
            Box("Ledger",new Vector3(4,.973f,20.85f),new Vector3(.41f,.03f,.27f),Mat("Paper"));
        }
    }
}
