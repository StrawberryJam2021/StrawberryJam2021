module SJ2021DashSequenceController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/DashSequenceController" DashController(x::Integer, y::Integer,
    dashCode::String="", flagLabel::String="", flagOnFailure::String="")

const placements = Ahorn.PlacementDict(
    "Dash Code Flag Sequence Controller\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        DashController
    )
)

function Ahorn.selection(entity::DashController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/StrawberryJam2021/dashSequenceController", x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DashController)
    Ahorn.drawSprite(ctx, "objects/StrawberryJam2021/dashSequenceController", 0, 0)
end

end