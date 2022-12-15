module SJ2021PhotosensitiveFlagController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/PhotosensitiveFlagController" PhotosensitiveFlagController(x::Integer, y::Integer, flag::String="")

const placements = Ahorn.PlacementDict(
    "Photosensitive Flag Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PhotosensitiveFlagController
    )
)

const sprite = "objects/StrawberryJam2021/photosensitiveFlagController/icon"

function Ahorn.selection(entity::PhotosensitiveFlagController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PhotosensitiveFlagController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end
