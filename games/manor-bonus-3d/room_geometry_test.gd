extends SceneTree

# Actual Godot scene-graph regression test. These problems were visible in the
# shipped WebGL rooms but were previously invisible to the smoke test.
func _initialize() -> void:
    call_deferred("_run")


func _run() -> void:
    var packed := load("res://main.tscn") as PackedScene
    if packed == null:
        _fail("Cannot load Manor scene")
        return
    var scene := packed.instantiate()
    root.add_child(scene)
    await process_frame
    await physics_frame

    for room_id in range(17):
        scene.call("_set_room", "Room %d" % room_id, room_id)
        var visuals := scene.get_node_or_null("RoomVisuals") as Node3D
        if visuals == null:
            _fail("No scene graph for room %d" % room_id)
            return

        # All room families must have an albedo texture on their floor and
        # masonry. Explicit UVs avoid WebGL compatibility triplanar artifacts.
        var floor := visuals.get_node_or_null("Floor") as MeshInstance3D
        var rear := visuals.get_node_or_null("RearWall") as MeshInstance3D
        if not _textured(floor) or not _textured(rear):
            _fail("Missing or triplanar floor/wall texture in room %d" % room_id)
            return
        var floor_mat := floor.material_override as StandardMaterial3D
        if floor_mat.uv1_scale.x < 2.0 or not floor_mat.texture_repeat:
            _fail("Room %d floor texture does not tile over large surfaces" % room_id)
            return
        if room_id not in [15, 16]:
            var ceiling := visuals.get_node_or_null("Ceiling") as MeshInstance3D
            if not _textured(ceiling):
                _fail("Missing or triplanar ceiling material in room %d" % room_id)
                return

        # Every candle requires a real base contact. The old hard-coded 0.52 m
        # center caused both floating candles and candles through tabletops.
        var candle_count := 0
        for node in visuals.find_children("*", "MeshInstance3D", true, false):
            var candle := node as MeshInstance3D
            if not candle.has_meta("support_y"):
                continue
            candle_count += 1
            var cylinder := candle.mesh as CylinderMesh
            if cylinder == null or not candle.has_meta("support_y"):
                _fail("Room %d has an unanchored candle" % room_id)
                return
            var support: float = float(candle.get_meta("support_y"))
            var bottom: float = candle.global_position.y - cylinder.height * 0.5
            if absf(bottom - (support + 0.05)) > 0.011:
                _fail("Room %d has a floating/embedded candle" % room_id)
                return
            if not _textured(candle):
                _fail("Room %d candle is missing its material" % room_id)
                return

        if room_id in [1, 2, 4, 5, 11, 12, 14] and candle_count < 3:
            _fail("Room %d candles are missing or not individually anchored" % room_id)
            return

        # Representative bespoke assets also need their actual texture maps.
        var landmark_name: String = ""
        match room_id:
            1: landmark_name = "ToolRack"
            3: landmark_name = "GrandStaircaseSteps"
            4: landmark_name = "BookcaseBack"
            5: landmark_name = "VelvetReadingSeat"
            8: landmark_name = "GrandPianoBody"
            9: landmark_name = "GardenFountainBasin"
            11: landmark_name = "BrokenAltar"
            12: landmark_name = "Sarcophagus"
            13: landmark_name = "MoonlitMonumentPlinth"
            14: landmark_name = "NarrowPassageLeft"
            15: landmark_name = "CourtyardWell"
            16: landmark_name = "MoonGateLintel"
        if landmark_name != "":
            var landmark := visuals.find_child(landmark_name, true, false) as MeshInstance3D
            if not _textured(landmark):
                _fail("Room %d prop %s has no texture" % [room_id, landmark_name])
                return

        match room_id:
            2:
                var dais := visuals.find_child("CandleStoneDais", true, false) as MeshInstance3D
                if dais == null or dais.position.y + (dais.mesh as BoxMesh).size.y*0.5 < 0.81:
                    _fail("Candle Landing has no proper candle base")
                    return
            3:
                # The stairs belong to the west wall, not in front of the
                # middle door/camera.
                for stair_node in visuals.find_children("*GrandStaircaseSteps*", "MeshInstance3D", true, false):
                    if (stair_node as MeshInstance3D).position.x > -5.4:
                        _fail("Grand Staircase still blocks the center door")
                        return
            4, 5:
                for shelf_node in visuals.find_children("*SideBookcase*", "Node3D", true, false):
                    var shelf := shelf_node as Node3D
                    if absf(shelf.position.x) < 8.7:
                        _fail("Library/Reading Room shelf is in front of the doors")
                        return
                if visuals.find_child("SideBookcase", true, false) == null:
                    _fail("Library/Reading Room shelves are not mounted to walls")
                    return
            8:
                if visuals.find_children("*MusicStandPole*", "MeshInstance3D", true, false).size() != 3:
                    _fail("Music room sheet stands have no floor-standing poles")
                    return
                if visuals.find_children("*MusicStandTripod*", "MeshInstance3D", true, false).size() != 9:
                    _fail("Music room stands are still floating")
                    return
            9:
                if visuals.find_child("SideWall", false, false) != null:
                    _fail("Winter Garden windows still face a solid wall")
                    return
                if visuals.find_children("*MoonGlass*", "MeshInstance3D", true, false).size() != 6:
                    _fail("Winter Garden does not have six real window openings")
                    return
                if visuals.find_children("*BetweenGlassBays*", "MeshInstance3D", true, false).size() < 6:
                    _fail("Winter Garden has no masonry framing around glass")
                    return
            11:
                if visuals.find_child("BrokenWallRelief", true, false) != null:
                    _fail("Forbidden Wing still has floating black wall block")
                    return
                var ceiling := visuals.get_node("Ceiling") as MeshInstance3D
                for vault_node in visuals.find_children("*VaultArch*", "MeshInstance3D", true, false):
                    var vault := vault_node as MeshInstance3D
                    if vault.position.y + (vault.mesh as BoxMesh).size.y*0.5 >= ceiling.position.y - (ceiling.mesh as BoxMesh).size.y*0.5:
                        _fail("Forbidden Wing vault beam intersects ceiling texture")
                        return
            12:
                if visuals.find_child("CryptCandlePedestal", true, false) == null:
                    _fail("Crypt candles still have no supporting altar")
                    return
            13:
                if visuals.find_child("AtticRafter", true, false) != null or visuals.find_child("AntiqueTrunk", true, false) != null:
                    _fail("Moonlit Landing still reuses Attic furniture")
                    return
                if visuals.find_child("LunarOrrery", true, false) == null:
                    _fail("Moonlit Landing lacks a unique environment")
                    return
            14:
                for passage_node in visuals.find_children("NarrowPassageLeft", "MeshInstance3D", true, false) + visuals.find_children("NarrowPassageRight", "MeshInstance3D", true, false):
                    if absf((passage_node as MeshInstance3D).position.x) < 7.25:
                        _fail("Secret Passage walls overlap the portal clearance")
                        return
                if visuals.find_child("PassageNicheTable", true, false) == null:
                    _fail("Secret Passage candles are floating")
                    return
            15, 16:
                if visuals.find_child("DistantNightSky", true, false) == null:
                    _fail("Outdoor scenery lacks the night backdrop")
                    return

    print("Manor 3D room geometry PASS")
    quit(0)


func _textured(node: MeshInstance3D) -> bool:
    if node == null:
        return false
    var mat := node.material_override as StandardMaterial3D
    return mat != null and mat.albedo_texture != null and not mat.uv1_triplanar


func _fail(message: String) -> void:
    push_error("Manor 3D room geometry FAIL: " + message)
    quit(1)
