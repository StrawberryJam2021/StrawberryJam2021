module SJ2021CassetteBadelineBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/CassetteBadelineBlock" CassetteBadelineBlock(
    x::Integer, y::Integer,
    width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight,
    nodes=Tuple{Int,Int}[],
    tiletype::String="g", hideFinalTransition::Bool=true, ignoredNodes::String="", offBeat::Bool=false,
    emitImpactParticles::Bool=true,
    centerSpriteName::String="", centerSpriteRotation::Integer=0, centerSpriteFlipX::Bool=false, centerSpriteFlipY::Bool=false
)

Ahorn.editingOrder(entity::CassetteBadelineBlock) = String[
    "x", "y", "width", "height",
    "tiletype", "ignoredNodes", "hideFinalTransition", "offBeat",
    "emitImpactParticles",
    "centerSpriteName", "centerSpriteRotation", "centerSpriteFlipX", "centerSpriteFlipY"
]

const placements = Ahorn.PlacementDict(
    "Cassette Timed Conveyor Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteBadelineBlock,
        "rectangle",
        Dict{String,Any}("playImpactSounds" => false, "emitImpactParticles" => false),
        function (entity)
            entity.data["nodes"] = [
                (Int(entity.data["x"]) + Int(entity.data["width"]) + 8,      Int(entity.data["y"])),
                (Int(entity.data["x"]) + Int(entity.data["width"]) * 2 + 16, Int(entity.data["y"]))
            ]
        end
    ),
    "Cassette Timed Swap Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteBadelineBlock,
        "rectangle",
        Dict{String,Any}("hideFinalTransition" => false, "ignoredNodes" => "1"),
        function (entity)
            entity.data["nodes"] = [
                (Int(entity.data["x"]) + Int(entity.data["width"]) + 8,      Int(entity.data["y"]))
            ]
        end
    ),
    "Cassette Timed Cycle Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteBadelineBlock,
        "rectangle",
        Dict{String,Any}("hideFinalTransition" => false),
        function (entity)
            entity.data["nodes"] = [
                (Int(entity.data["x"]) + Int(entity.data["width"]) + 8,      Int(entity.data["y"])),
                (Int(entity.data["x"]) + Int(entity.data["width"]) * 2 + 16, Int(entity.data["y"]))
            ]
        end
    )
)

Ahorn.editingOptions(entity::CassetteBadelineBlock) = Dict{String,Any}(
    "tiletype" => Ahorn.tiletypeEditingOptions()
)

Ahorn.nodeLimits(entity::CassetteBadelineBlock) = 1, -1
Ahorn.minimumSize(entity::CassetteBadelineBlock) = 8, 8
Ahorn.resizable(entity::CassetteBadelineBlock) = true, true

function Ahorn.selection(entity::CassetteBadelineBlock)
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

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CassetteBadelineBlock, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    ignored = strip(get(entity.data, "ignoredNodes", ""))
    ignoredNodes = tryparse.(Int, split(ignored, r", ?"))

    if 0 in ignoredNodes
        Ahorn.drawTileEntity(ctx, room, entity, material=get(entity.data, "tiletype", "g")[1], alpha=0.5, blendIn=false)
    else
        Ahorn.drawTileEntity(ctx, room, entity, material=get(entity.data, "tiletype", "g")[1], blendIn=false)
    end

    prev = (x, y)
    for (index, (nx, ny)) in enumerate(nodes)
        cox, coy = floor(Int, width / 2), floor(Int, height / 2)

        fakeTiles = Ahorn.createFakeTiles(room, nx, ny, width, height, get(entity.data, "tiletype", "g")[1], blendIn=false)

        if index in ignoredNodes
            Ahorn.drawFakeTiles(ctx, room, fakeTiles, room.objTiles, true, nx, ny, alpha=0.5, clipEdges=true)
        else
            Ahorn.drawFakeTiles(ctx, room, fakeTiles, room.objTiles, true, nx, ny, clipEdges=true)
        end

        Ahorn.drawArrow(ctx, prev[1] + cox, prev[2] + coy, nx + cox, ny + coy, Ahorn.colors.selection_selected_fc, headLength=6)
        prev = (nx, ny)
    end
end

end
