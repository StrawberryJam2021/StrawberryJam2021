module SJ2021SwitchDoor

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SwitchDoor" SwitchDoor(x::Integer, y::Integer, vertical::Bool=true, closes::Bool=false, height::Integer=48, switchId::Integer=-1)

const placements = Ahorn.PlacementDict(
    "SwitchDoor (Vertical)" => Ahorn.EntityPlacement(
        SwitchDoor,
        "point",
        Dict{String, Any}(
            "vertical" => true,
        )
    ),
    "SwitchDoor (Horizontal)" => Ahorn.EntityPlacement(
        SwitchDoor,
        "point",
        Dict{String, Any}(
            "vertical" => false,
        )
    ),
    "Closing SwitchDoor (Vertical)" => Ahorn.EntityPlacement(
        SwitchDoor,
        "point",
        Dict{String, Any}(
            "vertical" => true,
            "closes" => true,
        )
    ),
    "Closing SwitchDoor (Horizontal)" => Ahorn.EntityPlacement(
        SwitchDoor,
        "point",
        Dict{String, Any}(
            "vertical" => false,
            "closes" => true,
        )
    ),
)

function Ahorn.selection(entity::SwitchDoor)
    x, y = Ahorn.position(entity)
    height = Int(get(entity.data, "height", 8))

    if get(entity.data, "vertical", true)
        return Ahorn.Rectangle(x, y, 15, height)
    else
        return Ahorn.Rectangle(x, y, height, 15)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SwitchDoor, room::Maple.Room)

    sprite = "objects/StrawberryJam2021/SwitchDoor/SwitchDoor15"

    if get(entity.data, "vertical", true)
        Ahorn.drawSprite(ctx, sprite, 7, 24)
    else
        Ahorn.drawSprite(ctx, sprite, 12, 44, rot=-pi / 2)
    end
end

end
