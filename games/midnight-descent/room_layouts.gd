extends RefCounted
# Authored traversable floor plans. Templates are geometry, not decorative
# recolors: every floor has four combat plans and two boss approaches.
# Rectangles overlap by >=72 px so the player and enemies can cross seams.
const VIEW_SIZE := Vector2(1280,720)
const FLOOR_NAMES := ["Iron Vault","Elder Grove","Royal Ossuary"]
const FLOOR_NAMES_DE := ["Eisengewölbe","Alter Hain","Königliches Ossarium"]
const PLAN_NAMES := [
    ["Twin Vaults","Upper Gallery","Broken Bastion","Twin Passage"],
    ["Forked Grove","Serpent Roots","Sunken Thicket","Wildwood Crossing"],
    ["Processional Crypt","Ossuary Cross","Shattered Chapel","Royal Labyrinth"]
]
const PLAN_NAMES_DE := [
    ["Zwillingsgewölbe","Obere Galerie","Gebrochene Bastion","Doppelpassage"],
    ["Gabelhain","Schlangenwurzeln","Versunkenes Dickicht","Waldkreuzung"],
    ["Prozessionsgruft","Knochenkreuz","Zerfallene Kapelle","Königslabyrinth"]
]
const BOSS_NAMES := [
    ["The Watcher's Approach","Broken Gatehouse"],
    ["The Rooted Causeway","Heartwood Passage"],
    ["The King's Procession","Hall of Judgement"]
]
const BOSS_NAMES_DE := [
    ["Pfad des Wächters","Zerfallenes Torhaus"],
    ["Wurzelweg","Herzholzpassage"],
    ["Prozession des Königs","Halle des Urteils"]
]
const COMBAT_REGIONS := [
    [
        [[80,380,650,690],[650,600,530,190],[1100,265,1030,870]],
        [[80,400,610,620],[610,275,680,290],[620,820,670,215],[1210,280,930,850]],
        [[80,350,570,780],[580,260,790,840],[1300,380,830,660]],
        [[80,360,720,780],[730,370,920,180],[700,970,900,160],[1530,260,620,870]]
    ],
    [
        [[80,300,670,830],[675,425,525,340],[1135,185,1010,1040]],
        [[80,365,620,740],[620,340,750,235],[630,865,740,230],[1300,290,850,865]],
        [[80,300,720,845],[675,280,640,860],[1240,395,890,660]],
        [[80,320,640,850],[655,310,770,300],[700,945,725,190],[1350,225,810,940]]
    ],
    [
        [[80,375,680,735],[690,535,570,265],[1190,245,955,930]],
        [[80,350,710,795],[710,280,645,285],[720,870,670,225],[1300,320,850,800]],
        [[80,285,710,855],[705,400,765,640],[1390,295,760,840]],
        [[80,375,710,695],[700,345,785,210],[710,890,775,200],[1400,245,755,930]]
    ]
]
const BOSS_REGIONS := [
    [
        [[80,565,540,190],[570,560,455,165],[975,460,390,390],[1295,185,1075,1040]],
        [[80,625,600,170],[605,415,575,540],[1110,620,315,160],[1350,165,1030,1070]]
    ],
    [
        [[80,605,560,180],[590,565,490,200],[1010,420,375,440],[1310,145,1080,1110]],
        [[80,550,580,200],[620,360,420,445],[975,665,430,175],[1330,195,1040,1040]]
    ],
    [
        [[80,635,560,155],[610,560,475,270],[1020,510,390,310],[1340,170,1060,1080]],
        [[80,575,550,185],[590,400,500,475],[1010,605,465,160],[1400,150,1000,1100]]
    ]
]
const COMBAT_WALLS := [
    [
        [[315,525,96,145],[440,850,150,70],[1440,355,165,92],[1780,910,110,130]],
        [[275,680,85,140],[915,335,80,90],[1510,485,140,160],[1840,825,160,85]],
        [[340,790,150,80],[820,425,130,150],[1460,585,90,160],[1840,475,145,90]],
        [[400,500,105,190],[880,1050,175,70],[1710,465,120,150],[1870,895,135,105]]
    ],
    [
        [[305,470,125,140],[880,545,65,125],[1440,365,180,105],[1740,825,145,130]],
        [[290,585,110,125],[920,355,165,80],[1460,530,95,185],[1840,900,130,90]],
        [[315,450,85,180],[910,690,130,95],[1560,450,140,125],[1880,825,115,140]],
        [[350,490,150,110],[1050,395,95,165],[1660,490,185,90],[1730,930,110,95]]
    ],
    [
        [[370,550,150,130],[820,640,180,90],[1440,390,115,190],[1840,850,155,115]],
        [[355,615,110,175],[1000,335,165,110],[1570,440,120,190],[1750,875,180,95]],
        [[345,500,130,190],[1030,495,140,125],[1590,455,130,150],[1850,840,115,165]],
        [[340,585,180,95],[910,945,155,110],[1575,425,175,95],[1870,870,95,160]]
    ]
]
const BOSS_WALLS := [
    [
        [[325,585,65,95],[1085,480,80,95],[1560,340,110,130],[1930,950,150,115]],
        [[330,645,60,90],[865,490,115,85],[1650,290,150,120],[1900,970,135,150]]
    ],
    [
        [[380,625,80,120],[1135,535,75,120],[1650,350,150,100],[1860,950,145,100]],
        [[355,575,70,130],[810,450,80,120],[1660,360,130,115],[1940,940,120,145]]
    ],
    [
        [[325,660,70,85],[860,595,90,110],[1600,345,125,175],[1970,980,130,110]],
        [[290,590,100,110],[815,470,105,160],[1700,420,170,85],[1980,935,155,125]]
    ]
]
static func rects(raw: Array) -> Array[Rect2]:
    var output: Array[Rect2]=[]
    for record in raw:
        output.append(Rect2(float(record[0]),float(record[1]),float(record[2]),float(record[3])))
    return output

static func build(floor: int, variation: int, boss: bool=false) -> Dictionary:
    var f: int=clampi(floor,0,2)
    var index: int=posmod(variation,2 if boss else 4)
    var regions: Array[Rect2]=rects(BOSS_REGIONS[f][index] if boss else COMBAT_REGIONS[f][index])
    var walls: Array[Rect2]=rects(BOSS_WALLS[f][index] if boss else COMBAT_WALLS[f][index])
    var bounds:=Rect2(64,118,2400 if boss else 2200,1220)
    var boss_zone:=Rect2(1375,225,990,960) if boss else Rect2()
    if boss:
        boss_zone=regions[-1].grow(-34)
    var origin: Vector2=Vector2(180,700)
    var exit_point: Vector2=Vector2(regions[-1].end.x-120,700)
    var boss_point: Vector2=Vector2(boss_zone.position.x+boss_zone.size.x*0.62,boss_zone.get_center().y)
    # Gate sits in the *exclusive* approach corridor. Placing a door in the
    # overlap with the huge arena leaves a bypass around its upper/lower end.
    var gate: Rect2=Rect2()
    if boss:
        var approach: Rect2=regions[-2]
        var previous: Rect2=regions[-3]
        var left: float=maxf(previous.end.x,approach.position.x)+35.0
        var right: float=minf(boss_zone.position.x,approach.end.x)-35.0
        # Place the shutter near the far mouth, not through a pillar in
        # the middle of the approach (the Elder Grove has one there).
        var gate_x: float=clampf(boss_zone.position.x-82.0,left,right)
        gate=Rect2(gate_x-27,approach.position.y-5,54,approach.size.y+10)
    return {
        "regions":regions,"walls":walls,"bounds":bounds,
        "spawn":origin,"exit":exit_point,"boss_arena":boss_zone,
        "boss_spawn":boss_point,
        "boss_trigger":boss_zone.position.x+105,
        "boss_gate":gate,
        "name":(BOSS_NAMES[f][index] if boss else PLAN_NAMES[f][index]),
        "de_name":(BOSS_NAMES_DE[f][index] if boss else PLAN_NAMES_DE[f][index]),
        "floor_name":FLOOR_NAMES[f],"de_floor_name":FLOOR_NAMES_DE[f],
        "variant":index,"boss":boss
    }

static func accessible(point: Vector2, radius: float, regions: Array[Rect2], walls: Array[Rect2]) -> bool:
    var inside:=false
    for region in regions:
        if region.grow(-radius).has_point(point):
            inside=true
            break
    if not inside: return false
    for wall in walls:
        if wall.grow(radius).has_point(point): return false
    return true

static func smoke_test() -> bool:
    # Check all 18 authored layouts, corridor connectivity and combat space.
    for floor in range(3):
        for boss in [false,true]:
            for index in range(2 if boss else 4):
                var layout: Dictionary=build(floor,index,boss)
                var regions: Array[Rect2]=layout.regions
                var walls: Array[Rect2]=layout.walls
                if regions.size()<3 or walls.size()<3: return false
                if not accessible(layout.spawn,15,regions,walls): return false
                if not accessible(layout.exit,25,regions,walls): return false
                if boss and not accessible(layout.boss_spawn,32,regions,walls): return false
                if boss:
                    var door: Rect2=layout.boss_gate
                    var approach: Rect2=regions[-2]
                    if door.size.x<50 or door.size.y<150 or not approach.encloses(door.grow(-6)): return false
                    if door.position.x<=regions[-3].end.x or door.end.x>=regions[-1].position.x: return false
                    if not accessible(door.get_center(),15,regions,walls): return false
                    var sealed_walls: Array[Rect2]=walls.duplicate()
                    sealed_walls.append(door)
                    if not accessible(layout.boss_spawn,30,regions,sealed_walls): return false
                # Flood-fill at 24px resolution. Every course must be walkable
                # from the entrance to the far gate, even through bottlenecks.
                var origin: Vector2=layout.spawn
                var goal: Vector2=layout.exit
                var queue: Array[Vector2i]=[Vector2i(roundi(origin.x/24.0),roundi(origin.y/24.0))]
                var visited: Dictionary={queue[0]:true}
                var reached:=false
                var head:=0
                while head<queue.size() and queue.size()<16000:
                    var cell: Vector2i=queue[head]
                    head+=1
                    var at:=Vector2(cell.x*24,cell.y*24)
                    if at.distance_to(goal)<32:
                        reached=true
                        break
                    for move in [Vector2i(1,0),Vector2i(-1,0),Vector2i(0,1),Vector2i(0,-1)]:
                        var next: Vector2i=cell+move
                        if visited.has(next): continue
                        if not accessible(Vector2(next.x*24,next.y*24),15,regions,walls): continue
                        visited[next]=true
                        queue.append(next)
                if not reached:
                    push_error("Disconnected floor plan: "+str(floor)+" / "+str(index)+" boss="+str(boss))
                    return false
    return true
