module SJ2021

using ..Ahorn, Maple

@mapdef Entity "SJ2021/AirTimeMusicController" AirTimeMusicController(x::Integer, y::Integer, activationThreshold::Number=0.5, musicParam="")

const placements = Ahorn.PlacementDict(
    "Airtime Music Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        AirTimeMusicController
    )
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::AirTimeMusicController, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 0, 0, 8, 8, Ahorn.defaultWhiteColor, Ahorn.defaultBlackColor)
end


end
