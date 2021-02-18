module StrawberryJamDashSequenceController

using ..Ahorn, Maple

@mapdef Entity "StrawberryJam2021/DashSequenceController" DashController(x::Integer, y::Integer,
	dashCode::String="", flagLabel::String="")

const placements = Ahorn.PlacementDict(
    "Dash Code Flag Sequence Controller\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        DashController,
		"point"
    )
)

function Ahorn.selection(entity::DashController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/StrawberryJam2021/dashSequenceController.png", x, y, jx=0.5, jy=0.5)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DashController)
	Ahorn.drawSprite(ctx, "objects/StrawberryJam2021/dashSequenceController.png", 0, 0, jx=0.5, jy=0.5)
end

end