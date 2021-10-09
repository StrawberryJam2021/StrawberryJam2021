module SJ2021BulletTimeController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/BTController" BTController(x::Integer, y::Integer, speed::Number = 0.5)

const placements = Ahorn.PlacementDict(
    "Bullet Time Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BTController
    )
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BTController, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 0, 0, 8, 8, Ahorn.defaultWhiteColor, Ahorn.defaultBlackColor)
end


end
