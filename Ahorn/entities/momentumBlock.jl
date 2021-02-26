module SJ2021MomentumBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/MomentumBlock" MomentumBlock(x::Integer, y::Integer, width::Integer=16, height::Integer=16, speed::Number=10, direction::Number=0)

const placements = Ahorn.PlacementDict(
   "Momentum Block ((Strawberry Jam 2021))" => Ahorn.EntityPlacement(
      MomentumBlock,
      "rectangle"
   )
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MomentumBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height)
end



function Ahorn.selection(entity::DashZipMover)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height)]
end

end