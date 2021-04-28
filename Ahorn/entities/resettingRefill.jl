module SJ2021ResettingRefill

using ..Ahorn, Maple

@mapdef Entity "SJ2021/ResettingRefill" ResettingRefill(x::Integer, y::Integer, oneUse::Bool=false)

const placements = Ahorn.PlacementDict(
    "Resetting Refill (Extra Jump)\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ResettingRefill,
        "point",
        Dict{String, Any}(
            "dashes" => 0,
            "extraJump" => true,
            "persistJump" => false
        )
    ),

    "Resetting Refill (Extra Persistent Jump)\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ResettingRefill,
        "point",
        Dict{String, Any}(
            "dashes" => 0,
            "extraJump" => true,
            "persistJump" => true
        )
    ),

    "Resetting Refill (One Dash)\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ResettingRefill,
        "point",
        Dict{String, Any}(
            "dashes" => 1,
            "extraJump" => false,
            "persistJump" => false
        )
    ),

    "Resetting Refill (Two Dashes)\n(Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ResettingRefill,
        "point",
        Dict{String, Any}(
            "dashes" => 2,
            "extraJump" => false,
            "persistJump" => false
        )
    )
)

spriteExtraJump = "objects/ExtendedVariantMode/jumprefillblue/idle00"
spritePersistJump = "objects/ExtendedVariantMode/jumprefill/idle00"
spriteOneDash = "objects/refill/idle00"
spriteTwoDash = "objects/refillTwo/idle00"

function getSprite(entity::ResettingRefill)
    dashes = get(entity.data, "dashes", 0)
    extra = get(entity.data, "extraJump", false)
    persist = get(entity.data, "persistJump", false)

    if extra
        if persist
            return spritePersistJump
        else
            return spriteExtraJump
        end
    elseif dashes == 1
       return spriteOneDash
    else
       return spriteTwoDash
    end
end

function Ahorn.selection(entity::ResettingRefill)
    x, y = Ahorn.position(entity)
    sprite = getSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ResettingRefill, room::Maple.Room)
    sprite = getSprite(entity)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end
end
