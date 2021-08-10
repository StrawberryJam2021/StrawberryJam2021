module SJ2021NodedCloud

using ..Ahorn, Maple

@mapdef Entity "SJ2021/NodedCloud" NodedCloud(x::Integer, y::Integer, moveTime::Number=0.5,
nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])


const placements = Ahorn.PlacementDict(
    "Noded Cloud (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        NodedCloud
    )
)

Ahorn.nodeLimits(entity::NodedCloud) = 0,-1

sprite = "objects/clouds/fragile00"


function Ahorn.selection(entity::NodedCloud)
    nodes = get(entity.data, "nodes", ())

    x, y = Ahorn.position(entity)

    rect = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]

    for node in nodes
        nx, ny = Int.(node)
        push!(rect, Ahorn.getSpriteRectangle(sprite, nx, ny))
    end
    return rect
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::NodedCloud, room::Maple.Room)
    x,y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, sprite, x, y)

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)
        Ahorn.drawSprite(ctx, sprite, nx, ny, alpha=0.5)
    end
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::NodedCloud, room::Maple.Room)
    x, y = Ahorn.position(entity)
    oldx, oldy = x, y

    for node in get(entity.data, "nodes", ())
        nx, ny = Int.(node)
        Ahorn.drawArrow(ctx, oldx, oldy, nx, ny, Ahorn.colors.selection_selected_fc, headLength = 6)
        Ahorn.drawSprite(ctx, sprite, nx, ny, alpha=0.5)
        oldx, oldy = nx, ny
    end
end

end
