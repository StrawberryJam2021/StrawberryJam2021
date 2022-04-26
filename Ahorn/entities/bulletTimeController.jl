module SJ2021BulletTimeController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/BTController" BTController(x::Integer, y::Integer, speed::Number = 0.5, flag::String="")

const placements = Ahorn.PlacementDict(
    "Bullet Time Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BTController,
        "rectangle"
    )
)

function Ahorn.selection(entity::BTController)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x, y, 8, 8)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BTController, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 0, 0, 8, 8, Ahorn.defaultWhiteColor, Ahorn.defaultBlackColor)
end


end
