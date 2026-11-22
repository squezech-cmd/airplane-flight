from PIL import Image

OUT_ARMOR = "src/main/resources/assets/jetpack/textures/models/armor/jetpack_layer_1.png"
OUT_ITEM  = "src/main/resources/assets/jetpack/textures/item/jetpack.png"

STEEL_LIGHT=(176,186,200,255); STEEL=(128,138,152,255)
STEEL_DARK=(92,101,114,255);   STEEL_DEEP=(62,69,80,255); SHADOW=(40,45,53,255)
TANK_LIGHT=(214,84,58,255); TANK=(176,52,36,255)
TANK_DARK=(130,34,24,255);  TANK_DEEP=(94,22,16,255)
COP_LIGHT=(236,156,78,255); COP=(200,120,54,255); COP_DARK=(152,84,38,255)
CORE=(255,248,196,255); GLOW=(255,198,68,255); GLOW_MID=(246,142,38,255); GLOW_EDGE=(198,76,26,255)
CLEAR=(0,0,0,0)

class P:
    def __init__(s,img): s.img=img; s.px=img.load()
    def fill(s,x,y,w,h,c):
        for iy in range(y,y+h):
            for ix in range(x,x+w):
                if 0<=ix<s.img.width and 0<=iy<s.img.height: s.px[ix,iy]=c
    def outl(s,x,y,w,h,c):
        for ix in range(x,x+w): s.px[ix,y]=c; s.px[ix,y+h-1]=c
        for iy in range(y,y+h): s.px[x,iy]=c; s.px[x+w-1,iy]=c
    def face(s,x,y,w,h,base,light,dark,outline=None):
        s.fill(x,y,w,h,base)
        if h>1: s.fill(x,y,w,1,light); s.fill(x,y+h-1,w,1,dark)
        if outline: s.outl(x,y,w,h,outline)
    def metal(s,x,y,w,h,base,light,dark,deep):
        s.face(x,y,w,h,base,light,dark,deep)
        if w>=3 and h>=4:
            sx=x+max(1,w//2)
            for iy in range(y+1,y+h-1): s.px[sx,iy]=light
    def rivet(s,x,y,w,h,c):
        for cx,cy in ((x+1,y+1),(x+w-2,y+1),(x+1,y+h-2),(x+w-2,y+h-2)): s.px[cx,cy]=c
    def glow(s,x,y,w,h):
        s.fill(x,y,w,h,GLOW_EDGE)
        if w>2 and h>2: s.fill(x+1,y+1,w-2,h-2,GLOW_MID)
        if w>4 and h>4:
            s.fill(x+2,y+2,w-4,h-4,GLOW)
            s.px[x+w//2,y+h//2]=CORE
    def flame(s,x,y,w,h):
        s.fill(x,y,w,h,GLOW_EDGE)
        if h>0: s.fill(x,y,w,max(1,h//2),GLOW_MID)
        s.fill(x,y,w,1,GLOW)
        s.px[x+w//2,y]=CORE

def faces(ux,uy,w,h,d):
    return {
      "up":   (ux+d,        uy,   w,d),
      "down": (ux+d+w,      uy,   w,d),
      "east": (ux,          uy+d, d,h),
      "north":(ux+d,        uy+d, w,h),
      "west": (ux+d+w,      uy+d, d,h),
      "south":(ux+d+w+d,    uy+d, w,h),
    }

img=Image.new("RGBA",(64,64),CLEAR); p=P(img)

# ---- spine plate: 6 wide, 10 tall, 2 deep ----
for lbl,(x,y,w,h) in faces(0,0,6,10,2).items():
    if lbl=="up":    p.face(x,y,w,h,STEEL_LIGHT,STEEL_LIGHT,STEEL,STEEL_DEEP)
    elif lbl=="down":p.face(x,y,w,h,STEEL_DEEP,STEEL_DARK,SHADOW,SHADOW)
    elif lbl=="north":      # against the player's back
        p.face(x,y,w,h,STEEL,STEEL_LIGHT,STEEL_DARK,STEEL_DEEP)
        for iy in range(y+2,y+h-1,2): p.fill(x+1,iy,w-2,1,STEEL_DEEP)
    elif lbl=="south":      # visible from behind
        p.metal(x,y,w,h,STEEL,STEEL_LIGHT,STEEL_DARK,STEEL_DEEP)
        p.rivet(x,y,w,h,STEEL_LIGHT)
        p.fill(x+w//2-1,y+2,2,h-5,STEEL_DARK)
    else: p.face(x,y,w,h,STEEL_DARK,STEEL,STEEL_DEEP,STEEL_DEEP)

# ---- tanks: 3 wide, 8 tall, 4 deep ----
for uvx in (18,34):
    for lbl,(x,y,w,h) in faces(uvx,0,3,8,4).items():
        if lbl=="up":    p.face(x,y,w,h,COP_LIGHT,COP_LIGHT,COP,COP_DARK)
        elif lbl=="down":p.face(x,y,w,h,COP_DARK,COP,COP_DARK,SHADOW)
        elif lbl in ("east","west"):
            p.face(x,y,w,h,TANK,TANK_LIGHT,TANK_DEEP,TANK_DEEP)
            p.fill(x+w//2,y+1,1,h-2,TANK_LIGHT)
        else:
            p.face(x,y,w,h,TANK,TANK_LIGHT,TANK_DARK,TANK_DEEP)
            mid=y+h//2
            p.fill(x,mid,w,2,COP); p.fill(x,mid,w,1,COP_LIGHT); p.fill(x,mid+2,w,1,COP_DARK)

# ---- nozzles: 3 wide, 2 tall, 3 deep ----
for uvx in (0,14):
    for lbl,(x,y,w,h) in faces(uvx,14,3,2,3).items():
        if lbl=="down":  p.glow(x,y,w,h)
        elif lbl=="up":  p.face(x,y,w,h,COP_DARK,COP,SHADOW,SHADOW)
        else: p.metal(x,y,w,h,COP,COP_LIGHT,COP_DARK,SHADOW)

# ---- flames: 2 wide, 1 tall, 2 deep ----
for uvx in (28,38):
    for lbl,(x,y,w,h) in faces(uvx,14,2,1,2).items():
        p.flame(x,y,w,h)

img.save(OUT_ARMOR)

# ---------------- item sprite ----------------
i=Image.new("RGBA",(16,16),CLEAR); q=P(i)
q.fill(2,3,4,9,TANK); q.outl(2,3,4,9,TANK_DEEP); q.fill(3,4,1,7,TANK_LIGHT)
q.fill(10,3,4,9,TANK); q.outl(10,3,4,9,TANK_DEEP); q.fill(11,4,1,7,TANK_LIGHT)
q.fill(2,6,4,2,COP); q.fill(10,6,4,2,COP)
q.fill(2,6,4,1,COP_LIGHT); q.fill(10,6,4,1,COP_LIGHT)
q.fill(6,2,4,8,STEEL); q.outl(6,2,4,8,STEEL_DEEP); q.fill(7,3,1,6,STEEL_LIGHT)
q.fill(2,2,4,1,COP_LIGHT); q.fill(10,2,4,1,COP_LIGHT)
q.fill(3,12,2,2,COP_DARK); q.fill(11,12,2,2,COP_DARK)
q.fill(3,14,2,1,GLOW); q.fill(11,14,2,1,GLOW)
q.px[3,15]=GLOW_MID; q.px[4,15]=GLOW_MID; q.px[11,15]=GLOW_MID; q.px[12,15]=GLOW_MID
i.save(OUT_ITEM)
print("textures written")
