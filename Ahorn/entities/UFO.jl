module SJ2021UFO

using ..Ahorn, Maple

@mapdef Entity "SJ2021/UFO" UFO(x::Integer, y::Integer, nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[], RaySizeX::Integer = 13, RaySizeY::Integer = 60)

const placements = Ahorn.PlacementDict(
    "UFO" => Ahorn.EntityPlacement(
        UFO
    )				
)

function Ahorn.selection(entity::UFO)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    spinnersprite = "objects/StrawberryJam2021/UFO/UFO.png"
    res = [Ahorn.Rectangle(x - 13, y - 21, 26, 26)]
	
	for node in nodes
        nx, ny = node
        push!(res, Ahorn.getSpriteRectangle(spinnersprite, nx, ny))
	end
	
	return res
end

spinnersprite = "objects/StrawberryJam2021/UFO/UFO.png"
function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::UFO, room::Maple.Room)
    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = node
        Ahorn.drawSprite(ctx, spinnersprite, nx, ny)
    end
	sprite = "objects/StrawberryJam2021/UFO/UFO.png"
x, y = Ahorn.position(entity)
Ahorn.drawSprite(ctx, sprite, x, y)
end
Ahorn.nodeLimits(entity::UFO) = 1, -1
end