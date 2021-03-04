module SJ2021CassetteConveyorBlock

using ..Ahorn, Maple

@pardef CassetteConveyorBlock(x1::Integer, y1::Integer, x2::Integer =x1 + 16, y2::Integer=y1, 
        x3::Integer=x2 + 16, y3::Integer=y2, width::Integer=Maple.defaultBlockWidth, 
        height::Integer=Maple.defaultBlockHeight, tiletype::String="g", 
        waitTime::Integer=12, preDelay::Integer=0, transitionDuration::Integer=4) = 
    Entity("SJ2021/CassetteConveyorBlock", x = x1, y = y1, nodes = Tuple{Int, Int}[(x2, y2), (x3, y3)],
        width = width, height = height, tiletype = tiletype, preDelay = preDelay, waitTime = waitTime, transitionDuration = transitionDuration)

const placements = Ahorn.PlacementDict(
    "Cassette-based Conveyor Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteConveyorBlock,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"])), 
                                    (Int(entity.data["x"]) + Int(entity.data["width"]) * 2 + 16, Int(entity.data["y"]))]
        end
    )
)

Ahorn.editingOptions(entity::CassetteConveyorBlock) = Dict{String, Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions(),
)

Ahorn.nodeLimits(entity::CassetteConveyorBlock) = 1, -1
Ahorn.minimumSize(entity::CassetteConveyorBlock) = 8, 8
Ahorn.resizable(entity::CassetteConveyorBlock) = true, true

function Ahorn.selection(entity::CassetteConveyorBlock)
    if entity.name == "SJ2021/CassetteConveyorBlock"
        x, y = Ahorn.position(entity)
        width = Int(get(entity.data, "width", 8))
        height = Int(get(entity.data, "height", 8))
        
        res = [Ahorn.Rectangle(x, y, width, height)]
        
        nx, ny = Int.(entity.data["nodes"][1])
        
        for (nx, ny) in entity.data["nodes"]
            push!(res, Ahorn.Rectangle(nx, ny, width, height))
        end
        
        return res
    end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteConveyorBlock, room::Maple.Room)
    Ahorn.drawTileEntity(ctx, room, entity, material = get(entity.data, "tiletype", "g")[1], blendIn = false)
        
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    
    prev = (x, y)
    for (nx, ny) in nodes
        cox, coy = floor(Int, width / 2), floor(Int, height / 2)

        fakeTiles = Ahorn.createFakeTiles(room, nx, ny, width, height, get(entity.data, "tiletype", "g")[1], blendIn = false)
        Ahorn.drawFakeTiles(ctx, room, fakeTiles, room.objTiles, true, nx, ny, clipEdges = true)
        Ahorn.drawArrow(ctx, prev[1] + cox, prev[2] + coy, nx + cox, ny + coy, Ahorn.colors.selection_selected_fc, headLength = 6)
        prev = (nx, ny)
    end
end
end