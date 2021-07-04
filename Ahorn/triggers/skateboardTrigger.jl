module SJ2021SkateboardTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/SkateboardTrigger" SkateboardTrigger(x::Integer, y::Integer, width::Integer=16, height::Integer=16, mode::String="enable")

const placements = Ahorn.PlacementDict(
    "Skateboard Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SkateboardTrigger,
        "rectangle"
    )
)

Ahorn.editingOptions(entity::SkateboardTrigger) = Dict{String, Any}(
  "mode" => [
	"enable",
	"disable",
	"toggle"
  ]
)

end
