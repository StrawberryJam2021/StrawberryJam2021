module SJ2021CassetteBadalineBlock

using ..Ahorn, Maple

@pardef CassetteBadalineBlock(x1::Integer, y1::Integer, x2::Integer=x1 + 16, y2::Integer=y1, 
        width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, 
        tiletype::String="g", moveForwardBeat::Integer=0, moveBackBeat::Integer=8, 
        preDelay::Integer=0, transitionDuration::Integer=4, oneWay::Bool=false, teleportBack::Bool=false,
        alignToCassetteTimer::Bool=false) = 
    Entity("SJ2021/CassetteBadalineBlock", x = x1, y = y1, nodes = Tuple{Int, Int}[(x2, y2)],
        width = width, height = height, tiletype = tiletype,
        moveForwardBeat = moveForwardBeat, moveBackBeat = moveBackBeat, preDelay = preDelay, 
        transitionDuration = transitionDuration, oneWay = oneWay, teleportBack = teleportBack, 
        alignToCassetteTImer = alignToCassetteTimer)
        
Ahorn.editingOrder(entity::CassetteBadalineBlock) = String["x", "y", "width", "height", 
    "moveForwardBeat", "moveBackBeat", "preDelay", "transitionDuration", "oneWay", "teleportBack", 
    "alignToCassetteTimer", "tiletype"]

const placements = Ahorn.PlacementDict(
    "Cassette-based Moving Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteBadalineBlock,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
        end
    )
)

Ahorn.editingOptions(entity::CassetteBadalineBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions(),
)

Ahorn.nodeLimits(entity::CassetteBadalineBlock) = 1, 1
Ahorn.minimumSize(entity::CassetteBadalineBlock) = 8, 8
Ahorn.resizable(entity::CassetteBadalineBlock) = true, true

function Ahorn.selection(entity::CassetteBadalineBlock)
    if entity.name == "SJ2021/CassetteBadalineBlock"
        x, y = Ahorn.position(entity)
        nx, ny = Int.(entity.data["nodes"][1])

        width = Int(get(entity.data, "width", 8))
        height = Int(get(entity.data, "height", 8))

        return [Ahorn.Rectangle(x, y, width, height), Ahorn.Rectangle(nx, ny, width, height)]
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteBadalineBlock, room::Maple.Room)
    Ahorn.drawTileEntity(ctx, room, entity, material = get(entity.data, "tiletype", "g")[1], blendIn = false)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteBadalineBlock, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    
    if !isempty(nodes)
        nx, ny = Int.(nodes[1])
        cox, coy = floor(Int, width / 2), floor(Int, height / 2)

        fakeTiles = Ahorn.createFakeTiles(room, nx, ny, width, height, get(entity.data, "tiletype", "g")[1], blendIn = false)
        Ahorn.drawFakeTiles(ctx, room, fakeTiles, room.objTiles, true, nx, ny, clipEdges = true)
        Ahorn.drawArrow(ctx, x + cox, y + coy, nx + cox, ny + coy, Ahorn.colors.selection_selected_fc, headLength = 6)
    end
end
end