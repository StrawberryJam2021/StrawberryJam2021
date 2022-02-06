module SJ2021ZeroGTrigger
using ..Ahorn, Maple

@mapdef Trigger "SJ2021/ZeroGTrigger" ZeroGSpawnTrigger(x::Integer, y::Integer, width::Integer=8, height::Integer=8, state::Bool=true)

const placements = Ahorn.PlacementDict(
	"Zero G Set On Spawn Trigger (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
		ZeroGSpawnTrigger,
		"rectangle"
	)
)

end