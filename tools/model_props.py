"""Build the original 101 hero props in metres. Run this script in Blender 4.5."""

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

OUT = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
OUT.mkdir(parents=True, exist_ok=True)


def fresh():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1


def material(key):
    return bpy.data.materials.get(key) or bpy.data.materials.new(key)


def finish(obj, name, key):
    obj.name = name
    obj.data.materials.append(material(key))
    return obj


def box(name, loc, size, key, edge=.008, segments=3):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if edge > 0:
        bevel = obj.modifiers.new("Rounded forming edges", "BEVEL")
        bevel.width = edge
        bevel.segments = segments
        bevel.harden_normals = True
    return finish(obj, name, key)


def pipe(name, points, radius, key, resolution=3):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth = radius
    curve.bevel_resolution = resolution
    curve.use_fill_caps = True
    spline = curve.splines.new("POLY")
    spline.points.add(len(points)-1)
    for point, co in zip(spline.points, points):
        point.co = (*co, 1)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    return finish(obj,name,key)


def rod(name, start, end, radius, key, segments=16):
    delta = Vector(end)-Vector(start)
    bpy.ops.mesh.primitive_cylinder_add(vertices=segments, radius=radius, depth=delta.length,
                                        location=(Vector(start)+Vector(end))/2)
    obj = bpy.context.object
    obj.rotation_euler = delta.to_track_quat("Z","Y").to_euler()
    for poly in obj.data.polygons:
        poly.use_smooth = len(poly.vertices) == 4
    return finish(obj,name,key)


def sphere(name, origin, dims, key):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, location=origin)
    obj = bpy.context.object
    obj.dimensions = dims
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return finish(obj,name,key)


def cornered_u(y, bottom, top, extent):
    r=.07
    points = [(-extent,y,bottom),(-extent,y,top-r)]
    for i in range(1,8):
        angle = math.pi - (math.pi/2)*i/7
        points.append((-extent+r+r*math.cos(angle),y,top-r+r*math.sin(angle)))
    points.append((extent-r,y,top))
    for i in range(1,8):
        angle = math.pi/2 - (math.pi/2)*i/7
        points.append((extent-r+r*math.cos(angle),y,top-r+r*math.sin(angle)))
    points.append((extent,y,bottom))
    return points


def fabric(name, key, size, center, fold):
    w,l=size
    nx,ny=26,34
    verts=[]
    faces=[]
    for j in range(ny+1):
        for i in range(nx+1):
            u,v = i/nx,j/ny
            x = (u-.5)*w + center[0] + .008*math.sin(v*21+u)
            y = (v-.5)*l + center[1] + .018*math.sin(u*8.5+v*2)
            wrinkles = .008*math.sin(u*39 + v*7) * (.40+.60*math.sin(v*3.7)**2)
            droop = -.19*min(1, max(0, abs(x)-.363)/.083)**1.65
            downturn = -.12*max(0, (.13-v)/.13)**1.65
            verts.append((x,y,center[2]+wrinkles+droop+downturn*fold))
            if i and j:
                a=j*(nx+1)+i
                faces.append((a-nx-2,a-nx-1,a,a-1))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="Woven cloth")
    for face in mesh.polygons:
        face.use_smooth=True
        for index in face.loop_indices:
            j,i=divmod(mesh.loops[index].vertex_index,nx+1)
            uv.data[index].uv=(3*i/nx,5*j/ny)
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.modifiers.new("Hem thickness","SOLIDIFY").thickness=.007
    finish(obj,name,key)


def caster(x,y):
    rod("Swivel post",(x,y,.097),(x,y,.19),.026,"Steel")
    rod("Rubber wheel",(x-.022,y,.050),(x+.022,y,.050),.044,"Rubber",24)
    rod("Wheel axle",(x-.031,y,.050),(x+.031,y,.050),.013,"Steel",16)
    for side in (-1,1):
        box("Caster pressed bracket",(x+side*.028,y,.078),(.011,.054,.068),"Steel",.004)


def bed():
    for x in (-.38,.38):
        rod("Long support rail",(x,-.94,.558),(x,.94,.558),.026,"MetalPaint")
        for y in (-.867,.866):
            caster(x,y)
            rod("Leg tube",(x,y,.17),(x,y,.585),.022,"MetalPaint")
            sphere("Brazed joint",(x,y,.557),(.063,.061,.054),"MetalPaint")
    for y in (-.96,.96):
        top=1.109 if y>0 else .906
        pipe("Curved bed end",cornered_u(y,.235,top,.385),.024,"MetalPaint")
        box("End panel",(0,y,.721),(.681,.018,.191),"MetalPaint",.028)
        rod("Lower end crossbar",(-.38,y,.437),(.38,y,.437),.020,"MetalPaint")
        for x in (-.245,.245):
            rod("Rivet",(x,y-.016,.722),(x,y+.016,.722),.012,"Steel")
    for i in range(9):
        box("Pressed deck slat",(0,-.80+i*.20,.569),(.71,.089,.021),"MetalPaint",.004,2)
    box("Mattress",(0,.015,.644),(.786,1.824,.151),"Mattress",.038,5)
    box("Wrapped seam",(0,.015,.591),(.785,1.817,.030),"Cotton",.012)
    pillow=box("Pillow",(0,.642,.765),(.567,.379,.133),"Cotton",.061,8)
    pillow.rotation_euler.z=.08
    fabric("Folded blanket","Textile",(.915,1.28),(0,-.19,.733),1)
    rod("Side rail hinge",(-.401,.14,.563),(-.401,.14,.72),.021,"Steel")
    pipe("Side protection rail",[(-.401,.53,.57),(-.401,.49,.80),(-.401,.43,.86),
                                   (-.401,-.44,.865),(-.401,-.51,.811),(-.401,-.52,.56)],.016,"MetalPaint")
    rod("Side rail brace",(-.40,.06,.58),(-.40,.06,.86),.012,"MetalPaint")


def bedside():
    box("Welded body",(0,0,.385),(.493,.425,.665),"CabinetPaint",.019)
    box("Enamel lip",(0,0,.744),(.527,.467,.053),"CabinetPaint",.016)
    box("Drawer face",(0,-.227,.595),(.455,.043,.166),"CabinetPaint",.009)
    box("Lower compartment",(0,-.227,.313),(.455,.043,.350),"CabinetPaint",.009)
    for y in (-.176,.176):
        for x in (-.195,.195):
            rod("Leg ferrule",(x,y,.008),(x,y,.081),.019,"Rubber")
    pipe("Drawer pull",[(-.108,-.249,.596),(-.09,-.283,.596),(.09,-.283,.596),(.108,-.249,.596)],
         .012,"Steel")
    rod("Door latch",(.148,-.28,.407),(.148,-.24,.407),.015,"Steel")
    box("Inventory label",(0,-.252,.487),(.115,.002,.036),"Paper",.003)


def trolley():
    for x in (-.31,.31):
        for y in (-.88,.88):
            caster(x,y)
            rod("Lift frame",(x,y,.16),(x,y,.71),.023,"Steel")
        rod("Chassis", (x,-.89,.26),(x,.89,.26),.024,"Steel")
        rod("Cross brace",(x,-.82,.27),(x,.67,.69),.012,"Steel")
        rod("Cross brace",(x,.82,.27),(x,-.67,.69),.012,"Steel")
    box("Baked enamel carrier",(0,0,.755),(.675,1.91,.060),"MetalPaint",.025)
    box("Leatherette pad",(0,0,.822),(.639,1.86,.085),"Mattress",.036)
    for y in (-.976,.976):
        pipe("Push handle",cornered_u(y,.57,.91,.316),.020,"Steel")
    box("Under-shelf", (0,0,.446),(.47,.89,.027),"Steel",.01)


def export(name, make):
    fresh()
    make()
    bpy.ops.object.select_all(action="DESELECT")
    for obj in list(bpy.context.scene.objects):
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT / (name+".fbx")), use_selection=True,
                            apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS", axis_forward="-Z", axis_up="Y",
                            object_types={"MESH", "OTHER"}, mesh_smooth_type="FACE", use_mesh_modifiers=True,
                            add_leaf_bones=False, bake_anim=False, embed_textures=False)


export("CTCC_PROP_WardBed",bed)
export("CTCC_PROP_BedsideCabinet",bedside)
export("CTCC_PROP_TransportTrolley",trolley)
