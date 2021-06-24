module SJ2021SpeedPreservePuffer
using ..Ahorn, Maple

@mapdef Entity "SJ2021/SpeedPreservePuffer" Puffer(x::Integer, y::Integer, right::Bool=true, static::Bool=true)

const placements = Ahorn.PlacementDict(
    "Speed Preserving Puffer (Right) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        Puffer,
        "point",
        Dict{String, Any}(
            "right" => true
        )
    ),
    "Speed Preserving Puffer (Left) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        Puffer,
        "point",
        Dict{String, Any}(
            "right" => false
        )
    )
)

sprite = "objects/puffer/idle00"

function Ahorn.selection(entity::Puffer)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Puffer, room::Maple.Room)
    scaleX = get(entity, "right", false) ? 1 : -1
    Ahorn.drawSprite(ctx, sprite, 0, 0, sx=scaleX)
end

function Ahorn.flipped(entity::Puffer, horizontal::Bool)
    if horizontal
        entity.right = !entity.right
        return entity
    end
end

end