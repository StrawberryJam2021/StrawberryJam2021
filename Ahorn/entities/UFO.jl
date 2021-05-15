module SJ2021UFO

using ..Ahorn, Maple

@mapdef Entity "SJ2021/UFO" UFO(x::Integer, y::Integer, nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[], RaySizeX::Integer = 13, RaySizeY::Integer = 60)

const placements = Ahorn.PlacementDict(
    "UFO (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        UFO
    )				
)

UFOsprite = "objects/StrawberryJam2021/UFO/UFO.png"

function Ahorn.selection(entity::UFO)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    res = [Ahorn.Rectangle(x - 13, y - 21, 26, 26)]
	
    for node in nodes
        nx, ny = node
        push!(res, Ahorn.getSpriteRectangle(UFOsprite, nx, ny))
    end
	
    return res
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::UFO, room::Maple.Room)
    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = node
        Ahorn.drawSprite(ctx, UFOsprite, nx, ny)
    end
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, UFOsprite, x, y)
end
Ahorn.nodeLimits(entity::UFO) = 1, -1
end