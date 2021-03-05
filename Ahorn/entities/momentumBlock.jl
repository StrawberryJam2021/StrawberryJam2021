module SJ2021MomentumBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/MomentumBlock" MomentumBlock(x::Integer, y::Integer, width::Integer=Maple.defaultBlockWidth, height::Integer=Maple.defaultBlockHeight, speed::Number=10, direction::Number=0, startColor::String="9a0000", endColor::String="00ffff")

const placements = Ahorn.PlacementDict(
   "Momentum Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
      MomentumBlock,
      "rectangle"
   )
)

Ahorn.minimumSize(entity::MomentumBlock) = 8, 8
Ahorn.resizable(entity::MomentumBlock) = true, true

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MomentumBlock, room::Maple.Room)
    x = Int(get(entity.data, "x", 0))
    y = Int(get(entity.data, "y", 0))

    angle = Integer(get(entity.data, "direction",0))
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, Ahorn.defaultBlackColor, Ahorn.defaultWhiteColor)
end



function Ahorn.selection(entity::MomentumBlock)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [Ahorn.Rectangle(x, y, width, height)]
end

end