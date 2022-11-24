module SJ2021CustomBadelineBoost

using ..Ahorn, Maple

@mapdef Entity "SJ2021/CustomBadelineBoost" CustomBadelineBoost(x::Integer, y::Integer, spawnPointX::Integer=0, spawnPointY::Integer=0, room::String="", colorGrade::String="none", transitionType::String="Transition", wipeColor::String="ffffff", forceCameraUpdate::Bool=false, nodes::Array{Tuple{Integer, Integer}, 1}=Tuple{Integer, Integer}[])

const placements = Ahorn.PlacementDict(
	"Custom Badeline Boost (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
		CustomBadelineBoost,
		"point",
		Dict{String, Any}(
			"transitionType" => "Transition",
		)
	),
)

Ahorn.nodeLimits(entity::CustomBadelineBoost) = 0, -1

sprite = "objects/badelineboost/idle00.png"
const transitions = String[
	"Transition",
	"Respawn",
	"WalkInRight",
	"WalkInLeft",
	"Jump",
	"WakeUp",
	"Fall",
	"TempleMirrorVoid",
	"None",
	"ThinkForABit",
]

Ahorn.editingOptions(entity::CustomBadelineBoost) = Dict{String, Any}(
	"transitionType" => transitions,
)

function Ahorn.selection(entity::CustomBadelineBoost)
	nodes = get(entity.data, "nodes", ())
	x, y = Ahorn.position(entity)

	res = Ahorn.Rectangle[Ahorn.getSpriteRectangle(sprite, x, y)]

	for node in nodes
		nx, ny = Int.(node)

		push!(res, Ahorn.getSpriteRectangle(sprite, nx, ny))
	end

	return res
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomBadelineBoost)
	px, py = Ahorn.position(entity)

	for node in get(entity.data, "nodes", ())
		nx, ny = Int.(node)

		theta = atan(py - ny, px - nx)
		Ahorn.drawArrow(ctx, px, py, nx + cos(theta) * 8, ny + sin(theta) * 8, Ahorn.colors.selection_selected_fc, headLength=6)
		Ahorn.drawSprite(ctx, sprite, nx, ny)	
		px, py = nx, ny
	end
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CustomBadelineBoost, room::Maple.Room)
	x, y = Ahorn.position(entity)
	Ahorn.drawSprite(ctx, sprite, x, y)
end

end