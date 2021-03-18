module SJ2021SpinnerVines

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SpinnerVines" SpinnerVines(x::Integer, y::Integer, ID ::Integer = 0; TentacleColor :: String = "ffffff", SpinnerColor :: String = "ffffff", nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[], NewSpinnerWaitTime :: Number = 0.0, MaxThicknessDecrease :: Number = 0.0, TentacleWidth :: Number = 0.0)

const placements = Ahorn.PlacementDict(
    "SpinnerVines" => Ahorn.EntityPlacement(
        SpinnerVines
    )				
)

function Ahorn.selection(entity::SpinnerVines)
    x, y = Ahorn.position(entity)
    nodes = get(entity.data, "nodes", ())
    spinnersprite = "objects/StrawberryJam2021/spinnerVine/VineSpinner.png"
    res = [Ahorn.Rectangle(x - 13, y - 21, 26, 26)]
	
	for node in nodes
        nx, ny = node
  	x, y = Ahorn.position(entity)
	nx = nx + x
	ny = ny + y
        push!(res, Ahorn.getSpriteRectangle(spinnersprite, nx, ny))
	end
	
	return res
end

spinnersprite = "objects/StrawberryJam2021/spinnerVine/VineSpinner.png"
function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::SpinnerVines, room::Maple.Room)
    nodes = get(entity.data, "nodes", ())
    for node in nodes
        nx, ny = node
  	x, y = Ahorn.position(entity)
	nx = nx + x
	ny = ny + y
        Ahorn.drawSprite(ctx, spinnersprite, nx, ny)
    end
	sprite = "objects/StrawberryJam2021/spinnerVine/VineController.png"
x, y = Ahorn.position(entity)
Ahorn.drawSprite(ctx, sprite, x, y)
end
Ahorn.nodeLimits(entity::SpinnerVines) = 1, -1
end