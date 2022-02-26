module SJ2021CoreToggleNoFlash
using ..Ahorn, Maple

@mapdef Entity "SJ2021/CoreToggleNoFlash" CoreToggleNoFlash(x::Integer, y::Integer, onlyFire::Bool=false, onlyIce::Bool=false, persistent::Bool=false)

const placements = Ahorn.PlacementDict(
    "Core Mode Toggle No Flash - Fire (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CoreToggleNoFlash,
        "point",
        Dict{String, Any}(
            "onlyFire" => true
        )
    ),
    "Core Mode Toggle No Flash - Ice (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CoreToggleNoFlash,
        "point",
        Dict{String, Any}(
            "onlyIce" => true
        )
    ),
    "Core Mode Toggle No Flash - Both (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CoreToggleNoFlash
    ),
)

function switchSprite(entity::CoreToggleNoFlash)
    onlyIce = get(entity.data, "onlyIce", false)
    onlyFire = get(entity.data, "onlyFire", false)

    if onlyIce
        return "objects/coreFlipSwitch/switch13.png"

    elseif onlyFire
        return "objects/coreFlipSwitch/switch15.png"

    else
        return "objects/coreFlipSwitch/switch01.png"
    end
end

function Ahorn.selection(entity::CoreToggleNoFlash)
    x, y = Ahorn.position(entity)
    sprite = switchSprite(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CoreToggleNoFlash, room::Maple.Room)
    sprite = switchSprite(entity)

    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end