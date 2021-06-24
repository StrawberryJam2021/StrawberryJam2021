module SJ2021SwitchCrateHolder

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SwitchCrateHolder" SwitchCrateHolder(x::Integer, y::Integer, persistent::Bool=false, alwaysFlag::Bool=false)

const directions = [ "Up", "Down", "Left", "Right"]

const placements = Ahorn.PlacementDict(
    "Switch Crate Holder ($dir) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SwitchCrateHolder,
        "rectangle",
        Dict{String, Any}(
            "direction" => dir,
        )
    ) for dir in directions
)

Ahorn.editingOptions(entity::SwitchCrateHolder) = Dict{String, Any}(
    "direction" => directions
)

function Ahorn.selection(entity::SwitchCrateHolder)
    x, y = Ahorn.position(entity)
    dir = get(entity.data, "direction", "Up")
    if dir == "Up"
        return Ahorn.Rectangle(x, y - 4, 16, 12)
    elseif dir == "Down"
        return Ahorn.Rectangle(x, y, 16, 12)
    elseif dir == "Left"
        return Ahorn.Rectangle(x - 2, y, 10, 16)
    elseif dir == "Right"
        return Ahorn.Rectangle(x, y, 10, 16)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SwitchCrateHolder, room::Maple.Room)
    texture = "objects/StrawberryJam2021/SwitchCrate/switch"
    dir = get(entity.data, "direction", "Up")
    if dir == "Up"
        Ahorn.drawSprite(ctx, texture, 28, -2, rot=pi / 2)
    elseif dir == "Down"
        Ahorn.drawSprite(ctx, texture, 8, 30, rot=-pi / 2)
    elseif dir == "Left"
        Ahorn.drawSprite(ctx, texture, -2, 8)
    elseif dir == "Right"
        Ahorn.drawSprite(ctx, texture, 30, 28, rot=pi)
    end
end

function Ahorn.flipped(entity::SwitchCrateHolder, horizontal::Bool)
    dir = get(entity.data, "direction", "Up")
    if horizontal
        entity.data["direction"] = (dir == "Right" ? "Left" : "Right")
    else
        entity.data["direction"] = (dir == "Up" ? "Down" : "Up")
    end
    return entity
end

end

