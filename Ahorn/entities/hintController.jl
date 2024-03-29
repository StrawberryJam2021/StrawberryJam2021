module SJ2021HintController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/HintController" HintController(
    x::Integer, y::Integer,
    dialogId1::String="", dialogId2::String="",
    singleUse1::Bool=false, singleUse2::Bool=false,
    toggleFlag::String=""
)

const placements = Ahorn.PlacementDict(
    "Hint Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        HintController
    )
)

sprite = "objects/StrawberryJam2021/hintController"
Ahorn.selection(entity::HintController) = Ahorn.getSpriteRectangle(sprite, entity.x, entity.y)
Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::HintController, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end