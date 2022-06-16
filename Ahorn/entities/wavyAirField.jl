module SJ2021WavyAirField
using ..Ahorn, Maple

@mapdef Entity "SJ2021/WavyAirField" WavyAirField(x::Integer,y::Integer,width::Integer=8,height::Integer=8)

const placements = Ahorn.PlacementDict(
    "Wavy Air Field (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WavyAirField,
        "rectangle"
    )
)

Ahorn.minimumSize(entity::WavyAirField) = 8, 8
Ahorn.resizable(entity::WavyAirField) = true, true
Ahorn.selection(entity::WavyAirField) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WavyAirField, room::Maple.Room)
    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))
    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.3, 0.3, 1.0, 0.4), (0.3, 0.3, 1.0, 1.0))
end

end