module SJ2021MultiNodeBadelineBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/MultiNodeBadelineBlock" MultiNodeBadelineBlock(x::Integer, y::Integer, 
    width::Integer=Maple.defaultBlockWidth, 
    height::Integer=Maple.defaultBlockHeight, 
    nodeIndex::Integer=0, 
    nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])

const placements = Ahorn.PlacementDict(
    "Badeline Boss Moving Block (Multi-node) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        MultiNodeBadelineBlock,
        "rectangle",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
        end
    )
)

Ahorn.nodeLimits(entity::MultiNodeBadelineBlock) = 1, -1
Ahorn.minimumSize(entity::MultiNodeBadelineBlock) = 8, 8
Ahorn.resizable(entity::MultiNodeBadelineBlock) = true, true

function Ahorn.selection(entity::MultiNodeBadelineBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    rects = [Ahorn.Rectangle(x, y, width, height)]

    for node in entity.data["nodes"]
        nx, ny = Int.(node)
        push!(rects, Ahorn.Rectangle(nx, ny, width, height))
    end

    return rects
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::MultiNodeBadelineBlock, room::Maple.Room)
    Ahorn.drawTileEntity(ctx, room, entity, material='g', blendIn=false)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::MultiNodeBadelineBlock, room::Maple.Room)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    cox, coy = floor(Int, width / 2), floor(Int, height / 2)
    
    if !isempty(nodes)
        px, py = x, y
        for node in entity.data["nodes"]
            nx, ny = Int.(node)

            # Use 'G' instead of 'g', as that is the highlight color of the block (the active color)
            fakeTiles = Ahorn.createFakeTiles(room, nx, ny, width, height, 'G', blendIn=false)
            Ahorn.drawFakeTiles(ctx, room, fakeTiles, room.objTiles, true, nx, ny, clipEdges=true)
            Ahorn.drawArrow(ctx, px + cox, py + coy, nx + cox, ny + coy, Ahorn.colors.selection_selected_fc, headLength=6)

            px, py = nx, ny
        end
        Ahorn.drawArrow(ctx, px + cox, py + coy, x + cox, y + coy, Ahorn.colors.selection_selected_fc, headLength=6)
    end
end

end