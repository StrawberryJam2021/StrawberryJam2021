module SJ2021TimeFreezeMusicController
using ..Ahorn, Maple

@mapdef Entity "SJ2021/TimeFreezeMusicController" TimeFreezeMusicController(x::Integer, y::Integer, paramName::String="", paramOff::Number=0.0, paramOn::Number=1.0)

const placements = Ahorn.PlacementDict(
	"Time Freeze Music Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
		TimeFreezeMusicController,
		"rectangle"
	)
)

function Ahorn.selection(entity::TimeFreezeMusicController)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x-16, y-16, 32, 32)
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::TimeFreezeMusicController)
	x, y = Ahorn.position(entity)
	Ahorn.drawRectangle(ctx, x-16, y-16, 32, 32, (0.6, 0.2, 0.2, 0.6), (0.6, 0.2, 0.2, 0.4))
	Ahorn.drawCenteredText(ctx, "Time Freeze Music Controller (SJ2021)", x-16, y-16, 32, 32)
end

end