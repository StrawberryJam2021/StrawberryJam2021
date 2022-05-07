module SJ2021DarkMatter
using ..Ahorn, Maple

@mapdef Entity "SJ2021/DarkMatter" DarkMatter(x::Integer,y::Integer,width::Integer=8,height::Integer=8)

const placements = Ahorn.PlacementDict(
    "Dark Matter Remake (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        DarkMatter,
        "rectangle"
    )
)

Ahorn.minimumSize(entity::DarkMatter) = 8, 8
Ahorn.resizable(entity::DarkMatter) = true, true
Ahorn.selection(entity::DarkMatter) = Ahorn.getEntityRectangle(entity)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DarkMatter, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))
    realEdgeColor = (172.0, 41.0, 242.0, 255.0) ./ 255
    realCenterColor = (136.0, 1.0, 208.0, 255.0) ./ 255
    Ahorn.drawRectangle(ctx, 0, 0, width, height, realCenterColor, realEdgeColor)
end

end